# Base Architecture Implementation Plan

**Version**: 1.0  
**Created**: 2026-01-16  
**Status**: Planning  
**Specifications**: repository-port, cli-interface, config-format

## Executive Summary

This plan establishes the foundational architecture patterns for the Performance & Reliability Testing Platform across three critical specifications:

1. **Repository Port** - Persistence abstraction following Clean Architecture
2. **CLI Interface** - Deterministic command-line interface with standardized exit codes
3. **Configuration Format** - Explicit defaults with strict validation and fail-fast behavior

## Constitution Alignment

### Principle Compliance

**✅ Specification-Driven Development**
- All implementations derived from approved specifications (001-repository-port, 001-cli-interface, 001-config-format)
- No behavior invented beyond specifications

**✅ Domain-Driven Design**
- Repository ports defined in domain layer
- Configuration models expressed in domain language
- CLI acts as thin adapter to application use cases

**✅ Clean Architecture**
- Repository ports: Domain layer defines contracts, Infrastructure implements
- CLI: Infrastructure layer adapting to Application use cases
- Configuration: Application layer with domain-independent validation

**✅ Determinism & Reproducibility**
- Repository operations with consistent behavior
- CLI exit codes deterministic (0=SUCCESS, 1=FAIL, 2=INCONCLUSIVE)
- Configuration validation deterministic and fail-fast

**✅ Engine-Agnostic Abstraction**
- Repository ports independent of storage technology
- Configuration independent of file format
- CLI independent of execution mechanism

## Technical Context

### Existing Architecture

**Current Domain Structure:**
```
src/
├── PerformanceEngine.Metrics.Domain/
│   ├── Domain/ (entities, value objects, aggregations)
│   └── Ports/ (IPersistenceRepository - basic CRUD)
├── PerformanceEngine.Baseline.Domain/
│   ├── Domain/ (Baseline aggregate, comparisons, tolerances)
│   └── Ports/ (IBaselineRepository - Create, GetById, ListRecent)
├── PerformanceEngine.Evaluation.Domain/
├── PerformanceEngine.Profile.Domain/
├── PerformanceEngine.Application/ (orchestration use cases)
└── PerformanceEngine.Baseline.Infrastructure/ (RedisBaselineRepository)
```

**Current Repository Implementations:**
- `IPersistenceRepository` - Basic CRUD for Metric entities
- `IBaselineRepository` - Create, GetById, ListRecent for Baseline entities
- `RedisBaselineRepository` - Infrastructure adapter for Redis storage

**Missing Components:**
- ❌ Audit trail capability (FR-016 to FR-020 from repository-port spec)
- ❌ Versioning support (FR-021 to FR-025 from repository-port spec)
- ❌ Transaction boundaries (FR-026 to FR-030 from repository-port spec)
- ❌ Query specifications pattern (FR-011 to FR-015 from repository-port spec)
- ❌ CLI entry point and command structure
- ❌ Configuration validation framework
- ❌ Exit code standardization

### Technology Stack

**Language**: C# (.NET)  
**Current Dependencies**:
- Redis (StackExchange.Redis) for baseline persistence
- No CLI framework detected
- No configuration framework detected

**Proposed Additions** (implementation phase decision):
- CLI framework: System.CommandLine or custom implementation
- Configuration: System.Text.Json for parsing, custom validation
- Repository: No new dependencies (extend existing patterns)

## Phase 0: Research & Analysis

### Research Tasks

#### RT-001: Repository Pattern Extensions
**Question**: How to implement audit trails and versioning without breaking existing repository contracts?

**Approach**:
- Review existing IPersistenceRepository and IBaselineRepository
- Design backward-compatible extensions
- Consider separate audit/versioning ports vs. enhanced repository methods

**Decision Criteria**:
- Must not break existing RedisBaselineRepository implementation
- Must follow Single Responsibility Principle
- Must support multiple aggregate roots

#### RT-002: CLI Framework Selection
**Question**: System.CommandLine vs. custom implementation for deterministic CLI behavior?

**Approach**:
- Evaluate System.CommandLine exit code control
- Assess deterministic output formatting capabilities
- Consider implementation complexity vs. control

**Decision Criteria**:
- Must support exact exit codes (0, 1, 2, 3+)
- Must allow complete control over stdout/stderr
- Must support JSON and text output formats

