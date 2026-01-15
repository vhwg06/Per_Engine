# Implementation Plan: Baseline Domain

**Branch**: `baseline-domain-implementation` | **Date**: 2026-01-15 | **Spec**: [baseline-domain.spec.md](baseline-domain.spec.md)  
**Input**: Feature specification from `/specs/baseline-domain/baseline-domain.spec.md`

---

## Summary

The Baseline Domain implements **deterministic comparison logic** between performance test results and an immutable baseline snapshot. It provides pure, side-effect-free comparison semantics to detect regressions, improvements, and inconclusive results when variance is high. Comparison outcomes are byte-identical for identical inputs, enabling automated regression detection in CI/CD pipelines.

The domain operates independently of metrics collection or evaluation logic through clean architecture; it consumes Metric and ComparisonRequest interfaces and produces immutable ComparisonResult entities. All comparisons are deterministic, repeatable, and support configurable tolerance thresholds (absolute and relative) with confidence-level assessment.

---

## Technical Context

**Language/Version**: C# 13 (.NET 10.0 LTS)  
**Primary Dependencies**: PerformanceEngine.Metrics.Domain (Metric models), xUnit & FluentAssertions (testing)  
**Storage**: Redis (ephemeral, short-lived baseline caching; TTL and eviction policies as operational concerns)  
**Testing**: xUnit with determinism harness for 1000+ reproducibility runs; parameterized tolerance/confidence scenarios  
**Target Platform**: .NET 10 runtime (.NET Standard 2.1 compatible)  
**Project Type**: Single domain library (Clean Architecture with layered structure)  
**Performance Goals**: Compare metrics against baseline + produce result in <20ms; support 100+ concurrent comparisons  
**Constraints**: Deterministic byte-identical output; immutable baselines after creation; graceful handling of expired baseline data  
**Scale/Scope**: Foundation domain supporting regression detection in batch CI/CD contexts; estimated 2500-3500 LOC core domain  

---

## Constitution Check

✅ **Specification-Driven Development**:
- Feature defined through explicit specification (baseline-domain.spec.md)
- Specification version-controlled and precedes all implementation
- All comparison semantics and tolerance logic derived from specification

✅ **Domain-Driven Design**:
- Domain models (Baseline, Comparison, ComparisonResult, Tolerance, ConfidenceLevel) independent of Redis, persistence, CI/CD integration
- Core logic in ubiquitous language: compare current metrics deterministically against baseline immutable snapshot
- Repository pattern isolates persistence concerns (Redis) from domain logic

✅ **Clean Architecture**:
- Dependencies flow inward: Domain ← Application (use cases) ← Adapters (Redis) ← Infrastructure
- BaselineRepository port abstracts Redis; infrastructure layer implements repository
- No Redis imports in domain/application layers; storage is swappable infrastructure concern
- No domain logic embedded in Redis adapter

✅ **Layered Phase Independence**:
- Clear boundaries: Domain (comparison logic) → Application (baseline orchestration) → Ports (repository abstraction) → Adapters (Redis)
- Phases communicate through serializable DTOs and domain models
- Comparison logic independent of baseline storage mechanism (could be replaced: PostgreSQL, file store, etc.)

✅ **Determinism & Reproducibility**:
- Identical baseline + current metrics → byte-identical comparison result every time
- Tolerance calculation fully deterministic (no floating-point ambiguity mitigated by explicit precision rules)
- Confidence calculation deterministic based on comparison magnitude
- No timestamps, randomness, or concurrent ordering ambiguity in comparison logic

✅ **Engine-Agnostic Abstraction**:
- Comparison logic operates on domain Metric interface, not engine-specific formats
- Baseline comparison works with metrics from K6, JMeter, Gatling, or any conforming engine
- No execution engine data structures or APIs leak into domain

