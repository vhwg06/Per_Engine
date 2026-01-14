# Tasks: Profile Domain

**Input**: Design documents from `/specs/profile-domain/`  
**Prerequisites**: plan.md ✅ (complete), spec.md ✅ (complete)  
**Next Phases**: research.md, data-model.md, contracts/ (Phase 0 research documents)

**Organization**: Tasks grouped by user story (US1: Global Configuration, US2: Per-Context Overrides, US3: Multiple Dimensions, US4: Custom Scopes) to enable independent implementation of each capability.

---

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies on incomplete tasks)
- **[Story]**: User story label (US1, US2, US3, US4) - shows which feature this task belongs to
- File paths included for exact implementation location

---

## Phase 1: Setup & Project Initialization

**Purpose**: Project structure, dependencies, build configuration

- [ ] T001 Create project structure: `src/PerformanceEngine.Profile.Domain/` with subdirectories (Domain/, Application/, Ports/)
- [ ] T002 [P] Create test project structure: `tests/PerformanceEngine.Profile.Domain.Tests/` with mirrored layout
- [ ] T003 Create C# project file: `src/PerformanceEngine.Profile.Domain/PerformanceEngine.Profile.Domain.csproj`
- [ ] T004 [P] Create test project file: `tests/PerformanceEngine.Profile.Domain.Tests/PerformanceEngine.Profile.Domain.Tests.csproj`
- [ ] T005 [P] Add NuGet dependencies: xUnit, FluentAssertions to both projects
- [ ] T006 Add project reference from tests → domain
- [ ] T007 [P] Create global usings file: `src/PerformanceEngine.Profile.Domain/global.usings.cs`
- [ ] T008 [P] Create build configuration files (.editorconfig, Directory.Build.props)

---

## Phase 2: Foundational Domain Layer (Blocking Prerequisites)

**Purpose**: Core domain models that ALL user stories depend on

⚠️ **CRITICAL**: No user story implementation can begin until this phase completes

### Base Value Objects & Abstractions

- [ ] T009 Create ConfigKey immutable value object in `src/PerformanceEngine.Profile.Domain/Domain/Configuration/ConfigKey.cs`
  - Property: Name (string)
  - Immutable record type
  - Value-based equality
  - Tests: `tests/.../Domain/Configuration/ConfigKeyTests.cs` (equality, null handling)

- [ ] T010 [P] Create ConfigValue immutable value object in `src/PerformanceEngine.Profile.Domain/Domain/Configuration/ConfigValue.cs`
  - Properties: Value (object), Type (ConfigType enum)
  - Immutable record type
  - Supports types: String, Int, Duration, Double, Bool
  - Value-based equality
  - Tests: `tests/.../Domain/Configuration/ConfigValueTests.cs` (type validation, equality)

- [ ] T011 [P] Create ConfigType enum in `src/PerformanceEngine.Profile.Domain/Domain/Configuration/ConfigType.cs`
  - Values: String, Int, Duration, Double, Bool
  - Helper methods: `ToConfigType(object value) → ConfigType`

- [ ] T012 Create Profile immutable entity in `src/PerformanceEngine.Profile.Domain/Domain/Profiles/Profile.cs`
  - Properties: Scope, Configurations (ImmutableDictionary<ConfigKey, ConfigValue>)
  - Immutable record type
  - Tests: `tests/.../Domain/Profiles/ProfileTests.cs` (immutability, scope property)

### Scope Interface (Strategy Pattern Foundation)

- [ ] T013 Create Scope interface in `src/PerformanceEngine.Profile.Domain/Domain/Scopes/IScope.cs`
  - Properties: Id, Type, Precedence (int), Description
  - Method: `CompareTo(IScope other) → int` (for ordering)
  - Must support comparison and equality (`Equals`, `GetHashCode`)
  - Document: "All scope types must implement this contract"

- [ ] T014 Create ConflictHandler domain service in `src/PerformanceEngine.Profile.Domain/Domain/Configuration/ConflictHandler.cs`
  - Method: `DetectConflicts(IEnumerable<Profile> profiles) → List<ConfigurationConflictException>`
  - Logic: Two profiles at same scope with different values for same key = conflict
  - Tests: `tests/.../Domain/Configuration/ConflictHandlerTests.cs` (conflict detection, error messages)

