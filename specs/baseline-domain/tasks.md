# Tasks: Baseline Domain Implementation

**Input**: Design documents from `/specs/baseline-domain/`  
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/domain-contracts.md  
**Branch**: `baseline-domain-implementation`  
**Estimated Duration**: 4 weeks (320 person-hours)  

---

## Format: `[ID] [P?] [Story] Description with file path`

- **[ID]**: Sequential task identifier (T001, T002, ...)
- **[P]**: Can run in parallel (different files, no interdependencies)
- **[Story]**: User story this task belongs to (US1, US2, US3, US4, US5, or none for foundational)
- **File paths**: Exact locations in `src/`, `tests/` structure

---

## Phase 1: Setup & Infrastructure (Days 1-2)

**Purpose**: Project initialization, dependency configuration, build structure

**Duration**: ~24 hours (6 tasks)

### Domain Project Creation

- [x] T001 Create project directory structure per plan.md in `src/PerformanceEngine.Baseline.Domain/`
- [x] T002 Create project directory structure in `src/PerformanceEngine.Baseline.Infrastructure/`
- [x] T003 Create test project directories in `tests/PerformanceEngine.Baseline.Domain.Tests/` and `tests/PerformanceEngine.Baseline.Infrastructure.Tests/`

### Project Configuration

- [x] T004 [P] Create `src/PerformanceEngine.Baseline.Domain/PerformanceEngine.Baseline.Domain.csproj` with .NET 10 target, nullable reference types, LangVersion 13.0
- [x] T005 [P] Create `src/PerformanceEngine.Baseline.Infrastructure/PerformanceEngine.Baseline.Infrastructure.csproj` with Redis (StackExchange.Redis 2.8.0) and DependencyInjection NuGet references
- [x] T006 [P] Create test project `.csproj` files with xUnit 2.8.1, FluentAssertions 6.12.1, and project references
- [x] T007 Add projects to `PerformanceEngine.Metrics.sln` solution file
- [x] T008 [P] Create `src/PerformanceEngine.Baseline.Domain/global.usings.cs` with standard using directives
- [x] T009 [P] Create `README.md` and `IMPLEMENTATION_GUIDE.md` in domain project root

---

## Phase 2: Foundational Domain Layer (Days 3-5)

**Purpose**: Core domain logic independent of infrastructure; foundation for all user stories

**Duration**: ~72 hours (18 tasks)

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

**Architecture**: All domain code is pure, deterministic, immutable; no Redis/infrastructure dependencies

### Domain Exceptions & Invariants

- [x] T010 [P] Create exception hierarchy in `src/PerformanceEngine.Baseline.Domain/Domain/BaselineDomainException.cs`
  - Includes: BaselineDomainException, DomainInvariantViolatedException, BaselineNotFoundException, ToleranceValidationException, ConfidenceValidationException
- [x] T011 [P] Create `src/PerformanceEngine.Baseline.Domain/Domain/Baselines/BaselineInvariants.cs` (immutability enforcement, consistency checks)
- [x] T012 [P] Create `src/PerformanceEngine.Baseline.Domain/Domain/Comparisons/ComparisonResultInvariants.cs` (outcome aggregation validation)

### Baseline Aggregate Root

- [x] T013 [P] Create `BaselineId.cs` value object in `src/PerformanceEngine.Baseline.Domain/Domain/Baselines/` with:
  - UUID generation, equality semantics, immutability
  - Test: IEquatable, hash consistency
- [x] T014 [P] Create `Baseline.cs` aggregate root in `src/PerformanceEngine.Baseline.Domain/Domain/Baselines/` with:
  - Properties: Id, CreatedAt, Metrics, EvaluationResults, ToleranceConfig
  - Method: GetMetric(name) → IMetric?
  - Immutability: read-only collections, no setters
  - Constructor validation per BaselineInvariants
- [x] T015 [P] Create `BaselineFactory.cs` domain service in `src/PerformanceEngine.Baseline.Domain/Domain/Baselines/` for baseline creation with validation

### Tolerance Value Objects & Rules

- [x] T016 [P] Create `Tolerance.cs` value object in `src/PerformanceEngine.Baseline.Domain/Domain/Tolerances/` with:
  - Properties: MetricName, Type (enum), Amount
  - Method: IsWithinTolerance(baseline, current) → bool
  - Validation: non-negative amount, known metric name
