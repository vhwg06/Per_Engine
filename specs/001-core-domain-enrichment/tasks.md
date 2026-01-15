# Tasks: Core Domain Enrichment

**Input**: Design documents from `/specs/001-core-domain-enrichment/`
**Prerequisites**: plan.md ✅, spec.md ✅, research.md ✅, data-model.md ✅, contracts/ ✅

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story. Tasks follow strict checklist format:
```
- [ ] [TaskID] [P?] [Story?] Description with exact file path
```

**Format Key**:
- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story ([US1], [US2], [US3], [US4], [US5])
- **TaskID**: Sequential (T001, T002, T003...) in execution order
- **File paths**: Absolute paths from `src/` or `tests/`

---

## Phase 1: Setup (Shared Infrastructure & Project Structure)

**Purpose**: Project initialization and basic structure validation for enrichment implementation

**⚠️ GATE**: Foundation setup must complete before moving to Phase 2

- [x] T001 Verify solution structure aligns with plan.md architecture (3 domain projects + shared utilities)
- [x] T002 Create checklists/enrichment-implementation.md tracking document in specs/001-core-domain-enrichment/checklists/
- [x] T003 [P] Review and document existing Metric, Evaluation, and Profile domain implementations in src/PerformanceEngine.Metrics.Domain/Domain/, src/PerformanceEngine.Evaluation.Domain/Domain/, src/PerformanceEngine.Profile.Domain/Domain/

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core infrastructure that MUST be complete before ANY user story can be implemented

**⚠️ CRITICAL**: No user story work can begin until this phase is complete. These tasks establish shared value objects, base classes, and patterns used across all enrichments.

**Architecture Conformance**: All foundational tasks respect constitutional principles (DDD, Clean Architecture, determinism, immutability)

- [x] T004 Create ValueObject base class in src/PerformanceEngine.Metrics.Domain/Domain/ValueObjects/ (immutability, equality-based comparison) if not already present
- [x] T005 [P] Create ValueObject base class in src/PerformanceEngine.Evaluation.Domain/Domain/ValueObjects/ if not already present
- [x] T006 [P] Create ValueObject base class in src/PerformanceEngine.Profile.Domain/Domain/ValueObjects/ if not already present
- [x] T007 Setup determinism test utility pattern in tests/PerformanceEngine.Metrics.Domain.Tests/Determinism/ (1000+ iteration verification framework)
- [x] T008 [P] Setup determinism test utility pattern in tests/PerformanceEngine.Evaluation.Domain.Tests/Determinism/
- [x] T009 [P] Setup determinism test utility pattern in tests/PerformanceEngine.Profile.Domain.Tests/Determinism/
- [x] T010 Create shared test fixtures for metric, evaluation, and profile test doubles in tests/PerformanceEngine.*.Domain.Tests/Fixtures/

**Checkpoint**: Foundation ready for user story implementation ✓

---

## Phase 3: User Story 1 - Ensure Metrics Reliability Through Completeness Metadata (Priority: P1) 🎯 MVP

**Goal**: Expose metric completeness status (COMPLETE/PARTIAL) and evidence metadata (sample count, aggregation window) so evaluators can make informed decisions about partial metrics.

**Independent Test**: Metrics API returns completeness status and evidence metadata for all metrics. Partial metrics can be inspected and handled appropriately by evaluation rules.

**Story Dependencies**: None (independent; foundational only)

### Implementation for User Story 1

#### Models & Value Objects

- [x] T011 [P] [US1] Create CompletessStatus enum in src/PerformanceEngine.Metrics.Domain/Domain/CompletessStatus.cs with COMPLETE=1, PARTIAL=2 values
- [x] T012 [P] [US1] Create MetricEvidence value object in src/PerformanceEngine.Metrics.Domain/Domain/Metrics/MetricEvidence.cs with properties: SampleCount (int), RequiredSampleCount (int), AggregationWindow (string), and IsComplete (bool computed property)

#### Ports & Extensions

- [x] T013 [US1] Extend IMetric interface in src/PerformanceEngine.Metrics.Domain/Domain/Ports/IMetric.cs to include: CompletessStatus property, MetricEvidence GetEvidence() method
- [x] T014 [US1] Update Metric aggregate in src/PerformanceEngine.Metrics.Domain/Domain/Metrics/Metric.cs to: add CompletessStatus and Evidence properties (immutable), implement factory method Metric.Create(..., sampleCount, requiredSampleCount, aggregationWindow) with completeness logic

