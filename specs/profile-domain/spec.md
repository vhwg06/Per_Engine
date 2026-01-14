# Profile Domain Specification

**Feature**: Profile Domain - Configuration & Behavioral Context  
**Status**: Design Phase  
**Version**: 0.1.0  
**Last Updated**: January 14, 2026

---

## Executive Summary

The **Profile Domain** provides a mechanism for configuring system behavior based on context. It answers the question: **"In this context (API, endpoint, tag, environment), how should the system behave?"**

The domain establishes how configuration decisions are made (defaults vs. overrides, scope resolution, conflict handling) without tying configuration to specific file formats, persistence mechanisms, or deployment environments.

---

## Purpose & Goals

### Why This Domain?

**Problem**: Performance testing systems need flexible configuration that:
- Varies per API, endpoint, or environment
- Allows global defaults with specific overrides
- Resolves conflicts deterministically
- Works across K6, JMeter, and custom engines

**Solution**: A dedicated profile domain that:
- ✅ Defines configuration as domain concepts (not YAML files)
- ✅ Handles scope-based overrides systematically
- ✅ Resolves conflicts deterministically
- ✅ Separates configuration logic from persistence format

### Strategic Outcomes

| Outcome | How Achieved | Success Metric |
|---------|-------------|-----------------|
| **Context-aware config** | Scope-based profiles | Same config differs for global vs. per-API scope |
| **Deterministic resolution** | Fixed override rules | Same conflicts always resolve same way |
| **Format independence** | Domain models, not YAML/JSON | Config migrates without domain changes |
| **Extension support** | Custom scopes | New scope types added without modifying core |

---

## User Stories & Acceptance Criteria

### User Story 1: Apply Global Configuration
**Actor**: DevOps Engineer  
**Priority**: P1 (MVP)

**Scenario**: System has a global timeout configuration that applies to all tests.

```gherkin
Given a global profile with timeout = 30s
When a test runs for API X with no specific override
Then the test uses 30s timeout

Given a global profile with timeout = 30s
And an API-specific profile with timeout = 60s
When the test runs for API X
Then the test uses 60s timeout (override wins)
```

**Acceptance Criteria**:
- FR-001: Global profile applies to all contexts by default
- FR-002: Specific scope overrides global scope
- FR-003: No ambiguity in resolution (conflicts deterministic)

---

### User Story 2: Override Configuration Per Context
**Actor**: Performance Engineer  
**Priority**: P1 (MVP)

**Scenario**: Different APIs have different requirements. Engineer wants to configure timeout, retry count, and load pattern per API.

```gherkin
Given profiles:
  - Global: timeout=30s, retries=3, ramp=1m
  - API(payment): timeout=60s
  - API(search): retries=1
When evaluating API payment
Then config is: timeout=60s (override), retries=3 (global), ramp=1m (global)

When evaluating API search
Then config is: timeout=30s (global), retries=1 (override), ramp=1m (global)
```

**Acceptance Criteria**:
- FR-004: Multiple profiles can define same config key
- FR-005: Scope hierarchy determines which value applies
- FR-006: Partial overrides: unspecified keys use parent scope

---

### User Story 3: Support Multiple Scope Dimensions
**Actor**: System Architect  
**Priority**: P2

**Scenario**: Configuration needs to account for multiple dimensions (API, environment, deployment).

```gherkin
Given profiles:
  - Global: timeout=30s
  - Env(staging): timeout=60s
  - API(payment): timeout=90s
  - API(payment) + Env(staging): timeout=120s
When evaluating API payment in staging
Then config uses: timeout=120s (most specific scope wins)
```

**Acceptance Criteria**:
- FR-007: Multiple scope dimensions supported
- FR-008: More specific scopes override less specific
- FR-009: Composite scopes handled correctly

---

### User Story 4: Support Extensible Scopes
**Actor**: System Architect  
**Priority**: P2

**Scenario**: Built-in scopes (Global, API, Environment) don't cover all cases. Architect wants to add custom scopes (e.g., PaymentMethod-specific config).