- [x] T017 [P] Create `ToleranceType.cs` enum: RELATIVE (percentage), ABSOLUTE (value-based)
- [x] T018 [P] Create `ToleranceConfiguration.cs` collection in `src/PerformanceEngine.Baseline.Domain/Domain/Tolerances/` with:
  - Methods: GetTolerance(name), HasTolerance(name)
  - Validation: all metrics have rules, no duplicates
- [x] T019 [P] Create `ToleranceValidation.cs` in `src/PerformanceEngine.Baseline.Domain/Domain/Tolerances/` for invariant enforcement

### Confidence Level Value Object

- [x] T020 [P] Create `ConfidenceLevel.cs` value object in `src/PerformanceEngine.Baseline.Domain/Domain/Confidence/` with:
  - Property: Value [0.0, 1.0]
  - Method: IsConclusive(threshold) → bool
  - Immutability, equality, validation in [0.0, 1.0] range
- [x] T021 [P] Create `ConfidenceCalculator.cs` domain service in `src/PerformanceEngine.Baseline.Domain/Domain/Confidence/` with:
  - Method: CalculateConfidence(magnitude, tolerance) → ConfidenceLevel
  - Algorithm per research.md (magnitude-based formula)

### Comparison Logic (Pure Domain Service)

- [x] T022 [P] Create `ComparisonOutcome.cs` enum in `src/PerformanceEngine.Baseline.Domain/Domain/Comparisons/`:
  - Values: IMPROVEMENT, REGRESSION, NO_SIGNIFICANT_CHANGE, INCONCLUSIVE
- [x] T023 [P] Create `ComparisonMetric.cs` in `src/PerformanceEngine.Baseline.Domain/Domain/Comparisons/` with:
  - Properties: MetricName, BaselineValue, CurrentValue, AbsoluteChange, RelativeChange, Tolerance, Outcome, Confidence
  - Immutable, calculated at construction
- [x] T024 Create `ComparisonCalculator.cs` domain service in `src/PerformanceEngine.Baseline.Domain/Domain/Comparisons/` with:
  - Method: CalculateMetric(baseline, current, tolerance, threshold) → ComparisonMetric
  - Method: DetermineOutcome(tolerance, confidence) → ComparisonOutcome
  - Pure functions per research.md (confidence formula, tolerance evaluation)
  - Depends: T020, T016

### Comparison Result Aggregate Root

- [x] T025 [P] Create `ComparisonResult.cs` aggregate root in `src/PerformanceEngine.Baseline.Domain/Domain/Comparisons/` with:
  - Properties: Id, BaselineId, ComparedAt, OverallOutcome, OverallConfidence, MetricResults (read-only)
  - Method: HasRegression() → bool
  - Immutable, outcome aggregation per research.md (worst-case strategy)
  - Depends: T022, T023
- [x] T026 [P] Create `OutcomeAggregator.cs` in `src/PerformanceEngine.Baseline.Domain/Domain/Comparisons/` with:
  - Method: Aggregate(metrics) → ComparisonOutcome (priority: REGRESSION > IMPROVEMENT > ...)
  - Method: AggregateConfidence(metrics) → ConfidenceLevel (minimum confidence)

### Domain Events (Optional, Phase 1)

- [x] T027 [P] Create `BaselineCreatedEvent.cs` in `src/PerformanceEngine.Baseline.Domain/Domain/Events/`
- [x] T028 [P] Create `ComparisonPerformedEvent.cs` in `src/PerformanceEngine.Baseline.Domain/Domain/Events/`

### Repository Port (Infrastructure Boundary)

- [x] T029 [P] Create `IBaselineRepository.cs` port in `src/PerformanceEngine.Baseline.Domain/Ports/` with:
  - Method: CreateAsync(baseline) → BaselineId
  - Method: GetByIdAsync(id) → Baseline?
  - Method: ListRecentAsync(count) → IReadOnlyList<Baseline>
  - Semantics: No Redis-specific code; pure domain abstraction

---

## Phase 3: Domain Unit Tests (Days 3-5, Parallel with Phase 2)

**Purpose**: Verify domain logic correctness, determinism, invariants

**Duration**: ~40 hours (10 test tasks)

**Note**: Tests written incrementally as entities are implemented (TDD approach)

### Baseline & BaselineId Tests

