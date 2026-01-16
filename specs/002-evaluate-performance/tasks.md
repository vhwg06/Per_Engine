# Tasks: Evaluate Performance Orchestration

**Input**: Design documents from `/specs/002-evaluate-performance/`  
**Prerequisites**: plan.md ✅, spec.md ✅  
**Feature Branch**: `002-evaluate-performance`  
**Created**: January 15, 2026

**Tests**: Tests are NOT explicitly requested in the feature specification, so test tasks are minimal (only determinism validation tests which are critical to specification requirements).

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (US1, US2, US3, US4)
- Include exact file paths in descriptions

---

## Phase 1: Setup (Project Initialization)

**Purpose**: Create application project structure for orchestration layer

- [ ] T001 Create PerformanceEngine.Application project in src/PerformanceEngine.Application/
- [ ] T002 Configure project dependencies (reference existing domain projects) in src/PerformanceEngine.Application/PerformanceEngine.Application.csproj
- [ ] T003 [P] Enable nullable reference types and C# 13 features in src/PerformanceEngine.Application/PerformanceEngine.Application.csproj
- [ ] T004 [P] Create directory structure: Ports/, Orchestration/, Models/, Services/ in src/PerformanceEngine.Application/
- [ ] T005 [P] Create test project PerformanceEngine.Application.Tests in tests/PerformanceEngine.Application.Tests/
- [ ] T006 Configure test project dependencies (xUnit, FluentAssertions, application project reference) in tests/PerformanceEngine.Application.Tests/PerformanceEngine.Application.Tests.csproj

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core domain ports and models that ALL user stories depend on

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

**Architecture Conformance**: 
- Domain ports follow Clean Architecture (Application → Domain Ports inward)
- All models immutable (sealed records, no setters)
- No infrastructure dependencies

### Domain Port Abstractions

- [ ] T007 [P] Define IMetricsProvider port in src/PerformanceEngine.Application/Ports/IMetricsProvider.cs
- [ ] T008 [P] Define IProfileResolver port in src/PerformanceEngine.Application/Ports/IProfileResolver.cs
- [ ] T009 [P] Define IEvaluationRulesProvider port in src/PerformanceEngine.Application/Ports/IEvaluationRulesProvider.cs

### Core Application Models

- [ ] T010 [P] Create Outcome enum (PASS, WARN, FAIL, INCONCLUSIVE) in src/PerformanceEngine.Application/Models/Outcome.cs
- [ ] T011 [P] Create SeverityLevel enum (Critical, NonCritical) in src/PerformanceEngine.Application/Models/SeverityLevel.cs
- [ ] T012 [P] Create ExecutionContext value object in src/PerformanceEngine.Application/Models/ExecutionContext.cs
- [ ] T013 Create ExecutionMetadata immutable record in src/PerformanceEngine.Application/Models/ExecutionMetadata.cs
- [ ] T014 Create Violation immutable record in src/PerformanceEngine.Application/Models/Violation.cs
- [ ] T015 Create CompletenessReport immutable record in src/PerformanceEngine.Application/Models/CompletenessReport.cs
- [ ] T016 Create EvaluationResult immutable record in src/PerformanceEngine.Application/Models/EvaluationResult.cs (depends on T010, T013, T014, T015)

**Checkpoint**: Foundation ready - user story implementation can now begin in parallel

---

## Phase 3: User Story 1 - Determine if Performance Meets Requirements (Priority: P1) 🎯 MVP

**Goal**: Orchestrate end-to-end evaluation flow that takes metrics, profile, and rules, then produces deterministic EvaluationResult with outcome (PASS/WARN/FAIL/INCONCLUSIVE)

**Independent Test**: Execute orchestration with known inputs, verify correct outcome and byte-identical results on re-execution

### Implementation for User Story 1