#### Infrastructure & Adapter Updates

- [x] T015 [P] [US1] Update existing metric adapters/implementations in src/PerformanceEngine.Metrics.Domain/Infrastructure/ or Ports/ to provide CompletessStatus and MetricEvidence when creating metrics (no performance degradation)

#### Domain Tests

- [x] T016 [P] [US1] Unit tests for MetricEvidence value object in tests/PerformanceEngine.Metrics.Domain.Tests/Domain/Metrics/MetricEvidenceTests.cs: invariants, IsComplete property, equality
- [x] T017 [P] [US1] Unit tests for Metric.Create() factory in tests/PerformanceEngine.Metrics.Domain.Tests/Domain/Metrics/MetricTests.cs: COMPLETE/PARTIAL status determination, evidence creation, validation
- [x] T018 [US1] Contract tests for IMetric extension in tests/PerformanceEngine.Metrics.Domain.Tests/Domain/Ports/IMetricExtensionTests.cs: all implementations expose CompletessStatus and GetEvidence()
- [x] T019 [US1] Determinism verification tests in tests/PerformanceEngine.Metrics.Domain.Tests/Determinism/MetricDeterminismTests.cs: identical metrics with identical evidence across 1000+ iterations, JSON serialization identical

**Checkpoint**: User Story 1 complete ✓
- Metrics expose completeness status and evidence metadata
- Partial metrics can be identified and inspected
- No performance degradation in metric production
- Determinism verified (1000+ iteration tests pass)

---

## Phase 4: User Story 2 - Provide Transparent Evaluation Decisions With Evidence (Priority: P1)

**Goal**: Evaluation results include complete evidence (rule applied, metrics used, actual values, constraints, decision outcome) that fully explains decisions without log inspection.

**Independent Test**: Evaluation results contain EvaluationEvidence capturing: rule ID/name, metrics used with references, actual values, expected constraints, and outcome decision. Evidence is deterministically identical for identical inputs.

**Story Dependencies**: Depends on User Story 1 (metrics with evidence available); otherwise independent

### Implementation for User Story 2

#### Models & Enums Extension

- [ ] T020 [P] [US2] Extend Outcome enum in src/PerformanceEngine.Evaluation.Domain/Domain/Outcome.cs to add INCONCLUSIVE=3 value (keeping PASS=1, FAIL=2)
- [ ] T021 [P] [US2] Create MetricReference value object in src/PerformanceEngine.Evaluation.Domain/Domain/Evaluation/MetricReference.cs with properties: AggregationName (string), Value (double), Unit (string), CompletessStatus (enum reference)

#### Value Objects for Evidence Trail

- [ ] T022 [P] [US2] Create EvaluationEvidence value object in src/PerformanceEngine.Evaluation.Domain/Domain/Evaluation/EvaluationEvidence.cs with properties:
  - RuleId (string), RuleName (string)
  - MetricsUsed (IReadOnlyList<MetricReference>)
  - ActualValues (IReadOnlyDictionary<string, double>)
  - ExpectedConstraint (string)
  - ConstraintSatisfied (bool)
  - Decision (string)
  - EvaluatedAt (DateTime UTC)

#### Aggregate Extension

- [ ] T023 [US2] Extend EvaluationResult aggregate in src/PerformanceEngine.Evaluation.Domain/Domain/Evaluation/EvaluationResult.cs to: include Evidence property (EvaluationEvidence), add OutcomeReason (string), make immutable, add factory method CreateInconclusive(...) for INCONCLUSIVE outcomes

#### Service Update with Determinism Guarantee

- [ ] T024 [US2] Update Evaluator service in src/PerformanceEngine.Evaluation.Domain/Application/Evaluation/Evaluator.cs to:
  - Sort violations deterministically by (RuleId, MetricName) before aggregating
  - Capture DateTime.UtcNow once at evaluation start (not per-rule)
  - Build EvaluationEvidence with all rule, metric, and value details
  - Support INCONCLUSIVE outcomes when metrics incomplete
  - Guarantee: identical inputs → identical evidence + outcome + violations