- [x] T030 [P] Create `tests/PerformanceEngine.Baseline.Domain.Tests/Domain/Baselines/BaselineIdTests.cs` with:
  - Test: UUID generation
  - Test: Equality semantics
  - Test: Hash consistency
  - Test: Immutability

- [x] T031 [P] Create `tests/PerformanceEngine.Baseline.Domain.Tests/Domain/Baselines/BaselineTests.cs` with:
  - Test: Baseline creation with valid inputs
  - Test: Baseline rejects empty metrics (invariant)
  - Test: Baseline rejects duplicate metric names (invariant)
  - Test: GetMetric(name) returns correct metric or null
  - Test: Baseline is immutable (properties read-only)
  - Test: Baseline enforces BaselineInvariants

### Tolerance Tests

- [x] T032 [P] Create `tests/PerformanceEngine.Baseline.Domain.Tests/Domain/Tolerances/ToleranceTests.cs` with:
  - Test: RELATIVE tolerance: IsWithinTolerance calculation (±10%)
  - Test: ABSOLUTE tolerance: IsWithinTolerance calculation (±50ms)
  - Test: Tolerance rejects negative amount (invariant)
  - Test: Tolerance rejects empty metric name (invariant)

- [x] T033 [P] Create `tests/PerformanceEngine.Baseline.Domain.Tests/Domain/Tolerances/ToleranceConfigurationTests.cs` with:
  - Test: GetTolerance returns correct rule
  - Test: GetTolerance throws KeyNotFoundException if metric not found
  - Test: HasTolerance returns true/false correctly

### Confidence Tests

- [x] T034 [P] Create `tests/PerformanceEngine.Baseline.Domain.Tests/Domain/Confidence/ConfidenceLevelTests.cs` with:
  - Test: ConfidenceLevel rejects values < 0.0 or > 1.0 (invariant)
  - Test: IsConclusive(threshold) returns true/false correctly
  - Test: Equality semantics (floating-point precision tolerance)

- [x] T035 [P] Create `tests/PerformanceEngine.Baseline.Domain.Tests/Domain/Confidence/ConfidenceCalculatorTests.cs` with:
  - Test: CalculateConfidence produces [0.0, 1.0] range
  - Test: CalculateConfidence increases with magnitude beyond tolerance
  - Test: Boundary conditions (on tolerance, at 0%, at 100%)

### Comparison Tests

- [x] T036 [P] Create `tests/PerformanceEngine.Baseline.Domain.Tests/Domain/Comparisons/ComparisonMetricTests.cs` with:
  - Test: ComparisonMetric calculates absoluteChange correctly
  - Test: ComparisonMetric calculates relativeChange correctly
  - Test: ComparisonMetric outcome determined by tolerance + confidence
  - Test: Immutability (read-only properties)

- [x] T037 Create `tests/PerformanceEngine.Baseline.Domain.Tests/Domain/Comparisons/ComparisonCalculatorTests.cs` with:
  - Test: CalculateMetric with RELATIVE tolerance
  - Test: CalculateMetric with ABSOLUTE tolerance
  - Test: CalculateMetric determines correct outcome (REGRESSION/IMPROVEMENT/NO_SIGNIFICANT_CHANGE)
  - Test: Confidence below threshold → INCONCLUSIVE outcome
  - Test: Edge case: baseline = 0 (division by zero in relative change)
  - Depends: T024

- [x] T038 Create `tests/PerformanceEngine.Baseline.Domain.Tests/Domain/Comparisons/ComparisonResultTests.cs` with:
  - Test: ComparisonResult aggregates metric outcomes (worst-case strategy)
  - Test: ComparisonResult aggregates confidence (minimum)
  - Test: ComparisonResult enforces ComparisonResultInvariants
  - Test: HasRegression() returns true/false correctly
  - Depends: T025

### Determinism Test Harness

- [x] T039 [P] Create `tests/PerformanceEngine.Baseline.Domain.Tests/Domain/Comparisons/DeterminismTests.cs` with:
  - Test: CalculateMetric (1000 runs with identical input → identical result)
  - Test: ComparisonResult (1000 runs with identical input → identical result)
  - Purpose: Verify no floating-point ambiguity, no ordering effects, reproducibility

### Invariant Enforcement Tests

- [x] T040 [P] Create `tests/PerformanceEngine.Baseline.Domain.Tests/Domain/Baselines/BaselineInvariantTests.cs` with:
  - Test: AssertValid throws on empty metrics
  - Test: AssertValid throws on duplicate metric names
  - Test: AssertValid throws on invalid tolerance config
  - Test: AssertValid succeeds with valid baseline