- [ ] T017 [P] [US1] Create EvaluatePerformanceUseCase entry point in src/PerformanceEngine.Application/Orchestration/EvaluatePerformanceUseCase.cs
- [ ] T018 [P] [US1] Implement input validation logic (profile exists, rules not empty) in src/PerformanceEngine.Application/Orchestration/EvaluatePerformanceUseCase.cs
- [ ] T019 [P] [US1] Implement profile resolution orchestration in src/PerformanceEngine.Application/Orchestration/EvaluatePerformanceUseCase.cs
- [ ] T020 [P] [US1] Implement RuleEvaluationCoordinator for deterministic rule ordering in src/PerformanceEngine.Application/Orchestration/RuleEvaluationCoordinator.cs
- [ ] T021 [US1] Implement deterministic rule sorting (by rule ID, ASCII order) in src/PerformanceEngine.Application/Orchestration/RuleEvaluationCoordinator.cs
- [ ] T022 [US1] Implement rule evaluation loop with domain delegation in src/PerformanceEngine.Application/Orchestration/RuleEvaluationCoordinator.cs (depends on T020, T021)
- [ ] T023 [US1] Implement violation collection and sorting in src/PerformanceEngine.Application/Orchestration/RuleEvaluationCoordinator.cs (depends on T022)
- [ ] T024 [P] [US1] Implement OutcomeAggregator for outcome determination in src/PerformanceEngine.Application/Orchestration/OutcomeAggregator.cs
- [ ] T025 [US1] Implement outcome precedence rules (FAIL > WARN > INCONCLUSIVE > PASS) in src/PerformanceEngine.Application/Orchestration/OutcomeAggregator.cs (depends on T024)
- [ ] T026 [P] [US1] Implement ResultConstructor for building immutable results in src/PerformanceEngine.Application/Orchestration/ResultConstructor.cs
- [ ] T027 [US1] Integrate all orchestration steps in EvaluatePerformanceUseCase.Execute() method in src/PerformanceEngine.Application/Orchestration/EvaluatePerformanceUseCase.cs (depends on T017-T026)
- [ ] T028 [US1] Implement ExecutionMetadata population (profile applied, thresholds used) in src/PerformanceEngine.Application/Orchestration/ResultConstructor.cs (depends on T026)

### Determinism Tests for User Story 1 (Critical to Spec)

- [ ] T029 [US1] Create idempotency test: same inputs produce byte-identical results in tests/PerformanceEngine.Application.Tests/Integration/DeterminismTests.cs
- [ ] T030 [US1] Create deterministic ordering test: rules always evaluated in same order in tests/PerformanceEngine.Application.Tests/Integration/DeterminismTests.cs

---

## Phase 4: User Story 2 - Understand Evaluation Completeness and Data Gaps (Priority: P2)

**Goal**: Provide transparency into which metrics were available vs. missing, and how this affected evaluation

**Independent Test**: Execute evaluation with partial metrics, verify CompletenessReport accurately reflects data availability

### Implementation for User Story 2

- [ ] T031 [P] [US2] Create CompletenessAssessor for metric availability analysis in src/PerformanceEngine.Application/Orchestration/CompletenessAssessor.cs
- [ ] T032 [US2] Implement metric availability detection (provided vs. expected) in src/PerformanceEngine.Application/Orchestration/CompletenessAssessor.cs (depends on T031)
- [ ] T033 [US2] Implement missing metrics identification logic in src/PerformanceEngine.Application/Orchestration/CompletenessAssessor.cs (depends on T032)
- [ ] T034 [US2] Implement unevaluated rules tracking (rules skipped due to missing metrics) in src/PerformanceEngine.Application/Orchestration/CompletenessAssessor.cs (depends on T032)
- [ ] T035 [US2] Calculate completeness percentage (provided / expected) in src/PerformanceEngine.Application/Orchestration/CompletenessAssessor.cs (depends on T032)
- [ ] T036 [US2] Implement completeness threshold check (< 50% → INCONCLUSIVE) in src/PerformanceEngine.Application/Orchestration/OutcomeAggregator.cs
- [ ] T037 [US2] Integrate CompletenessAssessor into orchestration flow in src/PerformanceEngine.Application/Orchestration/EvaluatePerformanceUseCase.cs (depends on T031-T035)
- [ ] T038 [US2] Populate CompletenessReport in EvaluationResult in src/PerformanceEngine.Application/Orchestration/ResultConstructor.cs (depends on T037)

### Partial Metrics Handling for User Story 2

- [ ] T039 [US2] Implement rule skipping logic for missing required metrics in src/PerformanceEngine.Application/Orchestration/RuleEvaluationCoordinator.cs
- [ ] T040 [US2] Add graceful degradation: evaluation continues with available metrics in src/PerformanceEngine.Application/Orchestration/EvaluatePerformanceUseCase.cs (depends on T039)

---

## Phase 5: User Story 3 - Verify Data Integrity with Deterministic Fingerprints (Priority: P2)

**Goal**: Generate deterministic, cryptographic fingerprint of actual collected metrics for data integrity verification

**Independent Test**: Modify metric value, verify fingerprint changes; restore value, verify fingerprint matches original

### Implementation for User Story 3