```gherkin
Given a custom scope "PaymentMethod"
And implementation of custom scope resolver
When profile resolution encounters PaymentMethod scope
Then it applies resolution rules correctly
And result is deterministic across multiple runs
```

**Acceptance Criteria**:
- FR-010: Custom scope types can be added without modifying core
- FR-011: Custom scopes respect same resolution rules
- FR-012: Resolution is deterministic with custom scopes

---

## Functional Requirements

| ID | Requirement | Rationale |
|----|-------------|-----------|
| **FR-001** | Global profile serves as default configuration | Simplifies common case |
| **FR-002** | Specific scopes override global scope | Enables specialization |
| **FR-003** | Resolution is deterministic (no ambiguity) | Reproducible behavior |
| **FR-004** | Multiple profiles can define same config key | Allows partial overrides |
| **FR-005** | Scope hierarchy explicit and enforceable | Clear precedence rules |
| **FR-006** | Unspecified keys inherit from parent scope | Reduces duplication |
| **FR-007** | Multiple scope dimensions supported | Flexible categorization |
| **FR-008** | More specific scopes override less specific | Intuitive precedence |
| **FR-009** | Composite scopes handled without ambiguity | Complex scenarios supported |
| **FR-010** | Custom scope types can be added (strategy pattern) | Extensible without core changes |
| **FR-011** | Custom scopes follow same resolution rules | Consistent behavior |
| **FR-012** | Resolution is deterministic with custom scopes | Reproducible |
| **FR-013** | Profiles are immutable after resolution | Thread-safe, shareable |
| **FR-014** | ResolvedProfile includes audit trail of overrides | Debugging and transparency |
| **FR-015** | Illegal conflicts caught at resolution time (fail-fast) | No silent failures |

---

## Success Criteria

| Criterion | Definition | How Verified |
|-----------|-----------|--------------|
| **SC-001** | Determinism | Same profiles + context → identical resolved config every time |
| **SC-002** | Scope coverage | At least 5 built-in scope types (Global, API, Environment, Tag, Custom) |
| **SC-003** | Extensibility | New scope type added in <150 lines of code |
| **SC-004** | Performance | Resolution of 100-key config over 10 scope dimensions in <5ms |
| **SC-005** | Correctness | All override and inheritance scenarios pass tests |
| **SC-006** | No ambiguity | Illegal conflicts detected and reported clearly |

---

## Domain Model

### Core Concepts

```
Scope (abstract base or enum)
├── Global
├── ApiScope (api name)
├── EnvironmentScope (env name)
├── TagScope (tag name)
└── Custom implementations (extensible)

ConfigKey (value object)
├── name: string
├── immutable

ConfigValue (value object)
├── value: object
├── type: ConfigType (String, Int, Duration, etc.)
├── immutable

Profile (entity)
├── scope: Scope
├── configurations: Map<ConfigKey, ConfigValue>
├── immutable after creation

ResolvedProfile (entity)
├── configurations: Map<ConfigKey, ConfigValue> (resolved)
├── auditTrail: Map<ConfigKey, List<Scope>> (which scopes provided this value?)
├── immutable after creation

ProfileResolver (domain service)
├── Resolve(Profile[], context) → ResolvedProfile
├── Pure function, no side effects
├── Deterministic resolution

ResolutionRule (strategy)
├── Determines precedence between scopes
├── Custom implementations supported
└── Deterministic comparison

ConflictHandler (domain service)
├── Detects illegal conflicts
├── Throws ConfigurationConflictException
└── Clear error messages
```

### Architectural Constraints

- **No file I/O**: Reading/writing YAML, JSON, etc. is infrastructure layer responsibility
- **No environment variables**: Environment access happens at injection boundary
- **No persistence**: Profiles are in-memory; persistence is infrastructure
- **Immutable profiles**: Cannot modify after creation
- **Deterministic resolution**: Same inputs → identical output every time
- **No circular dependencies**: Global profile doesn't reference specific profiles

---

## Out of Scope