---

## Phase 4: Application Layer (Days 6-7)

**Purpose**: Orchestration services, DTOs, use cases; bridges domain to infrastructure

**Duration**: ~24 hours (6 tasks)

**Architecture**: Application layer orchestrates domain; no domain logic here; no infrastructure knowledge

### Application DTOs

- [x] T041 [P] Create `src/PerformanceEngine.Baseline.Domain/Application/Dto/BaselineDto.cs` with:
  - Properties: Id, CreatedAt, MetricDtos, Tolerance config
  - Mapper: Baseline ↔ BaselineDto (serialization)

- [x] T042 [P] Create `src/PerformanceEngine.Baseline.Domain/Application/Dto/ComparisonRequestDto.cs` with:
  - Properties: BaselineId, CurrentMetrics, Tolerance config, ConfidenceThreshold

- [x] T043 [P] Create `src/PerformanceEngine.Baseline.Domain/Application/Dto/ComparisonResultDto.cs` with:
  - Properties: OverallOutcome, Confidence, MetricResults (per-metric details)
  - Mapper: ComparisonResult ↔ ComparisonResultDto

### Application Services

- [x] T044 Create `src/PerformanceEngine.Baseline.Domain/Application/Services/ComparisonOrchestrator.cs` with:
  - Method: CreateBaselineAsync(metrics, config) → BaselineId
  - Method: CompareAsync(baselineId, currentMetrics, threshold) → ComparisonResult
  - Error handling: BaselineNotFoundException (expired), MetricNotFoundException (mismatch)
  - Depends: T015 (BaselineFactory), T025 (ComparisonResult)

- [x] T045 [P] Create `src/PerformanceEngine.Baseline.Domain/Application/Services/ComparisonService.cs` interface (application facade)

### Use Cases (Optional, Phase 1)

- [x] T046 [P] Create `src/PerformanceEngine.Baseline.Domain/Application/UseCases/CreateBaselineUseCase.cs` (optional orchestration wrapper)
- [x] T047 [P] Create `src/PerformanceEngine.Baseline.Domain/Application/UseCases/PerformComparisonUseCase.cs` (optional orchestration wrapper)

---

## Phase 5: Infrastructure Layer - Redis Adapter (Days 8-9)

**Purpose**: Redis implementation of IBaselineRepository port; no domain logic

**Duration**: ~32 hours (8 tasks)

**Architecture**: Infrastructure implements domain-defined port; no Redis knowledge leaks into domain

### Redis Connection & Configuration

- [x] T048 [P] Create `src/PerformanceEngine.Baseline.Infrastructure/Persistence/RedisConnectionFactory.cs` with:
  - Connection pooling, TTL configuration (24h default)
  - Configuration from appsettings.json (Redis:ConnectionString, BaselineTtl)

- [x] T049 [P] Create `src/PerformanceEngine.Baseline.Infrastructure/Configuration/BaselineInfrastructureExtensions.cs` with:
  - Dependency injection setup: IConnectionMultiplexer, IBaselineRepository
  - ServiceCollection.AddBaselineInfrastructure()

### Redis Adapter Implementation

- [x] T050 Create `src/PerformanceEngine.Baseline.Infrastructure/Persistence/RedisBaselineRepository.cs` implementing `IBaselineRepository` with:
  - CreateAsync: Serialize baseline, store in Redis with TTL, return BaselineId
  - GetByIdAsync: Retrieve from Redis, deserialize, return Baseline or null if expired
  - ListRecentAsync: Retrieve recent baselines (ordered by creation time)
  - Error handling: RepositoryException on connection failure
  - Depends: T048

### Serialization & Mapping

- [x] T051 [P] Create `src/PerformanceEngine.Baseline.Infrastructure/Persistence/BaselineRedisMapper.cs` with:
  - Serialize: Baseline → JSON (IMetric[], tolerance config)
  - Deserialize: JSON → Baseline (reconstruct from storage)
  - Round-trip fidelity: Same baseline serialized/deserialized → equal

- [x] T052 [P] Create `src/PerformanceEngine.Baseline.Infrastructure/Persistence/RedisKeyBuilder.cs` for:
  - Key naming convention: baseline:{id}, baseline:recent:{timestamp}
  - Prevents key collisions

### Infrastructure Tests