**Checkpoint**: ConfigKey, ConfigValue, Profile, IScope, ConflictHandler defined and tested - foundation ready for user story implementation

---

## Phase 3: User Story 1 - Apply Global Configuration (P1 - MVP)

**Goal**: System has global profile that applies to all contexts by default.

**Independent Test Criteria**:
- Global profile with timeout=30s → all contexts use 30s (no override)
- Global profile with timeout=30s + API profile with timeout=60s → API gets 60s (override wins)
- Deterministic resolution: same profiles always produce same result

### Implementation for US1

- [ ] T015 [P] [US1] Create GlobalScope implementation in `src/PerformanceEngine.Profile.Domain/Domain/Scopes/GlobalScope.cs`
  - Precedence: 0 (lowest)
  - Type: "Global"
  - Singleton instance
  - Tests: `tests/.../Domain/Scopes/GlobalScopeTests.cs` (precedence, equality)

- [ ] T016 [P] [US1] Create ResolvedProfile immutable entity in `src/PerformanceEngine.Profile.Domain/Domain/Profiles/ResolvedProfile.cs`
  - Properties: Configuration (ImmutableDictionary<ConfigKey, ConfigValue>), AuditTrail (ImmutableDictionary<ConfigKey, ImmutableList<IScope>>), ResolvedAt (DateTime)
  - Immutable record type
  - Tests: `tests/.../Domain/Profiles/ResolvedProfileTests.cs` (immutability, audit trail)

- [ ] T017 [US1] Create ProfileResolver domain service in `src/PerformanceEngine.Profile.Domain/Domain/Profiles/ProfileResolver.cs`
  - Method: `Resolve(IEnumerable<Profile> profiles, IScope requestedScope) → ResolvedProfile`
  - Logic: Global profile applies to all scopes; if requested scope matches, merge values (requested scope wins)
  - Pure function: no side effects, deterministic output
  - Tests: `tests/.../Domain/Profiles/ProfileResolverTests.cs` (global scope resolution, override behavior)

- [ ] T018 [US1] Create ProfileService application facade in `src/PerformanceEngine.Profile.Domain/Application/Services/ProfileService.cs`
  - Method: `Resolve(IEnumerable<Profile> profiles, IScope scope) → ResolvedProfile`
  - Error handling: ConflictHandler integration; fail-fast on conflicts
  - Tests: `tests/.../Application/ProfileServiceTests.cs` (end-to-end resolution)

- [ ] T019 [US1] Create DTOs in `src/PerformanceEngine.Profile.Domain/Application/Dto/`
  - `ProfileDto.cs` (serializable profile)
  - `ResolvedProfileDto.cs` (serializable result with audit trail)
  - `ConfigKeyDto.cs`, `ConfigValueDto.cs`
  - `ScopeDto.cs` (for serialization)
  - Mapping: Domain ↔ DTO (bidirectional)
  - Tests: `tests/.../Application/DtoTests.cs`

**Checkpoint**: US1 complete - can resolve global profile, applies to all contexts

---

## Phase 4: User Story 2 - Override Configuration Per Context (P1 - MVP)

**Goal**: Different APIs/environments have different requirements; each can override global config.

**Independent Test Criteria**:
- Global: timeout=30s, retries=3, ramp=1m
- API(payment): timeout=60s
- Resolving for API(payment) → timeout=60s (override), retries=3 (global), ramp=1m (global)
- Resolving for API(search): timeout=30s (global), retries=3 (global), ramp=1m (global)

### Implementation for US2

- [ ] T020 [P] [US2] Create ApiScope implementation in `src/PerformanceEngine.Profile.Domain/Domain/Scopes/ApiScope.cs`
  - Properties: ApiName
  - Precedence: 10 (higher than Global)
  - Type: "Api"
  - Tests: `tests/.../Domain/Scopes/ApiScopeTests.cs` (precedence, equality, api name)

- [ ] T021 [P] [US2] Create EnvironmentScope implementation in `src/PerformanceEngine.Profile.Domain/Domain/Scopes/EnvironmentScope.cs`
  - Properties: EnvironmentName (prod, staging, dev)
  - Precedence: 15 (higher than Global, slightly higher than Api)
  - Type: "Environment"
  - Tests: `tests/.../Domain/Scopes/EnvironmentScopeTests.cs`

