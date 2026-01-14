# Quick Start: Metrics Domain Implementation

**Audience**: Developers implementing domain entities, adapters, or use cases  
**Time to Skim**: 10 minutes | **Time to Implement**: 1-2 days (for core domain)

---

## Overview

This guide walks through implementing the metrics domain in C#/.NET 10, following Clean Architecture and Domain-Driven Design principles.

### Core Concepts You'll Build

```
Sample (immutable observation)
   ↓
SampleCollection (append-only container)
   ↓
Aggregation (Average, Max, Min, Percentile)
   ↓
Metric (computed result with metadata)
```

---

## Step 1: Create Domain Layer Structure

```bash
mkdir -p src/Domain/Metrics
mkdir -p src/Domain/Aggregations
mkdir -p src/Domain/Events
mkdir -p src/Domain/Ports

# Create test structure
mkdir -p tests/Domain.UnitTests/Metrics
mkdir -p tests/Domain.ContractTests/Aggregations
```

### Namespace Organization

```csharp
// Value objects and entities
namespace Domain.Metrics { }

// Aggregation operations
namespace Domain.Aggregations { }

// Domain events
namespace Domain.Events { }

// Port/adapter contracts
namespace Domain.Ports { }
```

---

## Step 2: Implement Value Objects (Bottom-Up)

### 2.1: LatencyUnit Enum

```csharp
// src/Domain/Metrics/LatencyUnit.cs
namespace Domain.Metrics;

public enum LatencyUnit
{
    Nanoseconds = 0,
    Microseconds = 1,
    Milliseconds = 2,
    Seconds = 3
}
```

**Test**:
```csharp
[Fact]
public void LatencyUnitEnumDefined()
{
    Assert.Equal(4, Enum.GetValues<LatencyUnit>().Length);
}
```

### 2.2: Latency Value Object

```csharp
// src/Domain/Metrics/Latency.cs
namespace Domain.Metrics;

public class Latency : IEquatable<Latency>, IComparable<Latency>
{
    public double Value { get; }
    public LatencyUnit Unit { get; }
    
    public Latency(double value, LatencyUnit unit)
    {
        if (value < 0)
            throw new ArgumentException("Latency cannot be negative", nameof(value));
        if (double.IsNaN(value) || double.IsInfinity(value))
            throw new ArgumentException("Latency cannot be NaN or Infinity", nameof(value));
        
        Value = value;
        Unit = unit;
    }
    
    // Conversion to nanoseconds (canonical unit)
    public long ToNanoseconds() => Unit switch
    {
        LatencyUnit.Nanoseconds => (long)Value,
        LatencyUnit.Microseconds => (long)(Value * 1_000),
        LatencyUnit.Milliseconds => (long)(Value * 1_000_000),
        LatencyUnit.Seconds => (long)(Value * 1_000_000_000),
        _ => throw new InvalidOperationException($"Unknown unit: {Unit}")
    };
    
    // Equality (value-based)
    public bool Equals(Latency? other) =>
        other != null && this.ToNanoseconds() == other.ToNanoseconds();
    
    public override bool Equals(object? obj) => Equals(obj as Latency);
    public override int GetHashCode() => ToNanoseconds().GetHashCode();
    
    // Comparison (for sorting)
    public int CompareTo(Latency? other) =>
        other == null ? 1 : this.ToNanoseconds().CompareTo(other.ToNanoseconds());
}
```

**Test** (from contracts/domain-model.md):
```csharp
[Fact]
public void NegativeLatencyThrows()
{
    Assert.Throws<ArgumentException>(() => new Latency(-100, LatencyUnit.Milliseconds));
}

[Fact]
public void EqualityAcrossUnits()
{
    var latency1 = new Latency(1, LatencyUnit.Seconds);
    var latency2 = new Latency(1_000, LatencyUnit.Milliseconds);
    Assert.Equal(latency1, latency2);
}
```

### 2.3: Percentile Value Object

