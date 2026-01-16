# Implementation Tasks: Persist Results for Audit & Replay

**Feature**: 001-persist-results | **Date**: 2026-01-16 | **Status**: Ready for Implementation

**Reference Documents**:
- Specification: [spec.md](spec.md)
- Implementation Plan: [plan.md](plan.md)
- Data Model: [data-model.md](data-model.md)
- Repository Port: [contracts/IEvaluationResultRepository.cs](contracts/IEvaluationResultRepository.cs)
- Quickstart: [quickstart.md](quickstart.md)

---

## Overview

This document breaks down the specification into independently executable, verifiable tasks organized by user story priority. Each task is specific enough that a developer can complete it without additional context beyond these documents.

**Total Task Count**: 45 tasks  
**Phases**: Setup (3) + Foundation (5) + US1-P1 (12) + US2-P2 (12) + US3-P3 (10) + Polish (3)

---

## Phase 1: Setup (Project Infrastructure)

Setup tasks initialize project structure and establish development environment.

- [ ] T001 Create `PerformanceEngine.Evaluation.Infrastructure` project structure in `src/`
- [ ] T002 Create `PerformanceEngine.Evaluation.Infrastructure.Tests` test project in `tests/`
- [ ] T003 Configure project files, references, and build settings per existing project patterns

---

## Phase 2: Foundational Tasks (Blocking Prerequisites)

All user stories depend on these foundational entities and ports being completed first.

- [ ] T004 [P] Implement `Severity` enum in `src/PerformanceEngine.Evaluation.Domain/Domain/Severity.cs`
- [ ] T005 [P] Implement `MetricReference` immutable value object in `src/PerformanceEngine.Evaluation.Domain/Domain/MetricReference.cs`
- [ ] T006 [P] Implement `Violation` immutable value object in `src/PerformanceEngine.Evaluation.Domain/Domain/Violation.cs`
- [ ] T007 [P] Implement `EvaluationEvidence` immutable value object in `src/PerformanceEngine.Evaluation.Domain/Domain/EvaluationEvidence.cs`
- [ ] T008 Implement `EvaluationResult` aggregate root in `src/PerformanceEngine.Evaluation.Domain/Domain/EvaluationResult.cs` with factory validation

---

## Phase 3: User Story 1 - Persist Evaluation Results (P1) ⭐ MVP

**Goal**: Enable atomic persistence of immutable evaluation results with complete context (metrics, violations, evidence).

**Independent Test Criteria**:
- Can create an evaluation result with Pass outcome and no violations
- Can create an evaluation result with Fail outcome and multiple violations
- Can persist result atomically through repository abstraction
- Can retrieve persisted result identically (byte-for-byte equal)
- Concurrent persist operations succeed without data corruption

### Contracts & Port Definition

- [ ] T009 [P] Implement `IEvaluationResultRepository` port interface in `src/PerformanceEngine.Evaluation.Domain/Ports/IEvaluationResultRepository.cs`
- [ ] T010 [P] Create repository DTOs in `src/PerformanceEngine.Evaluation.Domain/Dtos/`: `EvaluationResultDto`, `ViolationDto`, `EvaluationEvidenceDto`, `MetricReferenceDto`

### Infrastructure Implementation (In-Memory)

- [ ] T011 [P] Implement `InMemoryEvaluationResultRepository` in `src/PerformanceEngine.Evaluation.Infrastructure/Persistence/InMemoryEvaluationResultRepository.cs` with atomic TryAdd semantics
- [ ] T012 Implement `ServiceCollectionExtensions` in `src/PerformanceEngine.Evaluation.Infrastructure/ServiceCollectionExtensions.cs` to register in-memory repository

### Unit Tests: Immutability & Validation

- [ ] T013 [P] Create immutability tests in `tests/PerformanceEngine.Evaluation.Domain.Tests/PersistenceScenarios/ImmutabilityTests.cs`
- [ ] T014 [P] Create validation tests in `tests/PerformanceEngine.Evaluation.Domain.Tests/PersistenceScenarios/ValidationTests.cs`
- [ ] T015 [P] Create deterministic serialization tests in `tests/PerformanceEngine.Evaluation.Domain.Tests/PersistenceScenarios/DeterministicSerializationTests.cs`

### Integration Tests: Atomic Persistence

- [ ] T016 [P] Create atomic persistence tests in `tests/PerformanceEngine.Evaluation.Infrastructure.Tests/AtomicPersistenceTests.cs` (FR-002)
- [ ] T017 [P] Create duplicate ID prevention tests (FR-006, SC-001)
- [ ] T018 Create concurrent persistence tests in `tests/PerformanceEngine.Evaluation.Infrastructure.Tests/ConcurrencyTests.cs` (SC-007: 100 concurrent ops without corruption)

### Integration Tests: Persistence Scenarios

