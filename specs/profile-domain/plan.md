# Implementation Plan: Profile Domain

**Branch**: `profile-domain-implementation` | **Date**: 2026-01-14 | **Spec**: [profile-domain.spec.md](spec.md)  
**Input**: Feature specification from `/specs/profile-domain/spec.md`

---

## Summary

The Profile Domain implements **deterministic configuration resolution** based on context (scope). It provides pure, side-effect-free logic to resolve configuration values from hierarchical profiles (Global, API, Environment, Tag, Custom scopes) with explicit override rules. Results are immutable and deterministic, enabling portable configuration across different deployment contexts.

The domain operates independently of configuration file formats (YAML, JSON, etc.) through clean architecture; it consumes Profile entities and resolves them to ResolvedProfile with clear audit trails. All resolution is deterministic, conflict-detection is fail-fast, and custom scopes are extensible via strategy pattern.

---

## Technical Context

**Language/Version**: C# 13 (.NET 10.0 LTS)  
**Primary Dependencies**: PerformanceEngine.Metrics.Domain (optional: for scope examples), xUnit & FluentAssertions (testing)  
**Storage**: N/A (in-memory, immutable domain models)  
**Testing**: xUnit with determinism test harness for 1000+ reproducibility runs  
**Target Platform**: .NET 10 runtime (.NET Standard 2.1 compatible for cross-platform support)  
**Project Type**: Single domain library (Clean Architecture layered structure)  
**Performance Goals**: Resolve 100-key configuration over 10 scope dimensions in <5ms  
**Constraints**: Deterministic resolution; immutable results; fail-fast conflict detection; zero file I/O in domain  
**Scale/Scope**: Foundation domain supporting hierarchical configuration with custom scope extensibility; estimated 2500-3500 LOC core domain

**Project Structure**:
```
src/PerformanceEngine.Profile.Domain/
├── Domain/
│   ├── Scopes/
│   │   ├── IScope.cs
│   │   ├── GlobalScope.cs
│   │   ├── ApiScope.cs
│   │   ├── EnvironmentScope.cs
│   │   ├── TagScope.cs
│   │   └── CustomScope.cs (for extensibility)
│   ├── Configuration/
│   │   ├── ConfigKey.cs (value object)
│   │   ├── ConfigValue.cs (value object)
│   │   ├── ConfigType.cs (enum)
│   │   ├── Profile.cs (entity)
│   │   └── ResolvedProfile.cs (entity)
│   ├── Resolution/
│   │   ├── ProfileResolver.cs (domain service)
│   │   ├── ResolutionRule.cs (strategy)
│   │   ├── ConflictHandler.cs
│   │   └── ResolutionContext.cs
│   ├── Events/
│   │   ├── ProfileResolvedEvent.cs
│   │   └── IDomainEvent.cs
│   └── ValueObject.cs
│
├── Application/
│   ├── Services/
│   │   └── ProfileService.cs
│   ├── UseCases/
│   │   ├── ResolveProfileUseCase.cs
│   │   ├── ValidateProfileUseCase.cs
│   │   └── DetectConflictsUseCase.cs
│   └── Dto/
│       ├── ProfileDto.cs
│       ├── ResolvedProfileDto.cs
│       ├── ConfigKeyDto.cs
│       └── ConfigValueDto.cs
│
└── Ports/
    └── IProfileRepository.cs (deferred)

tests/PerformanceEngine.Profile.Domain.Tests/
├── Domain/
│   ├── ScopeTests.cs
│   ├── ConfigKeyValueTests.cs
│   ├── ProfileTests.cs
│   └── ResolvedProfileTests.cs
├── Resolution/
│   ├── ProfileResolverTests.cs
│   ├── ConflictHandlerTests.cs
│   └── DeterminismTests.cs
└── Integration/
    ├── ComplexResolutionTests.cs
    ├── CustomScopeTests.cs
    └── ProfileServiceTests.cs
```

---

## Architecture Overview

### Layering

```
┌──────────────────────────────┐
│    APPLICATION               │
│  ProfileService → UseCases   │
└──────────────┬───────────────┘
               ↓
┌──────────────────────────────┐
│    DOMAIN                    │
│  Scope → Profile →           │
│  ProfileResolver →           │
│  ResolvedProfile             │
└──────────────┬───────────────┘
               ↓
        (No dependencies)
```

### Core Concepts

**Scope**: Dimension for categorizing configuration. Built-in types:
- `GlobalScope`: Default for all contexts
- `ApiScope`: Specific to an API
- `EnvironmentScope`: Specific to environment (prod, staging, etc.)
- `TagScope`: For tagged scenarios
- Custom implementations via `IScope`

**ConfigKey/ConfigValue**: Key-value pair for a config setting:
- `ConfigKey`: Immutable identifier (e.g., "timeout", "retries")
- `ConfigValue`: Immutable value with type (String, Int, Duration)