- [ ] T022 [US2] Extend ProfileResolver to handle scope hierarchy in `src/PerformanceEngine.Profile.Domain/Domain/Profiles/ProfileResolver.cs`
  - Update: `Resolve()` logic to handle scope precedence
  - Logic: For each config key, find highest-precedence profile that defines it
  - Deterministic ordering: if multiple profiles at same precedence with same key = conflict
  - Tests: `tests/.../Domain/Profiles/ScopeHierarchyTests.cs` (precedence ordering, partial overrides)

- [ ] T023 [US2] Create ResolveProfileUseCase in `src/PerformanceEngine.Profile.Domain/Application/UseCases/ResolveProfileUseCase.cs`
  - Input: profiles collection + requested scope
  - Output: ResolvedProfile with audit trail
  - Error handling: catch conflicts, throw clear exception
  - Tests: `tests/.../Application/UseCases/ResolveProfileUseCaseTests.cs`

- [ ] T024 [US2] Extend ProfileService facade with conflict detection in `src/PerformanceEngine.Profile.Domain/Application/Services/ProfileService.cs`
  - Method: `Resolve()` calls ConflictHandler before resolution
  - Fail-fast: throw ConfigurationConflictException if conflicts found
  - Tests: `tests/.../Application/ProfileServiceConflictTests.cs`

- [ ] T025 [P] [US2] Create determinism tests for scope resolution in `tests/PerformanceEngine.Profile.Domain.Tests/Domain/Profiles/DeterminismTests.cs`
  - Test: 1000 consecutive resolutions produce identical results
  - Test: Identical profile sets in different orders produce identical results
  - Test: Serialization byte-identical across runs

**Checkpoint**: US2 complete - can resolve per-context overrides with deterministic scope hierarchy

---

## Phase 5: User Story 3 - Support Multiple Scope Dimensions (P2 - Extension)

**Goal**: Configuration depends on multiple dimensions (API + Environment + Tag); most specific scope wins.

**Independent Test Criteria**:
- Global: timeout=30s
- Env(staging): timeout=60s
- API(payment): timeout=90s
- API(payment) + Env(staging): timeout=120s
- Resolving for API(payment) in staging → timeout=120s (most specific wins)

### Implementation for US3

- [ ] T026 [P] [US3] Create CompositeScope for multi-dimensional contexts in `src/PerformanceEngine.Profile.Domain/Domain/Scopes/CompositeScope.cs`
  - Properties: BaseScopeA, BaseScopeB (composition of two scopes)
  - Precedence: calculated as max(scopeA.precedence, scopeB.precedence) + 5
  - Type: Composite
  - Tests: `tests/.../Domain/Scopes/CompositeScopeTests.cs`

- [ ] T027 [P] [US3] Create TagScope implementation in `src/PerformanceEngine.Profile.Domain/Domain/Scopes/TagScope.cs`
  - Properties: TagName
  - Precedence: 20 (configurable)
  - Type: "Tag"
  - Tests: `tests/.../Domain/Scopes/TagScopeTests.cs`

- [ ] T028 [US3] Create MultiDimensionalResolution tests in `tests/PerformanceEngine.Profile.Domain.Tests/Domain/Profiles/MultiDimensionalTests.cs`
  - Test: Global + Env + API + Tag combinations
  - Test: Verify precedence ordering is correct (most specific wins)
  - Test: Determinism across different resolution orders

- [ ] T029 [US3] Create ScopeComparison utility in `src/PerformanceEngine.Profile.Domain/Domain/Scopes/ScopeComparison.cs`
  - Helper methods: `IsMostSpecific(scope, otherScopes)`, `RankByPrecedence(scopes)`
  - Tests: `tests/.../Domain/Scopes/ScopeComparisonTests.cs`

- [ ] T030 [US3] Create comprehensive scope hierarchy documentation in `docs/SCOPE_HIERARCHY.md`
  - Precedence rules: Global < Api < Env < Tag < Composite(Api+Env)
  - Examples: Different dimension combinations
  - How conflicts are detected

**Checkpoint**: US3 complete - multi-dimensional scope resolution working with deterministic precedence

---

## Phase 6: User Story 4 - Support Extensible Scopes (P2 - Extension)

**Goal**: Custom scope types can be added without modifying core resolver.