#### Tests for User Story 2

- [ ] T025 [P] [US2] Unit tests for MetricReference in tests/PerformanceEngine.Evaluation.Domain.Tests/Domain/Evaluation/MetricReferenceTests.cs
- [ ] T026 [P] [US2] Unit tests for EvaluationEvidence in tests/PerformanceEngine.Evaluation.Domain.Tests/Domain/Evaluation/EvaluationEvidenceTests.cs: construction, validation, immutability
- [ ] T027 [P] [US2] Unit tests for Outcome enum extension in tests/PerformanceEngine.Evaluation.Domain.Tests/Domain/OutcomeTests.cs: includes INCONCLUSIVE, backward compatible with existing PASS/FAIL code
- [ ] T028 [US2] Unit tests for EvaluationResult extension in tests/PerformanceEngine.Evaluation.Domain.Tests/Domain/Evaluation/EvaluationResultTests.cs: evidence creation, outcome reasons, CreateInconclusive() factory
- [ ] T029 [US2] Integration tests for Evaluator service in tests/PerformanceEngine.Evaluation.Domain.Tests/Application/Evaluation/EvaluatorTests.cs: rules evaluated, violations sorted, evidence populated, outcome determined
- [ ] T030 [US2] Determinism verification tests in tests/PerformanceEngine.Evaluation.Domain.Tests/Determinism/EvaluationDeterminismTests.cs: identical inputs produce identical evidence (JSON byte-for-byte, 1000+ iterations), violations always sorted same order

**Checkpoint**: User Story 2 complete ✓
- Evaluation results include complete evidence trail
- Evidence explains decision without logs
- INCONCLUSIVE outcome supported
- Determinism verified for identical inputs

---

## Phase 5: User Story 3 - Handle Incomplete Evidence Gracefully With INCONCLUSIVE Outcome (Priority: P1)

**Goal**: Evaluation returns INCONCLUSIVE when metrics incomplete or execution partial, rather than forcing false PASS/FAIL.

**Independent Test**: Evaluation returns INCONCLUSIVE when: metrics marked PARTIAL, execution incomplete, or insufficient evidence exists. INCONCLUSIVE distinguishes from PASS/FAIL decisions.

**Story Dependencies**: Depends on User Story 1 (metric completeness) and User Story 2 (INCONCLUSIVE outcome); otherwise independent

### Implementation for User Story 3

#### Application Layer Validation Gates

- [ ] T031 [US3] Create IPartialMetricPolicy interface in src/PerformanceEngine.Evaluation.Domain/Ports/IPartialMetricPolicy.cs to allow/deny partial metrics per rule
- [ ] T032 [US3] Implement default PartialMetricPolicy in src/PerformanceEngine.Evaluation.Domain/Application/Evaluation/PartialMetricPolicy.cs: deny partial metrics by default, allow explicitly configured rules
- [ ] T033 [US3] Update Evaluator.Evaluate(...) in src/PerformanceEngine.Evaluation.Domain/Application/Evaluation/Evaluator.cs to:
  - Check if metric is PARTIAL via CompletessStatus
  - Query IPartialMetricPolicy to determine if PARTIAL allowed
  - Return INCONCLUSIVE with reason if PARTIAL not allowed or insufficient evidence exists
  - Include all incomplete metrics in EvaluationEvidence for auditing

#### Tests for User Story 3

- [ ] T034 [P] [US3] Unit tests for IPartialMetricPolicy in tests/PerformanceEngine.Evaluation.Domain.Tests/Ports/PartialMetricPolicyTests.cs: allow/deny decisions, default behavior
- [ ] T035 [P] [US3] Unit tests for partial metric handling in tests/PerformanceEngine.Evaluation.Domain.Tests/Application/Evaluation/PartialMetricHandlingTests.cs: INCONCLUSIVE returned when PARTIAL not allowed, reason populated
- [ ] T036 [US3] Integration tests for incomplete evaluation scenarios in tests/PerformanceEngine.Evaluation.Domain.Tests/Integration/IncompleteEvaluationTests.cs:
  - Partial metric + no allow policy → INCONCLUSIVE
  - Partial metric + allow policy → evaluation proceeds
  - Evidence captures all incomplete metrics
  - OutcomeReason explains why INCONCLUSIVE

