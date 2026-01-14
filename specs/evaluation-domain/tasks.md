# Tasks: Evaluation Domain

**Input**: Design documents from `/specs/evaluation-domain/`  
**Prerequisites**: plan.md ✅ (complete), spec.md ✅ (complete)  
**Next Phases**: research.md, data-model.md, contracts/ (Phase 0 research documents)

**Organization**: Tasks grouped by user story (US1: Single Rule Evaluation, US2: Batch Evaluation, US3: Custom Rules) to enable independent implementation of each capability.

---

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies on incomplete tasks)
- **[Story]**: User story label (US1, US2, US3) - shows which feature this task belongs to
- File paths included for exact implementation location

---

## Phase 1: Setup & Project Initialization

**Purpose**: Project structure, dependencies, build configuration

- [X] T001 Create project structure: `src/PerformanceEngine.Evaluation.Domain/` with subdirectories (Domain/, Application/, Ports/)
- [X] T002 [P] Create test project structure: `tests/PerformanceEngine.Evaluation.Domain.Tests/` with mirrored layout
- [X] T003 Create C# project file: `src/PerformanceEngine.Evaluation.Domain/PerformanceEngine.Evaluation.Domain.csproj`
- [X] T004 [P] Create test project file: `tests/PerformanceEngine.Evaluation.Domain.Tests/PerformanceEngine.Evaluation.Domain.Tests.csproj`
- [X] T005 [P] Add NuGet dependencies: xUnit, FluentAssertions to both projects
- [X] T006 Add project reference from tests → domain
- [X] T007 Add project reference from domain → PerformanceEngine.Metrics.Domain (input dependency)
- [X] T008 Create global usings file: `src/PerformanceEngine.Evaluation.Domain/global.usings.cs`
- [X] T009 [P] Create build configuration files (.editorconfig, Directory.Build.props)

---

## Phase 2: Foundational Domain Layer (Blocking Prerequisites)

**Purpose**: Core domain models that ALL user stories depend on

⚠️ **CRITICAL**: No user story implementation can begin until this phase completes

### Base Value Objects & Abstractions

- [X] T010 Create Severity enum in `src/PerformanceEngine.Evaluation.Domain/Domain/Evaluation/Severity.cs`
  - Enum: PASS, WARN, FAIL
  - Include severity escalation logic: `FAIL > WARN > PASS`
  - Tests in `tests/.../Domain/Evaluation/SeverityTests.cs`

- [X] T011 Create Violation immutable value object in `src/PerformanceEngine.Evaluation.Domain/Domain/Evaluation/Violation.cs`
  - Properties: RuleId, MetricName, ActualValue, Threshold, Message
  - Immutable record type with init accessors
  - Value-based equality
  - Tests in `tests/.../Domain/Evaluation/ViolationTests.cs` (immutability, equality, null handling)

- [X] T012 Create EvaluationResult immutable entity in `src/PerformanceEngine.Evaluation.Domain/Domain/Evaluation/EvaluationResult.cs`
  - Properties: Outcome (Severity), Violations (ImmutableList), EvaluatedAt (DateTime for reproducibility)
  - Immutable record type
  - Deterministic string representation for testing
  - Tests in `tests/.../Domain/Evaluation/EvaluationResultTests.cs` (immutability, outcome escalation)

### Rule Interface (Strategy Pattern Foundation)

- [X] T013 Create Rule interface in `src/PerformanceEngine.Evaluation.Domain/Domain/Rules/IRule.cs`
  - Properties: Id, Name, Description
  - Method: `Evaluate(IMetric metric) → EvaluationResult`
  - Must support comparison for equality (`Equals`, `GetHashCode`)
  - Document: "All rule types must implement this contract"

**Checkpoint**: Severity, Violation, EvaluationResult, IRule defined and tested - foundation ready for user story implementation

---

## Phase 3: User Story 1 - Evaluate Metrics Against Simple Rules (P1 - MVP)

**Goal**: Evaluate a single metric against a single rule and produce EvaluationResult with violations if rule fails.

**Independent Test Criteria**:
- Single metric (p95 latency = 150ms) + ThresholdRule (p95 < 200ms) → PASS, no violations
- Single metric (p95 latency = 250ms) + ThresholdRule (p95 < 200ms) → FAIL, 1 violation with actual/expected values
- RangeRule (10% < error < 20%): 15% → PASS; 5% → FAIL; 25% → FAIL

### Implementation for US1