- [ ] T041 [P] [US3] Create DeterministicFingerprintGenerator service in src/PerformanceEngine.Application/Services/DeterministicFingerprintGenerator.cs
- [ ] T042 [US3] Implement metric sorting by name (ASCII order) for deterministic ordering in src/PerformanceEngine.Application/Services/DeterministicFingerprintGenerator.cs (depends on T041)
- [ ] T043 [US3] Implement deterministic serialization (metric1=value1|metric2=value2|...) in src/PerformanceEngine.Application/Services/DeterministicFingerprintGenerator.cs (depends on T042)
- [ ] T044 [US3] Implement SHA256 hashing with fixed seed in src/PerformanceEngine.Application/Services/DeterministicFingerprintGenerator.cs (depends on T043)
- [ ] T045 [US3] Integrate fingerprint generation into orchestration flow in src/PerformanceEngine.Application/Orchestration/EvaluatePerformanceUseCase.cs (depends on T041-T044)
- [ ] T046 [US3] Populate DataFingerprint in EvaluationResult in src/PerformanceEngine.Application/Orchestration/ResultConstructor.cs (depends on T045)

### Fingerprint Determinism Tests for User Story 3 (Critical to Spec)

- [ ] T047 [US3] Create test: same metrics produce same fingerprint in tests/PerformanceEngine.Application.Tests/Unit/DeterministicFingerprintGeneratorTests.cs
- [ ] T048 [US3] Create test: different metrics produce different fingerprints in tests/PerformanceEngine.Application.Tests/Unit/DeterministicFingerprintGeneratorTests.cs
- [ ] T049 [US3] Create test: fingerprint reflects actual data (not expected) in tests/PerformanceEngine.Application.Tests/Unit/DeterministicFingerprintGeneratorTests.cs

---

## Phase 6: User Story 4 - Trace Rule Violations and Failure Details (Priority: P2)

**Goal**: Capture detailed violation information (rule, threshold, actual value, affected metric) for failure root-cause analysis

**Independent Test**: Execute evaluation with known rule violations, verify Violations list contains complete details

### Implementation for User Story 4

- [ ] T050 [P] [US4] Implement violation capture during rule evaluation in src/PerformanceEngine.Application/Orchestration/RuleEvaluationCoordinator.cs
- [ ] T051 [US4] Populate Violation records with complete details (rule, threshold, actual, metric) in src/PerformanceEngine.Application/Orchestration/RuleEvaluationCoordinator.cs (depends on T050)
- [ ] T052 [US4] Implement rule evaluation error handling (catch, convert to violation) in src/PerformanceEngine.Application/Orchestration/RuleEvaluationCoordinator.cs (depends on T050)
- [ ] T053 [US4] Ensure all violations captured (not just first) in src/PerformanceEngine.Application/Orchestration/RuleEvaluationCoordinator.cs (depends on T051)
- [ ] T054 [US4] Sort violations by rule ID for deterministic output in src/PerformanceEngine.Application/Orchestration/RuleEvaluationCoordinator.cs (depends on T053)
- [ ] T055 [US4] Populate Violations list in EvaluationResult in src/PerformanceEngine.Application/Orchestration/ResultConstructor.cs (depends on T054)

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Error handling, validation, documentation, final integration

### Error Handling

- [ ] T056 [P] Implement fail-fast validation for missing profile in src/PerformanceEngine.Application/Orchestration/EvaluatePerformanceUseCase.cs
- [ ] T057 [P] Implement fail-fast validation for empty rules collection in src/PerformanceEngine.Application/Orchestration/EvaluatePerformanceUseCase.cs
- [ ] T058 [P] Implement explicit error messages for invalid configuration in src/PerformanceEngine.Application/Orchestration/EvaluatePerformanceUseCase.cs
- [ ] T059 Ensure no infrastructure exceptions leak (all wrapped/handled) in src/PerformanceEngine.Application/Orchestration/EvaluatePerformanceUseCase.cs (depends on T056-T058)

### Integration & Validation

- [ ] T060 Create end-to-end integration test: full orchestration flow in tests/PerformanceEngine.Application.Tests/Integration/EvaluatePerformanceUseCaseTests.cs
- [ ] T061 Create integration test: invalid profile throws before evaluation in tests/PerformanceEngine.Application.Tests/Integration/EvaluatePerformanceUseCaseTests.cs
- [ ] T062 Create integration test: partial metrics handled gracefully in tests/PerformanceEngine.Application.Tests/Integration/PartialMetricsTests.cs
- [ ] T063 Create integration test: rule evaluation error captured as violation in tests/PerformanceEngine.Application.Tests/Integration/EvaluatePerformanceUseCaseTests.cs