- [ ] T019 [US1] Create acceptance test: Pass outcome with no violations persists and retrieves identically in `tests/PerformanceEngine.Evaluation.Infrastructure.Tests/US1_AcceptanceTests.cs`
- [ ] T020 [US1] Create acceptance test: Fail outcome with multiple violations persists atomically with all context intact
- [ ] T021 [US1] Create acceptance test: Concurrent persist operations from multiple threads succeed without race conditions

### Verification & Documentation

- [ ] T022 [US1] Run all US1 tests and verify green pipeline
- [ ] T023 [US1] Update `quickstart.md` with persistence examples (if needed)

---

## Phase 4: User Story 2 - Query Historical Results (P2)

**Goal**: Enable retrieval of persisted results by various criteria to support audit trails.

**Independent Test Criteria**:
- Can query results by timestamp range
- Results returned in chronological order
- Can query results by unique ID
- Empty query results handled gracefully (not as errors)
- Query operations never throw on missing data

### Port Enhancement

- [ ] T024 [P] Verify `IEvaluationResultRepository` has all query methods (GetByIdAsync, QueryByTimestampRangeAsync, QueryByTestIdAsync)

### Infrastructure Implementation (In-Memory Query)

- [ ] T025 [P] Implement `GetByIdAsync` in `InMemoryEvaluationResultRepository` - return null if not found
- [ ] T026 [P] Implement `QueryByTimestampRangeAsync` in `InMemoryEvaluationResultRepository` - return chronological order
- [ ] T027 [P] Implement `QueryByTestIdAsync` in `InMemoryEvaluationResultRepository` - filter by test identifier

### Unit Tests: Query Operations

- [ ] T028 [P] Create query by ID tests in `tests/PerformanceEngine.Evaluation.Infrastructure.Tests/QueryTests.cs` (FR-007)
- [ ] T029 [P] Create query by timestamp range tests (FR-008, chronological ordering)
- [ ] T030 [P] Create query by test ID tests

### Unit Tests: Empty Result Handling

- [ ] T031 [P] Create empty result handling tests - verify null/empty returned, not exceptions (FR-012)
- [ ] T032 [P] Create edge case tests - invalid query ranges, missing data

### Integration Tests: Query Acceptance Scenarios

- [ ] T033 [US2] Create acceptance test: Multiple results persisted, queried by timestamp range, returned chronologically in `tests/PerformanceEngine.Evaluation.Infrastructure.Tests/US2_AcceptanceTests.cs`
- [ ] T034 [US2] Create acceptance test: Query by specific ID returns exact immutable result
- [ ] T035 [US2] Create acceptance test: Query with no matches returns empty (not error)

### Verification & Documentation

- [ ] T036 [US2] Run all US2 tests and verify green pipeline
- [ ] T037 [US2] Update query examples in `quickstart.md` (if needed)

---

## Phase 5: User Story 3 - Replay Evaluation with Same Inputs (P3)

**Goal**: Support retrieving persisted metrics and evidence for deterministic replay to verify evaluation logic.

**Independent Test Criteria**:
- Can retrieve persisted metrics from evidence
- Can re-evaluate using persisted metrics + same rules
- Replay produces byte-identical results
- Evidence completeness validation works (fail fast if incomplete)
- Metric values preserved exactly (no floating-point loss)

### Repository Enhancement (Evidence Retrieval)

- [ ] T038 [P] Verify `IEvaluationResultRepository` provides complete evidence for replay (FR-010)

### Application Layer: Replay Service

- [ ] T039 Create `IEvaluationReplayService` interface in `src/PerformanceEngine.Application/Services/IEvaluationReplayService.cs`
- [ ] T040 Implement `EvaluationReplayService` in `src/PerformanceEngine.Application/Services/EvaluationReplayService.cs` to orchestrate replay scenarios

### Unit Tests: Deterministic Replay

- [ ] T041 [P] Create deterministic replay tests in `tests/PerformanceEngine.Evaluation.Domain.Tests/PersistenceScenarios/DeterministicReplayTests.cs` (SC-003)
- [ ] T042 [P] Create evidence completeness validation tests (FR-010)
- [ ] T043 [P] Create metric value precision tests (string preservation verification)

### Integration Tests: Replay Scenarios

- [ ] T044 [US3] Create acceptance test: Persisted result with evidence replayed produces byte-identical outcome in `tests/PerformanceEngine.Application.Tests/US3_ReplayAcceptanceTests.cs`
- [ ] T045 [US3] Create acceptance test: Incomplete evidence fails fast with clear error message

---

## Phase 6: Polish & Cross-Cutting Concerns

Final polish, documentation, and CI/CD validation.

- [ ] T046 Create comprehensive integration test suite that validates all 12 functional requirements
- [ ] T047 Run full test suite and verify green CI pipeline (all phases)
- [ ] T048 Create IMPLEMENTATION_COMPLETE.md documenting all completed tasks, test coverage, and validation results