```csharp
// src/Domain/Metrics/Percentile.cs
namespace Domain.Metrics;

public class Percentile : IEquatable<Percentile>, IComparable<Percentile>
{
    public decimal Value { get; }
    
    public static readonly Percentile P50 = new(50);
    public static readonly Percentile P95 = new(95);
    public static readonly Percentile P99 = new(99);
    
    public Percentile(decimal value)
    {
        if (value < 0 || value > 100)
            throw new ArgumentException("Percentile must be between 0 and 100", nameof(value));
        Value = value;
    }
    
    public string ToDisplayString() => $"p{Value}";
    
    public bool Equals(Percentile? other) => other != null && Value == other.Value;
    public override bool Equals(object? obj) => Equals(obj as Percentile);
    public override int GetHashCode() => Value.GetHashCode();
    public int CompareTo(Percentile? other) => other == null ? 1 : Value.CompareTo(other.Value);
}
```

### 2.4: ErrorClassification Enum

```csharp
// src/Domain/Metrics/ErrorClassification.cs
namespace Domain.Metrics;

public enum ErrorClassification
{
    Timeout,
    NetworkError,
    ApplicationError,
    UnknownError
}
```

---

## Step 3: Implement Core Entities

### 3.1: Sample Entity

```csharp
// src/Domain/Metrics/Sample.cs
namespace Domain.Metrics;

public class Sample
{
    public Guid Id { get; }
    public DateTime Timestamp { get; }
    public Latency Duration { get; }
    public SampleStatus Status { get; }
    public ErrorClassification? ErrorClassification { get; }
    public ExecutionContext ExecutionContext { get; }
    public IReadOnlyDictionary<string, object> Metadata { get; }
    
    public Sample(
        DateTime timestamp,
        Latency duration,
        SampleStatus status,
        ErrorClassification? errorClassification,
        ExecutionContext executionContext,
        Dictionary<string, object>? metadata = null)
    {
        // Invariant checks
        if (timestamp > DateTime.UtcNow)
            throw new ArgumentException("Timestamp cannot be in future", nameof(timestamp));
        
        if (duration.Value < 0)
            throw new ArgumentException("Duration cannot be negative", nameof(duration));
        
        if (status == SampleStatus.Failure && errorClassification == null)
            throw new ArgumentException(
                "ErrorClassification required when Status is Failure", 
                nameof(errorClassification));
        
        if (status == SampleStatus.Success && errorClassification != null)
            throw new ArgumentException(
                "ErrorClassification must be null when Status is Success", 
                nameof(errorClassification));
        
        Id = Guid.NewGuid();
        Timestamp = timestamp;
        Duration = duration;
        Status = status;
        ErrorClassification = errorClassification;
        ExecutionContext = executionContext;
        Metadata = new Dictionary<string, object>(metadata ?? new()).AsReadOnly();
    }
}

public enum SampleStatus
{
    Success,
    Failure
}

public class ExecutionContext
{
    public string EngineName { get; }
    public Guid ExecutionId { get; }
    public string? ScenarioName { get; }
    
    public ExecutionContext(string engineName, Guid executionId, string? scenarioName = null)
    {
        if (string.IsNullOrWhiteSpace(engineName))
            throw new ArgumentException("EngineName cannot be empty", nameof(engineName));
        
        EngineName = engineName;
        ExecutionId = executionId;
        ScenarioName = scenarioName;
    }
}
```

### 3.2: SampleCollection Container

```csharp
// src/Domain/Metrics/SampleCollection.cs
namespace Domain.Metrics;

using System.Collections.Immutable;

public sealed class SampleCollection
{
    private ImmutableList<Sample> _samples = ImmutableList<Sample>.Empty;
    
    public int Count => _samples.Count;
    public ImmutableList<Sample> Samples => _samples;
    
    public SampleCollection() { }
    
    private SampleCollection(ImmutableList<Sample> samples)
    {
        _samples = samples;
    }
    
    public SampleCollection Add(Sample sample)
    {
        if (sample == null)
            throw new ArgumentNullException(nameof(sample));
        
        return new SampleCollection(_samples.Add(sample));
    }
    
    public SampleCollection AddRange(IEnumerable<Sample> samples)
    {
        if (samples == null)
            throw new ArgumentNullException(nameof(samples));
        
        return new SampleCollection(_samples.AddRange(samples));
    }
    
    public IEnumerable<Sample> OrderedByTimestamp()
    {
        return _samples.OrderBy(s => s.Timestamp);
    }
    
    public IEnumerable<Sample> OrderedByDuration(bool ascending = true)
    {
        return ascending
            ? _samples.OrderBy(s => s.Duration)
            : _samples.OrderByDescending(s => s.Duration);
    }
}
```