### Documentation

- [ ] T064 [P] Add XML documentation comments to all public APIs in src/PerformanceEngine.Application/
- [ ] T065 [P] Create README.md explaining orchestration layer purpose in src/PerformanceEngine.Application/README.md
- [ ] T066 [P] Document domain port contracts in src/PerformanceEngine.Application/Ports/README.md

---

## Dependencies & Parallel Execution

### User Story Completion Order

```
Phase 1 (Setup) → Phase 2 (Foundational)
                        ↓
    ┌───────────────────┼───────────────────┐
    ↓                   ↓                   ↓
Phase 3: US1 (P1)  Phase 4: US2 (P2)  Phase 5: US3 (P2)
    │                   │                   │
    │                   ↓                   │
    │              Phase 6: US4 (P2)        │
    │                   │                   │
    └───────────────────┼───────────────────┘
                        ↓
                   Phase 7: Polish
```

**MVP Scope**: Phase 1 + Phase 2 + Phase 3 (US1) = Core orchestration with outcome determination

**Critical Path**: T001-T016 (Setup + Foundation) → T017-T028 (US1 Core) → T029-T030 (US1 Tests)

### Parallel Execution Examples

**After Foundation Complete (T007-T016)**:
- US1 Core (T017-T019, T020-T023, T024-T025, T026-T028) can start
- US2 Completeness (T031-T035) can start in parallel
- US3 Fingerprint (T041-T044) can start in parallel
- US4 Violations (T050-T051) can start in parallel

**Within Each User Story**:
- US1: T017, T018, T019 parallel; T020, T024, T026 parallel
- US2: T031 independent of other stories
- US3: T041-T044 fully parallelizable (separate service)
- US4: T050-T051 extend existing RuleEvaluationCoordinator

---

## Implementation Strategy

### MVP First (US1 Only)
1. Complete Phase 1: Setup (T001-T006)
2. Complete Phase 2: Foundational (T007-T016)
3. Complete Phase 3: US1 (T017-T030)
4. **Checkpoint**: Working orchestration with outcome determination

### Incremental Delivery
- **Iteration 1**: US1 (P1) - Core evaluation outcome
- **Iteration 2**: US2 (P2) - Add completeness transparency
- **Iteration 3**: US3 (P2) - Add data integrity fingerprints
- **Iteration 4**: US4 (P2) - Add violation details
- **Iteration 5**: Polish - Error handling, documentation

### Independent Testing Per Story
- US1: Test outcome correctness and idempotency (T029-T030)
- US2: Test with partial metrics, verify CompletenessReport
- US3: Test fingerprint determinism and data integrity (T047-T049)
- US4: Test violation capture completeness

---

## Task Summary

| Phase | Task Range | Count | Parallelizable | Story |
|-------|------------|-------|----------------|-------|
| Setup | T001-T006 | 6 | 2 tasks | - |
| Foundational | T007-T016 | 10 | 7 tasks | - |
| US1 (P1) | T017-T030 | 14 | 5 tasks | US1 |
| US2 (P2) | T031-T040 | 10 | 1 task | US2 |
| US3 (P2) | T041-T049 | 9 | 1 task | US3 |
| US4 (P2) | T050-T055 | 6 | 1 task | US4 |
| Polish | T056-T066 | 11 | 7 tasks | - |
| **Total** | **T001-T066** | **66** | **24** | **4** |

**Estimated Effort**:
- Setup + Foundation: 1 day
- US1 (MVP): 2-3 days
- US2-US4: 1-2 days each
- Polish: 1 day
- **Total**: 7-10 days for complete implementation

---

## Validation Checklist

Before marking complete:
- [ ] All 4 user stories independently testable
- [ ] Idempotency verified (same input → byte-identical output)
- [ ] Determinism verified (rule ordering, fingerprints, violations)
- [ ] Immutability enforced (sealed records, no setters)
- [ ] No infrastructure dependencies in Application layer
- [ ] Clean Architecture: dependencies point inward only
- [ ] All functional requirements (FR-001 through FR-012) implemented
- [ ] All success criteria (SC-001 through SC-008) met

---

**Tasks Status**: ✅ READY FOR IMPLEMENTATION  
**MVP Scope**: Phase 1 + Phase 2 + Phase 3 (US1)  
**Full Feature**: All 7 phases
