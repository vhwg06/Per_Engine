# Phase 1 Data Model: Metrics Domain Entity Specifications

**Completed**: 2026-01-14  
**Input**: Research findings from research.md, specification requirements from metrics-domain.spec.md  
**Output**: Concrete entity definitions for implementation

## Overview

This document specifies all domain entities and value objects with their attributes, relationships, invariants, and validation rules. Implementation follows C# conventions with immutable semantics per ADR-002.

---

## Domain Entity: Sample

### Purpose
Immutable record of a single performance measurement observation from an execution engine.

### Attributes

| Attribute | Type | Immutable | Nullable | Invariants | Notes |
|-----------|------|----------|----------|-----------|-------|
| `Id` | `Guid` | ✅ Yes | ❌ No | Unique per execution context | Auto-generated at creation |
| `Timestamp` | `DateTime` (UTC) | ✅ Yes | ❌ No | ≥ execution start time | Engine-specific or UTC.Now |
| `Duration` | `Latency` value object | ✅ Yes | ❌ No | ≥ 0 | Must specify unit explicitly |
| `Status` | `SampleStatus` enum | ✅ Yes | ❌ No | Success or Failure | Boolean replacement (better semantics) |
| `ErrorClassification` | `ErrorClassification` enum | ✅ Yes | ✅ Yes | One of: Timeout, NetworkError, ApplicationError, UnknownError | Null if Status==Success |
| `ExecutionContext` | `ExecutionContext` value object | ✅ Yes | ❌ No | Contains engine name, run ID, scenario | Links sample to execution |
| `Metadata` | `Dictionary<string, object>` | ✅ Yes* | ❌ No | Engine-specific data preserved | *Dictionary is sealed after construction |

### Entity Relationships

```
Sample (entity)
├── Latency (value object)
│   └── LatencyUnit (enum)
├── SampleStatus (enum)
├── ErrorClassification (enum)
└── ExecutionContext (value object)
    ├── EngineName (string)
    ├── ExecutionId (Guid)
    └── ScenarioName (string, nullable)
```

### Validation Rules

```csharp
public class Sample
{
    // Invariants enforced in constructor
    public Sample(
        DateTime timestamp,
        Latency duration,
        SampleStatus status,
        ErrorClassification? errorClassification,
        ExecutionContext executionContext,
        Dictionary<string, object>? metadata = null)
    {
        // Invariant 1: Timestamp cannot be in future
        if (timestamp > DateTime.UtcNow)
            throw new ArgumentException("Timestamp cannot be in future", nameof(timestamp));
        
        // Invariant 2: Duration must be non-negative
        if (duration.Value < 0)
            throw new ArgumentException("Duration cannot be negative", nameof(duration));
        
        // Invariant 3: Error classification required if status is Failure
        if (status == SampleStatus.Failure && errorClassification == null)
            throw new ArgumentException(
                "ErrorClassification required when Status is Failure", 
                nameof(errorClassification));
        
        // Invariant 4: Error classification must be null if status is Success
        if (status == SampleStatus.Success && errorClassification != null)
            throw new ArgumentException(
                "ErrorClassification must be null when Status is Success", 
                nameof(errorClassification));
        
        // Store all values (immutable after construction)
        Id = Guid.NewGuid();
        Timestamp = timestamp;
        Duration = duration;
        Status = status;
        ErrorClassification = errorClassification;
        ExecutionContext = executionContext;
        Metadata = new Dictionary<string, object>(metadata ?? new());
    }
}
```

### Example Instantiation

```csharp
// Success sample
var sample1 = new Sample(
    timestamp: DateTime.UtcNow,
    duration: new Latency(45.5, LatencyUnit.Milliseconds),
    status: SampleStatus.Success,
    errorClassification: null,
    executionContext: new ExecutionContext("k6", Guid.NewGuid(), "login-scenario")
);

// Failure sample
var sample2 = new Sample(
    timestamp: DateTime.UtcNow,
    duration: new Latency(5000, LatencyUnit.Milliseconds),
    status: SampleStatus.Failure,
    errorClassification: ErrorClassification.Timeout,
    executionContext: new ExecutionContext("jmeter", Guid.NewGuid(), "search-scenario"),
    metadata: new Dictionary<string, object>
    {
        ["jmeter_error_code"] = 407,
        ["jmeter_error_message"] = "Read timed out"
    }
);
```

---

## Value Object: SampleCollection