**Independent Test Criteria**:
- Custom scope implementing IScope interface resolved successfully
- Resolver works with custom scope without type checks
- Custom scope respects hierarchy rules (precedence comparison)
- New scope types can be added without changing ProfileResolver

### Implementation for US4

- [ ] T031 [P] [US4] Create CustomPaymentMethodScope example in `tests/PerformanceEngine.Profile.Domain.Tests/Domain/Scopes/CustomScopeTests.cs`
  - Implements IScope
  - PaymentMethod-specific configuration (visa, mastercard, paypal)
  - Demonstrates extensibility without core changes
  - Not in production code; example for documentation

- [ ] T032 [US4] Create ScopeFactory utility in `src/PerformanceEngine.Profile.Domain/Domain/Scopes/ScopeFactory.cs`
  - Static methods for creating built-in scope types
  - Document: "Custom scopes can be instantiated directly by application code"
  - Tests: `tests/.../Domain/Scopes/ScopeFactoryTests.cs`

- [ ] T033 [P] [US4] Create scope extension documentation in `docs/CUSTOM_SCOPES.md`
  - How to implement IScope interface
  - Precedence selection guidelines
  - Example: CustomPaymentMethodScope walkthrough
  - How to register with ProfileResolver

- [ ] T034 [US4] Create ScopeRegistry for runtime scope registration in `src/PerformanceEngine.Profile.Domain/Application/Services/ScopeRegistry.cs`
  - Allow registration of custom scopes at runtime
  - Query: `GetScopeByType(string type) → IScope`
  - Tests: `tests/.../Application/ScopeRegistryTests.cs`

**Checkpoint**: US4 complete - custom scope extensibility demonstrated and tested

---

## Phase 7: Testing & Determinism Verification

**Purpose**: Comprehensive testing across all user stories and determinism guarantees

- [ ] T035 [P] Create determinism test harness in `tests/PerformanceEngine.Profile.Domain.Tests/Determinism/DeterminismTestBase.cs`
  - Base class for running operation 1000+ times
  - Serialize result each time
  - Assert all serializations byte-identical

- [ ] T036 Create complex scenario tests in `tests/PerformanceEngine.Profile.Domain.Tests/Domain/Profiles/ComplexResolutionTests.cs`
  - 10+ scopes with varying precedence
  - Multiple profiles per scope
  - Verify deterministic resolution across different orderings
  - Verify audit trail accuracy

- [ ] T037 [P] Create conflict detection comprehensive tests in `tests/PerformanceEngine.Profile.Domain.Tests/Domain/Configuration/ConflictDetectionTests.cs`
  - Illegal conflicts caught (same scope, different values)
  - Clear error messages
  - All conflict types covered

- [ ] T038 Create integration tests in `tests/PerformanceEngine.Profile.Domain.Tests/Integration/ProfileServiceIntegrationTests.cs`
  - Full resolution pipeline with conflict handling
  - End-to-end scenario: create profiles, resolve, verify audit trail
  - Error handling verification

- [ ] T039 [P] Create architecture compliance tests in `tests/PerformanceEngine.Profile.Domain.Tests/Architecture/ArchitectureTests.cs`
  - Verify no file I/O in domain layer
  - Verify no environment variable access in domain layer
  - Verify immutability of ResolvedProfile and ConfigValue
  - Verify IScope interface implemented by all scope types
  - Verify no non-deterministic code (DateTime.Now, Random)

- [ ] T040 Create edge case tests in `tests/PerformanceEngine.Profile.Domain.Tests/Domain/EdgeCaseTests.cs`
  - Null scopes/profiles
  - Empty configuration dictionaries
  - Scope with infinite precedence
  - Circular composite scopes (should be prevented)
  - Single profile scenarios

**Checkpoint**: All tests passing, determinism verified, architecture compliance confirmed

---

## Phase 8: Documentation & Quick Start

**Purpose**: Developer guides and API documentation

- [ ] T041 Create README.md in `src/PerformanceEngine.Profile.Domain/README.md`
  - Architecture overview
  - Quick start: resolve global profile
  - Built-in scope types (Global, Api, Environment, Tag)
  - Extension guide (custom scopes)

- [ ] T042 Create IMPLEMENTATION_GUIDE.md in `src/PerformanceEngine.Profile.Domain/IMPLEMENTATION_GUIDE.md`
  - Step-by-step walkthrough (similar to Metrics Domain)
  - Code examples for each scope type
  - Scope precedence rules
  - Conflict detection and handling
  - Testing strategy