✅ **Evolution-Friendly Design**:
- No specific technology locked (C# version, Redis, database are plan-level decisions)
- Tolerance strategies extend via strategy pattern without modifying core comparison logic
- Future integration with profiling, trending, or statistical models as separate bounded contexts
- Baseline immutability enforced at domain level; versioning handled as separate feature

---

## Project Structure

### Documentation (this feature)

```text
specs/baseline-domain/
├── baseline-domain.spec.md  # Feature specification
├── plan.md                  # This file (implementation plan)
├── research.md              # Phase 0: Technology decisions (Redis, .NET patterns)
├── data-model.md            # Phase 1: Domain entities, value objects, services
├── quickstart.md            # Phase 1: Developer quick start guide
├── contracts/               # Phase 1: Domain-level interface contracts
│   ├── baseline-interface.md
│   ├── comparison-interface.md
│   ├── repository-port.md
│   └── tolerance-config.md
└── checklists/
    └── requirements.md      # Quality validation checklist
```

### Source Code (repository root)

```text
src/
├── PerformanceEngine.Metrics.Domain/       # Existing: foundation domain
├── PerformanceEngine.Baseline.Domain/      # NEW: baseline domain
│   ├── Domain/
│   │   ├── Baselines/
│   │   │   ├── Baseline.cs                 # Immutable snapshot aggregate root
│   │   │   ├── BaselineId.cs               # Value object: baseline identifier
│   │   │   ├── BaselineSnapshot.cs         # Immutable metrics + evaluation capture
│   │   │   └── BaselineInvariants.cs       # Enforces immutability contract
│   │   │
│   │   ├── Comparisons/
│   │   │   ├── Comparison.cs               # Pure comparison operation (no state)
│   │   │   ├── ComparisonRequest.cs        # Input: current metrics + configuration
│   │   │   ├── ComparisonResult.cs         # Immutable result entity
│   │   │   ├── ComparisonMetric.cs         # Per-metric comparison details
│   │   │   ├── ComparisonOutcome.cs        # Enum: IMPROVEMENT, REGRESSION, NO_SIGNIFICANT_CHANGE, INCONCLUSIVE
│   │   │   └── ComparisonCalculator.cs     # Domain service: pure comparison logic
│   │   │
│   │   ├── Tolerances/
│   │   │   ├── Tolerance.cs                # Value object: tolerance configuration
│   │   │   ├── ToleranceType.cs            # Enum: RELATIVE, ABSOLUTE
│   │   │   ├── ToleranceRule.cs            # Evaluates if change within tolerance
│   │   │   └── ToleranceValidation.cs      # Validates tolerance constraints (non-negative)
│   │   │
│   │   ├── Confidence/
│   │   │   ├── ConfidenceLevel.cs          # Value object: [0.0, 1.0]
│   │   │   ├── ConfidenceCalculator.cs     # Domain service: determines certainty
│   │   │   └── ConfidenceThreshold.cs      # Minimum threshold for conclusive results
│   │   │
│   │   └── Events/
│   │       ├── BaselineCreatedEvent.cs     # Domain event: baseline snapshot captured
│   │       └── ComparisonPerformedEvent.cs # Domain event: comparison executed
│   │
│   ├── Application/
│   │   ├── Services/
│   │   │   ├── BaselineService.cs          # Application facade
│   │   │   └── ComparisonOrchestrator.cs   # Orchestrates baseline retrieval + comparison
│   │   │
│   │   ├── Dto/
│   │   │   ├── CreateBaselineRequestDto.cs
│   │   │   ├── BaselineDto.cs
│   │   │   ├── ComparisonRequestDto.cs
│   │   │   ├── ComparisonResultDto.cs
│   │   │   └── ComparisonMetricDto.cs
│   │   │
│   │   └── UseCases/
│   │       ├── CreateBaselineUseCase.cs
│   │       └── PerformComparisonUseCase.cs
│   │
│   └── Ports/
│       └── IBaselineRepository.cs          # Port: baseline persistence abstraction
│
├── PerformanceEngine.Baseline.Infrastructure/  # NEW: Redis adapter
│   ├── Persistence/
│   │   ├── RedisBaselineRepository.cs      # Redis implementation of IBaselineRepository
│   │   ├── BaselineRedisMapper.cs          # Serialization: domain ↔ Redis format
│   │   └── RedisConnectionFactory.cs       # Redis connection management
│   │
│   └── Configuration/
│       └── BaselineInfrastructureExtensions.cs # Dependency injection configuration
│
tests/
├── PerformanceEngine.Baseline.Domain.Tests/
│   ├── Domain/
│   │   ├── Baselines/
│   │   │   ├── BaselineTests.cs            # Aggregate behavior
│   │   │   └── ImmutabilityTests.cs        # Immutability enforcement
│   │   │
│   │   ├── Comparisons/
│   │   │   ├── ComparisonCalculatorTests.cs # Core comparison logic
│   │   │   ├── ComparisonOutcomeTests.cs    # Outcome state transitions
│   │   │   ├── DeterminismTests.cs          # 1000 consecutive runs identical
│   │   │   ├── MultiMetricComparisonTests.cs
│   │   │   └── EdgeCaseTests.cs             # Missing metrics, null values, etc.
│   │   │
│   │   ├── Tolerances/
│   │   │   ├── RelativeToleranceTests.cs
│   │   │   ├── AbsoluteToleranceTests.cs
│   │   │   └── ToleranceValidationTests.cs
│   │   │
│   │   └── Confidence/
│   │       ├── ConfidenceLevelTests.cs
│   │       ├── InconclusiveThresholdTests.cs
│   │       └── ConfidenceRangeTests.cs
│   │
│   └── Integration/
│       ├── ComparisonWorkflowTests.cs      # End-to-end comparison
│       └── MetricsDomainIntegrationTests.cs
│
├── PerformanceEngine.Baseline.Infrastructure.Tests/
│   ├── RedisBaselineRepositoryTests.cs
│   ├── SerializationTests.cs               # Ensure round-trip fidelity
│   └── ConcurrencyTests.cs                 # Concurrent baseline access
```

**Structure Decision**: Clean Architecture with Domain-Driven Design. Single domain library (PerformanceEngine.Baseline.Domain) with separate infrastructure layer (PerformanceEngine.Baseline.Infrastructure) for Redis adapter. Domain isolated from persistence; Repository port abstracts Redis implementation. Tests mirror source structure with determinism and edge-case harness for reproducibility verification.

---

## Architecture Overview

### Layering & Dependency Flow

```
┌────────────────────────────────────────────┐
│       APPLICATION                           │
│  BaselineService → UseCases → DTOs          │
│  ComparisonOrchestrator                     │
└────────────────────┬───────────────────────┘
                     ↓
┌────────────────────────────────────────────┐
│       DOMAIN                                │
│  Baseline (aggregate root)                  │
│  Comparison → ComparisonResult              │
│  Tolerance → ToleranceRule                  │
│  Confidence → ConfidenceCalculator          │
│  ComparisonCalculator (pure logic)          │
└────────────────────┬───────────────────────┘
                     ↑
        ┌────────────┴───────────┐
        ↓                         ↓
┌──────────────────┐   ┌──────────────────────┐
│ Metrics Domain   │   │  Ports/              │
│ (input models)   │   │ IBaselineRepository  │
└──────────────────┘   └──────┬───────────────┘
                               ↓
                    ┌──────────────────────────┐
                    │  INFRASTRUCTURE          │
                    │  RedisBaselineRepository │
                    │  BaselineRedisMapper     │
                    │  (Redis implementation)  │
                    └──────────────────────────┘
```

### Core Concepts & Responsibilities

**Baseline Aggregate Root**: Immutable snapshot of metrics + evaluation results + tolerance configuration captured at a point in time. Once created, baseline is read-only; updates create new baselines, not modifications.

**ComparisonRequest**: Input contract specifying:
- Target baseline ID (identifies which baseline to compare against)
- Current metrics (results from latest test execution)
- Tolerance configuration (per-metric rules for acceptable variance)
- Confidence threshold (minimum certainty required for conclusive outcome)

**ComparisonResult**: Immutable outcome containing:
- Overall comparison outcome (IMPROVEMENT/REGRESSION/NO_SIGNIFICANT_CHANGE/INCONCLUSIVE)
- Per-metric comparison details (baseline value, current value, change magnitude, metric-level outcome)
- Confidence level (0.0-1.0) indicating certainty in result
- Comparison timestamp

**Tolerance**: Configuration specifying acceptable variance:
- Type: RELATIVE (e.g., ±10%) or ABSOLUTE (e.g., ±50ms)
- Per-metric tolerance rules
- Validation: non-negative, reasonable bounds

**ConfidenceLevel**: [0.0, 1.0] calculated by ComparisonCalculator:
- Based on change magnitude relative to tolerance
- Higher confidence = larger deviation from tolerance
- Below threshold → result marked INCONCLUSIVE

**ComparisonCalculator**: Pure domain service performing core logic:
- Change magnitude calculation (absolute + relative)
- Tolerance evaluation (within or exceeding threshold)
- Confidence assessment (certainty calculation)
- Outcome classification (REGRESSION > IMPROVEMENT > NO_SIGNIFICANT_CHANGE > INCONCLUSIVE)
- Multi-metric aggregation (worst outcome wins)

---

## Technical Decisions & Rationale

### Technology Choices

**C# .NET 10**: 
- Rationale: Consistency with existing Metrics & Evaluation domains; .NET 10 LTS provides long-term support for production workloads; strong type system enables domain-driven design with value objects and domain events
- Fits: Deterministic language semantics; immutable reference types; strong decimal precision for comparison calculations

**Redis for Ephemeral Storage**:
- Rationale: Baselines are short-lived (duration of CI/CD pipeline cycle); Redis provides fast access for frequent comparisons; no need for long-term audit/retention (out of scope)
- Fits: Comparison latency <20ms target; TTL policies managed as operational concern; simplicity over persistence complexity
- Trade-off: Not suitable for historical analysis (intentionally deferred); long-term archival handled by separate historical domain

**Repository Pattern (IBaselineRepository port)**:
- Rationale: Isolates domain from storage mechanism; enables testing via in-memory implementations; Redis remains swappable infrastructure decision
- Fits: Clean architecture dependency inversion; facilitates unit testing without Redis; future migration to PostgreSQL/other stores transparent to domain

### Design Patterns

**Domain Events** (BaselineCreatedEvent, ComparisonPerformedEvent):
- Enables event-driven integration with Reporting/Analytics domains
- Deferred: Event store/event sourcing beyond scope; simple domain events sufficient for Phase 1

**Strategy Pattern for Tolerance**:
- ToleranceRule interface allows custom tolerance implementations (weighted, statistical, etc.)
- Extensibility without modifying core ComparisonCalculator

**Value Objects** (Tolerance, ConfidenceLevel, BaselineId):
- Encapsulate invariants (e.g., confidence ∈ [0.0, 1.0])
- Type safety: prevents mixing baseline IDs across comparisons

### Immutability Constraints

**Baseline Immutability**:
- Enforced at domain layer: Baseline entity has no setters; snapshot captured at creation
- Redis adapter: Read-only from consumer perspective; create operations append new baseline versions
- Invariant: Baseline timestamp immutable; no "update baseline" operation exists

**ComparisonResult Immutability**:
- Result constructed once; outcome and metrics frozen
- Enables safe passing across layer boundaries without defensive copying

---

## Interfaces & Contracts (Ports)

### IBaselineRepository (Domain Port)

Abstraction for baseline persistence:

```csharp
interface IBaselineRepository
{
    // Store new baseline snapshot
    Task<BaselineId> CreateAsync(Baseline baseline, CancellationToken cancellationToken);
    
    // Retrieve baseline by ID (may be expired/missing)
    Task<Baseline?> GetByIdAsync(BaselineId id, CancellationToken cancellationToken);
    
    // List baselines (optional, for CI/CD dashboard queries)
    Task<IReadOnlyList<Baseline>> ListRecentAsync(int count, CancellationToken cancellationToken);
}
```

- **Semantics**: Repository returns null if baseline expired/missing; consumer must handle gracefully (cannot compare without baseline)
- **Error Handling**: Exceptions thrown for infrastructure failures (connection lost, etc.); consumer decides recovery strategy
- **Concurrency**: Multiple concurrent reads allowed; creates serializable (single writer, multiple readers pattern via Redis transactions)

### Comparison Service Interface (Application)

```csharp
interface IComparisonService
{
    // Perform deterministic comparison
    Task<ComparisonResult> CompareAsync(
        ComparisonRequest request, 
        CancellationToken cancellationToken);
}
```

- **Contract**: Given identical ComparisonRequest, returns identical ComparisonResult
- **Error Handling**: 
  - Baseline not found → throws BaselineNotFoundException (consumer decides recovery)
  - Invalid tolerance → throws ToleranceValidationException
  - No infrastructure errors leak into domain

---

## Cross-Cutting Constraints

### Determinism Enforcement

1. **Reproducibility Harness**: Test suite includes 1000-run determinism verification
   - Same baseline + metrics → identical result 1000 times
   - Validates no floating-point ambiguity, ordering issues, or timing dependencies

2. **Precision Rules**:
   - Relative change calculated with decimal (not double) for precision
   - Comparison outcome determined by rules, never approximate/heuristic logic
   - No concurrent ordering affecting outcome (aggregation commutative)

3. **No Non-Deterministic Sources**:
   - Timestamps: Recorded by infrastructure, not domain
   - Random numbers: None used in comparison logic
   - Concurrent ordering: Comparison logic independent of execution order

### Immutability Guarantees

1. **Baseline Protection**:
   - Domain: Baseline aggregate has no setters; snapshot captured at construction
   - Infrastructure: Redis adapter enforces read-only semantics; TTL for expiration, not modification
   - Invariant enforcement: BaselineInvariants class validates immutability constraints

2. **Result Protection**:
   - ComparisonResult frozen after creation; no post-facto modification
   - Enables safe sharing across trust boundaries

### Missing/Expired Baseline Handling

1. **Design Principle**: Graceful degradation, explicit error reporting
   - Baseline expired → Repository.GetByIdAsync returns null
   - Consumer decides recovery: retry with new baseline, report to CI/CD, etc.
   - Exception semantics: Only for unexpected infrastructure failures

2. **Error Categories**:
   - **Expected**: Baseline expired (TTL) → return null, consumer handles
   - **Unexpected**: Redis connection failure → throw exception, observe/alert
   - **Validation**: Invalid tolerance → throw ValidationException immediately

---

## Non-Goals, Assumptions & Open Questions

### Intentional Non-Goals (Out of Scope)

- ❌ **Long-term baseline history**: Baselines discarded after TTL; historical analysis deferred to separate Analytics domain
- ❌ **Statistical modeling**: Initial implementation uses deterministic threshold comparison; advanced statistics (hypothesis testing, Bayesian confidence) deferred
- ❌ **Baseline versioning strategy**: "Which baseline is authoritative?" is organizational/CI/CD policy question, not domain problem
- ❌ **Reporting/visualization**: Baseline comparison results feed to other systems; no domain-level reporting
- ❌ **CI/CD exit codes**: Integration with build systems (make decision "pass" vs "fail") deferred; domain only provides ComparisonResult

### Key Assumptions

1. **Metrics pre-normalized**: Baseline domain assumes input metrics already normalized (consistent units, valid ranges); Metrics Domain responsible
2. **Metric direction known**: System knows latency "lower is better" vs throughput "higher is better"; configured externally (not domain-level)
3. **Single comparison context**: Comparison is isolated; no trend analysis or historical correlation within baseline domain
4. **Baseline selection is external**: Organization chooses "which run becomes baseline"; baseline domain doesn't auto-promote baselines
5. **No statistical confidence inference**: Confidence calculated from comparison magnitude; Bayesian/statistical inference deferred

### Critical Open Questions (Pre-Implementation)

- **Question 1: Confidence calculation formula?**
  - Current spec: "Based on comparison magnitude and baseline variance"
  - Need to clarify: How does baseline variance factor in? (Not recorded in immutable snapshot)
  - Decision gate: Determines ConfidenceCalculator algorithm
  - **Proposed resolution**: Phase 0 Research phase

- **Question 2: Metric direction metadata?**
  - Current assumption: Caller knows "latency lower is good"
  - Need to clarify: Is this encoded in metric schema? Tolerance configuration? External config?
  - Decision gate: Determines ToleranceRule implementation
  - **Proposed resolution**: Clarify with Metrics Domain contract; Phase 1 Design

- **Question 3: Multi-metric outcome priority?**
  - Spec states: REGRESSION > IMPROVEMENT > NO_SIGNIFICANT_CHANGE > INCONCLUSIVE
  - Need to clarify: What if 50% metrics show improvement, 50% regression? Always worst outcome?
  - Decision gate: Determines ComparisonCalculator.AggregateOutcome logic
  - **Proposed resolution**: Phase 1 Design review; clarify with stakeholders

- **Question 4: Baseline TTL policy?**
  - Current assumption: Redis TTL is operational (not domain concern)
  - Need to clarify: Who configures TTL? Default? Min/max? Metric-specific?
  - Decision gate: Redis adapter configuration; doesn't affect domain
  - **Proposed resolution**: Phase 1 Quickstart; infrastructure documentation

- **Question 5: Concurrent baseline modification?**
  - Current: Baselines immutable; no updates
  - Need to clarify: If new baseline created while old one in use, can they coexist? Version pinning needed?
  - Decision gate: Determines BaselineId semantics and repository concurrency model
  - **Proposed resolution**: Phase 1 Design; clarify with CI/CD integration team

---

## Risk Assessment & Mitigation

### Technical Risks

| Risk | Impact | Mitigation |
|------|--------|-----------|
| **Confidence calculation ambiguity** | Comparison results unreliable if confidence formula undefined | Phase 0 Research clarifies formula; Design review gates implementation |
| **Floating-point precision in comparisons** | Determinism violated by rounding errors | Use decimal type; precision rules documented; 1000-run determinism tests |
| **Redis connection latency** | Comparison exceeds <20ms target if baseline retrieval slow | Redis caching strategy; local in-memory fallback tested; performance profiling phase |
| **Expired baseline during comparison** | Comparison fails if baseline TTL expires mid-operation | Graceful null handling; explicit error reporting; consumer recovery documented |
| **Metric schema mismatch** | Baseline metrics don't align with current metrics | Validation in comparison logic; clear error messages; version compatibility phase |

### Schedule Risks

| Risk | Mitigation |
|------|-----------|
| **Unclear confidence semantics** | Phase 0 Research front-loaded; design review before implementation |
| **Redis adapter underestimation** | Implement adapter in parallel during Phase 1; spike testing concurrency patterns |
| **Multi-metric aggregation complexity** | Prototype aggregation logic early; test suite for all combinations |

---

## Success Metrics (Implementation Validation)

- ✅ **Determinism**: 1000 consecutive comparisons (baseline + metrics unchanged) produce byte-identical results
- ✅ **Immutability**: Attempt to modify baseline raises BaselineImmutableException
- ✅ **Latency**: Comparison (baseline retrieval + calculation) completes in <20ms
- ✅ **Tolerance coverage**: All tolerance types (relative, absolute) validated with edge cases (0%, 100%, negative rejection)
- ✅ **Confidence range**: ConfidenceLevel always ∈ [0.0, 1.0]; never exceeds bounds
- ✅ **Multi-metric aggregation**: Outcome priority (REGRESSION > IMPROVEMENT > ...) verified for all combinations
- ✅ **Edge case handling**: Missing metrics, null values, expired baselines all handled with clear errors
- ✅ **Redis integration**: Concurrent reads, TTL expiration, serialization round-trip verified

---

## Next Steps

1. **Phase 0 Research** (days 1-2):
   - Confidence calculation formula research
   - Metric direction metadata design
   - Outcome aggregation strategy documentation
   - Redis performance baseline (target <20ms)

2. **Phase 1 Design** (days 3-5):
   - data-model.md: Complete domain entity definitions
   - contracts/: Domain-level interface specifications
   - quickstart.md: Developer setup and usage examples
   - Infrastructure design: Redis adapter architecture

3. **Phase 2+ Implementation** (days 6+):
   - Domain layer: Baseline, Comparison, Tolerance, Confidence
   - Application layer: Services, DTOs, UseCases
   - Infrastructure: Redis adapter, connection management
   - Test harness: Determinism, edge cases, integration tests
   - Task breakdown generated from this plan