---

## Task Dependency Graph

```
Phase 1: Setup
    ↓
Phase 2: Foundation (T004-T008)
    ├─ Severity, MetricReference, Violation, Evidence, Result
    ↓
Phase 3: US1 - Persist (T009-T023) [depends on Phase 2]
    ├─ Port definition (T009-T010)
    ├─ In-memory repository (T011-T012)
    ├─ Unit tests (T013-T015)
    ├─ Integration tests (T016-T021)
    ├─ Verification (T022-T023)
    ↓
Phase 4: US2 - Query (T024-T037) [depends on Phase 3]
    ├─ Query implementation (T025-T027)
    ├─ Query tests (T028-T032)
    ├─ Acceptance scenarios (T033-T035)
    ├─ Verification (T036-T037)
    ↓
Phase 5: US3 - Replay (T038-T045) [depends on Phase 3 & 4]
    ├─ Replay service (T039-T040)
    ├─ Replay tests (T041-T043)
    ├─ Acceptance scenarios (T044-T045)
    ↓
Phase 6: Polish (T046-T048)
    └─ Full validation, documentation
```

## Parallel Execution Opportunities

### Within US1 (Persist):
- [P] T004-T007 (Severity, MetricReference, Violation, Evidence) can run in parallel
- [P] T013-T015 (unit tests) can run in parallel
- [P] T016-T017 (integration tests) can run in parallel

### Within US2 (Query):
- [P] T028-T032 (query tests) can run in parallel
- [P] T025-T027 (repository query methods) can run in parallel

### Within US3 (Replay):
- [P] T041-T043 (replay tests) can run in parallel

### Cross-Story Sequential:
US1 → US2 → US3 (hard dependencies prevent full parallelization)

---

## Task Execution Strategy

### MVP Scope (Minimum Viable Product)
Complete **Phase 1 + Phase 2 + Phase 3** to deliver core persistence capability:
- Atomic persistence of evaluation results
- Immutable storage with complete context
- Unit tests validating immutability and serialization
- Integration tests validating atomic persistence and concurrency

**Estimated Effort**: 15-20 developer hours

### Full Scope
Complete **all phases** including query and replay:
- Phase 3: Core persistence (MVP)
- Phase 4: Query historical results for audit trails
- Phase 5: Replay evaluation for debugging and verification
- Phase 6: Polish and full validation

**Estimated Effort**: 25-30 developer hours

---

## Success Criteria Mapping

Each task is traced to specification requirements:

| Requirement | Tasks | Status |
|-------------|-------|--------|
| FR-001: Repository abstraction | T009, T024 | Design artifact |
| FR-002: Atomic persistence | T011, T016, T018, T021 | Implementation |
| FR-003: Append-only semantics | T009 (port omits Update/Delete) | Design artifact |
| FR-004: Immutable entities | T004-T008, T013 | Implementation |
| FR-005: Complete context | T008, T019, T020 | Implementation |
| FR-006: Unique identifiers | T011, T017 | Implementation |
| FR-007: Query by ID | T025, T028, T034 | Implementation |
| FR-008: Query by timestamp | T026, T029, T033 | Implementation |
| FR-009: Consistency boundary | T008, T019 | Implementation |
| FR-010: Metric preservation | T005, T038, T042 | Implementation |
| FR-011: Clear error messages | Port contracts + tests | Implementation |
| FR-012: Empty results graceful | T031, T035 | Implementation |

---

## Test Coverage Summary

### Unit Tests
- Immutability enforcement (T013)
- Validation rules (T014)
- Deterministic serialization (T015, T041)
- Evidence completeness (T042)

### Integration Tests
- Atomic persistence (T016)
- Concurrent operations (T018)
- Query operations (T028-T030)
- Empty result handling (T031-T032)
- Replay scenarios (T044)

### Acceptance Tests
- US1 scenarios (T019-T021)
- US2 scenarios (T033-T035)
- US3 scenarios (T044-T045)

**Total Test Count**: ~60 test cases across all phases

---

## Quality Gates

Each phase must satisfy:

1. **Compilation**: No warnings, TreatWarningsAsErrors enabled
2. **Tests**: All tests passing (green pipeline)
3. **Code Review**: Architecture alignment confirmed
4. **Documentation**: Task-level documentation complete
5. **Audit Trail**: Commit references and CI validation recorded

---

## Task Checklist Template

Use this template for each completed task:

```
- [x] T###: [Description]
  - Commits: [commit refs]
  - CI Run: [CI link]
  - Tests: [test count] passing
  - Code Review: [reviewer] ✓
  - Blockers: None
```

---

## Next: Phase 2 Execution

Ready to begin Phase 1 (Setup) with T001. Each task is independent and verifiable against clear acceptance criteria from the specification.

**Status**: ✅ Tasks generated | **Ready for**: Phase 1 execution | **Date**: 2026-01-16