- [ ] T043 Create quickstart.md in `specs/profile-domain/quickstart.md`
  - Setup: clone, build, run tests
  - Basic resolution example
  - Custom scope template
  - Testing your code

- [ ] T044 [P] Create API documentation in `specs/profile-domain/contracts/`
  - `scope-interface.md` (IScope contract)
  - `resolver-interface.md` (ProfileResolver contract)
  - `resolved-profile.md` (ResolvedProfile contract)
  - `conflict-handling.md` (ConflictHandler contract)

**Checkpoint**: Documentation complete, quick start guide validated

---

## Phase 9: Polish & Cross-Cutting Concerns

**Purpose**: Refinements affecting the entire domain

- [ ] T045 Code review: Domain layer for DDD compliance
- [ ] T046 Code review: Application layer for clean architecture
- [ ] T047 [P] Performance profiling: Resolve 100-key config over 10 dimensions, verify <5ms
- [ ] T048 [P] Code cleanup: Remove dead code, unused usings
- [ ] T049 Add XML documentation comments to all public APIs
- [ ] T050 Update main README.md with Profile Domain status
- [ ] T051 Run full test suite, verify all green
- [ ] T052 Validate against Constitution v1.0.0 compliance checklist

---

## Dependencies & Execution Order

### Phase Dependencies

```
Phase 1 (Setup)
    ↓ (blocks)
Phase 2 (Foundation: ConfigKey, ConfigValue, Profile, IScope, ConflictHandler)
    ↓ (blocks)
Phases 3-6 (User Stories: US1, US2, US3, US4) ← CAN RUN IN PARALLEL
    ↓
Phase 7 (Testing & Determinism)
    ↓
Phase 8 (Documentation)
    ↓
Phase 9 (Polish)
```

### User Story Parallelization

Once Phase 2 (Foundation) completes:
- **US1 & US2 run in parallel** (different scope types: Global, Api, Environment)
- **US3 (composite/multi-dimensional) can start after US1 & US2** (depends on ApiScope, EnvironmentScope)
- **US4 (custom scopes) can run in parallel with US3** (depends on IScope interface, which is in Phase 2)

### Within User Stories

Parallel tasks within each story:
- **US1**: T015 & T016 (GlobalScope, ResolvedProfile) can run in parallel
- **US2**: T020 & T021 (ApiScope, EnvironmentScope) can run in parallel
- **US3**: T026 & T027 (CompositeScope, TagScope) can run in parallel
- **US4**: T031 & T032 (custom scope example, factory) can run in parallel

### Suggested Execution Plan (Sequential for Single Developer)

1. Phase 1: 1 day (T001-T008)
2. Phase 2: 2 days (T009-T014) ← Foundation blocking
3. Phase 3: 1.5 days (T015-T025) ← US1 complete
4. Phase 4: 1.5 days (overlaps with US3, T020-T025) ← US2 complete
5. Phase 5: 1 day (T026-T030) ← US3 complete (multi-dimensional)
6. Phase 6: 1 day (T031-T034) ← US4 complete (custom scopes)
7. Phase 7: 1.5 days (T035-T040) ← Cross-domain & architecture tests
8. Phase 8: 1 day (T041-T044) ← Documentation
9. Phase 9: 1 day (T045-T052) ← Final polish

**Total: ~12-14 days** (can reduce to 8-9 with parallel team)

### Suggested Execution Plan (Team with 2+ Developers)

- Developer A: Phase 1-2 (setup, foundation)
- Developer A: US1 + US3 while Developer B: US2 + US4
- Both: Phase 7-9 (testing, documentation, polish)

**Total: ~6-8 days** (with parallel work)

---

## Acceptance Criteria (All Tasks)

✅ All 52 tasks completed
✅ All tests passing (120+ total)
✅ Determinism: 1000+ consecutive runs produce byte-identical results
✅ Conflict detection: illegal conflicts caught with clear error messages
✅ Custom scope extensibility demonstrated (CustomPaymentMethodScope example)
✅ Multi-dimensional scope resolution verified (API + Environment + Tag)
✅ Architecture compliance: zero file I/O and environment access in domain
✅ Documentation complete (README, guides, contracts, scope hierarchy)
✅ Quick start guide validated by running through all steps