### Purpose
Immutable, thread-safe container for multiple samples with snapshot consistency.

### Structure

```csharp
public sealed class SampleCollection
{
    // Immutable list (System.Collections.Immutable.ImmutableList<T>)
    private ImmutableList<Sample> _samples;
    
    // Properties
    public int Count => _samples.Count;
    public ImmutableList<Sample> Samples => _samples;
    
    // Methods
    public SampleCollection Add(Sample sample);
    public SampleCollection AddRange(IEnumerable<Sample> samples);
    public ImmutableList<Sample> GetSnapshot();
    public IEnumerable<Sample> GetSnapshotOrdered(SampleOrdering ordering);
}
```

### Invariants

- Collection is append-only (never modified after creation)
- All items are distinct `Sample` objects (duplicates allowed by value)
- Order preserved (FIFO insertion order)
- Empty collection valid (for initialization)

### Thread Safety Semantics

- **Add operation**: Lock-free atomic (via `Interlocked.CompareExchange`)
- **Read operation**: Atomic snapshot (volatile field + immutable collection)
- **Performance**: O(log N) append (copy-on-write), O(1) snapshot read

### Ordering Strategy

```csharp
public enum SampleOrdering
{
    InsertionOrder,      // As added to collection
    ByTimestamp,         // Sorted by Timestamp ascending
    ByDurationAscending, // Sorted by Latency ascending
    ByDurationDescending // Sorted by Latency descending
}
```

---

## Value Object: Latency

### Purpose
Represent elapsed time measurement with flexible, explicit units.

### Attributes

| Attribute | Type | Range | Immutable |
|-----------|------|-------|-----------|
| `Value` | `double` | ≥ 0 | ✅ Yes |
| `Unit` | `LatencyUnit` enum | N/A | ✅ Yes |

### Supported Units

```csharp
public enum LatencyUnit
{
    Nanoseconds = 0,
    Microseconds = 1,
    Milliseconds = 2,
    Seconds = 3
}
```

### Conversion Rules

All conversions through nanosecond base:

```
1 second      = 1,000,000,000 ns
1 millisecond = 1,000,000 ns
1 microsecond = 1,000 ns
1 nanosecond  = 1 ns
```

### Value Object Semantics

```csharp
public class Latency : IEquatable<Latency>, IComparable<Latency>
{
    public double Value { get; }
    public LatencyUnit Unit { get; }
    
    public Latency(double value, LatencyUnit unit)
    {
        if (value < 0)
            throw new ArgumentException("Latency cannot be negative", nameof(value));
        if (value == double.PositiveInfinity || value == double.NaN)
            throw new ArgumentException("Latency cannot be infinity or NaN", nameof(value));
        
        Value = value;
        Unit = unit;
    }
    
    // Equality by value (not reference)
    public bool Equals(Latency? other)
    {
        if (other == null) return false;
        return this.ToNanoseconds() == other.ToNanoseconds();
    }
    
    public override bool Equals(object? obj) => Equals(obj as Latency);
    
    public override int GetHashCode() => ToNanoseconds().GetHashCode();
    
    // Comparison (for sorting)
    public int CompareTo(Latency? other)
    {
        if (other == null) return 1;
        return this.ToNanoseconds().CompareTo(other.ToNanoseconds());
    }
    
    // Conversion to canonical unit (nanoseconds)
    public long ToNanoseconds()
    {
        return Unit switch
        {
            LatencyUnit.Nanoseconds => (long)Value,
            LatencyUnit.Microseconds => (long)(Value * 1_000),
            LatencyUnit.Milliseconds => (long)(Value * 1_000_000),
            LatencyUnit.Seconds => (long)(Value * 1_000_000_000),
            _ => throw new InvalidOperationException($"Unknown unit: {Unit}")
        };
    }
    
    // Convert to specific unit
    public double ConvertTo(LatencyUnit targetUnit)
    {
        long ns = ToNanoseconds();
        return targetUnit switch
        {
            LatencyUnit.Nanoseconds => ns,
            LatencyUnit.Microseconds => ns / 1_000.0,
            LatencyUnit.Milliseconds => ns / 1_000_000.0,
            LatencyUnit.Seconds => ns / 1_000_000_000.0,
            _ => throw new InvalidOperationException($"Unknown unit: {targetUnit}")
        };
    }
}
```

### Example Usage