#### RT-003: Configuration Validation Strategy
**Question**: How to implement fail-fast validation with source attribution across file/env/CLI sources?

**Approach**:
- Review .NET configuration providers (IConfiguration)
- Design custom validation layer
- Design source precedence resolution

**Decision Criteria**:
- Must validate before test execution
- Must report all semantic errors
- Must track configuration source for each value

#### RT-004: Audit Trail Storage Pattern
**Question**: Should audit logs be stored in same repository as entities or separate audit store?

**Approach**:
- Evaluate domain boundary for audit records
- Consider query patterns for audit trail
- Assess storage efficiency

**Decision Criteria**:
- Audit records must be immutable (FR-019)
- Audit trail must survive entity deletion (FR-020)
- Must support time-range queries (FR-018)

### Research Findings

**[To be completed during Phase 0 execution]**

## Phase 1: Design Artifacts

### 1.1 Repository Port Enhancements

**Design Goal**: Extend existing repository pattern with audit, versioning, and transactions while maintaining backward compatibility.

**Components to Design**:

#### IRepository<TEntity, TId> - Base Repository Contract
```
Responsibilities:
- Generic CRUD operations (Create, Read, Update, Delete)
- Query by ID
- Entity existence checks

Why Generic:
- Eliminates duplication across aggregate roots
- Enforces consistent repository behavior
- Type-safe entity operations

Constraints:
- TEntity must be an aggregate root
- TId must be a value object representing entity identifier
```

#### IAuditLog - Audit Trail Port
```
Responsibilities:
- Record Create/Update/Delete operations
- Store operation timestamp, entity ID, operation type
- Optionally capture changed properties
- Query audit records by entity and time range

Separation Rationale:
- Audit is cross-cutting concern
- Different persistence requirements (append-only)
- Different query patterns (time-series)
```

#### IVersionStore<TEntity, TId> - Versioning Port
```
Responsibilities:
- Store entity snapshots on each modification
- Retrieve entity at specific version
- Retrieve entity at specific point in time
- List version history

Separation Rationale:
- Versioning is advanced feature (P3)
- Not all aggregates need versioning
- Different storage optimization strategies
```

#### IUnitOfWork - Transaction Boundary
```
Responsibilities:
- Begin transaction scope
- Commit all operations atomically
- Rollback on failure
- Track enlisted operations

Considerations:
- May not be supported by all storage backends
- Must handle nested transaction scenarios
- Must integrate with existing repository operations
```

#### IQuerySpecification<TEntity> - Query Pattern
```
Responsibilities:
- Express filter criteria in domain language
- Define ordering rules
- Support pagination (offset, limit)

Examples:
- BaselinesByDateRange
- MetricsAboveThreshold
- ProfilesByStatus
```

**Design Outputs**:
- `docs/architecture/repository-contracts.md` - Interface definitions
- `docs/architecture/audit-trail-design.md` - Audit storage patterns
- `docs/architecture/versioning-design.md` - Version storage patterns

### 1.2 CLI Interface Design

**Design Goal**: Define deterministic CLI structure with standardized exit codes and dual output formats.

**Components to Design**:

#### Command Structure
```
Root: performance-engine [command] [options]

Commands:
- run      : Execute performance tests
- baseline : Manage baseline metrics (save, compare)
- export   : Export test results
- validate : Validate configuration without running

Common Options:
- --config <path>      : Configuration file
- --format <json|text> : Output format
- --verbose           : Verbose logging
- --help              : Show help
```

#### Exit Code Enumeration
```
0  : SUCCESS - All tests passed, operations succeeded
1  : FAIL - Tests failed (thresholds violated, regressions detected)
2  : INCONCLUSIVE - Insufficient data, partial results, ambiguous
3  : CONFIG_ERROR - Invalid configuration, missing files
4  : VALIDATION_ERROR - Validation failures before execution
5  : RUNTIME_ERROR - System errors during execution
130: INTERRUPTED - SIGINT received (Ctrl+C)
143: TERMINATED - SIGTERM received
```