**Checkpoint**: User Story 3 complete ✓
- INCONCLUSIVE outcomes returned appropriately
- Partial metrics handled with clear policy
- Evidence includes incomplete metric context
- False FAIL outcomes eliminated for partial data

---

## Phase 6: User Story 4 - Guarantee Profile Resolution Determinism (Priority: P2)

**Goal**: Profile resolution produces identical results regardless of input order or runtime context, enabling auditable, repeatable evaluations.

**Independent Test**: Identical profile + identical overrides in different orders always produce byte-for-byte identical resolved profiles.

**Story Dependencies**: Independent (depends on foundational only); can parallelize with User Stories 2-3

### Implementation for User Story 4

#### State Machine & Lifecycle

- [ ] T037 [P] [US4] Create ProfileState enum in src/PerformanceEngine.Profile.Domain/Domain/ProfileState.cs with values: Unresolved=1, Resolved=2, Invalid=3
- [ ] T038 [P] [US4] Extend Profile aggregate in src/PerformanceEngine.Profile.Domain/Domain/Profile/Profile.cs to: include State property, gate ApplyOverride() to Unresolved only, gate Get() to Resolved only, add state transition logic

#### Deterministic Resolution Algorithm

- [ ] T039 [US4] Create ProfileResolver service in src/PerformanceEngine.Profile.Domain/Application/Profile/ProfileResolver.cs implementing:
  - Pure function: Resolve(profile, overrides) → IReadOnlyDictionary<string, object>
  - Sort overrides by (scope priority DESC: global=1, api=2, endpoint=3) then (key ASC) for deterministic order
  - Apply overrides in sorted order
  - Return immutable sorted dictionary
  - Guarantee: same input set in any order → identical output (byte-identical JSON)
  - Time complexity: O(n log n) sort (acceptable for < 100 profiles)

#### Tests for User Story 4

- [ ] T040 [P] [US4] Unit tests for ProfileState enum and state transitions in tests/PerformanceEngine.Profile.Domain.Tests/Domain/ProfileStateTests.cs
- [ ] T041 [P] [US4] Unit tests for Profile state gating in tests/PerformanceEngine.Profile.Domain.Tests/Domain/Profile/ProfileStateGatingTests.cs: ApplyOverride throws if not Unresolved, Get throws if not Resolved
- [ ] T042 [US4] Unit tests for ProfileResolver sorting algorithm in tests/PerformanceEngine.Profile.Domain.Tests/Application/Profile/ProfileResolverSortingTests.cs: scope priority order, key alphabetical order, sorting correct
- [ ] T043 [US4] Determinism verification tests in tests/PerformanceEngine.Profile.Domain.Tests/Determinism/ProfileDeterminismTests.cs:
  - Order independence: {A,B,C}, {C,A,B}, {B,C,A} → identical resolved profiles
  - Byte-identical JSON serialization across 1000+ iterations
  - Scope priority consistently applied
  - No runtime context dependency (CPU timing, thread scheduling, GC)

**Checkpoint**: User Story 4 complete ✓
- Profile resolution deterministic
- Input order independence verified
- Byte-identical results guaranteed
- Audit trail reliable

---

## Phase 7: User Story 5 - Prevent Invalid Profile Use Through Validation Gates (Priority: P2)

**Goal**: Profiles are validated before use, preventing invalid configurations from corrupting evaluations.

**Independent Test**: Invalid profiles block evaluation with clear error messages. Valid profiles allow evaluation.

**Story Dependencies**: Depends on User Story 4 (profile resolution); otherwise independent

### Implementation for User Story 5

#### Validation Framework

- [ ] T044 [P] [US5] Create ValidationError value object in src/PerformanceEngine.Profile.Domain/Domain/Validation/ValidationError.cs with properties: ErrorCode (string), Message (string), FieldName (string)
- [ ] T045 [P] [US5] Create ValidationResult value object in src/PerformanceEngine.Profile.Domain/Domain/Validation/ValidationResult.cs with properties: IsValid (bool), Errors (IReadOnlyList<ValidationError>)

#### Validation Port & Implementation