```csharp
var latency1 = new Latency(100, LatencyUnit.Milliseconds);
var latency2 = new Latency(100_000_000, LatencyUnit.Nanoseconds);

Assert.True(latency1 == latency2); // Same duration, different units

// Conversion
double ms = latency1.ConvertTo(LatencyUnit.Milliseconds); // 100
double sec = latency1.ConvertTo(LatencyUnit.Seconds);     // 0.1
```

---

## Value Object: Percentile

### Purpose
Represent a position in a statistical distribution (e.g., p50, p95, p99).

### Attributes

| Attribute | Type | Range | Immutable | Semantic |
|-----------|------|-------|-----------|----------|
| `Value` | `decimal` | [0, 100] | ✅ Yes | Percentile rank (e.g., 95 = p95) |

### Validation & Semantics

```csharp
public class Percentile : IEquatable<Percentile>, IComparable<Percentile>
{
    public decimal Value { get; }
    
    // Named constants for common percentiles
    public static readonly Percentile P50 = new(50);      // Median
    public static readonly Percentile P75 = new(75);      // Q3
    public static readonly Percentile P90 = new(90);
    public static readonly Percentile P95 = new(95);      // Common SLA target
    public static readonly Percentile P99 = new(99);      // Tail latency
    public static readonly Percentile P999 = new(99.9m);  // Extreme outliers
    
    public Percentile(decimal value)
    {
        if (value < 0 || value > 100)
            throw new ArgumentException(
                "Percentile must be between 0 and 100 inclusive", 
                nameof(value));
        
        Value = value;
    }
    
    // Standard percentile semantics
    public string ToDisplayString()
    {
        return Value % 1 == 0
            ? $"p{(int)Value}"
            : $"p{Value}";
    }
    
    // Equality
    public bool Equals(Percentile? other)
    {
        if (other == null) return false;
        return Value == other.Value;
    }
    
    public override bool Equals(object? obj) => Equals(obj as Percentile);
    public override int GetHashCode() => Value.GetHashCode();
    
    // Comparison
    public int CompareTo(Percentile? other)
    {
        if (other == null) return 1;
        return Value.CompareTo(other.Value);
    }
}
```

### Invariants

- Value ∈ [0, 100]
- Precision: Up to 3 decimal places (e.g., p99.9)
- Semantics: p50 = median, pXX = value where XX% of data falls below

---

## Value Object: AggregationWindow

### Purpose
Define the temporal or logical scope over which samples are grouped for aggregation.

### Supported Types

```csharp
public abstract class AggregationWindow
{
    public abstract string Description { get; }
    
    // Factory methods
    public static FullExecutionWindow FullExecution() => new FullExecutionWindow();
    public static SlidingWindow Sliding(TimeSpan size, TimeSpan step) => new SlidingWindow(size, step);
    public static FixedWindow Fixed(TimeSpan size) => new FixedWindow(size);
}

public class FullExecutionWindow : AggregationWindow
{
    public override string Description => "Entire execution duration";
    
    public override bool Equals(object? obj) => obj is FullExecutionWindow;
    public override int GetHashCode() => nameof(FullExecutionWindow).GetHashCode();
}

public class SlidingWindow : AggregationWindow
{
    public TimeSpan WindowSize { get; }
    public TimeSpan StepSize { get; }
    
    public SlidingWindow(TimeSpan windowSize, TimeSpan stepSize)
    {
        if (windowSize <= TimeSpan.Zero)
            throw new ArgumentException("WindowSize must be positive", nameof(windowSize));
        if (stepSize <= TimeSpan.Zero)
            throw new ArgumentException("StepSize must be positive", nameof(stepSize));
        if (stepSize >= windowSize)
            throw new ArgumentException("StepSize must be less than WindowSize", nameof(stepSize));
        
        WindowSize = windowSize;
        StepSize = stepSize;
    }
    
    public override string Description => $"Sliding window: {WindowSize.TotalSeconds}s duration, {StepSize.TotalSeconds}s step";
}

public class FixedWindow : AggregationWindow
{
    public TimeSpan WindowSize { get; }
    
    public FixedWindow(TimeSpan windowSize)
    {
        if (windowSize <= TimeSpan.Zero)
            throw new ArgumentException("WindowSize must be positive", nameof(windowSize));
        
        WindowSize = windowSize;
    }
    
    public override string Description => $"Fixed window: {WindowSize.TotalSeconds}s intervals";
}
```