**Test**:
```csharp
[Fact]
public void AddSampleToCollection()
{
    var collection = new SampleCollection()
        .Add(sample1)
        .Add(sample2);
    
    Assert.Equal(2, collection.Count);
}

[Fact]
public void CollectionPreservesOrder()
{
    var collection = new SampleCollection()
        .Add(sample1)
        .Add(sample2)
        .Add(sample3);
    
    var samples = collection.Samples.ToList();
    Assert.Same(sample1, samples[0]);
    Assert.Same(sample2, samples[1]);
    Assert.Same(sample3, samples[2]);
}
```

---

## Step 4: Implement Aggregation Operations

### 4.1: Aggregation Interface

```csharp
// src/Domain/Aggregations/IAggregationOperation.cs
namespace Domain.Aggregations;

public interface IAggregationOperation
{
    AggregationResult Aggregate(SampleCollection samples);
}
```

### 4.2: AggregationResult

```csharp
// src/Domain/Aggregations/AggregationResult.cs
namespace Domain.Aggregations;

public class AggregationResult
{
    public double Value { get; }
    public LatencyUnit Unit { get; }
    public Percentile? Percentile { get; }
    public DateTime ComputedAt { get; }
    
    public AggregationResult(double value, LatencyUnit unit, Percentile? percentile = null)
    {
        if (double.IsNaN(value) || double.IsInfinity(value))
            throw new ArgumentException("Value cannot be NaN or Infinity", nameof(value));
        
        Value = value;
        Unit = unit;
        Percentile = percentile;
        ComputedAt = DateTime.UtcNow;
    }
}

public enum AggregationOperationType
{
    Average,
    Max,
    Min,
    Percentile
}
```

### 4.3: Average Aggregation

```csharp
// src/Domain/Aggregations/AverageAggregation.cs
namespace Domain.Aggregations;

public class AverageAggregation : IAggregationOperation
{
    public AggregationResult Aggregate(SampleCollection samples)
    {
        if (samples.Count == 0)
            throw new ArgumentException("Cannot aggregate empty collection", nameof(samples));
        
        double sum = 0;
        foreach (var sample in samples.Samples)
        {
            sum += sample.Duration.ToNanoseconds();
        }
        
        double averageNs = sum / samples.Count;
        double averageMs = averageNs / 1_000_000;
        
        return new AggregationResult(averageMs, LatencyUnit.Milliseconds);
    }
}
```

### 4.4: Max Aggregation

```csharp
// src/Domain/Aggregations/MaxAggregation.cs
namespace Domain.Aggregations;

public class MaxAggregation : IAggregationOperation
{
    public AggregationResult Aggregate(SampleCollection samples)
    {
        if (samples.Count == 0)
            throw new ArgumentException("Cannot aggregate empty collection", nameof(samples));
        
        long maxNs = samples.Samples.Max(s => s.Duration.ToNanoseconds());
        double maxMs = maxNs / 1_000_000.0;
        
        return new AggregationResult(maxMs, LatencyUnit.Milliseconds);
    }
}
```

### 4.5: Percentile Aggregation (Most Important)