**Profile**: Configuration at a specific scope:
- Scope: Which context this profile applies to
- Configurations: Map<ConfigKey, ConfigValue>
- Immutable after creation

**ResolvedProfile**: Result of resolving profiles:
- Configurations: Final resolved config values
- AuditTrail: Which scope provided each value
- Immutable after creation

**ProfileResolver**: Pure function that resolves profiles:
- Input: Profile[], context (which scopes active)
- Output: ResolvedProfile (merged, conflict-free)
- Deterministic: Same input → same output

**ConflictHandler**: Detects and reports illegal conflicts:
- Two profiles at same scope with conflicting values = error
- Clear error messages guide resolution

---

## Implementation Phases

### Phase 1: Domain Foundations (5 tasks)

**Purpose**: Core configuration logic and scope handling

#### Task 1.1: Create Scope Abstraction & Built-in Types
- `IScope` interface (property: `ScopeLevel` for precedence)
- `GlobalScope` (level 0)
- `ApiScope` (level 1, parameterized by API name)
- `EnvironmentScope` (level 1, parameterized by env)
- `TagScope` (level 1, parameterized by tag)
- Unit tests for scope equality and precedence

#### Task 1.2: Create ConfigKey & ConfigValue Value Objects
- `ConfigKey` value object (immutable, comparable)
- `ConfigValue` value object with `ConfigType` enum
- Type enum: `String`, `Integer`, `Duration`, `Boolean`, `Custom`
- Equals/GetHashCode implementations
- Unit tests for immutability and equality

#### Task 1.3: Create Profile Entity
- Properties: `scope`, `configurations` (Map<ConfigKey, ConfigValue>)
- Immutable construction (no setters)
- Validation: no null keys, deterministic key ordering
- Factory methods for common profiles (Global, PerApi, etc.)
- Unit tests for immutability

#### Task 1.4: Create ResolvedProfile & ProfileResolver
- `ResolvedProfile` entity with resolved configurations and audit trail
- `ProfileResolver` service: `Resolve(Profile[], context) → ResolvedProfile`
- Resolution algorithm: Merge profiles by scope precedence
- Determinism: Same profiles + context = identical result every time
- Determinism tests (1000+ runs)

#### Task 1.5: Create ConflictHandler
- Detects illegal conflicts: Two profiles at same scope with conflicting keys
- Exception: `ConfigurationConflictException` with clear message
- Exception includes: scope, conflicting keys, suggested resolution
- Unit tests for conflict detection

---

### Phase 2: Application Layer (2 tasks)

**Purpose**: Service facade and data transfer

#### Task 2.1: Create DTOs & Mapping
- `ProfileDto`: Serializable profile representation
- `ResolvedProfileDto`: Result transfer object
- `ConfigKeyDto` and `ConfigValueDto`: Individual setting DTOs
- Bidirectional mapping (domain ↔ DTO)

#### Task 2.2: Create Use Cases & ProfileService
- `ResolveProfileUseCase`: Resolve profiles for given context
- `ValidateProfileUseCase`: Validate profile before use
- `DetectConflictsUseCase`: Pre-check for conflicts
- `ProfileService` application facade:
  - `Resolve(ProfileDtos[], context) → ResolvedProfileDto`
  - Error handling for conflicts

---

### Phase 3: Testing & Validation (4 tasks)

**Purpose**: Comprehensive test coverage and verification

#### Task 3.1: Unit Tests - Scopes & Config
- `ScopeTests`: Equality, precedence, immutability
- `ConfigKeyValueTests`: Type validation, immutability
- `ProfileTests`: Profile creation, immutability
- Coverage: 30+ tests

#### Task 3.2: Determinism & Resolution Tests
- `DeterminismTests`: 1000+ consecutive resolutions with identical results
- `SimpleResolutionTests`: Global + single override scenarios
- `ComplexResolutionTests`: Multiple scopes, multiple keys
- `HierarchyTests`: Scope precedence validated

#### Task 3.3: Conflict & Error Handling Tests
- `ConflictHandlerTests`: Detection of illegal conflicts
- `ErrorMessageTests`: Clear, actionable error messages
- `EdgeCaseTests`: Null handling, empty profiles, duplicate scopes

#### Task 3.4: Architecture Compliance Tests
- Verify no file I/O in domain
- Verify no env variable access
- Verify immutability of all entities
- Verify determinism across runs

---

### Phase 4: Documentation (2 tasks)

**Purpose**: Guides and API documentation

#### Task 4.1: Create README & Quick Start
- Architecture overview
- Quick start: resolve profiles for an API
- Scope type examples
- Conflict resolution guide

#### Task 4.2: Create Implementation Guide
- Step-by-step walkthrough
- Complete code examples
- Custom scope template
- Resolution algorithm explanation

---

## Task List (Detailed)