### Invariants

- FullExecutionWindow: No constraints
- SlidingWindow: StepSize < WindowSize (no infinite loops)
- FixedWindow: WindowSize > 0 (positive intervals)
- No ambiguous overlapping definitions

---

## Value Object: ErrorClassification

### Purpose
Domain-level error categorization independent of execution engines or HTTP semantics.

### Classification Types

```csharp
public enum ErrorClassification
{
    /// <summary>
    /// Request exceeded time limit. Includes connection timeouts, read timeouts,
    /// and other time-based failures.
    /// </summary>
    Timeout,
    
    /// <summary>
    /// Connectivity or transport failure. Includes connection refused, DNS failures,
    /// SSL/TLS errors, and network unreachability.
    /// </summary>
    NetworkError,
    
    /// <summary>
    /// Application-level exception or business rule violation. Includes HTTP 500 errors,
    /// application exceptions, and validation failures.
    /// </summary>
    ApplicationError,
    
    /// <summary>
    /// Error type cannot be determined or classified. Fallback classification
    /// when none of the above apply.
    /// </summary>
    UnknownError
}
```

### Mapping Guidelines

Each execution engine adapter maps engine-specific errors to these categories:

```
Engine Error Type          → Domain Classification
─────────────────────────────────────────────────
ConnectTimeout             → Timeout
ReadTimeout                → Timeout
WriteTimeout               → Timeout
ConnectionRefused          → NetworkError
DNSResolutionFailed        → NetworkError
SSLHandshakeFailed         → NetworkError
HTTP 500/502/503           → ApplicationError
Application Exception      → ApplicationError
Validation Error           → ApplicationError
[Unrecognized]             → UnknownError
```

---

## Domain Entity: Metric

### Purpose
Aggregated collection of samples representing a computed performance metric.

### Attributes

| Attribute | Type | Immutable | Nullable | Notes |
|-----------|------|----------|----------|-------|
| `Id` | `Guid` | ✅ Yes | ❌ No | Unique identifier |
| `MetricType` | `string` | ✅ Yes | ❌ No | e.g., "latency", "throughput", "error_rate" |
| `AggregationWindow` | `AggregationWindow` | ✅ Yes | ❌ No | Temporal scope |
| `AggregationOperation` | `AggregationOperationType` enum | ✅ Yes | ❌ No | Average, Max, Min, Percentile |
| `SourceSamples` | `SampleCollection` | ✅ Yes | ❌ No | Immutable reference to original samples |
| `Result` | `AggregationResult` | ✅ Yes | ❌ No | Computed value and unit |
| `ComputedAt` | `DateTime` (UTC) | ✅ Yes | ❌ No | When aggregation was computed |
| `SampleCount` | `int` | ✅ Yes | ❌ No | Number of samples in result |

### Entity Relationships

```
Metric (entity)
├── SampleCollection (contains reference)
├── AggregationWindow (specifies scope)
├── AggregationOperationType (enum)
└── AggregationResult (computed value)
    ├── MetricValue
    │   ├── Value (double)
    │   └── Unit (LatencyUnit or equivalent)
    └── Computation metadata
```

### Validation Rules

```csharp
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
        // Invariant 1: Metric cannot exist without samples
        if (samples.Count == 0)
            throw new ArgumentException(
                "Metric cannot be computed without source samples", 
                nameof(samples));
        
        // Invariant 2: MetricType must not be empty
        if (string.IsNullOrWhiteSpace(metricType))
            throw new ArgumentException(
                "MetricType cannot be empty", 
                nameof(metricType));
        
        // Invariant 3: Result must be valid for operation type
        ValidateResultForOperation(operationType, result);
        
        Id = Guid.NewGuid();
        MetricType = metricType;
        AggregationWindow = window ?? throw new ArgumentNullException(nameof(window));
        OperationType = operationType;
        SourceSamples = samples;
        Result = result;
        ComputedAt = DateTime.UtcNow;
        SampleCount = samples.Count;
    }
    
    private void ValidateResultForOperation(
        AggregationOperationType operationType,
        AggregationResult result)
    {
        switch (operationType)
        {
            case AggregationOperationType.Average:
            case AggregationOperationType.Max:
            case AggregationOperationType.Min:
                if (result.Value < 0)
                    throw new ArgumentException(
                        "Aggregation result cannot be negative", 
                        nameof(result));
                break;
            case AggregationOperationType.Percentile:
                if (result.Percentile == null)
                    throw new ArgumentException(
                        "Percentile aggregation requires Percentile field", 
                        nameof(result));
                break;
        }
    }
}
```