#### Output Formatters
```
IOutputFormatter interface:
- FormatSuccess(TestResult result)
- FormatFailure(TestResult result)
- FormatError(ErrorContext error)

Implementations:
- JsonOutputFormatter: Valid JSON to stdout
- TextOutputFormatter: Human-readable tables
- VerboseFormatter: Detailed diagnostics
```

#### Command Orchestration
```
CLI Layer (Infrastructure):
├── Parse arguments → ConfigurationRequest
├── Invoke Application Use Case
├── Transform Result → Output
└── Return Exit Code

Application Layer:
├── Validate configuration
├── Execute use case logic
└── Return Result<T, Error>

Separation ensures:
- Application logic testable without CLI
- CLI is thin adapter
- No business logic in presentation layer
```

**Design Outputs**:
- `docs/architecture/cli-commands.md` - Command specifications
- `docs/architecture/exit-codes.md` - Exit code semantics
- `docs/architecture/output-formats.md` - Format specifications

### 1.3 Configuration Format Design

**Design Goal**: Explicit defaults, strict validation, fail-fast behavior with multi-source composition.

**Components to Design**:

#### Configuration Schema
```
Root Configuration:
- SchemaVersion (required, string, semantic version)
- TestExecution (test duration, user count, ramp-up)
- RequestSettings (timeout, retries, headers)
- RateLimits (requests/sec, concurrency)
- Profiles (named test configurations)
- Thresholds (pass/fail criteria)

Validation Rules:
- Schema version must be supported
- Test duration must be positive
- User counts must be non-negative
- All required fields present
- Logical consistency (min ≤ max)
```

#### Configuration Sources & Precedence
```
Precedence (highest to lowest):
1. CLI arguments (--option=value)
2. Environment variables (PERF_ENGINE_*)
3. Configuration file (YAML/JSON)
4. Explicit defaults

Composition Strategy:
- Load defaults
- Apply configuration file
- Apply environment variables
- Apply CLI arguments
- Validate merged result
```

#### Validation Engine
```
Validation Phases:
1. Structural: Parse file, check syntax
2. Type: Validate field types
3. Range: Check numeric constraints
4. Logic: Cross-field consistency
5. Dependency: Required field checks

Error Reporting:
- Field path (e.g., "profiles.smoke-test.duration")
- Invalid value
- Expected constraint
- Source (file:line or env var name)
```

#### Default Values Registry
```
Defaults Registry:
- Centralized definition of all defaults
- Documented in single location
- Applied only when value not specified
- Visible in effective configuration output

Example Defaults:
- Request timeout: 30 seconds
- Retry count: 3
- Concurrency: 10
- Error threshold: 1%
```

**Design Outputs**:
- `docs/architecture/config-schema.md` - Schema structure
- `docs/architecture/config-validation.md` - Validation rules
- `docs/architecture/config-defaults.md` - Default values

## Phase 2: Implementation Plan

### Layer Mapping

```
┌─────────────────────────────────────────────────────┐
│ Infrastructure Layer                                │
│ - CLI Entry Point (Program.cs)                     │
│ - Command Handlers                                  │
│ - Output Formatters (JSON, Text)                   │
│ - RedisBaselineRepository (existing)                │
│ - RedisAuditLog (new)                              │
│ - RedisVersionStore (new)                          │
└────────────────┬────────────────────────────────────┘
                 │ implements ports
┌────────────────┴────────────────────────────────────┐
│ Application Layer                                   │
│ - Use Cases (RunTestsUseCase, SaveBaselineUseCase) │
│ - Configuration Validator                           │
│ - Configuration Source Composer                     │
│ - Exit Code Mapper                                  │
└────────────────┬────────────────────────────────────┘
                 │ uses domain
┌────────────────┴────────────────────────────────────┐
│ Domain Layer                                        │
│ - IRepository<T, TId> (new)                        │
│ - IAuditLog (new)                                   │
│ - IVersionStore<T, TId> (new)                      │
│ - IUnitOfWork (new)                                │
│ - ConfigurationSchema (value object)                │
│ - ValidationError (value object)                    │
│ - Existing domain entities (Baseline, Metric, etc) │
└─────────────────────────────────────────────────────┘
```

### Implementation Tasks

#### Domain Layer Tasks