```markdown
# Profile Domain Tasks (13 total)

## Phase 1: Domain Foundations (5 tasks)

- [ ] T001 Create Scope abstraction & built-in types: `src/Domain/Scopes/IScope.cs`
- [ ] T002 Create ConfigKey & ConfigValue value objects
- [ ] T003 Create Profile entity: `src/Domain/Configuration/Profile.cs`
- [ ] T004 Create ResolvedProfile & ProfileResolver service
- [ ] T005 Create ConflictHandler: `src/Domain/Resolution/ConflictHandler.cs`

## Phase 2: Application Layer (2 tasks)

- [ ] T006 Create DTOs: `ProfileDto`, `ResolvedProfileDto`, etc.
- [ ] T007 Create use cases & ProfileService facade

## Phase 3: Testing (4 tasks)

- [ ] T008 Create unit tests (scopes, config, profiles)
- [ ] T009 Create determinism and resolution tests
- [ ] T010 Create conflict handling and error tests
- [ ] T011 Create architecture compliance tests

## Phase 4: Documentation (2 tasks)

- [ ] T012 Create README.md and quick start
- [ ] T013 Create IMPLEMENTATION_GUIDE.md
```

---

## Testing Strategy

### Test Pyramid

```
         ┌──────────┐
         │Integration  │ 20 tests
      ┌──┴──────────┴──┐
      │  Complex       │  30 tests
   ┌──┴────────────────┴──┐
   │    Unit Tests         │  80 tests
   └───────────────────────┘
```

### Determinism Testing

Critical for profile domain:

```csharp
[Fact]
public void ProfileResolver_ProducesDeterministicResults()
{
    var profiles = new[]
    {
        new Profile(GlobalScope.Instance, new Dictionary<ConfigKey, ConfigValue> { /* ... */ }),
        new Profile(new ApiScope("payment"), new Dictionary<ConfigKey, ConfigValue> { /* ... */ })
    };
    
    var context = new ResolutionContext(new ApiScope("payment"));
    
    var results = new HashSet<string>();
    for (int i = 0; i < 1000; i++)
    {
        var resolved = ProfileResolver.Resolve(profiles, context);
        results.Add(resolved.ToString());  // Deterministic string representation
    }
    
    // MUST be single result (all identical)
    Assert.Single(results);
}
```

### Complex Resolution Scenarios

Test scope hierarchy with multiple dimensions:

```csharp
[Fact]
public void ProfileResolver_HandlesMultipleDimensions()
{
    var profiles = new[]
    {
        new Profile(GlobalScope.Instance, new Map { timeout: 30s }),
        new Profile(new ApiScope("payment"), new Map { timeout: 60s }),
        new Profile(new EnvironmentScope("staging"), new Map { timeout: 90s }),
        new Profile(new CompositeScope(ApiScope("payment"), EnvironmentScope("staging")), new Map { timeout: 120s })
    };
    
    var context = new ResolutionContext(new ApiScope("payment"), new EnvironmentScope("staging"));
    var resolved = ProfileResolver.Resolve(profiles, context);
    
    Assert.Equal(120, resolved.Get("timeout"));  // Most specific scope wins
}
```

---

## Constitutional Compliance

### Specification-Driven Development ✅
- Specification precedes implementation
- All tasks derived from functional requirements

### Domain-Driven Design ✅
- Pure domain logic (Scope, Profile, Resolution)
- Ubiquitous language (ConfigKey, ResolvedProfile)
- Strategy pattern for scope types

### Clean Architecture ✅
- Profile domain has no infrastructure dependencies
- File I/O and env access are infrastructure responsibilities
- Application layer orchestrates domain logic

### Determinism & Reproducibility ✅
- No randomness, timestamps, or external dependencies
- Identical profiles + context → byte-identical resolution
- Critical for reproducible configurations

### Engine-Agnostic Design ✅
- Profiles work with any system, any engine
- No K6/JMeter/domain-specific code

### Evolution-Friendly ✅
- Strategy pattern for custom scope types
- New scopes added without modifying core
- Open/closed principle enforced

---

## Success Criteria

- ✅ All 13 tasks completed
- ✅ 130+ tests passing
- ✅ Determinism verified (1000+ identical runs)
- ✅ Complex multi-dimension resolution working
- ✅ Custom scope support demonstrated
- ✅ Zero file I/O or env variable access
- ✅ Documentation complete

---

## Timeline Estimate

- **Phase 1**: 2-3 days (domain foundations + resolver)
- **Phase 2**: 1-2 days (application layer)
- **Phase 3**: 2-3 days (testing)
- **Phase 4**: 1 day (documentation)

**Total**: 6-9 days

---

## References

- **Specification**: [spec.md](spec.md)
- **Metrics Domain**: ../metrics-domain/
- **Constitution**: docs/coding-rules/constitution.md
