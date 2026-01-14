# Tasks: Metrics Domain - Ubiquitous Language

**Input**: Design documents from `/specs/` | **Status**: Implementation Ready  
**Prerequisites**: ✅ plan.md, ✅ spec.md, ✅ research.md, ✅ data-model.md, ✅ contracts/  
**Organization**: Grouped by user story to enable independent implementation of each story  
**Format**: `[ID] [P?] [Story] Description with file path`

---

## Phase 1: Setup (Project Infrastructure)

**Purpose**: Project initialization and C# domain project structure

- [ ] T001 Create solution structure: `src/PerformanceEngine.Metrics.Domain/` (C# Class Library)
- [ ] T002 Create test project structure: `tests/PerformanceEngine.Metrics.Domain.Tests/` (xUnit)
- [ ] T003 [P] Configure xUnit 2.8+ and FluentAssertions in test project via .csproj
- [ ] T004 [P] Create `.gitignore` with C# patterns (bin/, obj/, .vs/, etc.)
- [ ] T005 Create directory structure: `src/Domain/Metrics/`, `src/Domain/Aggregations/`, `src/Domain/Events/`
- [ ] T006 Create directory structure: `src/Application/`, `src/Ports/`, `src/Infrastructure/Adapters/`
- [ ] T007 [P] Initialize root `global.usings.cs` with common System namespaces
- [ ] T008 [P] Create `src/PerformanceEngine.Metrics.Domain.csproj` with .NET 10 LTS target framework

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core domain abstractions and infrastructure all stories depend on

**⚠️ CRITICAL**: All foundational tasks must complete before user story work begins

- [ ] T009 Create value object base class: `src/Domain/ValueObject.cs` (abstract base for immutable value objects)
- [ ] T010 Create domain event interface: `src/Domain/Events/IDomainEvent.cs` (empty marker interface)
- [ ] T011 [P] Create enumeration: `src/Domain/Metrics/LatencyUnit.cs` (Nanoseconds, Microseconds, Milliseconds, Seconds)
- [ ] T012 [P] Create enumeration: `src/Domain/Metrics/SampleStatus.cs` (Success, Failure)
- [ ] T013 [P] Create enumeration: `src/Domain/Metrics/ErrorClassification.cs` (Timeout, NetworkError, ApplicationError, UnknownError)
- [ ] T014 Create value object: `src/Domain/Metrics/Latency.cs` with unit conversion logic (Latency(45.5, LatencyUnit.Milliseconds))
- [ ] T015 Create value object: `src/Domain/Metrics/LatencyUnitConverter.cs` (unit conversion between all LatencyUnit types)
- [ ] T016 Create value object: `src/Domain/Metrics/Percentile.cs` with range validation [0, 100] invariant
- [ ] T017 Create value object: `src/Domain/Metrics/AggregationWindow.cs` (abstract base for Full/Sliding/Fixed window types)
- [ ] T018 [P] Create value object: `src/Domain/Metrics/ExecutionContext.cs` (engine name, execution ID, scenario name)
- [ ] T019 Create value object: `src/Domain/Metrics/AggregationResult.cs` (value, unit, computed timestamp)
- [ ] T020 Create port interface: `src/Ports/IExecutionEngineAdapter.cs` (MapResultsToDomain(rawResults) → ImmutableList<Sample>)
- [ ] T021 Create port interface: `src/Ports/IPersistenceRepository.cs` (SaveMetric, RetrieveMetric signatures with deferral note)

**Checkpoint**: Foundation complete - all value objects and ports ready for user story implementation

---

## Phase 3: User Story 1 - Domain Analyst Defines Metrics Vocabulary (Priority: P1) 🎯 MVP

**Goal**: Establish immutable, engine-agnostic domain model so all system components reference only domain terms (Sample, Metric, Latency) rather than engine-specific jargon.

**Independent Test**: Verify that Sample, Metric, and value objects can be instantiated with valid data; all engine-specific concepts excluded from domain layer.

### Implementation for User Story 1

- [ ] T022 [P] [US1] Create domain entity: `src/Domain/Metrics/Sample.cs` (Guid Id, DateTime Timestamp UTC, Latency Duration, SampleStatus Status, ErrorClassification? errorClassification, ExecutionContext context, Dictionary<string,object>? metadata)
  - Implement immutable constructor with 4 invariants (timestamp ≤ now, duration ≥ 0, error classification required iff Status==Failure, null iff Status==Success)
  - Implement GetHashCode/Equals for comparison
- [ ] T023 [P] [US1] Create domain entity: `src/Domain/Metrics/SampleCollection.cs` (ImmutableList<Sample> sealed container)
  - Implement Add(Sample), AddRange(IEnumerable<Sample>) methods
  - Implement GetSnapshot() and GetSnapshotOrdered(SampleOrdering) for safe iteration
  - Enforce append-only semantics
- [ ] T024 [US1] Create domain entity: `src/Domain/Metrics/Metric.cs` (entity representing aggregated samples)
  - Properties: Guid Id, SampleCollection samples, AggregationWindow window, string metricType, DateTime computedAt
  - Enforce invariant: Metric cannot exist without samples (samples.Count > 0)
  - Implement immutable after construction
- [ ] T025 [US1] Create unit tests: `tests/Domain/Metrics/SampleTests.cs`
  - Test immutability: timestamp, duration, status not settable after construction
  - Test invariant violations: future timestamp throws, negative duration throws, missing error classification throws when Status==Failure
  - Test valid instantiation with success sample and failure sample
- [ ] T026 [P] [US1] Create unit tests: `tests/Domain/Metrics/SampleCollectionTests.cs`
  - Test Add() appends correctly
  - Test AddRange() adds multiple samples
  - Test GetSnapshot() returns immutable list
  - Test append-only: verify new Add() returns new instance (functional pattern)
- [ ] T027 [P] [US1] Create unit tests: `tests/Domain/Metrics/MetricTests.cs`
  - Test metric created with samples
  - Test metric cannot be created with empty collection (invariant violation throws)
  - Test immutability: metric properties not settable
- [ ] T028 [US1] Create value object tests: `tests/Domain/Metrics/ValueObjectTests.cs` (comprehensive tests for Latency, Percentile, AggregationWindow)
  - Latency: test unit conversions (ms → ns, ns → ms, etc.), test invalid negative values throw, test equality
  - Percentile: test valid range [0, 100], test boundary values (0, 50, 100), test out-of-range throws
  - AggregationWindow: test FullExecution, SlidingWindow, FixedWindow types
- [ ] T029 [US1] Create contract test: `tests/Domain/SampleInvariants.cs` (verify all invariants enforced consistently)
  - Test that no Sample can ever be created violating the 4 invariants
  - Test that SampleCollection maintains insertion order
  - Test that Metric cannot exist without samples
- [ ] T030 [P] [US1] Verify no infrastructure dependencies in domain layer
  - Grep verification: no imports of System.Data.*, System.Net.*, HttpClient, or engine-specific types
  - Grep verification: `Domain/**/*.cs` contains zero `using` directives for Infrastructure or persistence

**Checkpoint**: User Story 1 complete - all domain entities immutable and testable; engine-agnostic vocabulary established

---

## Phase 4: User Story 2 - System Ensures Metrics Determinism & Reproducibility (Priority: P1)

**Goal**: Guarantee that identical samples and aggregation parameters always produce byte-identical results, enabling reliable CI/CD quality gates and automated governance decisions.

**Independent Test**: Run aggregation twice with identical sample inputs and aggregation specs; verify exact result equivalence (not approximate floating-point equality).

### Implementation for User Story 2

- [ ] T031 [P] [US2] Create aggregation interface: `src/Domain/Aggregations/IAggregationOperation.cs` (Aggregate(samples: SampleCollection, window: AggregationWindow) → AggregationResult)
- [ ] T032 [P] [US2] Create aggregation: `src/Domain/Aggregations/AverageAggregation.cs` (sum all latencies, divide by count, return as AggregationResult)
  - Normalize all samples to consistent unit before calculation
  - Return exact decimal precision (no floating-point approximation)
- [ ] T033 [P] [US2] Create aggregation: `src/Domain/Aggregations/MaxAggregation.cs` (find maximum latency value)
  - Normalize all samples to consistent unit before calculation
  - Return exact value from largest sample
- [ ] T034 [P] [US2] Create aggregation: `src/Domain/Aggregations/MinAggregation.cs` (find minimum latency value)
  - Normalize all samples to consistent unit before calculation
  - Return exact value from smallest sample
- [ ] T035 [US2] Create aggregation: `src/Domain/Aggregations/PercentileAggregation.cs` (compute pXX percentile using nearest-rank algorithm)
  - Implement deterministic nearest-rank algorithm (no interpolation)
  - Normalize all samples to consistent unit before calculation
  - Handle edge cases: single sample, all equal values, exact percentile boundaries
- [ ] T036 [US2] Create normalizer: `src/Domain/Aggregations/AggregationNormalizer.cs`
  - Implement NormalizeSamples(samples, targetUnit) → normalized samples with consistent LatencyUnit
  - Preserve all metadata during normalization
  - Verify no sample values lost or modified during unit conversion
- [ ] T037 [US2] Create contract test: `tests/Domain/Aggregations/DeterminismContract.cs` (enforce determinism across all operations)
  - Run AverageAggregation twice with identical inputs → verify byte-identical output
  - Run PercentileAggregation(p95) twice with identical inputs → verify byte-identical output
  - Run all aggregations with large sample set (1M samples) → verify deterministic results
- [ ] T038 [P] [US2] Create unit tests: `tests/Domain/Aggregations/AggregationTests.cs`
  - Test AverageAggregation: empty collection, single sample, multiple samples, all equal values
  - Test MaxAggregation: empty collection, single sample, multiple samples
  - Test MinAggregation: empty collection, single sample, multiple samples
  - Test PercentileAggregation: p0, p50, p95, p99, p100 with various sample distributions
- [ ] T039 [P] [US2] Create unit tests: `tests/Domain/Aggregations/NormalizationTests.cs`
  - Test normalization from milliseconds to nanoseconds (verify no precision loss)
  - Test normalization from nanoseconds to milliseconds (verify rounding strategy)
  - Test normalization with mixed-unit input (verify all converted to target)
  - Test metadata preservation during normalization
- [ ] T040 [US2] Create verification test: `tests/Domain/Aggregations/ReproducibilityTests.cs` (verify reproducibility across multiple runs)
  - Generate fixed seed sample data (deterministic pseudo-random)
  - Compute metric aggregation in Run 1
  - Re-compute same metric aggregation in Run 2
  - Assert: Run1 results == Run2 results (bit-for-bit exact match)
  - Repeat for all 4 aggregation types

**Checkpoint**: User Story 2 complete - all aggregations deterministic and reproducible; identical inputs produce identical outputs

---

## Phase 5: User Story 3 - Evaluation Logic Operates on Engine-Agnostic Models (Priority: P1)

**Goal**: Enable evaluation, analysis, and persistence logic to work with any execution engine without modification by ensuring all engines adapt to domain models, never the reverse.

**Independent Test**: Verify that two different engine adapters (k6, JMeter) can produce results that map to identical domain models and be processed by a single evaluation rule without changes.

### Implementation for User Story 3

- [ ] T041 [P] [US3] Create domain event: `src/Domain/Events/SampleCollectedEvent.cs` (implements IDomainEvent)
  - Properties: Sample sample, DateTime collectedAt, string sourceName (engine)
  - Immutable after construction
- [ ] T042 [P] [US3] Create domain event: `src/Domain/Events/MetricComputedEvent.cs` (implements IDomainEvent)
  - Properties: Metric metric, AggregationOperation operation, DateTime computedAt
  - Immutable after construction
- [ ] T043 [US3] Create application use case: `src/Application/UseCases/NormalizeSamplesUseCase.cs`
  - Input: SampleCollection with mixed LatencyUnits or invalid values
  - Output: normalized SampleCollection with consistent units and valid ranges
  - Enforce: never mutate input; return new normalized collection
- [ ] T044 [US3] Create application use case: `src/Application/UseCases/ValidateAggregationUseCase.cs`
  - Input: AggregationRequest (samples, window, aggregationSpec)
  - Output: ValidationResult (valid/invalid with error messages)
  - Enforce: all samples have consistent units, window spec is valid, no null samples
- [ ] T045 [US3] Create application use case: `src/Application/UseCases/ComputeMetricUseCase.cs` (main orchestration)
  - Input: SampleCollection, AggregationWindow, aggregation operation type
  - Steps:
    1. Validate samples (ValidateAggregationUseCase)
    2. Normalize to consistent units (NormalizeSamplesUseCase)
    3. Execute aggregation (IAggregationOperation)
    4. Create Metric entity with result
    5. Publish MetricComputedEvent
  - Output: Metric with AggregationResult
- [ ] T046 [P] [US3] Create DTO: `src/Application/Dto/SampleDto.cs` (data transfer format, maps to/from Sample)
  - Properties matching Sample: timestamp, duration, status, errorClassification, executionContext, metadata
  - Immutable after construction
- [ ] T047 [P] [US3] Create DTO: `src/Application/Dto/MetricDto.cs` (data transfer format, maps to/from Metric)
- [ ] T048 [P] [US3] Create DTO: `src/Application/Dto/AggregationRequestDto.cs` (request format for computation)
- [ ] T049 [US3] Create application service: `src/Application/Services/MetricService.cs` (orchestrator)
  - Method: ComputeMetric(AggregationRequestDto) → MetricDto
  - Delegate to ComputeMetricUseCase internally
  - Handle errors and return DTO
- [ ] T050 [US3] Create adapter implementation: `src/Infrastructure/Adapters/K6EngineAdapter.cs` (implements IExecutionEngineAdapter)
  - MapResultsToDomain method converts k6-specific JSON/object format to SampleCollection
  - Ensure all k6 results map to standard ErrorClassification (not k6-specific error codes)
  - Example: k6 HTTP error 504 → ErrorClassification.Timeout
- [ ] T051 [US3] Create adapter implementation: `src/Infrastructure/Adapters/JMeterEngineAdapter.cs` (implements IExecutionEngineAdapter)
  - MapResultsToDomain method converts JMeter-specific format to SampleCollection
  - Ensure all JMeter results map to standard ErrorClassification
  - Example: JMeter "Read timed out" → ErrorClassification.Timeout
- [ ] T052 [P] [US3] Create contract test: `tests/Application/UseCaseTests.cs` (verify use cases work without infrastructure)
  - Test NormalizeSamplesUseCase with mixed units
  - Test ValidateAggregationUseCase with invalid samples
  - Test ComputeMetricUseCase end-to-end
- [ ] T053 [P] [US3] Create adapter contract test: `tests/Infrastructure/Adapters/K6AdapterTests.cs`
  - Test K6 success sample maps to domain Sample with Status==Success
  - Test K6 HTTP timeout (504) maps to ErrorClassification.Timeout
  - Test K6 connection error maps to ErrorClassification.NetworkError
  - Verify no k6-specific fields leak into domain model
- [ ] T054 [P] [US3] Create adapter contract test: `tests/Infrastructure/Adapters/JMeterAdapterTests.cs`
  - Test JMeter success sample maps to domain Sample with Status==Success
  - Test JMeter timeout maps to ErrorClassification.Timeout
  - Test JMeter connection error maps to ErrorClassification.NetworkError
  - Verify no JMeter-specific fields leak into domain model
- [ ] T055 [US3] Create integration test: `tests/Application/IntegrationTests/CrossAdapterCompatibilityTests.cs` (verify different adapters work with same evaluation rule)
  - Create sample data from K6 adapter → normalize → aggregate
  - Create sample data from JMeter adapter → normalize → aggregate
  - Verify both produce identical Metric results when aggregation parameters identical
  - Verify evaluation rule references only domain concepts (Metric, Sample, Latency)
- [ ] T056 [P] [US3] Create integration test: `tests/Application/IntegrationTests/MetricServiceIntegrationTests.cs`
  - Test MetricService.ComputeMetric with multiple aggregation types
  - Test end-to-end: raw results → adapter → use case → metric → DTO
- [ ] T057 [US3] Verify no engine-specific references in domain or application layers
  - Grep verification: no imports of "K6", "JMeter", "Gatling" in `Domain/**` or `Application/**`
  - Grep verification: ports/ directory references only domain abstractions
  - Manual code review: all engine logic isolated to Infrastructure/Adapters/

**Checkpoint**: User Story 3 complete - domain layer completely engine-agnostic; adapters map all engines to standard domain models

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Documentation, configuration, and final validation

- [ ] T058 Create project documentation: `README.md` in project root
  - Quick start: how to use MetricService to compute metrics
  - Architecture overview (Clean Architecture diagram)
  - How to add new aggregation operation
  - How to add new engine adapter
- [ ] T059 Create implementation guide: `IMPLEMENTATION_GUIDE.md`
  - Step-by-step walkthrough of all 3 user stories
  - Code examples for each domain entity
  - Adapter template for new engines
- [ ] T060 [P] Create code style guide: `.editorconfig` with C# formatting conventions
  - Enforce immutability: property initialization over setters
  - Enforce null safety: nullable reference types enabled
- [ ] T061 Create build configuration: `Directory.Build.props` with shared version and target framework
- [ ] T062 [P] Update `.csproj`: enable nullable reference types (`<Nullable>enable</Nullable>`)
- [ ] T063 [P] Create CI/CD validation task: ensure zero compiler warnings
- [ ] T064 Create final checklist verification: `VERIFICATION_CHECKLIST.md`
  - [ ] All domain entities immutable (no setters)
  - [ ] All value objects implement Equals/GetHashCode
  - [ ] All invariants enforced in constructors (4 Sample invariants, Percentile [0,100], Latency ≥ 0, etc.)
  - [ ] All aggregations deterministic (identical inputs → byte-identical outputs)
  - [ ] Zero engine-specific references in Domain/ or Application/ layers
  - [ ] All unit tests passing
  - [ ] All contract tests passing
  - [ ] All integration tests passing
  - [ ] Code coverage ≥ 85% for domain logic
  - [ ] No compiler warnings
  - [ ] Documentation complete

---

## Implementation Dependencies & Execution Order

### Phase Ordering (Sequential - Must Complete in Order)
1. **Phase 1** (Setup) → 2. **Phase 2** (Foundation) → 3. **Phase 3-5** (User Stories can start, may overlap) → 6. **Phase 6** (Polish)

### User Story Parallelization

**After Phase 2 is complete**, the 3 user stories can be implemented in parallel:

- **US1 (Vocabulary)** and **US2 (Determinism)** can run simultaneously (no dependencies)
  - US1 creates domain entities (Sample, Metric)
  - US2 creates aggregation operations (Average, Percentile)
  - Both depend on Phase 2 value objects but don't depend on each other
  
- **US3 (Engine-Agnostic)** requires US1 to be mostly complete (needs Sample, Metric entities)
  - US3 can start after T022-T024 complete but doesn't need US2 complete
  - US3 creates adapters (K6, JMeter), DTOs, use cases
  - Adapters reference domain entities from US1

### Parallel Task Examples

**Parallel within Phase 2** (these tasks modify different files):
- T011, T012, T013 (create enums) - can run simultaneously
- T018 (ExecutionContext) alongside T014 (Latency) - different files
- T020, T021 (port interfaces) - can run simultaneously

**Parallel within US1** (after T022 completes):
- T023 (SampleCollection) and T024 (Metric) can run simultaneously
- T025 (Sample tests) and T026 (SampleCollection tests) can run simultaneously

**Parallel within US2** (after T031 completes):
- T032, T033, T034 (Average, Max, Min) can run simultaneously
- T038, T039, T040 (unit tests) can run simultaneously

**Parallel within US3** (after T043 completes):
- T050, T051 (K6 and JMeter adapters) can run simultaneously
- T053, T054 (adapter contract tests) can run simultaneously

---

## Success Metrics & Acceptance Criteria

**All Phase 3-5 tasks complete** when:
- ✅ All 57 tasks (T022-T057) have passing implementations
- ✅ 100% of domain entities pass invariant validation tests
- ✅ 100% of aggregation operations pass determinism contract tests
- ✅ 100% of adapters successfully map engine results to domain models
- ✅ Zero references to engine-specific concepts in domain or application layers
- ✅ All integration tests pass: K6 and JMeter adapters work with same evaluation rule

**Features meeting specification success criteria**:
- **SC-001**: ✅ All system components reference only domain terms (Sample, Metric, Latency, Percentile)
- **SC-002**: ✅ Identical samples + aggregation parameters → byte-identical results
- **SC-003**: ✅ Domain sufficient to express results from 2+ execution engines (k6, JMeter implemented)
- **SC-004**: ✅ Documentation clear enough for consistent terminology (README + IMPLEMENTATION_GUIDE)

---

## Notes for Implementation Team

### Constitutional Compliance (Verified)

- **Specification-Driven Development**: All tasks derived from metrics-domain.spec.md (12 FRs, 4 success criteria)
- **Domain-Driven Design**: Domain layer free of infrastructure dependencies (Phase 2 foundation enforces this)
- **Clean Architecture**: Dependencies point inward; adapters implement ports, never the reverse
- **Layered Phase Independence**: Each user story independently testable and deliverable
- **Determinism & Reproducibility**: US2 specifically validates bit-identical output reproducibility
- **Engine-Agnostic Abstraction**: US3 ensures evaluation logic works with any adapter
- **Evolution-Friendly Design**: Value object pattern enables new metric types; strategy pattern enables new aggregations

### Quality Gates

- ✅ T030, T057: Verify zero infrastructure imports in domain/application
- ✅ T029, T037: Verify all invariants enforced and contracts satisfied
- ✅ T055: Verify cross-adapter compatibility before considering US3 complete

### Recommended Starting Point (MVP)

Start with **User Story 1 (T022-T030)** for immediate value:
- Establishes domain vocabulary
- Creates foundational Sample and Metric entities
- Enables other teams to reference domain language
- Time estimate: 3-4 days

Then add **User Story 2 (T031-T040)** to guarantee determinism:
- Implements all aggregation operations
- Validates reproducibility contract
- Enables reliable metric computation
- Time estimate: 4-5 days

Finally add **User Story 3 (T041-T057)** for engine independence:
- Demonstrates pluggable adapters
- Validates cross-adapter compatibility
- Completes clean architecture separation
- Time estimate: 4-5 days

**Total estimated effort**: 11-14 days for complete Phase 1 implementation