- [ ] T014 [P] [US1] Create ThresholdRule implementation in `src/PerformanceEngine.Evaluation.Domain/Domain/Rules/ThresholdRule.cs`
  - Support operators: <, ≤, >, ≥, ==, !=
  - Deterministic comparison logic
  - Tests: `tests/.../Domain/Rules/ThresholdRuleTests.cs` (all 6 operators, edge cases, boundary values)

- [ ] T015 [P] [US1] Create RangeRule implementation in `src/PerformanceEngine.Evaluation.Domain/Domain/Rules/RangeRule.cs`
  - Properties: MinBound, MaxBound
  - Logic: MinBound < metric < MaxBound
  - Tests: `tests/.../Domain/Rules/RangeRuleTests.cs` (in-range, below-min, above-max, equal bounds)

- [ ] T016 [P] [US1] Create Evaluator domain service in `src/PerformanceEngine.Evaluation.Domain/Domain/Evaluation/Evaluator.cs`
  - Method: `Evaluate(IMetric metric, IRule rule) → EvaluationResult`
  - Pure function: no side effects, deterministic output
  - Logic: Call rule.Evaluate(metric) → collect violations → return EvaluationResult with outcome
  - Tests: `tests/.../Domain/Evaluation/EvaluatorTests.cs` (single rule, single metric, determinism verification)

- [ ] T017 [US1] Create EvaluationService application facade in `src/PerformanceEngine.Evaluation.Domain/Application/Services/EvaluationService.cs`
  - Method: `Evaluate(metric, rule) → EvaluationResult` (delegates to Evaluator)
  - Error handling: graceful failures (null metric/rule → return error result)
  - Tests: `tests/.../Application/EvaluationServiceTests.cs` (end-to-end single metric)

- [ ] T018 [US1] Create DTOs in `src/PerformanceEngine.Evaluation.Domain/Application/Dto/`
  - `RuleDto.cs` (serializable rule representation)
  - `EvaluationResultDto.cs` (serializable result with violations)
  - `ViolationDto.cs` (serializable violation)
  - Mapping: Domain ↔ DTO (bidirectional)
  - Tests: `tests/.../Application/DtoTests.cs` (mapping correctness, null handling)

**Checkpoint**: US1 complete - can evaluate single metric against single rule with deterministic results

---

## Phase 4: User Story 2 - Evaluate Multiple Metrics in Batch (P1 - MVP)

**Goal**: Evaluate multiple metrics against multiple rules in a single operation, producing batch EvaluationResult list.

**Independent Test Criteria**:
- 2 metrics (p95=150ms, error_rate=0.5%) + 2 rules (p95<200ms, error_rate<1%) → 2 PASS results, no violations
- 2 metrics (p95=250ms, error_rate=2.5%) + 2 rules → 2 FAIL results, 2 violations total
- Rules evaluated in deterministic order regardless of input order

### Implementation for US2

- [ ] T019 [P] [US2] Create EvaluateMultipleMetricsUseCase in `src/PerformanceEngine.Evaluation.Domain/Application/UseCases/EvaluateMultipleMetricsUseCase.cs`
  - Input: `IEnumerable<IMetric>`, `IEnumerable<IRule>`
  - Output: `IEnumerable<EvaluationResult>` (one result per metric-rule pair, or one result per metric with all violations)
  - Logic: Batch evaluation with stable ordering
  - Tests: `tests/.../Application/UseCases/EvaluateMultipleMetricsUseCaseTests.cs`

- [ ] T020 [US2] Extend Evaluator with batch method in `src/PerformanceEngine.Evaluation.Domain/Domain/Evaluation/Evaluator.cs`
  - Method: `EvaluateMultiple(IEnumerable<IMetric> metrics, IEnumerable<IRule> rules) → IEnumerable<EvaluationResult>`
  - Logic: Deterministic ordering (sort metrics by name, rules by ID)
  - Tests: `tests/.../Domain/Evaluation/EvaluatorBatchTests.cs` (batch evaluation, ordering stability, determinism across different input orders)

- [ ] T021 [US2] Extend EvaluationService facade with batch method in `src/PerformanceEngine.Evaluation.Domain/Application/Services/EvaluationService.cs`
  - Method: `EvaluateBatch(List<EvaluationRequestDto>) → List<EvaluationResultDto>`
  - Tests: `tests/.../Application/EvaluationServiceBatchTests.cs` (end-to-end batch, DTO mapping)