- [x] T053 Create `tests/PerformanceEngine.Baseline.Infrastructure.Tests/Persistence/RedisBaselineRepositoryTests.cs` with:
  - Test: CreateAsync stores baseline, returns BaselineId
  - Test: GetByIdAsync retrieves stored baseline (equality)
  - Test: GetByIdAsync returns null if baseline expired
  - Test: ListRecentAsync returns recent baselines in order
  - Test: Serialization round-trip preserves baseline
  - Test: Concurrent reads don't interfere
  - Depends: T050

- [x] T054 [P] Create `tests/PerformanceEngine.Baseline.Infrastructure.Tests/Persistence/BaselineRedisMapperTests.cs` with:
  - Test: Serialize/deserialize round-trip fidelity
  - Test: Handles null evaluation results
  - Test: Handles edge case metrics (zero values, very large values)

---

## Phase 6: Integration Tests (Days 10)

**Purpose**: End-to-end workflows; domain + application + infrastructure

**Duration**: ~16 hours (4 tasks)

### Full Workflow Integration

- [x] T055 Create `tests/PerformanceEngine.Baseline.Domain.Tests/Integration/BaselineComparisonWorkflowTests.cs` with:
  - Test: Create baseline with metrics
  - Test: Compare identical current metrics → NO_SIGNIFICANT_CHANGE
  - Test: Compare regressed metrics → REGRESSION
  - Test: Compare improved metrics → IMPROVEMENT
  - Test: Compare with low confidence → INCONCLUSIVE
  - Test: Multiple metrics aggregation (worst-case priority)

- [x] T056 Create `tests/PerformanceEngine.Baseline.Infrastructure.Tests/Integration/RedisBaselineWorkflowTests.cs` with:
  - Test: Create baseline → store in Redis → retrieve → compare
  - Test: Expired baseline returns null → comparison fails gracefully
  - Test: Concurrent baseline creation/retrieval (no race conditions)

### Cross-Domain Integration

- [x] T057 [P] Create `tests/PerformanceEngine.Baseline.Domain.Tests/Integration/MetricsDomainIntegrationTests.cs` with:
  - Test: Baseline accepts Metrics from Metrics Domain (IMetric interface)
  - Test: Comparison works with real Metric objects from Metrics Domain
  - Depends: PerformanceEngine.Metrics.Domain assembly

- [x] T058 [P] Create `tests/PerformanceEngine.Baseline.Domain.Tests/Integration/EvaluationDomainIntegrationTests.cs` with:
  - Test: Baseline can store optional evaluation results from Evaluation Domain
  - Test: Baseline with/without evaluation results both valid

---

## Phase 7: Documentation & Validation (Days 11)

**Purpose**: Developer guides, API documentation, performance validation

**Duration**: ~16 hours (4 tasks)

### Implementation Guides

- [x] T059 [P] Create `src/PerformanceEngine.Baseline.Domain/IMPLEMENTATION_GUIDE.md` with:
  - Architecture overview (domain layers, dependencies)
  - Key classes & responsibilities
  - Extension points (tolerance strategies, custom rules - Phase 2)
  - Troubleshooting guide

- [x] T060 [P] Create `src/PerformanceEngine.Baseline.Infrastructure/INFRASTRUCTURE_GUIDE.md` with:
  - Redis setup & configuration
  - Connection pooling details
  - TTL & eviction policy
  - Scaling considerations

### Validation & Performance

- [x] T061 Create `tests/PerformanceEngine.Baseline.Domain.Tests/Performance/LatencyTests.cs` with:
  - Test: Comparison latency < 20ms (p95)
  - Test: 100 concurrent comparisons complete without error
  - Profile: Memory usage, allocation patterns
  - Target: Meets success criteria SC-002 (all comparisons < 100ms)

- [x] T062 Create `tests/PerformanceEngine.Baseline.Infrastructure.Tests/Performance/RedisLatencyTests.cs` with:
  - Test: Redis create + retrieve + deserialize < 15ms (p95)
  - Test: Redis handles 1000 qps baseline storage
  - Profile: Connection pool efficiency

---

## Phase 8: Polish & Cross-Cutting Concerns (Days 12-14)

**Purpose**: Code quality, documentation, edge cases, error paths

**Duration**: ~24 hours (6 tasks)

### Edge Case & Error Handling