- [ ] T046 [P] [US5] Create IProfileValidator port in src/PerformanceEngine.Profile.Domain/Ports/IProfileValidator.cs: ValidationResult Validate(Profile)
- [ ] T047 [US5] Implement ProfileValidator in src/PerformanceEngine.Profile.Domain/Application/Validation/ProfileValidator.cs checking:
  - No circular override dependencies
  - All required keys present
  - Type correctness for values
  - Scope validity (global/api/endpoint only)
  - Range constraints per override definition
  - Returns all errors at once (non-early-exit) for complete feedback

#### Application Layer Integration

- [ ] T048 [US5] Update EvaluationService or EnrichmentOrchestrator in src/PerformanceEngine.Evaluation.Domain/Application/ to:
  - Call validator.Validate(profile) before evaluation
  - Block evaluation if validation fails
  - Return error result with ValidationError details
  - Include validation result in audit trail

#### Tests for User Story 5

- [ ] T049 [P] [US5] Unit tests for ValidationError in tests/PerformanceEngine.Profile.Domain.Tests/Domain/Validation/ValidationErrorTests.cs
- [ ] T050 [P] [US5] Unit tests for ValidationResult in tests/PerformanceEngine.Profile.Domain.Tests/Domain/Validation/ValidationResultTests.cs
- [ ] T051 [P] [US5] Unit tests for IProfileValidator port in tests/PerformanceEngine.Profile.Domain.Tests/Ports/IProfileValidatorTests.cs
- [ ] T052 [US5] Unit tests for ProfileValidator implementation in tests/PerformanceEngine.Profile.Domain.Tests/Application/Validation/ProfileValidatorTests.cs covering:
  - Circular dependency detection
  - Required keys validation
  - Type correctness checks
  - Scope validation
  - Range constraint validation
  - All errors collected (non-early-exit)
- [ ] T053 [US5] Integration tests for validation gates in tests/PerformanceEngine.Evaluation.Domain.Tests/Integration/ProfileValidationGatesTests.cs:
  - Invalid profile blocks evaluation (returns error)
  - Valid profile allows evaluation
  - Validation errors appear in audit trail
  - Determinism unaffected by validation

**Checkpoint**: User Story 5 complete ✓
- Profile validation gates operational
- Invalid profiles blocked with clear errors
- Evaluation determinism preserved
- Audit trail includes validation results

---

## Phase 8: Polish & Cross-Cutting Concerns

**Purpose**: Finalize enrichment implementation with backward compatibility, documentation, and integration verification

- [ ] T054 Backward compatibility verification in tests/PerformanceEngine.*.Domain.Tests/Compatibility/: existing code patterns still work, old implementations unmodified, PASS/FAIL behavior unchanged for valid metrics
- [ ] T055 [P] Update IMPLEMENTATION_GUIDE.md files in src/PerformanceEngine.Metrics.Domain/, src/PerformanceEngine.Evaluation.Domain/, src/PerformanceEngine.Profile.Domain/ with new enrichment details, code examples, integration points
- [ ] T056 [P] Update README.md files in each domain with new capabilities (CompletessStatus, EvaluationEvidence, ProfileState, etc.)
- [ ] T057 Integration test suite in tests/Integration/EnrichmentIntegrationTests.cs: end-to-end evaluation with all enrichments active, metrics → evaluation with evidence → profile resolution → validation
- [ ] T058 [P] Contract tests for all endpoints in tests/Contract/ verifying API changes for metrics, evaluation, profile endpoints
- [ ] T059 Performance regression tests in tests/Performance/ verifying no degradation: metric evidence < 1% overhead, evaluation evidence < 2% overhead, profile resolution < 10ms for 100-entry profiles
- [ ] T060 Documentation update: specs/001-core-domain-enrichment/IMPLEMENTATION_COMPLETE.md marking phase completion, lessons learned, deployment readiness

**Checkpoint**: All enrichments complete, tested, documented ✓
- Backward compatibility verified
- Zero performance degradation
- Documentation updated
- Ready for production deployment

---

## Task Dependencies & Parallelization

### Critical Path (Sequential)

```
Phase 1 (Setup)
  ↓
Phase 2 (Foundation)
  ↓
Phase 3 (US1: Metrics)
  ↓
Phase 4 (US2: Evaluation Evidence)
  ↓
Phase 5 (US3: INCONCLUSIVE)
  ↓
Phase 6 (US4: Profile Determinism) — can parallelize with US2-3
  ↓
Phase 7 (US5: Validation Gates)
  ↓
Phase 8 (Polish & Integration)
```