- [ ] T022 [US2] Create batch DTOs in `src/PerformanceEngine.Evaluation.Domain/Application/Dto/`
  - `EvaluationRequestDto.cs` (metric + rule pairing)
  - Extend `EvaluationResultDto.cs` for batch scenarios
  - Tests: `tests/.../Application/BatchDtoTests.cs`

- [ ] T023 [P] [US2] Create determinism tests for batch evaluation in `tests/PerformanceEngine.Evaluation.Domain.Tests/Domain/Evaluation/DeterminismTests.cs`
  - Test: 1000 consecutive batch evaluations produce identical results
  - Test: Identical rule sets in different orders produce identical results
  - Test: Serialization byte-identical across runs

**Checkpoint**: US2 complete - can evaluate multiple metrics against multiple rules with stable, deterministic ordering

---

## Phase 5: User Story 3 - Support Custom Rule Types (P2 - Extension)

**Goal**: Enable application code to implement custom rule types without modifying core evaluation engine.

**Independent Test Criteria**:
- Custom rule implementing IRule interface evaluated successfully
- Evaluator works with custom rule without type checks
- Custom rule produces expected EvaluationResult
- New rule types can be added without changing Evaluator or domain logic

### Implementation for US3

- [ ] T024 [P] [US3] Create CustomPercentileRule example in `tests/PerformanceEngine.Evaluation.Domain.Tests/Domain/Rules/CustomRuleTests.cs`
  - Implements IRule
  - Evaluates percentile (e.g., p99 < threshold)
  - Demonstrates strategy pattern extensibility
  - Not in production code; example for documentation

- [ ] T025 [US3] Create RuleFactory utility in `src/PerformanceEngine.Evaluation.Domain/Domain/Rules/RuleFactory.cs`
  - Static methods for creating built-in rule types (ThresholdRule, RangeRule)
  - Document: "Custom rules can be instantiated directly by application code"
  - Tests: `tests/.../Domain/Rules/RuleFactoryTests.cs`

- [ ] T026 [US3] Create rule composition support (optional) in `src/PerformanceEngine.Evaluation.Domain/Domain/Rules/CompositeRule.cs`
  - Allow AND/OR combination of rules
  - Example: `AND(p95 < 200ms, error_rate < 1%)`
  - Tests: `tests/.../Domain/Rules/CompositeRuleTests.cs`

- [ ] T027 [P] [US3] Create rule extension documentation in `docs/CUSTOM_RULES.md`
  - How to implement IRule interface
  - Example: CustomPercentileRule walkthrough
  - How to register with EvaluationService

**Checkpoint**: US3 complete - custom rule extensibility demonstrated and tested

---

## Phase 6: Testing & Determinism Verification

**Purpose**: Comprehensive testing across all user stories and determinism guarantees

- [ ] T028 [P] Create determinism test harness in `tests/PerformanceEngine.Evaluation.Domain.Tests/Determinism/DeterminismTestBase.cs`
  - Base class for running operation 1000+ times
  - Serialize result each time
  - Assert all serializations byte-identical

- [ ] T029 Create cross-engine metric tests in `tests/PerformanceEngine.Evaluation.Domain.Tests/Integration/CrossEngineTests.cs`
  - Test same rule evaluates K6, JMeter, Gatling metrics identically
  - Create mock metrics simulating each engine format
  - Tests: Different engines, same metrics, same results

- [ ] T030 Create integration tests with Metrics Domain in `tests/PerformanceEngine.Evaluation.Domain.Tests/Integration/MetricsDomainIntegrationTests.cs`
  - Use real Metric objects from Metrics Domain
  - Evaluate with built-in rules
  - Verify no breaking changes to Metrics Domain interface

- [ ] T031 [P] Create architecture compliance tests in `tests/PerformanceEngine.Evaluation.Domain.Tests/Architecture/ArchitectureTests.cs`
  - Verify no infrastructure dependencies in domain layer
  - Verify immutability of EvaluationResult and Violation
  - Verify IRule interface implemented by all rule types
  - Verify no DateTime.Now, Random, or non-deterministic code in domain

- [ ] T032 Create edge case tests in `tests/PerformanceEngine.Evaluation.Domain.Tests/Domain/EdgeCaseTests.cs`
  - Null metrics/rules
  - Extreme values (double.MaxValue, double.MinValue)
  - Empty violation lists
  - Boundary conditions for all operators

**Checkpoint**: All tests passing, determinism verified, architecture compliance confirmed

---