**Task Group 1: Repository Port Contracts** (Priority: P1)
- [ ] Create `IRepository<TEntity, TId>` interface with CRUD operations
- [ ] Create `IAuditLog` interface with audit record operations
- [ ] Create `IVersionStore<TEntity, TId>` interface with versioning operations
- [ ] Create `IUnitOfWork` interface for transaction boundaries
- [ ] Create `IQuerySpecification<TEntity>` interface for query patterns
- [ ] Create `AuditRecord` value object
- [ ] Create `EntityVersion<TEntity>` value object
- [ ] Update existing repository interfaces to extend IRepository<T, TId>

**Task Group 2: Configuration Domain Models** (Priority: P1)
- [ ] Create `ConfigurationSchema` value object
- [ ] Create `ConfigurationSource` enumeration (File, Environment, CLI, Default)
- [ ] Create `ValidationError` value object
- [ ] Create `ValidationResult` aggregate
- [ ] Create `DefaultValuesRegistry` service

#### Application Layer Tasks

**Task Group 3: Configuration Validation** (Priority: P1)
- [ ] Create `ConfigurationValidator` service
- [ ] Implement structural validation (parsing)
- [ ] Implement type validation
- [ ] Implement range validation
- [ ] Implement logical consistency validation
- [ ] Create `ConfigurationComposer` for multi-source merging
- [ ] Implement precedence resolution logic

**Task Group 4: Use Case Orchestration** (Priority: P2)
- [ ] Create `RunTestsUseCase` with configuration validation
- [ ] Create `SaveBaselineUseCase` 
- [ ] Create `CompareBaselineUseCase`
- [ ] Create `ValidateConfigurationUseCase`
- [ ] Integrate repository operations with audit logging
- [ ] Implement exit code mapping logic

#### Infrastructure Layer Tasks

**Task Group 5: CLI Implementation** (Priority: P2)
- [ ] Create CLI entry point (`Program.cs`)
- [ ] Implement command parser (run, baseline, export, validate)
- [ ] Implement argument parser for each command
- [ ] Create `JsonOutputFormatter`
- [ ] Create `TextOutputFormatter`
- [ ] Implement exit code handler
- [ ] Integrate with application use cases

**Task Group 6: Repository Implementations** (Priority: P3)
- [ ] Create `RedisAuditLog` implementing `IAuditLog`
- [ ] Create `RedisVersionStore<T>` implementing `IVersionStore<T, TId>`
- [ ] Create `RedisUnitOfWork` implementing `IUnitOfWork`
- [ ] Update `RedisBaselineRepository` to extend `IRepository<Baseline, BaselineId>`
- [ ] Add audit logging to all repository operations
- [ ] Add versioning to repository update operations

### Testing Strategy

**Unit Tests** (per layer):
- Domain: Repository contracts behavior (mocks)
- Application: Configuration validation with various invalid inputs
- Application: Use case orchestration with mocked repositories
- Infrastructure: CLI command parsing
- Infrastructure: Output formatter correctness

**Integration Tests**:
- CLI end-to-end with real configuration files
- Repository implementations with test Redis instance
- Configuration composition from multiple sources
- Audit trail persistence and retrieval

**Acceptance Tests** (from specs):
- Repository-port: CRUD operations, audit trail, versioning
- CLI-interface: Exit codes for success/fail/inconclusive scenarios
- Config-format: Validation errors, defaults, source precedence

## Phase 3: Verification & Documentation

### Verification Checklist

**Architecture Compliance**:
- [ ] Domain layer has no infrastructure dependencies
- [ ] Repository ports defined in domain layer
- [ ] CLI is thin adapter to application layer
- [ ] All use cases testable without CLI

**Specification Compliance**:
- [ ] All FR requirements from repository-port spec implemented
- [ ] All FR requirements from cli-interface spec implemented
- [ ] All FR requirements from config-format spec implemented
- [ ] All acceptance scenarios from specs pass

**Constitution Compliance**:
- [ ] Specification-driven (no invented behavior)
- [ ] Domain-driven (pure domain logic)
- [ ] Clean architecture (dependency inversion)
- [ ] Deterministic (reproducible behavior)

### Documentation Deliverables

