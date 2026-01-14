# Implementation Plan: Metrics Domain - Ubiquitous Language

**Branch**: `metrics-domain` | **Date**: 2026-01-14 | **Spec**: [specs/metrics-domain.spec.md](metrics-domain.spec.md)  
**Input**: Feature specification from `/specs/metrics-domain/spec.md`

## Summary

Establish the foundational metrics domain with ubiquitous language and DDD-compliant models using C# and .NET 10. This creates the domain layer that all system components (execution engines, evaluation logic, persistence) adapt to, establishing clear boundaries between domain logic and infrastructure. The design prioritizes determinism, reproducibility, and clean architecture with zero infrastructure dependencies in the domain core.

## Technical Context

**Language/Version**: C# 13 (.NET 10 LTS)  
**Primary Dependencies**: 
- .NET 10 base libraries
- xUnit 2.8+ (testing framework)
- FluentAssertions (test readability)
- Polly 8+ (resilience patterns, future integration)

**Storage**: N/A (domain layer has no direct persistence responsibility; repositories abstracted via ports)  
**Testing**: xUnit (unit tests), custom test doubles for adapters  
**Target Platform**: Linux server (cross-platform via .NET 10), container-ready  
**Project Type**: Single library package (foundational domain, no frontend)  
**Performance Goals**: Aggregation operations complete in <1ms for 1M samples; no GC pauses during deterministic computation  
**Constraints**: 
- Zero non-deterministic operations in domain (all randomness, timestamps external via ports)
- Immutable samples after creation
- All aggregation operations must be testable without I/O
- Thread-safe sample collection without locks (append-only immutable structures)

**Scale/Scope**: 
- Support 100+ metric types through extensible value-object pattern
- Handle aggregations over 1M+ samples efficiently
- Support 10+ aggregation window types without new code changes

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

**Specification-Driven Development**: ✅
- [x] Feature defined through explicit specification (specs/metrics-domain.spec.md with 12 FRs, 4 success criteria)
- [x] Specification version-controlled and precedes all implementation
- [x] Implementation will be derived from domain concepts, not externally sourced

**Domain-Driven Design**: ✅
- [x] Domain models (Sample, Metric, Latency, Percentile, etc.) independent of infrastructure
- [x] Core logic expressed in ubiquitous language; no C#-specific terminology in domain
- [x] Persistence via repositories pattern (abstraction layer only in plan; implementation deferred)
- [x] All adapters map external data INTO domain, never consuming engine/persistence specifics