## Phase 7: Documentation & Quick Start

**Purpose**: Developer guides and API documentation

- [ ] T033 Create README.md in `src/PerformanceEngine.Evaluation.Domain/README.md`
  - Architecture overview
  - Quick start: evaluate single metric
  - Rule types available (Threshold, Range)
  - Extension guide (custom rules)

- [ ] T034 Create IMPLEMENTATION_GUIDE.md in `src/PerformanceEngine.Evaluation.Domain/IMPLEMENTATION_GUIDE.md`
  - Step-by-step walkthrough (similar to Metrics Domain)
  - Code examples for each rule type
  - Common patterns and anti-patterns
  - Testing strategy

- [ ] T035 Create quickstart.md in `specs/evaluation-domain/quickstart.md`
  - Setup: clone, build, run tests
  - Basic evaluation example
  - Custom rule template
  - Testing your code

- [ ] T036 [P] Create API documentation in `specs/evaluation-domain/contracts/`
  - `rule-interface.md` (IRule contract)
  - `evaluator-interface.md` (Evaluator contract)
  - `evaluation-result.md` (EvaluationResult contract)

**Checkpoint**: Documentation complete, quick start guide validated

---

## Phase 8: Polish & Cross-Cutting Concerns

**Purpose**: Refinements affecting the entire domain

- [ ] T037 Code review: Domain layer for DDD compliance
- [ ] T038 Code review: Application layer for clean architecture
- [ ] T039 [P] Performance profiling: Evaluate 100 rules × 10 metrics, verify <10ms
- [ ] T040 [P] Code cleanup: Remove dead code, unused usings
- [ ] T041 Add XML documentation comments to all public APIs
- [ ] T042 Update main README.md with Evaluation Domain status
- [ ] T043 Run full test suite, verify all green
- [ ] T044 Validate against Constitution v1.0.0 compliance checklist

---

## Dependencies & Execution Order

### Phase Dependencies

```
Phase 1 (Setup)
    ↓ (blocks)
Phase 2 (Foundation: Severity, Violation, EvaluationResult, IRule)
    ↓ (blocks)
Phases 3-5 (User Stories: US1, US2, US3) ← CAN RUN IN PARALLEL
    ↓
Phase 6 (Testing & Determinism)
    ↓
Phase 7 (Documentation)
    ↓
Phase 8 (Polish)
```

### User Story Parallelization

Once Phase 2 (Foundation) completes:
- **US1 & US2 run in parallel** (different files: ThresholdRule, RangeRule, Evaluator)
- Both must complete before Phase 6 determinism testing begins
- **US3 (custom rules) can start during US1/US2** or wait until after

### Within User Stories

Parallel tasks within each story:
- **US1**: T014 & T015 (ThresholdRule, RangeRule) can run in parallel
- **US2**: T019 & T020 (use case, evaluator batch) can run mostly in parallel
- **US3**: T024 & T025 can run in parallel

### Suggested Execution Plan (Sequential for Single Developer)

1. Phase 1: 1 day (T001-T009)
2. Phase 2: 2 days (T010-T013) ← Foundation blocking
3. Phase 3: 2 days (T014-T023) ← US1 complete
4. Phase 4: 1.5 days (T019-T023) ← US2 complete (overlaps with US1 if parallelized)
5. Phase 5: 1.5 days (T024-T027) ← US3 complete
6. Phase 6: 1.5 days (T028-T032) ← Cross-domain & architecture tests
7. Phase 7: 1 day (T033-T036) ← Documentation
8. Phase 8: 1 day (T037-T044) ← Final polish

**Total: ~12-14 days** (can reduce to 8-9 with parallel team)

### Suggested Execution Plan (Team with 2+ Developers)

- Developer A: Phase 1-2 (setup, foundation) in parallel with Developer B
- Developer A: US1 while Developer B: US2 (phases 3-4 in parallel)
- Both: Phase 5-8 (testing, documentation, polish)

**Total: ~6-8 days** (with parallel work)

---

## Acceptance Criteria (All Tasks)

✅ All 44 tasks completed
✅ All tests passing (120+ total)
✅ Determinism: 1000+ consecutive runs produce byte-identical results
✅ Custom rule extensibility demonstrated (CustomPercentileRule example)
✅ Cross-domain integration with Metrics Domain verified
✅ Architecture compliance: zero infrastructure dependencies in domain
✅ Documentation complete (README, guides, contracts)
✅ Quick start guide validated by running through all steps