- [ ] `docs/architecture/repository-contracts.md` - Repository port specifications
- [ ] `docs/architecture/cli-usage.md` - CLI command reference
- [ ] `docs/architecture/config-reference.md` - Configuration schema reference
- [ ] `docs/architecture/exit-codes.md` - Exit code semantics
- [ ] `docs/examples/sample-config.yaml` - Example configuration
- [ ] `docs/examples/cli-examples.md` - CLI usage examples
- [ ] README updates with architecture overview

## Success Criteria

**Repository Port**:
- ✅ SC-001: Domain layer has no infrastructure imports (100% verified)
- ✅ SC-002: Repository ports support multiple storage technologies
- ✅ SC-003: All CRUD operations work through port interfaces
- ✅ SC-004: Audit trail captures all changes within 100ms
- ✅ SC-005: Entity versioning with <500ms retrieval latency
- ✅ SC-006: Transactions succeed or rollback atomically

**CLI Interface**:
- ✅ SC-001: Tests execute from CLI within 5 seconds of completion
- ✅ SC-002: Exit codes enable CI/CD integration (0/1/2/3+)
- ✅ SC-003: JSON output is valid and parseable (100%)
- ✅ SC-009: Exit codes are deterministic

**Config Format**:
- ✅ SC-002: 100% of errors detected before execution (fail-fast)
- ✅ SC-003: Validation completes in <100ms
- ✅ SC-004: Error messages include field, value, constraint
- ✅ SC-005: Source attribution for all configuration values

## Risk Assessment

### Technical Risks

**Risk 1: Breaking Existing Repository Implementations**
- **Impact**: High (RedisBaselineRepository currently functional)
- **Mitigation**: Extend interfaces backward-compatibly, create adapter if needed
- **Contingency**: Keep existing interfaces, create new enhanced interfaces

**Risk 2: CLI Framework Limitations**
- **Impact**: Medium (Exit code control critical)
- **Mitigation**: Research phase validates framework capabilities
- **Contingency**: Custom CLI implementation if framework insufficient

**Risk 3: Configuration Validation Performance**
- **Impact**: Low (Must be <100ms per spec)
- **Mitigation**: Lazy validation, efficient rule evaluation
- **Contingency**: Profile and optimize validation pipeline

### Schedule Risks

**Risk 4: Scope Creep**
- **Impact**: Medium (Three major specifications)
- **Mitigation**: Strict adherence to specifications, no invented features
- **Contingency**: Phase implementation, deliver incrementally

## Assumptions

1. Existing domain entities (Baseline, Metric) remain stable
2. Redis remains acceptable for persistence (not changing storage technology)
3. .NET environment and tooling available
4. No UI/web interface required for CLI (console only)
5. Audit and versioning can share storage backend with entities
6. Configuration files are YAML or JSON (not binary)
7. Single-user CLI execution (no concurrent CLI sessions)

## Dependencies

**Internal Dependencies**:
- Existing domain models: Baseline, Metric, Profile, Evaluation
- Existing repository implementations: RedisBaselineRepository
- Existing application use cases: EvaluatePerformanceUseCase

**External Dependencies**:
- StackExchange.Redis (already present)
- System.CommandLine or equivalent (TBD in research phase)
- System.Text.Json (already in .NET)

## Timeline Estimate

**Phase 0: Research** - 2-3 days
- Repository pattern research: 0.5 day
- CLI framework evaluation: 0.5 day
- Configuration validation design: 0.5 day
- Audit storage pattern: 0.5 day
- Buffer: 0.5 day

**Phase 1: Design** - 3-4 days
- Repository contracts: 1 day
- CLI commands & exit codes: 1 day
- Configuration schema & validation: 1 day
- Integration design: 0.5 day
- Buffer: 0.5 day

**Phase 2: Implementation** - 10-12 days
- Domain layer (contracts & models): 2 days
- Application layer (validation & use cases): 3 days
- Infrastructure layer (CLI & repositories): 4 days
- Unit tests: 2 days
- Integration tests: 1 day

**Phase 3: Verification** - 2-3 days
- Architecture compliance: 0.5 day
- Specification compliance: 1 day
- Documentation: 1 day
- Buffer: 0.5 day

**Total Estimate**: 17-22 days

---

**Next Steps**:
1. Execute Phase 0 research tasks
2. Document research findings in this plan
3. Proceed to Phase 1 design artifact creation
4. Begin implementation following layer order (Domain → Application → Infrastructure)