```csharp
// src/Domain/Aggregations/PercentileAggregation.cs
namespace Domain.Aggregations;

public class PercentileAggregation : IAggregationOperation
{
    private readonly Percentile _percentile;
    
    public PercentileAggregation(Percentile percentile)
    {
        _percentile = percentile ?? throw new ArgumentNullException(nameof(percentile));
    }
    
    public AggregationResult Aggregate(SampleCollection samples)
    {
        if (samples.Count == 0)
            throw new ArgumentException("Cannot aggregate empty collection", nameof(samples));
        
        // Sort samples by duration ascending
        var sorted = samples.Samples
            .OrderBy(s => s.Duration.ToNanoseconds())
            .ToList();
        
        // Nearest-rank algorithm (deterministic)
        int rank = (int)Math.Ceiling(_percentile.Value / 100m * samples.Count);
        rank = Math.Max(1, Math.Min(rank, samples.Count)); // Clamp to [1, count]
        
        long valueNs = sorted[rank - 1].Duration.ToNanoseconds(); // 0-indexed
        double valueMs = valueNs / 1_000_000.0;
        
        return new AggregationResult(valueMs, LatencyUnit.Milliseconds, _percentile);
    }
}
```

**Test - Determinism**:
```csharp
[Fact]
public void PercentileDeterminism_10000Runs_AllIdentical()
{
    var collection = CreateTestSampleCollection(1000);
    var aggregation = new PercentileAggregation(Percentile.P95);
    
    var results = new List<AggregationResult>();
    for (int i = 0; i < 10000; i++)
    {
        results.Add(aggregation.Aggregate(collection));
    }
    
    // Verify all results identical
    var first = results[0];
    foreach (var result in results.Skip(1))
    {
        Assert.Equal(first.Value, result.Value, precision: 10);
        Assert.Equal(first.Unit, result.Unit);
    }
}
```

---

## Step 5: Implement Metric Entity

```csharp
// src/Domain/Metrics/Metric.cs
namespace Domain.Metrics;

public class Metric
{
    public Guid Id { get; }
    public string MetricType { get; }
    public AggregationWindow AggregationWindow { get; }
    public AggregationOperationType OperationType { get; }
    public SampleCollection SourceSamples { get; }
    public AggregationResult Result { get; }
    public DateTime ComputedAt { get; }
    public int SampleCount { get; }
    
    public Metric(
        string metricType,
        AggregationWindow window,
        AggregationOperationType operationType,
        SampleCollection samples,
        AggregationResult result)
    {
        if (samples.Count == 0)
            throw new ArgumentException("Metric cannot exist without samples", nameof(samples));
        if (string.IsNullOrWhiteSpace(metricType))
            throw new ArgumentException("MetricType cannot be empty", nameof(metricType));
        
        Id = Guid.NewGuid();
        MetricType = metricType;
        AggregationWindow = window ?? throw new ArgumentNullException(nameof(window));
        OperationType = operationType;
        SourceSamples = samples;
        Result = result;
        ComputedAt = DateTime.UtcNow;
        SampleCount = samples.Count;
    }
}

public enum AggregationOperationType
{
    Average,
    Max,
    Min,
    Percentile
}

public abstract class AggregationWindow
{
    public abstract string Description { get; }
}

public class FullExecutionWindow : AggregationWindow
{
    public override string Description => "Full execution duration";
}

public class SlidingWindow : AggregationWindow
{
    public TimeSpan WindowSize { get; }
    public TimeSpan StepSize { get; }
    
    public SlidingWindow(TimeSpan windowSize, TimeSpan stepSize)
    {
        if (windowSize <= TimeSpan.Zero) throw new ArgumentException("Window size must be positive");
        if (stepSize <= TimeSpan.Zero) throw new ArgumentException("Step size must be positive");
        if (stepSize >= windowSize) throw new ArgumentException("Step size must be less than window size");
        
        WindowSize = windowSize;
        StepSize = stepSize;
    }
    
    public override string Description => $"Sliding {WindowSize.TotalSeconds}s window, {StepSize.TotalSeconds}s step";
}

public class FixedWindow : AggregationWindow
{
    public TimeSpan WindowSize { get; }
    
    public FixedWindow(TimeSpan windowSize)
    {
        if (windowSize <= TimeSpan.Zero) throw new ArgumentException("Window size must be positive");
        WindowSize = windowSize;
    }
    
    public override string Description => $"Fixed {WindowSize.TotalSeconds}s window";
}
```

---

## Step 6: Define Ports (Interfaces Only)