- [x] T063 [P] Create `tests/PerformanceEngine.Baseline.Domain.Tests/EdgeCases/EdgeCaseTests.cs` with:
  - Test: Missing metric in current results (metric in baseline but not current)
  - Test: New metric in current results (not in baseline) → error or warning?
  - Test: Null/NaN metric values → error handling
  - Test: Tolerance = 0 (exact match required)
  - Test: Baseline with 1 metric vs 100 metrics → handles both
  - Test: Very small metrics (floating-point precision edges)
  - Test: Very large metrics (overflow edges)

- [x] T064 [P] Create comprehensive exception handling tests in `tests/PerformanceEngine.Baseline.Domain.Tests/ExceptionTests.cs` with:
  - Test: BaselineNotFoundException on missing baseline
  - Test: ToleranceValidationException on invalid tolerance
  - Test: DomainInvariantViolatedException on constraint violation
  - Test: RepositoryException on Redis connection failure

### Code Quality & Standards

- [ ] T065 [P] Add `.editorconfig` to baseline domain project root (C# style/formatting rules)
- [ ] T066 [P] Add XML documentation comments to all public types (IBaselineRepository, Baseline, ComparisonResult, etc.)
- [x] T067 [P] Add VERIFICATION_CHECKLIST.md following pattern from PerformanceEngine.Metrics.Domain

### Continuous Integration Setup

- [ ] T068 [P] Create or update `.github/workflows/baseline-domain-tests.yml` to run:
  - Restore dependencies
  - Build domain + infrastructure + tests
  - Run unit tests
  - Run integration tests
  - Collect coverage
  - Report failures

---

## Phase 9: Final Validation & Release (Day 15)

**Purpose**: Verify all success criteria met, create release artifacts

**Duration**: ~8 hours (2 tasks)

### Success Criteria Verification

- [ ] T069 Create comprehensive validation document `COMPLETION_VALIDATION.md` verifying:
  - SC-001: Regression detection accuracy (test suite coverage)
  - SC-002: Latency <100ms (performance test results)
  - SC-003: Baseline immutability 100% (invariant tests)
  - SC-004: Determinism 100% (1000-run test results)
  - SC-005: Multi-metric aggregation (all combination tests)
  - SC-006: Tolerance 0-100% (edge case tests)
  - SC-007: Confidence [0.0, 1.0] range (invariant tests)
  - SC-008: Edge case error handling (exception tests)

### Release & Handoff

- [ ] T070 [P] Create NuGet package specifications for:
  - PerformanceEngine.Baseline.Domain
  - PerformanceEngine.Baseline.Infrastructure
- [ ] T071 [P] Create release notes documenting:
  - Version, date, contributors
  - Features delivered (user stories 1-5)
  - Known limitations (deferred: Phase 2 items)
  - Integration points (Metrics Domain, Evaluation Domain)

---

## Task Dependencies & Critical Path

```
CRITICAL PATH (Sequential Dependencies):

Phase 1 (Setup) → Phase 2 (Domain) → Phase 3 (Domain Tests) 
  ↓
Phase 4 (Application) → Phase 5 (Infrastructure) 
  ↓
Phase 6 (Integration Tests) → Phase 7 (Documentation) 
  ↓
Phase 8 (Polish) → Phase 9 (Validation & Release)

PARALLELIZABLE WORK:
  • Phase 2 & 3: Domain implementation + testing (parallel with [P] markers)
  • Phase 4 & 5: Application + Infrastructure (independent layers)
  • Phase 7 & 8: Documentation + Edge case testing (parallel)

BLOCKED PATHS:
  • Phase 3 cannot start until Phase 2 entities created
  • Phase 4 cannot start until Phase 2 domain complete
  • Phase 5 cannot start until Phase 4 DTOs defined
  • Phase 6 cannot start until Phase 5 repository implemented
```

---

## Task Complexity Estimates

| Phase | Complexity | Duration | Tasks |
|-------|-----------|----------|-------|
| Phase 1 (Setup) | Low | 1 day | 9 |
| Phase 2 (Domain) | High | 3 days | 20 |
| Phase 3 (Tests) | High | 2.5 days | 11 |
| Phase 4 (Application) | Medium | 1.5 days | 6 |
| Phase 5 (Infrastructure) | Medium | 2 days | 8 |
| Phase 6 (Integration) | High | 1 day | 4 |
| Phase 7 (Documentation) | Low | 1 day | 4 |
| Phase 8 (Polish) | Medium | 1.5 days | 6 |
| Phase 9 (Validation) | Low | 1 day | 2 |

**Total**: 71 tasks, ~15 days (4 weeks at standard velocity), 320 person-hours

---

## Parallelization Strategy

### Week 1 (Phase 1-3):
- **Stream A**: Domain classes (T013-T028, T041-T047)
- **Stream B**: Domain tests (T030-T040, parallel with Stream A)
- **Resource**: 2-3 developers

### Week 2 (Phase 4-5):
- **Stream A**: Application services (T044-T047)
- **Stream B**: Redis infrastructure (T048-T054)
- **Resource**: 1-2 developers per stream

### Week 3 (Phase 6-7):
- **Stream A**: Integration tests (T055-T058)
- **Stream B**: Documentation (T059-T062)
- **Resource**: 1 developer per stream

### Week 4 (Phase 8-9):
- **Stream A**: Edge cases & polish (T063-T068)
- **Stream B**: Validation & release (T069-T071)
- **Resource**: 1-2 developers

---

## Quality Gates & Checkpoints

| Milestone | Tasks | Gate Criteria |
|-----------|-------|--------------|
| **Setup Complete** | T001-T009 | All projects build, dependencies resolve |
| **Domain Foundation** | T010-T029 | All domain classes created, compile successfully |
| **Domain Tested** | T030-T040 | 100% unit test pass rate, determinism verified |
| **Application Ready** | T041-T047 | Orchestrators compile, DTOs map correctly |
| **Infrastructure Ready** | T048-T054 | Redis adapter compiles, store/retrieve works |
| **Integration OK** | T055-T058 | Full workflows execute end-to-end |
| **Documentation Done** | T059-T062 | Guides complete, performance validated |
| **Polish Complete** | T063-T068 | Edge cases handled, CI/CD green |
| **Release Ready** | T069-T071 | All success criteria met, validation document signed |

---

## Handoff Criteria

Implementation is **COMPLETE** when:

✅ All 71 tasks marked DONE  
✅ Latest CI/CD pipeline is GREEN  
✅ All success criteria (SC-001 through SC-008) validated  
✅ Code reviewed and approved  
✅ Documentation complete  
✅ Performance targets met (<20ms latency for comparisons)  
✅ Integration with Metrics Domain verified  
✅ Phase 2 deferred items logged (versioning, statistical confidence, etc.)  

---

## Known Risks & Mitigation

| Risk | Impact | Mitigation | Task |
|------|--------|-----------|------|
| **Metric.Direction unavailable** | Tolerance config complexity increases | Design review with Metrics Domain early | T024 |
| **Redis performance regression** | Latency > 20ms | Performance testing (T061, T062) + optimization |  |
| **Floating-point precision issues** | Determinism violated | Decimal type + 1000-run tests (T039) | T039 |
| **Complex edge cases** | Bugs in production | Comprehensive edge case tests (T063) | T063 |

---

## Next Steps After Task Completion

1. **Phase 2 Planning**: Implement metric weighting, versioning, statistical confidence
2. **Integration with Reporting**: Create trends, historical analysis (separate domain)
3. **Performance Optimization**: Caching, batch comparisons, async workflows
4. **Monitoring & Observability**: Metrics tracking, logging strategies
5. **Advanced Tolerance Strategies**: Statistical bounds, weighted metrics

---

## Appendix: Task Checklist Template

For each task, maintain:

```markdown
## Task [ID]: [Title]

**Status**: NOT STARTED | IN PROGRESS | BLOCKED | DONE
**Assignee**: [Name]
**Effort**: [hours]
**Due**: [date]

### Definition of Done
- [ ] Code compiles without errors
- [ ] Implementation matches specification
- [ ] Tests pass (if applicable)
- [ ] Code reviewed and approved
- [ ] CI/CD pipeline green
- [ ] Documentation updated
- [ ] Commit with descriptive message

### Acceptance Criteria
- [List specific, measurable criteria]
```

---

## References

- **Specification**: [baseline-domain.spec.md](baseline-domain.spec.md)
- **Implementation Plan**: [plan.md](plan.md)
- **Research & Decisions**: [research.md](research.md)
- **Domain Model**: [data-model.md](data-model.md)
- **Domain Contracts**: [contracts/domain-contracts.md](contracts/domain-contracts.md)
- **Developer Quickstart**: [quickstart.md](quickstart.md)