### Parallelization Opportunities

**Within Phase 1**:
- T001, T002, T003 can run in parallel (independent setup tasks)

**Within Phase 2**:
- T004, T005, T006 (ValueObject base classes) can parallelize across domains
- T007, T008, T009 (Determinism test utilities) can parallelize
- T010 depends on T004-T009

**Within Phase 3 (US1)**:
- T011, T012 (models) can parallelize
- T013, T014 (ports, aggregates) sequentially depend
- T015 (adapter updates) depends on T011-T014
- T016-T019 (tests) can parallelize

**Within Phase 4 (US2)**:
- T020, T021, T022 (models, enums, value objects) can parallelize
- T023, T024 (aggregates, services) sequentially depend
- T025-T030 (tests) can parallelize after T023-T024

**Within Phase 6 (US4)** — Parallelize with Phase 4-5:
- T037, T038 (state machine) sequential
- T039 (resolver) depends on T038
- T040-T043 (tests) can parallelize after T037-T039

**Example Parallel Execution** (after Phase 2):

```
User Story 1 (Metrics):     T011→T012 [P] → T013→T014→T015 (7 days)
    Parallelize with ↓
User Story 2 (Evidence):    T020→T021→T022 [P] → T023→T024 (8 days)
    Parallelize with ↓
User Story 4 (Determinism): T037→T038→T039 (5 days)
```

All three stories can be worked simultaneously → Phase 6 faster

---

## Testing Strategy

### Unit Tests (Per Domain)

- **CompletessStatus**: Enum values, serialization
- **MetricEvidence**: Invariants, IsComplete computation, value equality
- **Metric.Create()**: Factory logic, completeness determination
- **EvaluationEvidence**: Construction, immutability, serialization
- **ProfileResolver**: Sorting algorithm, order independence, immutability
- **ValidationError/Result**: Construction, collection, serialization

### Determinism Tests (1000+ Iterations)

- **Metric Evidence**: Identical input → identical evidence 1000+ times
- **Evaluation Results**: Identical inputs → identical outcome + evidence 1000+ times
- **Profile Resolution**: Same overrides in 100+ different orders → byte-identical output
- **JSON Serialization**: No randomization in JSON output; reproducible hashing

### Integration Tests (End-to-End)

- Metric completeness → Evaluation evidence → Profile validation → Result
- Partial metrics + policy → INCONCLUSIVE outcome
- Invalid profiles blocked before evaluation
- All enrichments active simultaneously

### Contract Tests (API Level)

- Metrics endpoint returns CompletessStatus + Evidence
- Evaluation endpoint returns EvaluationResult with Evidence + INCONCLUSIVE support
- Profile endpoint supports state transitions + validation gates

### Backward Compatibility Tests

- Existing metric implementations work unchanged
- PASS/FAIL behavior preserved for valid metrics
- Old code not accessing Evidence still works
- Partial metrics default to safe behavior

---

## Acceptance Criteria Summary

| User Story | Acceptance Criterion | Verification Task |
|---|---|---|
| US1 | Metrics expose completeness + evidence | T019 (Contract tests) |
| US2 | Evaluation includes evidence trail | T029 (Integration tests) |
| US3 | INCONCLUSIVE returned for incomplete data | T036 (Integration tests) |
| US4 | Profile resolution deterministic | T043 (Determinism tests, 1000+ iterations) |
| US5 | Invalid profiles blocked | T053 (Integration tests) |
| General | Zero performance degradation | T059 (Performance tests) |
| General | Backward compatible | T054 (Compatibility tests) |

---

## Implementation Notes

### Technology Stack Alignment

- **Language**: C# with .NET 10
- **Architecture**: Clean Architecture + DDD (domain purity, dependency inversion)
- **Patterns**: Value Objects (immutability), State Machine (Profile lifecycle), Factory Methods (complex construction), Port/Adapter (validation, resolution strategies)
- **Testing**: xUnit with 1000+ iteration determinism verification
- **Data Immutability**: C# records (init accessors), readonly properties

### Key Files Created/Modified