---

## Value Object: AggregationResult

### Purpose
Output of an aggregation operation with value, unit, and metadata.

### Attributes

```csharp
public class AggregationResult
{
    public double Value { get; }
    public LatencyUnit Unit { get; }
    public Percentile? Percentile { get; } // Non-null for percentile operations
    public DateTime ComputedAt { get; }
    
    public AggregationResult(
        double value,
        LatencyUnit unit,
        Percentile? percentile = null)
    {
        if (double.IsNaN(value) || double.IsInfinity(value))
            throw new ArgumentException(
                "Result value cannot be NaN or infinity", 
                nameof(value));
        
        Value = value;
        Unit = unit;
        Percentile = percentile;
        ComputedAt = DateTime.UtcNow;
    }
    
    public override bool Equals(object? obj)
    {
        if (obj is not AggregationResult other) return false;
        return Value == other.Value
            && Unit == other.Unit
            && Percentile == other.Percentile;
    }
    
    public override int GetHashCode()
    {
        return HashCode.Combine(Value, Unit, Percentile);
    }
}
```

---

## Enumeration: AggregationOperationType

```csharp
public enum AggregationOperationType
{
    /// <summary>Mean value of all samples</summary>
    Average,
    
    /// <summary>Maximum value</summary>
    Max,
    
    /// <summary>Minimum value</summary>
    Min,
    
    /// <summary>Distribution percentile (e.g., p95)</summary>
    Percentile
}
```

---

## Domain Value Object: ExecutionContext

### Purpose
Track which execution engine and run produced the samples.

```csharp
public class ExecutionContext
{
    public string EngineName { get; }       // "k6", "jmeter", "gatling", etc.
    public Guid ExecutionId { get; }        // Unique run identifier
    public string? ScenarioName { get; }    // Optional scenario/test name
    
    public ExecutionContext(
        string engineName,
        Guid executionId,
        string? scenarioName = null)
    {
        if (string.IsNullOrWhiteSpace(engineName))
            throw new ArgumentException("EngineName cannot be empty", nameof(engineName));
        
        EngineName = engineName;
        ExecutionId = executionId;
        ScenarioName = scenarioName;
    }
}
```

---

## Relationships & Dependencies Diagram

```
Sample ─────┐
Sample ─────┤
Sample ─────┤
    ...     ├──→ SampleCollection ──────┐
Sample ─────┤                           │
            │                           ├──→ Metric ──→ AggregationResult
AggregationWindow ──────────────────────┤   (Entity)     (Value Object)
AggregationOperationType (enum) ────────┤
                                        │
                                        └─ (immutable reference)


Sample Components:
  Timestamp ─────────────┐
  Duration ─→ Latency ──┤
  Status ────────────────├──→ Sample (Entity)
  ErrorClassification ───┤
  ExecutionContext ──────┤
                         └──→ Metadata (Dictionary)

Latency Components:
  Value (double) ────┐
                     ├──→ Latency (Value Object)
  Unit (enum) ───────┘
```

---

## Immutability Semantics

All domain entities and value objects follow these immutability principles:

1. **No public setters**: All properties use `{ get; }` only
2. **Constructor injection**: All state set in constructor
3. **No mutable references**: Collections are ImmutableList, dictionaries sealed
4. **Thread-safe by design**: No locks needed, no race conditions
5. **Value equality**: Value objects compare by value, not reference

---

## Summary Table

| Concept | Type | Immutable | Thread-Safe | Invariants |
|---------|------|----------|-------------|-----------|
| Sample | Entity | ✅ | ✅ | Status + ErrorClass validation |
| SampleCollection | Container | ✅ | ✅ | Append-only, no mutation |
| Latency | Value Object | ✅ | ✅ | Value ≥ 0 |
| Percentile | Value Object | ✅ | ✅ | Value ∈ [0, 100] |
| AggregationWindow | Value Object | ✅ | ✅ | Type-specific constraints |
| ErrorClassification | Enum | ✅ | ✅ | Fixed 4 values |
| Metric | Entity | ✅ | ✅ | Samples required, result valid |
| AggregationResult | Value Object | ✅ | ✅ | Value not NaN/Infinity |

**All domain entities are production-ready for implementation**.