- ❌ **File format**: YAML, JSON, TOML syntax (infrastructure responsibility)
- ❌ **Environment variables**: Reading from system environment
- ❌ **Secrets management**: Vault integration, encryption
- ❌ **Profile persistence**: Database storage, versioning
- ❌ **Profile validation**: Schema validation (application layer)
- ❌ **Hot reload**: Runtime profile updates without restart

---

## Invariants & Rules

### Resolution Invariants

1. **Determinism**: Same profile set + context = same resolution every run
2. **Completeness**: Every config key has a value (from some scope)
3. **Uniqueness**: No two profiles at same scope define conflicting values
4. **Hierarchy**: Specific scopes always override general scopes
5. **Immutability**: ResolvedProfile cannot be modified after creation

### Scope Hierarchy Rules

```
Less Specific                          More Specific
Global
   ├── Dimension1(value1)
   │      ├── Dimension2(value2)
   │      └── Dimension2(value3)
   └── Dimension1(value2)
           └── Dimension2(value4)
```

- Deeper nesting = more specific = higher priority
- Same nesting level with different values = conflict (error)
- Unrelated dimensions orthogonal (can combine: API + Environment both apply)

### Profile Immutability Rules

- Once a Profile is created, its scope and configurations cannot change
- Once a ResolvedProfile is created, the resolved config cannot change
- Profiles are safe to share across threads

---

## Technical Approach

### Design Principles

1. **Domain-First Configuration**: Config logic is domain logic, not infrastructure
2. **Strategy Pattern**: Scope types are strategies; custom scopes extend via interface
3. **Fail-Fast Conflict Detection**: Illegal conflicts caught immediately at resolution time
4. **Deterministic Resolution**: Scope precedence rules are explicit and fixed
5. **Immutable Results**: ResolvedProfile immutable, safe for multi-threaded access

### Technology Stack

- **Language**: C# 13 (.NET 8.0)
- **Testing**: xUnit, FluentAssertions
- **Dependencies**: Metrics Domain (potentially input), none else required

### Dependency Graph

```
Profile Domain (depends on)
└── Potentially Metrics Domain (for scope examples)
    └── Domain foundations
```

No other domain depends on Profile Domain (it's a leaf).

---

## Implementation Phases

### Phase 1: Foundation (5 tasks)
- [ ] Scope abstraction and built-in types
- [ ] ConfigKey and ConfigValue value objects
- [ ] Profile entity with immutability
- [ ] ProfileResolver service (deterministic resolution)
- [ ] ConflictHandler for error detection

### Phase 2: Application Layer (2 tasks)
- [ ] ProfileDto and resolution DTOs
- [ ] ProfileService application facade

### Phase 3: Testing & Validation (4 tasks)
- [ ] Unit tests for scope types
- [ ] Determinism tests (reproducibility)
- [ ] Integration tests (complex resolution scenarios)
- [ ] Architecture compliance tests

### Phase 4: Documentation (2 tasks)
- [ ] README and quick start guide
- [ ] Architecture documentation

---

## Conformance to Constitutional Principles

**Specification-Driven Development**: ✅  
All implementation derived from this specification.

**Domain-Driven Design**: ✅  
Pure domain logic: Scope, Profile, ResolvedProfile, Resolution rules.

**Clean Architecture**: ✅  
Domain has no infrastructure dependencies; file I/O is separate layer.

**Determinism & Reproducibility**: ✅  
All resolutions deterministic; same profiles + context = same result.

**Engine-Agnostic Abstraction**: ✅  
Profiles work with any execution engine.

**Evolution-Friendly Design**: ✅  
Strategy pattern enables custom scope types without modifying core.

---

## Acceptance Gates

- [ ] All user stories have passing tests
- [ ] 100+ unit tests covering all scope types
- [ ] Determinism tests pass 1000 consecutive runs
- [ ] Complex resolution scenarios (5+ dimensions) working
- [ ] Custom scope support demonstrated
- [ ] Zero file I/O or env variable access in domain
- [ ] Documentation complete (README, guides, examples)