**Metrics Domain**:
- NEW: `src/PerformanceEngine.Metrics.Domain/Domain/CompletessStatus.cs`
- NEW: `src/PerformanceEngine.Metrics.Domain/Domain/Metrics/MetricEvidence.cs`
- MODIFIED: `src/PerformanceEngine.Metrics.Domain/Domain/Ports/IMetric.cs` (extend)
- MODIFIED: `src/PerformanceEngine.Metrics.Domain/Domain/Metrics/Metric.cs` (extend)

**Evaluation Domain**:
- MODIFIED: `src/PerformanceEngine.Evaluation.Domain/Domain/Outcome.cs` (add INCONCLUSIVE)
- NEW: `src/PerformanceEngine.Evaluation.Domain/Domain/Evaluation/MetricReference.cs`
- NEW: `src/PerformanceEngine.Evaluation.Domain/Domain/Evaluation/EvaluationEvidence.cs`
- MODIFIED: `src/PerformanceEngine.Evaluation.Domain/Domain/Evaluation/EvaluationResult.cs` (extend)
- MODIFIED: `src/PerformanceEngine.Evaluation.Domain/Application/Evaluation/Evaluator.cs` (update for evidence + determinism)
- NEW: `src/PerformanceEngine.Evaluation.Domain/Ports/IPartialMetricPolicy.cs`
- NEW: `src/PerformanceEngine.Evaluation.Domain/Application/Evaluation/PartialMetricPolicy.cs`

**Profile Domain**:
- NEW: `src/PerformanceEngine.Profile.Domain/Domain/ProfileState.cs`
- MODIFIED: `src/PerformanceEngine.Profile.Domain/Domain/Profile/Profile.cs` (add state + gating)
- NEW: `src/PerformanceEngine.Profile.Domain/Application/Profile/ProfileResolver.cs`
- NEW: `src/PerformanceEngine.Profile.Domain/Domain/Validation/ValidationError.cs`
- NEW: `src/PerformanceEngine.Profile.Domain/Domain/Validation/ValidationResult.cs`
- NEW: `src/PerformanceEngine.Profile.Domain/Ports/IProfileValidator.cs`
- NEW: `src/PerformanceEngine.Profile.Domain/Application/Validation/ProfileValidator.cs`

**Test Infrastructure**:
- NEW: Determinism test utilities in `tests/PerformanceEngine.*.Domain.Tests/Determinism/`
- NEW: Comprehensive test suites for all new entities/services
- NEW: Integration test suites for end-to-end enrichment validation
- NEW: Contract tests for API-level changes

---

## Success Metrics

### Completion Criteria

- ✅ All 60 tasks completed with passing tests
- ✅ Determinism verified: 1000+ iteration tests pass for Metrics, Evaluation, Profile
- ✅ Backward compatibility verified: existing code unchanged, old behavior preserved
- ✅ Zero performance degradation: enrichments add < 1% overhead to metric production
- ✅ All user stories independently testable and deliverable
- ✅ Documentation updated with new capabilities and implementation guidance

### Task Count Summary

| Phase | Tasks | Parallelizable |
|-------|-------|---|
| Phase 1: Setup | T001-T003 (3 tasks) | 3 in parallel |
| Phase 2: Foundation | T004-T010 (7 tasks) | Partial (T004-T009 parallel, T010 sequential) |
| Phase 3: US1 (Metrics) | T011-T019 (9 tasks) | Partial (models parallel, dependencies sequential) |
| Phase 4: US2 (Evidence) | T020-T030 (11 tasks) | Partial (models parallel, tests parallel) |
| Phase 5: US3 (INCONCLUSIVE) | T031-T036 (6 tasks) | Partial (depends on US1-US2) |
| Phase 6: US4 (Profile Determinism) | T037-T043 (7 tasks) | Can parallelize with US2-US3 |
| Phase 7: US5 (Validation) | T044-T053 (10 tasks) | Partial (errors/results parallel, tests parallel) |
| Phase 8: Polish | T054-T060 (7 tasks) | Mostly parallel after earlier phases |
| **TOTAL** | **60 tasks** | **~20 sequential, ~40 parallelizable** |

### Estimated Timeline

- **Sequential path** (all tasks serialized): ~40-50 developer-days
- **Optimized parallel** (3 stories simultaneously): ~15-20 developer-days
- **Recommended MVP scope** (Phase 1 + 2 + 3 + 4): ~10-12 developer-days