```csharp
// src/Domain/Ports/IExecutionEngineAdapter.cs
namespace Domain.Ports;

public interface IExecutionEngineAdapter
{
    Task<IEnumerable<Sample>> MapResultsToSamplesAsync(string engineResultData);
    bool CanHandle(string engineFormat);
    string Description { get; }
}

// src/Domain/Ports/IPersistenceRepository.cs
namespace Domain.Ports;

public interface IPersistenceRepository
{
    Task SaveMetricsAsync(IEnumerable<Metric> metrics);
    Task<IEnumerable<Metric>> QueryMetricsByWindowAsync(AggregationWindow window);
    Task<IEnumerable<Sample>> QuerySamplesByMetricAsync(Guid metricId);
}
```

---

## Step 7: Build & Test

```bash
# Compile domain layer (no external dependencies)
dotnet build src/Domain

# Run unit tests
dotnet test tests/Domain.UnitTests

# Run contract tests
dotnet test tests/Domain.ContractTests

# Verify no infrastructure imports
grep -r "using.*Infrastructure" src/Domain || echo "✅ No infrastructure imports"
```

---

## What's Next

1. **Application Layer**: Create use cases (ComputeMetricUseCase, NormalizeSamplesUseCase)
2. **Adapters**: Implement K6EngineAdapter, JMeterEngineAdapter
3. **Persistence**: Implement InMemoryRepository, then database adapters
4. **Integration Tests**: Test full flow (engine result → sample → metric)

---

## Common Patterns

### Pattern: Creating a Sample
```csharp
var sample = new Sample(
    timestamp: DateTime.UtcNow,
    duration: new Latency(150.5, LatencyUnit.Milliseconds),
    status: SampleStatus.Success,
    errorClassification: null,
    executionContext: new ExecutionContext("k6", executionId, "login-test")
);
```

### Pattern: Computing a Metric
```csharp
var collection = new SampleCollection();
// ... add samples ...

var aggregation = new PercentileAggregation(Percentile.P95);
var result = aggregation.Aggregate(collection);

var metric = new Metric(
    metricType: "latency",
    window: new FullExecutionWindow(),
    operationType: AggregationOperationType.Percentile,
    samples: collection,
    result: result
);
```

### Pattern: Adapter Implementation (Template)
```csharp
public class CustomEngineAdapter : IExecutionEngineAdapter
{
    public bool CanHandle(string engineFormat) => engineFormat == "custom";
    public string Description => "Custom Engine Adapter";
    
    public async Task<IEnumerable<Sample>> MapResultsToSamplesAsync(string engineResultData)
    {
        var engineSamples = JsonConvert.DeserializeObject<CustomSampleRow[]>(engineResultData);
        
        return engineSamples.Select(row => new Sample(
            timestamp: row.Timestamp,
            duration: new Latency(row.DurationMs, LatencyUnit.Milliseconds),
            status: row.Success ? SampleStatus.Success : SampleStatus.Failure,
            errorClassification: MapErrorType(row.ErrorCode),
            executionContext: new ExecutionContext("custom", executionId, row.ScenarioName)
        ));
    }
    
    private ErrorClassification? MapErrorType(int? errorCode)
    {
        if (errorCode == null) return null;
        return errorCode switch
        {
            408 or 504 => ErrorClassification.Timeout,
            0 => ErrorClassification.NetworkError,
            500 => ErrorClassification.ApplicationError,
            _ => ErrorClassification.UnknownError
        };
    }
}
```

---

## Resources

- **Full Data Model**: `specs/data-model.md`
- **Port Contracts**: `specs/contracts/domain-model.md`
- **Architecture Decisions**: `specs/plan.md` (ADR section)
- **Research Findings**: `specs/research.md`

---

## Estimated Timeline

| Task | Hours | Notes |
|------|-------|-------|
| Step 1-3: Value Objects & Sample | 4 | Most critical for correctness |
| Step 4: Aggregations | 6 | Determinism testing essential |
| Step 5-6: Metric & Ports | 3 | Straightforward |
| Step 7: Testing & Validation | 8 | Contract & unit tests |
| **Total** | **21 hours** | ~2.5 days of focused work |

**Start with aggregations** - they're the complexity hotspot. Get determinism right before moving to adapters.
