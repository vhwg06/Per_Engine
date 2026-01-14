# PerformanceEngine.Metrics.Domain

**A Clean Architecture domain library for performance testing metrics**

[![Tests](https://img.shields.io/badge/tests-162%20passing-brightgreen)]()
[![.NET](https://img.shields.io/badge/.NET-8.0-blue)]()
[![Architecture](https://img.shields.io/badge/architecture-Clean%20Architecture-orange)]()
[![DDD](https://img.shields.io/badge/design-Domain%20Driven-purple)]()

---

## Overview

The **Metrics Domain** provides an **engine-agnostic**, **deterministic**, and **immutable** domain model for representing performance test results. It establishes a **ubiquitous language** that all system components reference, eliminating engine-specific terminology (JMeter, K6, Gatling) from your core business logic.

### Key Features

- ✅ **Engine-Agnostic**: Domain knows nothing about K6, JMeter, or Gatling specifics
- ✅ **Deterministic**: Identical inputs produce byte-identical outputs (reproducible CI/CD gates)
- ✅ **Immutable**: All domain entities immutable after construction
- ✅ **Clean Architecture**: Zero infrastructure dependencies in domain layer
- ✅ **DDD Patterns**: Value objects, entities, aggregates, domain events, ports
- ✅ **162 Tests**: Comprehensive coverage (unit, integration, contract tests)

---

## Quick Start

### Installation

```bash
# Clone repository
git clone <repository-url>
cd Per_Engine

# Build project
dotnet build src/PerformanceEngine.Metrics.Domain/

# Run tests
dotnet test
```

### Basic Usage

```csharp
using PerformanceEngine.Metrics.Domain.Domain.Metrics;
using PerformanceEngine.Metrics.Domain.Domain.Aggregations;
using PerformanceEngine.Metrics.Domain.Domain.Application.Services;
using PerformanceEngine.Metrics.Domain.Domain.Application.Dto;

// 1. Create samples (engine-agnostic)
var context = new ExecutionContext(
    engineName: "MyEngine",
    executionId: Guid.NewGuid(),
    scenarioName: "API Load Test"
);

var sample1 = new Sample(
    id: Guid.NewGuid(),
    timestamp: DateTime.UtcNow.AddSeconds(-10),
    duration: new Latency(45.5, LatencyUnit.Milliseconds),
    status: SampleStatus.Success,
    errorClassification: null,
    executionContext: context,
    metadata: new Dictionary<string, object> { ["http_status", 200] }
);

var sample2 = new Sample(
    id: Guid.NewGuid(),
    timestamp: DateTime.UtcNow.AddSeconds(-5),
    duration: new Latency(120.3, LatencyUnit.Milliseconds),
    status: SampleStatus.Success,
    errorClassification: null,
    executionContext: context,
    metadata: new Dictionary<string, object> { ["http_status", 200] }
);

var samples = SampleCollection.Empty
    .Add(sample1)
    .Add(sample2);

// 2. Compute metrics using the service
var metricService = new MetricService();

var request = new AggregationRequestDto
{
    Samples = samples.AllSamples.ToList(),
    Window = "FullExecution",
    AggregationOperation = "Average"
};

var metricDto = metricService.ComputeMetric(request);

// 3. Use results
Console.WriteLine($"Average Latency: {metricDto.AggregatedValues[0].Value} {metricDto.AggregatedValues[0].Unit}");
Console.WriteLine($"Computed At: {metricDto.ComputedAt}");
Console.WriteLine($"Sample Count: {metricDto.Samples.Count}");
```

---

## Architecture

### Clean Architecture Layers

```
┌─────────────────────────────────────────────────────────┐
│                    INFRASTRUCTURE                        │
│  ┌──────────────┐ ┌──────────────┐ ┌────────────────┐  │
│  │ K6 Adapter   │ │ JMeter       │ │ (Future: DB    │  │
│  │ (K6 results  │ │ Adapter      │ │  adapters)     │  │
│  │  → Samples)  │ │ (JMeter      │ │                │  │
│  │              │ │  → Samples)  │ │                │  │
│  └──────────────┘ └──────────────┘ └────────────────┘  │
│          ↓              ↓                                │
├─────────────────────────────────────────────────────────┤
│                        PORTS                             │
│  ┌────────────────────────────────────────────────┐    │
│  │ IExecutionEngineAdapter (maps engine → domain) │    │
│  │ IPersistenceRepository  (deferred)             │    │
│  └────────────────────────────────────────────────┘    │
│                         ↑                                │
├─────────────────────────────────────────────────────────┤
│                    APPLICATION                           │
│  ┌────────────────────────────────────────────────┐    │
│  │ MetricService                                   │    │
│  │ Use Cases: ComputeMetric, Normalize, Validate  │    │
│  │ DTOs: SampleDto, MetricDto                     │    │
│  └────────────────────────────────────────────────┘    │
│                         ↑                                │
├─────────────────────────────────────────────────────────┤
│                      DOMAIN                              │
│  ┌────────────────────────────────────────────────┐    │
│  │ Entities: Sample, Metric, SampleCollection     │    │
│  │ Value Objects: Latency, Percentile, Window     │    │
│  │ Aggregations: Average, Max, Min, Percentile    │    │
│  │ Events: SampleCollected, MetricComputed        │    │
│  └────────────────────────────────────────────────┘    │
└─────────────────────────────────────────────────────────┘
```

**Dependency Rule**: Outer layers depend on inner layers, never reverse.
- **Domain**: Pure business logic (no infrastructure dependencies)
- **Application**: Use cases orchestrating domain logic
- **Ports**: Interfaces for infrastructure adapters
- **Infrastructure**: External systems (engines, databases, etc.)

---

## Core Concepts

### 1. Sample (Domain Entity)

An **immutable observation** from a performance test execution.

```csharp
public class Sample
{
    public Guid Id { get; }
    public DateTime Timestamp { get; }        // When observed (UTC)
    public Latency Duration { get; }          // How long (with unit)
    public SampleStatus Status { get; }       // Success | Failure
    public ErrorClassification? ErrorClassification { get; }  // Why failed?
    public ExecutionContext ExecutionContext { get; }  // Where from?
    public IReadOnlyDictionary<string, object>? Metadata { get; }  // Engine-specific extras
}
```

**Invariants** (enforced in constructor):
1. `Timestamp ≤ DateTime.UtcNow` (no future timestamps)
2. `Duration.Value ≥ 0` (no negative latency)
3. If `Status == Failure`, then `ErrorClassification != null`
4. If `Status == Success`, then `ErrorClassification == null`

### 2. SampleCollection (Aggregate Root)

An **append-only, immutable container** for samples.

```csharp
public sealed class SampleCollection
{
    public ImmutableList<Sample> AllSamples { get; }
    
    // Functional API (returns new instance)
    public SampleCollection Add(Sample sample);
    public SampleCollection AddRange(IEnumerable<Sample> samples);
}
```

**Usage**:
```csharp
var collection = SampleCollection.Empty
    .Add(sample1)
    .Add(sample2);  // Returns NEW collection (immutable)
```

### 3. Metric (Domain Entity)

An **aggregated result** computed from a sample collection.

```csharp
public sealed class Metric
{
    public Guid Id { get; }
    public SampleCollection Samples { get; }
    public AggregationWindow Window { get; }
    public string MetricType { get; }  // "Average", "Percentile:95", etc.
    public ImmutableList<AggregationResult> AggregatedValues { get; }
    public DateTime ComputedAt { get; }
}
```

### 4. Latency (Value Object)

A **time measurement** with automatic unit conversion.

```csharp
public class Latency : ValueObject
{
    public double Value { get; }
    public LatencyUnit Unit { get; }  // Nanoseconds, Microseconds, Milliseconds, Seconds
    
    // Unit conversion
    public Latency ConvertTo(LatencyUnit targetUnit);
}
```

**Examples**:
```csharp
var latency1 = new Latency(45.5, LatencyUnit.Milliseconds);
var latency2 = latency1.ConvertTo(LatencyUnit.Seconds);  // 0.0455 seconds
```

### 5. Aggregation Operations

**Deterministic computations** over sample collections.

#### Available Operations:

| Operation | Description | Example |
|-----------|-------------|---------|
| **Average** | Arithmetic mean | `new AverageAggregation()` |
| **Max** | Maximum value | `new MaxAggregation()` |
| **Min** | Minimum value | `new MinAggregation()` |
| **Percentile** | Distribution position | `new PercentileAggregation(95.0)` |

**All operations**:
- Normalize units before computation (all latencies → common unit)
- Filter invalid samples (negative durations, null values)
- Produce **deterministic** results (same input → same output)

**Example**:
```csharp
var p95 = new PercentileAggregation(95.0);
var result = p95.Compute(
    samples: collection,
    window: new FullExecutionWindow(),
    computedAt: DateTime.UtcNow
);
// result.Value = 95th percentile latency
// result.Unit = normalized unit
```

---

## Adapters (Engine Integration)

### K6 Adapter

Maps K6 HTTP check results to domain `Sample` entities.

```csharp
public class K6EngineAdapter : IExecutionEngineAdapter
{
    public SampleCollection MapK6ResultsToDomain(
        IEnumerable<K6ResultData> k6Results,
        Guid executionId,
        string scenarioName
    );
}

public class K6ResultData
{
    public DateTime Timestamp { get; set; }
    public double HttpReqDurationMs { get; set; }
    public int? HttpStatusCode { get; set; }
    public bool HttpReqFailed { get; set; }
    public string? HttpErrorCode { get; set; }  // ERR_K6_TIMEOUT, ERR_K6_DIAL_SOCKET, etc.
}
```

**Error Classification**:
- `ERR_K6_DIAL_SOCKET`, `ERR_K6_SSL` → `ErrorClassification.NetworkError`
- `ERR_K6_TIMEOUT` → `ErrorClassification.Timeout`
- HTTP 4xx/5xx → `ErrorClassification.ApplicationError`
- Other failures → `ErrorClassification.UnknownError`

### JMeter Adapter

Maps JMeter sampler results to domain `Sample` entities.

```csharp
public class JMeterEngineAdapter : IExecutionEngineAdapter
{
    public SampleCollection MapJMeterResultsToDomain(
        IEnumerable<JMeterResultData> jmeterResults,
        Guid executionId,
        string testPlanName
    );
}

public class JMeterResultData
{
    public DateTime Timestamp { get; set; }
    public long ElapsedMs { get; set; }
    public string? ResponseCode { get; set; }
    public string? ResponseMessage { get; set; }
    public bool Success { get; set; }
    public string? SamplerLabel { get; set; }
}
```

**Error Classification**:
- `java.net.ConnectException`, `java.net.UnknownHostException`, `SSLException` → `ErrorClassification.NetworkError`
- `java.net.SocketTimeoutException`, `TimeoutException` → `ErrorClassification.Timeout`
- HTTP 4xx/5xx → `ErrorClassification.ApplicationError`
- Other failures → `ErrorClassification.UnknownError`

---

## Testing

### Test Coverage: 162 Tests

```bash
# Run all tests
dotnet test

# Run specific test category
dotnet test --filter "FullyQualifiedName~SampleTests"
dotnet test --filter "FullyQualifiedName~AggregationTests"
dotnet test --filter "FullyQualifiedName~AdapterTests"
```

### Test Structure

```
tests/
├── Domain/
│   ├── SampleTests.cs                (21 tests) - Immutability, invariants
│   ├── LatencyTests.cs               (14 tests) - Unit conversions
│   ├── PercentileTests.cs            (7 tests)  - Range validation
│   ├── SampleCollectionTests.cs      (10 tests) - Immutable operations
│   └── MetricTests.cs                (8 tests)  - Metric creation
│
├── Aggregations/
│   ├── AverageAggregationTests.cs    (8 tests)  - Mean computation
│   ├── MaxAggregationTests.cs        (7 tests)  - Maximum finding
│   ├── MinAggregationTests.cs        (7 tests)  - Minimum finding
│   ├── PercentileAggregationTests.cs (9 tests)  - Percentile computation
│   ├── NormalizerTests.cs            (8 tests)  - Unit normalization
│   └── DeterminismTests.cs           (15 tests) - Reproducibility
│
├── Application/
│   ├── UseCaseTests.cs               (13 tests) - Use case validation
│   └── MetricServiceIntegrationTests.cs (10 tests) - End-to-end
│
├── Infrastructure/
│   ├── K6AdapterTests.cs             (12 tests) - K6 mapping
│   ├── JMeterAdapterTests.cs         (13 tests) - JMeter mapping
│   └── CrossAdapterCompatibilityTests.cs (8 tests) - Adapter equivalence
│
└── InfrastructureLayerVerification.cs (2 tests) - Architecture compliance
```

### Example Test

```csharp
[Fact]
public void Sample_EnforcesInvariant_TimestampCannotBeInFuture()
{
    // Arrange
    var futureTimestamp = DateTime.UtcNow.AddMinutes(5);
    
    // Act & Assert
    var exception = Assert.Throws<ArgumentException>(() =>
        new Sample(
            id: Guid.NewGuid(),
            timestamp: futureTimestamp,  // INVALID
            duration: new Latency(100, LatencyUnit.Milliseconds),
            status: SampleStatus.Success,
            errorClassification: null,
            executionContext: new ExecutionContext("Engine", Guid.NewGuid(), "Scenario"),
            metadata: null
        )
    );
    
    Assert.Contains("Timestamp cannot be in the future", exception.Message);
}
```

---

## Extension Guide

### Adding a New Aggregation Operation

1. **Create operation class** implementing `IAggregationOperation`:

```csharp
public class MedianAggregation : IAggregationOperation
{
    public string OperationName => "Median";
    
    public AggregationResult Compute(
        SampleCollection samples,
        AggregationWindow window,
        DateTime computedAt)
    {
        // 1. Normalize units
        var normalized = new AggregationNormalizer().NormalizeToMilliseconds(samples);
        
        // 2. Filter valid samples
        var validLatencies = normalized.AllSamples
            .Where(s => s.Status == SampleStatus.Success)
            .Select(s => s.Duration.Value)
            .OrderBy(v => v)
            .ToList();
        
        // 3. Compute median
        double median = validLatencies.Count % 2 == 0
            ? (validLatencies[validLatencies.Count / 2 - 1] + validLatencies[validLatencies.Count / 2]) / 2.0
            : validLatencies[validLatencies.Count / 2];
        
        // 4. Return result
        return new AggregationResult(
            value: median,
            unit: LatencyUnit.Milliseconds,
            computedAt: computedAt
        );
    }
}
```

2. **Add tests**:

```csharp
[Fact]
public void MedianAggregation_ComputesCorrectly()
{
    var samples = SampleCollection.Empty
        .Add(CreateSample(10.0))
        .Add(CreateSample(20.0))
        .Add(CreateSample(30.0));
    
    var median = new MedianAggregation();
    var result = median.Compute(samples, new FullExecutionWindow(), DateTime.UtcNow);
    
    Assert.Equal(20.0, result.Value);
}
```

### Adding a New Engine Adapter

1. **Create adapter class** implementing `IExecutionEngineAdapter`:

```csharp
public class GatlingEngineAdapter : IExecutionEngineAdapter
{
    public SampleCollection MapGatlingResultsToDomain(
        IEnumerable<GatlingResultData> gatlingResults,
        Guid executionId,
        string simulationName)
    {
        var collection = SampleCollection.Empty;
        
        foreach (var result in gatlingResults)
        {
            // Map Gatling-specific fields → domain Sample
            var sample = new Sample(
                id: Guid.NewGuid(),
                timestamp: result.Timestamp,
                duration: new Latency(result.ResponseTimeMs, LatencyUnit.Milliseconds),
                status: result.Status == "OK" ? SampleStatus.Success : SampleStatus.Failure,
                errorClassification: ClassifyGatlingError(result.ErrorMessage),
                executionContext: new ExecutionContext("Gatling", executionId, simulationName),
                metadata: new Dictionary<string, object>
                {
                    ["request_name"] = result.RequestName,
                    ["status"] = result.Status
                }
            );
            
            collection = collection.Add(sample);
        }
        
        return collection;
    }
    
    private ErrorClassification? ClassifyGatlingError(string? errorMessage)
    {
        if (string.IsNullOrEmpty(errorMessage))
            return null;
        
        if (errorMessage.Contains("timeout"))
            return ErrorClassification.Timeout;
        if (errorMessage.Contains("Connection refused"))
            return ErrorClassification.NetworkError;
        if (errorMessage.Contains("500") || errorMessage.Contains("503"))
            return ErrorClassification.ApplicationError;
        
        return ErrorClassification.UnknownError;
    }
}
```

2. **Add contract tests**:

```csharp
[Fact]
public void GatlingAdapter_MapsSuccessfulSampleCorrectly()
{
    var adapter = new GatlingEngineAdapter();
    var results = new[]
    {
        new GatlingResultData
        {
            Timestamp = DateTime.UtcNow.AddSeconds(-5),
            ResponseTimeMs = 123,
            Status = "OK",
            RequestName = "HomePage"
        }
    };
    
    var samples = adapter.MapGatlingResultsToDomain(results, Guid.NewGuid(), "LoadTest");
    
    Assert.Single(samples.AllSamples);
    Assert.Equal(SampleStatus.Success, samples.AllSamples[0].Status);
    Assert.Equal(123, samples.AllSamples[0].Duration.Value);
}
```

---

## Project Structure

```
src/PerformanceEngine.Metrics.Domain/
├── Domain/
│   ├── Metrics/
│   │   ├── Sample.cs                    # Core entity
│   │   ├── SampleCollection.cs          # Aggregate root
│   │   ├── Metric.cs                    # Computed result
│   │   ├── Latency.cs                   # Value object
│   │   ├── LatencyUnit.cs               # Enum
│   │   ├── LatencyUnitConverter.cs      # Unit conversion
│   │   ├── Percentile.cs                # Value object
│   │   ├── AggregationWindow.cs         # Window abstraction
│   │   ├── AggregationResult.cs         # Computation output
│   │   ├── ExecutionContext.cs          # Execution metadata
│   │   ├── SampleStatus.cs              # Enum
│   │   └── ErrorClassification.cs       # Enum
│   │
│   ├── Aggregations/
│   │   ├── IAggregationOperation.cs     # Operation interface
│   │   ├── AverageAggregation.cs
│   │   ├── MaxAggregation.cs
│   │   ├── MinAggregation.cs
│   │   ├── PercentileAggregation.cs
│   │   └── AggregationNormalizer.cs     # Unit normalization
│   │
│   ├── Events/
│   │   ├── IDomainEvent.cs              # Event marker
│   │   ├── SampleCollectedEvent.cs
│   │   └── MetricComputedEvent.cs
│   │
│   ├── Application/
│   │   ├── UseCases/
│   │   │   ├── ComputeMetricUseCase.cs  # Orchestration
│   │   │   ├── NormalizeSamplesUseCase.cs
│   │   │   └── ValidateAggregationUseCase.cs
│   │   ├── Services/
│   │   │   └── MetricService.cs         # Application facade
│   │   └── Dto/
│   │       ├── SampleDto.cs             # Transfer object
│   │       ├── MetricDto.cs
│   │       └── AggregationRequestDto.cs
│   │
│   ├── Infrastructure/
│   │   └── Adapters/
│   │       ├── K6EngineAdapter.cs       # K6 → domain
│   │       └── JMeterEngineAdapter.cs   # JMeter → domain
│   │
│   └── Ports/
│       ├── IExecutionEngineAdapter.cs   # Adapter port
│       └── IPersistenceRepository.cs    # Repository port (deferred)
│
└── ValueObject.cs                       # Base class for value objects
```

---

## Design Principles

### 1. Immutability

All domain entities and value objects are **immutable** after construction.

**Benefits**:
- Thread-safe by default (no locking needed)
- Deterministic (state cannot change unexpectedly)
- Cacheable (safe to share references)

**Example**:
```csharp
// ❌ WRONG - No setters allowed
sample.Status = SampleStatus.Failure;  // Compile error

// ✅ CORRECT - Create new instance
var failedSample = new Sample(
    id: sample.Id,
    timestamp: sample.Timestamp,
    duration: sample.Duration,
    status: SampleStatus.Failure,  // NEW value
    errorClassification: ErrorClassification.Timeout,
    executionContext: sample.ExecutionContext,
    metadata: sample.Metadata
);
```

### 2. Determinism

All aggregation operations produce **byte-identical results** for identical inputs.

**Ensured by**:
- No randomness (no `Guid.NewGuid()` in domain logic)
- No current time (no `DateTime.Now` in computations)
- Deterministic sorting (stable sort for percentiles)
- Unit normalization (consistent conversion order)

**Example**:
```csharp
[Fact]
public void PercentileAggregation_ProducesDeterministicResults()
{
    var samples = CreateSampleCollection();
    var p95 = new PercentileAggregation(95.0);
    
    // Run 1000 times
    var results = new List<double>();
    for (int i = 0; i < 1000; i++)
    {
        var result = p95.Compute(samples, new FullExecutionWindow(), DateTime.UtcNow);
        results.Add(result.Value);
    }
    
    // All results MUST be identical
    Assert.Single(results.Distinct());
}
```

### 3. Engine-Agnostic Domain

The domain layer **never imports** engine-specific libraries or references.

**Verified by**:
- Architecture tests (grep verification)
- No K6/JMeter/Gatling namespaces in domain
- All engine knowledge isolated to adapters

```csharp
// ❌ WRONG - Engine-specific type leaks into domain
public class Sample
{
    public K6HttpResult K6Result { get; set; }  // ❌ Violates engine-agnostic principle
}

// ✅ CORRECT - Domain uses only domain concepts
public class Sample
{
    public Latency Duration { get; }  // ✅ Domain concept
    public SampleStatus Status { get; }  // ✅ Domain concept
    public IReadOnlyDictionary<string, object>? Metadata { get; }  // ✅ Generic container for engine extras
}
```

---

## Roadmap

### Completed (Phase 1-5)
- ✅ Domain vocabulary (Sample, Metric, Latency, etc.)
- ✅ Aggregation operations (Average, Max, Min, Percentile)
- ✅ K6 and JMeter adapters
- ✅ Application services and DTOs
- ✅ 162 comprehensive tests

### Planned (Future Phases)
- ⏳ Persistence adapters (PostgreSQL, MongoDB)
- ⏳ Additional aggregation operations (Standard Deviation, Quantiles)
- ⏳ Advanced window types (Sliding, Fixed time windows)
- ⏳ Event sourcing for metric history
- ⏳ GraphQL API for metric queries
- ⏳ Evaluation engine integration (SLO/SLA definitions)

---

## Contributing

### Prerequisites
- .NET 8.0 SDK or later
- IDE: Visual Studio 2022, VS Code, or Rider

### Development Workflow

1. **Clone repository**:
   ```bash
   git clone <repository-url>
   cd Per_Engine
   ```

2. **Create feature branch**:
   ```bash
   git checkout -b feature/your-feature-name
   ```

3. **Implement changes** following Clean Architecture:
   - Domain changes → Update domain entities/value objects
   - Application changes → Update use cases/services
   - Infrastructure changes → Update adapters

4. **Add tests** (REQUIRED):
   ```bash
   # Add test to appropriate test file
   # Run tests to verify
   dotnet test
   ```

5. **Ensure all tests pass**:
   ```bash
   dotnet test
   # Must show: Failed: 0, Passed: 162+
   ```

6. **Commit and push**:
   ```bash
   git add .
   git commit -m "feat: your feature description"
   git push origin feature/your-feature-name
   ```

### Code Style

- **Immutability**: Use `readonly` fields, no setters
- **Null safety**: Use nullable reference types (`string?`, `int?`)
- **Value objects**: Override `Equals` and `GetHashCode`
- **Entities**: Use `Guid` for identities
- **Naming**: PascalCase for public members, camelCase for private

---

## License

[Your license here]

---

## Support

For questions, issues, or contributions:
- 📧 Email: [your-email]
- 🐛 Issues: [GitHub Issues URL]
- 📖 Docs: [Documentation URL]

---

**Built with ❤️ using Clean Architecture and Domain-Driven Design**