**Clean Architecture**: ✅
- [x] Dependency direction: Infrastructure adapters → Ports → Application → Domain (inward)
- [x] External systems (engines, databases) access domain only through ports
- [x] Domain layer imports nothing from infrastructure or framework (pure C# core library only)
- [x] Application layer orchestrates domain logic without infrastructure specifics

**Layered Phase Independence**: ✅
- [x] Clear phase boundaries: Domain (metrics) → Application (use cases: compute, normalize) → Adapters (engine mappings)
- [x] Phases communicate via serializable interfaces (domain value objects, port contracts)
- [x] Engine adapter changes don't affect domain; evaluation changes don't affect engines

**Determinism & Reproducibility**: ✅
- [x] Identical samples + aggregation spec = byte-identical results (all operations pure functions)
- [x] No DateTime.Now, Guid.NewGuid, or randomness in domain
- [x] All inputs explicitly versioned (aggregation spec, sample normalization rules)
- [x] Immutable samples ensure reproducibility across runs

**Engine-Agnostic Abstraction**: ✅
- [x] Results normalized into domain models (no engine-specific types exposed)
- [x] Evaluation logic operates on domain Metric objects; engine API details hidden in adapters
- [x] No JMeter, k6, Gatling APIs or data structures in domain or application layers

**Evolution-Friendly Design**: ✅
- [x] C# and .NET 10 chosen as implementation technology (not locked at constitution level)
- [x] Metric types extensible via value-object pattern without modifying core
- [x] Aggregation operations extensible via strategy pattern
- [x] Error classifications extensible with Unknown fallback always present

## Project Structure

### Documentation

```text
specs/metrics-domain/
├── spec.md                      # Feature specification (✅ complete)
├── plan.md                      # This file (/speckit.plan output)
├── research.md                  # Phase 0 output (to be generated)
├── data-model.md                # Phase 1 output (to be generated)
├── contracts/                   # Phase 1 output (to be generated)
│   ├── domain-model.md          # Domain entity contracts
│   ├── ports.md                 # Port/adapter contracts
│   └── aggregation-contracts.md # Aggregation operation contracts
├── quickstart.md                # Phase 1 output (to be generated)
└── checklists/
    └── metrics-domain-requirements.md  # (✅ complete)
```

### Source Code

```text
src/
├── Domain/
│   ├── Metrics/
│   │   ├── Sample.cs                      # Core domain entity
│   │   ├── SampleCollection.cs            # Immutable sample container
│   │   ├── Metric.cs                      # Aggregated metric
│   │   ├── MetricValue.cs                 # Quantitative value (double + unit)
│   │   ├── Latency.cs                     # Time measurement value object
│   │   ├── LatencyUnit.cs                 # Flexible time units (enum)
│   │   ├── Percentile.cs                  # Distribution position value object
│   │   ├── AggregationWindow.cs           # Temporal grouping logic
│   │   ├── ErrorClassification.cs         # Error type enumeration
│   │   └── AggregationResult.cs           # Aggregation operation output
│   ├── Aggregations/
│   │   ├── IAggregationOperation.cs       # Domain interface (not a port; internal contract)
│   │   ├── AverageAggregation.cs
│   │   ├── MaxAggregation.cs
│   │   ├── MinAggregation.cs
│   │   ├── PercentileAggregation.cs
│   │   └── AggregationNormalizer.cs       # Sample normalization before aggregation
│   └── Events/
│       ├── SampleCollectedEvent.cs        # Domain event
│       ├── MetricComputedEvent.cs
│       └── IDomainEvent.cs
│
├── Application/
│   ├── UseCases/
│   │   ├── ComputeMetricUseCase.cs        # Orchestrate: collect samples → normalize → aggregate
│   │   ├── NormalizeSamplesUseCase.cs     # Ensure consistent units, valid ranges
│   │   └── ValidateAggregationUseCase.cs  # Verify constraints before aggregation
│   ├── Dto/
│   │   ├── SampleDto.cs                   # Application layer transfer object
│   │   ├── MetricDto.cs
│   │   └── AggregationRequestDto.cs
│   └── Services/
│       └── MetricService.cs               # Orchestrator (application-level)
│
├── Ports/
│   ├── IPersistenceRepository.cs          # Abstract repository (no implementation here)
│   ├── IMetricCache.cs                    # Optional caching abstraction
│   ├── IExecutionEngineAdapter.cs         # Abstract engine result mapper
│   └── IIntegrationPublisher.cs           # Optional event publishing
│
└── Infrastructure/
    ├── Adapters/
    │   ├── K6EngineAdapter.cs             # Engine-specific mapping to domain
    │   ├── JMeterEngineAdapter.cs
    │   └── GenericEngineAdapter.cs        # Template for new engines
    ├── Persistence/
    │   ├── InMemoryRepository.cs          # Test/demo implementation
    │   └── [Database adapters TBD]
    └── Integration/
        └── [Event publishers TBD]

tests/
├── Domain.UnitTests/
│   ├── Metrics/
│   │   ├── SampleTests.cs                 # Immutability, invariant validation
│   │   ├── LatencyTests.cs                # Unit conversions, boundaries
│   │   ├── PercentileTests.cs             # Range validation, semantics
│   │   └── ErrorClassificationTests.cs    # Classification types
│   └── Aggregations/
│       ├── AverageAggregationTests.cs
│       ├── PercentileAggregationTests.cs
│       ├── DeterminismTests.cs            # Reproducibility verification
│       └── NormalizationTests.cs
│
├── Domain.ContractTests/
│   ├── AggregationContracts.cs            # Behavioral contracts for aggregations
│   ├── SampleInvariants.cs                # Domain constraints validation
│   └── DeterminismContract.cs             # Identical input → identical output
│
├── Application.IntegrationTests/
│   ├── MetricServiceTests.cs              # End-to-end domain + application
│   ├── UseCaseTests.cs
│   └── NormalizationIntegrationTests.cs
│
└── Adapters.ContractTests/
    ├── K6AdapterTests.cs                  # Engine adapter correctness
    ├── JMeterAdapterTests.cs
    └── EngineAdapterContract.cs           # Verify all engines map to domain correctly
```

## High-Level Architecture

### Layering (Clean Architecture)

```
┌─────────────────────────────────────────────────────────────┐
│                    INFRASTRUCTURE                            │
│  ┌──────────────┐ ┌──────────────┐ ┌─────────────────────┐  │
│  │  K6 Adapter  │ │ JMeter       │ │ Persistence         │  │
│  │  (engine     │ │ Adapter      │ │ Adapters (DB/file)  │  │
│  │   mapping)   │ │ (engine      │ │                     │  │
│  │              │ │  mapping)    │ │ Event Publishers    │  │
│  └──────────────┘ └──────────────┘ └─────────────────────┘  │
│                    ↓ ↓ ↓ (implements)                         │
├─────────────────────────────────────────────────────────────┤
│                        PORTS                                 │
│  ┌─────────────────────────────────────────────────────┐   │
│  │ IExecutionEngineAdapter  (engine results → domain)  │   │
│  │ IPersistenceRepository   (domain → storage)         │   │
│  │ IIntegrationPublisher    (domain events → systems)  │   │
│  └─────────────────────────────────────────────────────┘   │
│                    ↑ ↑ ↑ (used by)                           │
├─────────────────────────────────────────────────────────────┤
│                    APPLICATION                              │
│  ┌──────────────────────────────────────────────────────┐  │
│  │ Use Cases (ComputeMetricUseCase, etc.)              │  │
│  │ Services (MetricService orchestration)              │  │
│  │ DTOs (data transfer objects)                        │  │
│  └──────────────────────────────────────────────────────┘  │
│                    ↑ ↑ ↑ (uses)                              │
├─────────────────────────────────────────────────────────────┤
│                      DOMAIN                                 │
│  ┌──────────────────────────────────────────────────────┐  │
│  │ Sample, Metric, Latency, Percentile (entities)      │  │
│  │ AggregationWindow, ErrorClassification (values)     │  │
│  │ IAggregationOperation (internal contract only)      │  │
│  │ Domain Events (SampleCollectedEvent, etc.)          │  │
│  │ Pure domain logic, zero external dependencies       │  │
│  └──────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────┘

Dependencies ALWAYS point inward (down).
Domain has ZERO imports of infrastructure or application code.
```

### Communication Patterns

**Domain → Application**: Value objects (Sample, Metric) passed as-is  
**Application → Adapters**: Via ports (interfaces); never expose domain directly  
**Adapters → Domain**: Engine results mapped to domain value objects via ports  
**Cross-cutting**: Domain events propagate via application-managed event bus (not infrastructure-coupled)

## Domain Concepts → Implementation Mapping

### Core Entities

| Domain Concept | C# Implementation | Responsibility | Constraints |
|---|---|---|---|
| **Sample** | `Sample` class | Immutable record of single observation | Timestamp, Duration, Status (success/failure), optional ErrorClassification; no setters after construction |
| **SampleCollection** | `SampleCollection` sealed class | Container for append-only sample sequence | Iteration without mutation; snapshot consistency |
| **Metric** | `Metric` class | Aggregated samples with scope | Links to sample collection, aggregation window, metric type; never exists without samples |
| **Latency** | `Latency` value object | Time measurement | Supports flexible units (ms, ns, s); consistent within metric scope; ≥ 0 |
| **LatencyUnit** | `LatencyUnit` enum | Unit designation | Options: Nanoseconds, Microseconds, Milliseconds, Seconds; conversion rules explicit |
| **Percentile** | `Percentile` value object | Distribution position | Value ∈ [0, 100]; semantic meaning (p50=median, p95=95th percentile) |
| **AggregationWindow** | `AggregationWindow` sealed class | Temporal grouping spec | Types: FullExecution, SlidingWindow, FixedWindow; no ambiguous overlaps |
| **ErrorClassification** | `ErrorClassification` enum | Domain-level error type | Values: Timeout, NetworkError, ApplicationError, UnknownError; required, no null |

### Value Objects vs Entities

**Entities** (identity-based equality):
- `Sample`: Identity by timestamp + execution context (immutable identity)
- `Metric`: Identity by window + type + aggregation spec

**Value Objects** (equality by value):
- `Latency`: Equality by numeric value + unit
- `Percentile`: Equality by value (0-100)
- `AggregationWindow`: Equality by specification
- `ErrorClassification`: Equality by enumeration value

### Aggregation Operations

| Operation | Input | Output | Properties |
|---|---|---|---|
| **Average** | `SampleCollection` (Latency values) | `AggregationResult` (single Latency value) | Pure function; deterministic with normalization |
| **Max** | `SampleCollection` | `AggregationResult` | Pure function; no state mutations |
| **Min** | `SampleCollection` | `AggregationResult` | Pure function; no state mutations |
| **Percentile** | `SampleCollection` + `Percentile` spec | `AggregationResult` | Pure function; reproducible across runs |

All operations:
- Accept only normalized samples (consistent units, valid ranges)
- Produce byte-identical results given identical input
- Never mutate underlying samples
- Composable (output of one can feed another if semantically valid)

## Required Ports (Infrastructure Abstractions)

### Port: IExecutionEngineAdapter

```csharp
namespace Domain.Ports;

/// <summary>
/// Abstraction for engine-specific result mapping.
/// Adapters implement this to normalize engine outputs into domain models.
/// </summary>
public interface IExecutionEngineAdapter
{
    /// <summary>
    /// Map engine-specific result format to domain Sample objects.
    /// Responsibility: handle unit conversion, error classification, timestamp normalization.
    /// </summary>
    Task<IEnumerable<Sample>> MapResultsToSamplesAsync(string engineResultData);
    
    /// <summary>
    /// Verify adapter can handle the given engine result format.
    /// </summary>
    bool CanHandle(string engineFormat);
}
```

**Implementations** (infrastructure layer):
- `K6EngineAdapter`: Maps k6 JSON summary format to samples
- `JMeterEngineAdapter`: Maps JMeter JTL format to samples
- `GenericEngineAdapter`: Template for new engines

**Design note**: Adapters are "inbound adapters" (translation layer); domain knows nothing of engine specifics.

### Port: IPersistenceRepository

```csharp
namespace Domain.Ports;

/// <summary>
/// Abstraction for metrics storage.
/// No persistence implementation in domain; only interface.
/// </summary>
public interface IPersistenceRepository
{
    Task SaveMetricsAsync(IEnumerable<Metric> metrics);
    Task<IEnumerable<Metric>> QueryMetricsByWindowAsync(AggregationWindow window);
    Task<IEnumerable<Sample>> QuerySamplesByMetricAsync(Guid metricId);
}
```

**Implementations** (infrastructure layer):
- `InMemoryRepository`: For testing and demos
- Database-specific adapters (SQL, NoSQL) - deferred to phase 2

### Port: IIntegrationPublisher

```csharp
namespace Domain.Ports;

/// <summary>
/// Abstraction for propagating domain events to external systems.
/// </summary>
public interface IIntegrationPublisher
{
    Task PublishAsync(IDomainEvent @event);
}
```

**Events** (domain):
- `SampleCollectedEvent`: When sample is created (for traceability)
- `MetricComputedEvent`: When metric is aggregated (for downstream systems)

**Implementations** (infrastructure): Message bus adapters, webhooks, etc. (deferred)

## Cross-Cutting Constraints

### Determinism & Reproducibility

**Principle**: Given identical samples and aggregation specification, results must be identical bytes.

**Implementation approach**:
1. All domain operations are pure functions (no side effects)
2. Sample normalization explicitly specified (unit conversion rules, precision handling)
3. Aggregation algorithms deterministic (no floating-point rounding variance if possible; document precision semantics)
4. No randomness, timestamps, or I/O in domain layer
5. Unit tests verify: `Aggregate(samples, spec) == Aggregate(samples, spec)` (exact equality)

**Test coverage**:
- `DeterminismTests.cs`: Run aggregation 1000 times, verify byte-identical results
- `PercentileAggregationTests.cs`: Verify reproducibility across different input orderings
- `NormalizationTests.cs`: Verify unit conversion produces identical results

### Traceability & Auditability

**Principle**: Every metric computation can be traced to source samples, aggregation spec, and timestamp.

**Implementation approach**:
1. `Metric` includes immutable reference to source `SampleCollection`
2. `Metric` includes aggregation specification details
3. `Sample` includes context (engine name, execution ID)
4. Domain events (SampleCollectedEvent, MetricComputedEvent) logged for audit trail
5. Adapters preserve engine identifiers in samples for lineage tracking

**Example**: `Metric.AuditTrail()` returns human-readable breakdown of computation.

### Testability

**Principle**: Domain logic testable without I/O, external services, or timing dependencies.

**Implementation approach**:
1. Domain layer: Pure .NET Core library (no external dependencies)
2. Unit tests: Instantiate domain objects directly; no mocks needed for domain logic
3. Contract tests: Validate aggregation and normalization contracts via property-based testing
4. Integration tests: Application layer orchestration with test doubles for ports
5. Adapter tests: Verify each engine adapter correctly maps to domain (test doubles for engine output)

**Test doubles pattern**:
- Create `TestEngineAdapter` that returns fixed, reproducible samples
- Create `TestRepository` (in-memory) for persistence testing
- No real I/O in domain or application tests

## Architecture Decision Records (ADRs)

### ADR-001: Value Objects for Measurements

**Decision**: Use value objects (Latency, Percentile) with unit specification, not primitive doubles.

**Rationale**:
- Prevent unit mix-up (e.g., aggregating milliseconds + nanoseconds)
- Enforce invariants (percentile ∈ [0,100], latency ≥ 0) at type level
- Support multiple time units without locked choice
- Increase code readability and intent clarity

**Implications**:
- Slightly more verbose code (e.g., `new Latency(100, LatencyUnit.Milliseconds)` vs `100.0`)
- Better compile-time correctness; fewer unit conversion bugs
- Extensibility: adding new units doesn't require core logic changes

### ADR-002: Immutable Samples

**Decision**: `Sample` objects are fully immutable after construction (no setters).

**Rationale**:
- Supports determinism (no surprise mutations during aggregation)
- Enables safe sharing between threads without locks
- Aligns with functional programming paradigm for aggregation operations
- Snapshot consistency for audit trail

**Implications**:
- Sample creation requires all fields upfront
- Corrections require creating new samples, not mutating existing ones
- Builder pattern for convenience in complex scenarios

### ADR-003: Ports for External Dependencies

**Decision**: Infrastructure components accessed only through port interfaces (not directly).

**Rationale**:
- Domain and application independent of specific adapters
- Adapters (engines, databases) interchangeable without code changes
- Clean architecture dependency inversion
- Testability via test doubles

**Implications**:
- Adapter implementations deferred (not part of domain planning)
- Ports defined in application or domain layer, implemented in infrastructure
- Integration tests verify adapter contracts

### ADR-004: No Direct Persistence in Domain

**Decision**: Domain models have no `Save()` or database access methods.

**Rationale**:
- Persistence is infrastructure concern, not domain responsibility
- Supports multiple storage backends (SQL, NoSQL, files) simultaneously
- Enables stateless domain logic
- Clearer separation of concerns

**Implications**:
- Application layer orchestrates: domain logic → port call (repository)
- Repositories are infrastructure implementations
- Testing domain logic requires no database setup

## Non-Goals & Deferred Decisions

### Explicitly NOT in Scope (Phase 1)

1. **Persistence Implementation**: Repositories defined as ports; implementations deferred
2. **Engine Adapter Implementations**: K6, JMeter adapters are contract examples; actual adapters deferred
3. **Evaluation & Scoring Logic**: How metrics are judged (pass/fail) is separate feature
4. **Reporting & Visualization**: Output formats (JSON, CSV, dashboards) deferred
5. **Performance Optimization**: Caching, indexing, parallel aggregation deferred
6. **Multi-tenant Isolation**: Single-tenant assumed; multi-tenant patterns deferred
7. **Authentication & Authorization**: Access control deferred to integration phase

### Open Questions & Risks

**Q1: Floating-Point Precision in Percentile Calculation**
- **Issue**: Percentile algorithms (e.g., nearest-rank, linear interpolation) may produce different results due to floating-point rounding
- **Risk**: Could violate determinism constraint if implementation not careful
- **Resolution approach**: Define explicit precision semantics (e.g., "round to 3 decimal places"); document in data-model.md
- **Owner**: Phase 1 design; test-driven development will expose issues

**Q2: Sample Normalization Rules**
- **Issue**: Converting samples from different engines to domain units (e.g., ms → ns) requires explicit rules
- **Risk**: Lossy conversions or ambiguous semantics could cause data integrity issues
- **Resolution approach**: Define normalization algebra in contracts; verify bijective mappings
- **Owner**: Phase 1 contracts; implemented in adapters

**Q3: Thread Safety for Concurrent Sample Collection**
- **Issue**: Multiple engines may report samples concurrently; `SampleCollection` must be thread-safe
- **Risk**: Race conditions or lost samples if not carefully designed
- **Resolution approach**: Evaluate immutable collection patterns (e.g., `System.Collections.Immutable.ImmutableList<T>`); consider lock-free structures
- **Owner**: Phase 1 data model; benchmarks in phase 2

**Q4: Aggregation Result Composition**
- **Issue**: Can aggregation results be chained (e.g., max of percentiles)?
- **Risk**: Semantic ambiguity (e.g., "max of 3 p95 values" could be misleading)
- **Resolution approach**: Explicitly define which compositions are valid; document in contracts
- **Owner**: Phase 1 contracts

**Q5: Error Classification Extensibility**
- **Issue**: Domain specifies Timeout, NetworkError, ApplicationError, UnknownError; what about engine-specific error types?
- **Risk**: Adapters forced into Unknown category, losing information
- **Resolution approach**: Allow adapters to map to closest domain category with optional metadata; preserve engine error code in Sample context
- **Owner**: Phase 1 adapters; documented in ADR

**Q6: Metrics Retention Policy**
- **Issue**: When should old metrics be deleted or archived?
- **Risk**: Unbounded storage growth
- **Resolution approach**: Deferred to persistence layer; repository contracts include optional retention parameters
- **Owner**: Phase 2+ (infrastructure concern)

## Constitution Re-Check (Phase 1 Post-Design)

After design artifacts are generated, will re-validate:

- ✅ Domain models free of C#-specific implementation details (ready)
- ⚠️ Application use cases properly orchestrate domain without infrastructure bleeding (to verify in contracts)
- ⚠️ Port contracts clearly separate infrastructure concerns (to verify in contracts)
- ⚠️ Determinism semantics (floating-point precision) documented (open question Q1)
- ⚠️ Engine-agnostic abstraction (error classification extensibility) resolved (open question Q5)

## Next Phase Deliverables

**Phase 0 (Research)**: Generate `research.md` resolving open questions Q1-Q6

**Phase 1 (Design & Contracts)**:
- `data-model.md`: Complete domain model with class hierarchies, invariants, value object specifications
- `contracts/domain-model.md`: Domain entity contracts and aggregation semantics
- `contracts/ports.md`: Port interface specifications (adapters, repositories, publishers)
- `contracts/aggregation-contracts.md`: Aggregation operation behavioral contracts
- `quickstart.md`: Getting started guide for implementing adapters and using domain API

**Phase 2 (Task Breakdown)**:
- Implementation tasks per domain entity
- Adapter contract verification tasks
- Determinism verification task suite
- Integration test task suite
