# Implementation Guide: Metrics Domain

**Purpose**: Step-by-step guide for implementing the metrics domain from scratch  
**Audience**: Developers new to the domain, architects reviewing the design  
**Time to Read**: 30 minutes | **Time to Implement**: 2-3 days

---

## Table of Contents

1. [Overview](#overview)
2. [User Story 1: Domain Vocabulary](#user-story-1-domain-vocabulary)
3. [User Story 2: Deterministic Aggregations](#user-story-2-deterministic-aggregations)
4. [User Story 3: Engine-Agnostic Architecture](#user-story-3-engine-agnostic-architecture)
5. [Adapter Templates](#adapter-templates)
6. [Testing Strategy](#testing-strategy)

---

## Overview

This guide walks through the **three user stories** that define the metrics domain:

| User Story | Purpose | Priority | Tests |
|------------|---------|----------|-------|
| **US1** | Define ubiquitous language (Sample, Metric, Latency) | **P1 - MVP** | 79 tests |
| **US2** | Ensure deterministic aggregations | **P1 - MVP** | 34 tests |
| **US3** | Achieve engine-agnostic design | **P2 - Critical** | 49 tests |

**Total**: 162 tests covering all domain logic, aggregations, use cases, and adapters.

---

## User Story 1: Domain Vocabulary

**Goal**: Establish immutable, engine-agnostic domain model so all system components reference only domain terms (Sample, Metric, Latency) rather than engine-specific jargon.

### Phase 1: Foundation - Value Objects

Value objects are **immutable, identity-less** objects defined by their values.

#### 1.1 LatencyUnit Enum

```csharp
// src/Domain/Metrics/LatencyUnit.cs
namespace PerformanceEngine.Metrics.Domain.Domain.Metrics;

/// <summary>
/// Time units for latency measurements.
/// Ordered from smallest to largest for conversion logic.
/// </summary>
public enum LatencyUnit
{
    Nanoseconds = 0,
    Microseconds = 1,
    Milliseconds = 2,
    Seconds = 3
}
```

**Key Points**:
- Explicit ordering enables conversion logic
- No fractional units (use conversion instead)

#### 1.2 Latency Value Object

```csharp
// src/Domain/Metrics/Latency.cs
namespace PerformanceEngine.Metrics.Domain.Domain.Metrics;

/// <summary>
/// Represents a time measurement with automatic unit conversion.
/// </summary>
public class Latency : ValueObject
{
    public double Value { get; }
    public LatencyUnit Unit { get; }

    public Latency(double value, LatencyUnit unit)
    {
        // Invariant 1: No negative latency
        if (value < 0)
            throw new ArgumentException("Latency cannot be negative", nameof(value));

        // Invariant 2: No invalid floating point
        if (double.IsNaN(value) || double.IsInfinity(value))
            throw new ArgumentException("Latency cannot be NaN or Infinity", nameof(value));

        Value = value;
        Unit = unit;
    }

    /// <summary>
    /// Convert latency to a different unit.
    /// </summary>
    public Latency ConvertTo(LatencyUnit targetUnit)
    {
        if (Unit == targetUnit)
            return this;

        var converter = new LatencyUnitConverter();
        var newValue = converter.Convert(Value, Unit, targetUnit);
        return new Latency(newValue, targetUnit);
    }

    // Value object equality
    protected override IEnumerable<object> GetEqualityComponents()
    {
        // Normalize to nanoseconds for comparison
        var normalizedValue = new LatencyUnitConverter().Convert(Value, Unit, LatencyUnit.Nanoseconds);
        yield return normalizedValue;
    }

    public override string ToString() => $"{Value} {Unit}";
}
```

**Key Points**:
- **Immutable**: No setters, values assigned in constructor
- **Validated**: Constructor enforces invariants
- **Comparable**: Equality based on normalized values (45.5ms == 45500μs)

#### 1.3 LatencyUnitConverter

```csharp
// src/Domain/Metrics/LatencyUnitConverter.cs
namespace PerformanceEngine.Metrics.Domain.Domain.Metrics;

/// <summary>
/// Converts latency values between different units.
/// </summary>
public class LatencyUnitConverter
{
    private static readonly Dictionary<(LatencyUnit, LatencyUnit), double> ConversionFactors = new()
    {
        // To Nanoseconds
        [(LatencyUnit.Microseconds, LatencyUnit.Nanoseconds)] = 1000.0,
        [(LatencyUnit.Milliseconds, LatencyUnit.Nanoseconds)] = 1_000_000.0,
        [(LatencyUnit.Seconds, LatencyUnit.Nanoseconds)] = 1_000_000_000.0,

        // To Microseconds
        [(LatencyUnit.Nanoseconds, LatencyUnit.Microseconds)] = 0.001,
        [(LatencyUnit.Milliseconds, LatencyUnit.Microseconds)] = 1000.0,
        [(LatencyUnit.Seconds, LatencyUnit.Microseconds)] = 1_000_000.0,

        // To Milliseconds
        [(LatencyUnit.Nanoseconds, LatencyUnit.Milliseconds)] = 0.000001,
        [(LatencyUnit.Microseconds, LatencyUnit.Milliseconds)] = 0.001,
        [(LatencyUnit.Seconds, LatencyUnit.Milliseconds)] = 1000.0,

        // To Seconds
        [(LatencyUnit.Nanoseconds, LatencyUnit.Seconds)] = 0.000000001,
        [(LatencyUnit.Microseconds, LatencyUnit.Seconds)] = 0.000001,
        [(LatencyUnit.Milliseconds, LatencyUnit.Seconds)] = 0.001,
    };

    public double Convert(double value, LatencyUnit fromUnit, LatencyUnit toUnit)
    {
        if (fromUnit == toUnit)
            return value;

        if (!ConversionFactors.TryGetValue((fromUnit, toUnit), out var factor))
            throw new ArgumentException($"No conversion defined from {fromUnit} to {toUnit}");

        return value * factor;
    }
}
```

**Key Points**:
- **Explicit conversions**: All pairs defined (no chaining)
- **Deterministic**: Fixed factors ensure reproducibility

#### 1.4 Additional Value Objects

```csharp
// src/Domain/Metrics/Percentile.cs
public class Percentile : ValueObject
{
    public double Value { get; }

    public Percentile(double value)
    {
        if (value < 0 || value > 100)
            throw new ArgumentException("Percentile must be between 0 and 100", nameof(value));

        Value = value;
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }
}

// src/Domain/Metrics/ExecutionContext.cs
public class ExecutionContext : ValueObject
{
    public string EngineName { get; }
    public Guid ExecutionId { get; }
    public string ScenarioName { get; }

    public ExecutionContext(string engineName, Guid executionId, string scenarioName)
    {
        if (string.IsNullOrWhiteSpace(engineName))
            throw new ArgumentException("Engine name cannot be empty", nameof(engineName));
        if (string.IsNullOrWhiteSpace(scenarioName))
            throw new ArgumentException("Scenario name cannot be empty", nameof(scenarioName));

        EngineName = engineName;
        ExecutionId = executionId;
        ScenarioName = scenarioName;
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return EngineName;
        yield return ExecutionId;
        yield return ScenarioName;
    }
}
```

### Phase 2: Core Entities

Entities are objects with **identity** (defined by ID, not values).

#### 2.1 Sample Entity

```csharp
// src/Domain/Metrics/Sample.cs
namespace PerformanceEngine.Metrics.Domain.Domain.Metrics;

/// <summary>
/// Represents a single performance measurement from an execution.
/// Immutable after creation.
/// </summary>
public sealed class Sample
{
    public Guid Id { get; }
    public DateTime Timestamp { get; }
    public Latency Duration { get; }
    public SampleStatus Status { get; }
    public ErrorClassification? ErrorClassification { get; }
    public ExecutionContext ExecutionContext { get; }
    public IReadOnlyDictionary<string, object>? Metadata { get; }

    public Sample(
        Guid id,
        DateTime timestamp,
        Latency duration,
        SampleStatus status,
        ErrorClassification? errorClassification,
        ExecutionContext executionContext,
        Dictionary<string, object>? metadata = null)
    {
        // Invariant 1: Timestamp cannot be in the future
        if (timestamp > DateTime.UtcNow)
            throw new ArgumentException("Timestamp cannot be in the future", nameof(timestamp));

        // Invariant 2: Duration must be non-negative (enforced by Latency constructor)
        
        // Invariant 3: Failed samples MUST have error classification
        if (status == SampleStatus.Failure && errorClassification == null)
            throw new ArgumentException("Failed samples must have an error classification", nameof(errorClassification));

        // Invariant 4: Successful samples MUST NOT have error classification
        if (status == SampleStatus.Success && errorClassification != null)
            throw new ArgumentException("Successful samples cannot have an error classification", nameof(errorClassification));

        Id = id;
        Timestamp = timestamp;
        Duration = duration;
        Status = status;
        ErrorClassification = errorClassification;
        ExecutionContext = executionContext;
        Metadata = metadata != null
            ? new Dictionary<string, object>(metadata)
            : null;
    }

    // Factory method for success samples
    public static Sample CreateSuccess(
        DateTime timestamp,
        Latency duration,
        ExecutionContext executionContext,
        Dictionary<string, object>? metadata = null)
    {
        return new Sample(
            id: Guid.NewGuid(),
            timestamp: timestamp,
            duration: duration,
            status: SampleStatus.Success,
            errorClassification: null,
            executionContext: executionContext,
            metadata: metadata
        );
    }

    // Factory method for failed samples
    public static Sample CreateFailure(
        DateTime timestamp,
        Latency duration,
        ErrorClassification errorClassification,
        ExecutionContext executionContext,
        Dictionary<string, object>? metadata = null)
    {
        return new Sample(
            id: Guid.NewGuid(),
            timestamp: timestamp,
            duration: duration,
            status: SampleStatus.Failure,
            errorClassification: errorClassification,
            executionContext: executionContext,
            metadata: metadata
        );
    }
}
```

**Key Points**:
- **4 Invariants** enforced in constructor
- **Factory methods** simplify creation
- **Metadata** preserves engine-specific extras without polluting domain

#### 2.2 SampleCollection (Aggregate Root)

```csharp
// src/Domain/Metrics/SampleCollection.cs
namespace PerformanceEngine.Metrics.Domain.Domain.Metrics;

/// <summary>
/// Immutable collection of samples.
/// All operations return new instances.
/// </summary>
public sealed class SampleCollection
{
    private readonly ImmutableList<Sample> _samples;

    public ImmutableList<Sample> AllSamples => _samples;
    public int Count => _samples.Count;

    public static SampleCollection Empty => new(ImmutableList<Sample>.Empty);

    private SampleCollection(ImmutableList<Sample> samples)
    {
        _samples = samples ?? throw new ArgumentNullException(nameof(samples));
    }

    /// <summary>
    /// Add a sample to the collection.
    /// Returns a NEW collection (immutable pattern).
    /// </summary>
    public SampleCollection Add(Sample sample)
    {
        if (sample == null)
            throw new ArgumentNullException(nameof(sample));

        return new SampleCollection(_samples.Add(sample));
    }

    /// <summary>
    /// Add multiple samples to the collection.
    /// Returns a NEW collection (immutable pattern).
    /// </summary>
    public SampleCollection AddRange(IEnumerable<Sample> samples)
    {
        if (samples == null)
            throw new ArgumentNullException(nameof(samples));

        return new SampleCollection(_samples.AddRange(samples));
    }

    /// <summary>
    /// Filter samples by status.
    /// Returns a NEW collection.
    /// </summary>
    public SampleCollection FilterByStatus(SampleStatus status)
    {
        var filtered = _samples.Where(s => s.Status == status).ToImmutableList();
        return new SampleCollection(filtered);
    }

    /// <summary>
    /// Get samples within a time window.
    /// Returns a NEW collection.
    /// </summary>
    public SampleCollection FilterByTimeWindow(DateTime start, DateTime end)
    {
        var filtered = _samples
            .Where(s => s.Timestamp >= start && s.Timestamp <= end)
            .ToImmutableList();
        return new SampleCollection(filtered);
    }
}
```

**Key Points**:
- **Immutable**: All methods return new instances
- **Functional API**: No mutation, only transformations
- **Empty singleton**: `SampleCollection.Empty` for initial state

---

## User Story 2: Deterministic Aggregations

**Goal**: Ensure that identical sample collections and aggregation parameters produce byte-identical results every time.

### Phase 1: Aggregation Interface

```csharp
// src/Domain/Aggregations/IAggregationOperation.cs
namespace PerformanceEngine.Metrics.Domain.Domain.Aggregations;

/// <summary>
/// Contract for deterministic aggregation operations.
/// </summary>
public interface IAggregationOperation
{
    /// <summary>
    /// Name of the aggregation (e.g., "Average", "Percentile:95").
    /// </summary>
    string OperationName { get; }

    /// <summary>
    /// Compute aggregation over samples.
    /// MUST be deterministic: same inputs → same output.
    /// </summary>
    AggregationResult Compute(
        SampleCollection samples,
        AggregationWindow window,
        DateTime computedAt
    );
}
```

### Phase 2: Sample Normalization

Before aggregating, **normalize all samples to a common unit**.

```csharp
// src/Domain/Aggregations/AggregationNormalizer.cs
namespace PerformanceEngine.Metrics.Domain.Domain.Aggregations;

/// <summary>
/// Normalizes samples to a common unit before aggregation.
/// Ensures deterministic unit handling.
/// </summary>
public class AggregationNormalizer
{
    /// <summary>
    /// Convert all sample latencies to milliseconds.
    /// </summary>
    public SampleCollection NormalizeToMilliseconds(SampleCollection samples)
    {
        var normalized = samples.AllSamples
            .Select(s => new Sample(
                id: s.Id,
                timestamp: s.Timestamp,
                duration: s.Duration.ConvertTo(LatencyUnit.Milliseconds),
                status: s.Status,
                errorClassification: s.ErrorClassification,
                executionContext: s.ExecutionContext,
                metadata: s.Metadata != null
                    ? new Dictionary<string, object>(s.Metadata)
                    : null
            ))
            .ToList();

        return SampleCollection.Empty.AddRange(normalized);
    }

    /// <summary>
    /// Filter out invalid samples before aggregation.
    /// </summary>
    public SampleCollection FilterValid(SampleCollection samples)
    {
        var valid = samples.AllSamples
            .Where(s => s.Duration.Value >= 0)  // No negative latencies
            .Where(s => !double.IsNaN(s.Duration.Value))  // No NaN
            .ToList();

        return SampleCollection.Empty.AddRange(valid);
    }
}
```

### Phase 3: Aggregation Implementations

#### 3.1 Average Aggregation

```csharp
// src/Domain/Aggregations/AverageAggregation.cs
namespace PerformanceEngine.Metrics.Domain.Domain.Aggregations;

public class AverageAggregation : IAggregationOperation
{
    public string OperationName => "Average";

    public AggregationResult Compute(
        SampleCollection samples,
        AggregationWindow window,
        DateTime computedAt)
    {
        // Step 1: Normalize units
        var normalizer = new AggregationNormalizer();
        var normalized = normalizer.NormalizeToMilliseconds(samples);
        normalized = normalizer.FilterValid(normalized);

        // Step 2: Filter successful samples only
        var successSamples = normalized.FilterByStatus(SampleStatus.Success);

        if (successSamples.Count == 0)
            throw new InvalidOperationException("Cannot compute average: no successful samples");

        // Step 3: Compute average
        var average = successSamples.AllSamples
            .Select(s => s.Duration.Value)
            .Average();

        // Step 4: Return result
        return new AggregationResult(
            value: average,
            unit: LatencyUnit.Milliseconds,
            computedAt: computedAt
        );
    }
}
```

#### 3.2 Percentile Aggregation

```csharp
// src/Domain/Aggregations/PercentileAggregation.cs
namespace PerformanceEngine.Metrics.Domain.Domain.Aggregations;

public class PercentileAggregation : IAggregationOperation
{
    private readonly Percentile _percentile;

    public string OperationName => $"Percentile:{_percentile.Value}";

    public PercentileAggregation(double percentileValue)
    {
        _percentile = new Percentile(percentileValue);
    }

    public AggregationResult Compute(
        SampleCollection samples,
        AggregationWindow window,
        DateTime computedAt)
    {
        // Step 1: Normalize
        var normalizer = new AggregationNormalizer();
        var normalized = normalizer.NormalizeToMilliseconds(samples);
        normalized = normalizer.FilterValid(normalized);

        var successSamples = normalized.FilterByStatus(SampleStatus.Success);

        if (successSamples.Count == 0)
            throw new InvalidOperationException($"Cannot compute P{_percentile.Value}: no successful samples");

        // Step 2: Sort values (STABLE SORT for determinism)
        var sortedValues = successSamples.AllSamples
            .Select(s => s.Duration.Value)
            .OrderBy(v => v)
            .ToList();

        // Step 3: Compute percentile index
        // Use "nearest rank" method for determinism
        var index = (int)Math.Ceiling(_percentile.Value / 100.0 * sortedValues.Count) - 1;
        index = Math.Max(0, Math.Min(index, sortedValues.Count - 1));

        var percentileValue = sortedValues[index];

        return new AggregationResult(
            value: percentileValue,
            unit: LatencyUnit.Milliseconds,
            computedAt: computedAt
        );
    }
}
```

**Key Points**:
- **Deterministic sorting**: Always `OrderBy`, never unstable sorts
- **Fixed method**: "Nearest rank" method (no interpolation variations)
- **Normalized units**: All values in milliseconds before computation

---

## User Story 3: Engine-Agnostic Architecture

**Goal**: Adapters translate engine-specific data INTO domain models, ensuring domain never depends on engine specifics.

### Phase 1: Port Definition

```csharp
// src/Ports/IExecutionEngineAdapter.cs
namespace PerformanceEngine.Metrics.Domain.Ports;

/// <summary>
/// Port for adapting engine-specific results to domain models.
/// Each execution engine (K6, JMeter, Gatling) implements this.
/// </summary>
public interface IExecutionEngineAdapter
{
    /// <summary>
    /// Map engine-specific results to domain Sample collection.
    /// </summary>
    /// <param name="rawResults">Engine-specific result objects</param>
    /// <param name="executionId">Unique execution identifier</param>
    /// <param name="scenarioName">Test scenario/plan name</param>
    /// <returns>Engine-agnostic sample collection</returns>
    SampleCollection MapResultsToDomain(
        object rawResults,
        Guid executionId,
        string scenarioName
    );
}
```

### Phase 2: K6 Adapter Implementation

```csharp
// src/Infrastructure/Adapters/K6EngineAdapter.cs
namespace PerformanceEngine.Metrics.Domain.Domain.Infrastructure.Adapters;

/// <summary>
/// Adapter for K6 load testing engine.
/// Maps K6 HTTP check results to domain Sample entities.
/// </summary>
public class K6EngineAdapter : IExecutionEngineAdapter
{
    public SampleCollection MapK6ResultsToDomain(
        IEnumerable<K6ResultData> k6Results,
        Guid executionId,
        string scenarioName)
    {
        if (k6Results == null)
            throw new ArgumentNullException(nameof(k6Results));
        if (string.IsNullOrWhiteSpace(scenarioName))
            throw new ArgumentException("Scenario name cannot be empty", nameof(scenarioName));

        var collection = SampleCollection.Empty;

        foreach (var result in k6Results)
        {
            var sample = MapK6ResultToSample(result, executionId, scenarioName);
            collection = collection.Add(sample);
        }

        return collection;
    }

    private Sample MapK6ResultToSample(
        K6ResultData k6Result,
        Guid executionId,
        string scenarioName)
    {
        // Create execution context
        var context = new ExecutionContext(
            engineName: "K6",
            executionId: executionId,
            scenarioName: scenarioName
        );

        // Create latency
        var latency = new Latency(
            value: k6Result.HttpReqDurationMs,
            unit: LatencyUnit.Milliseconds
        );

        // Determine status and error classification
        var status = k6Result.HttpReqFailed ? SampleStatus.Failure : SampleStatus.Success;
        var errorClassification = k6Result.HttpReqFailed
            ? ClassifyK6Error(k6Result.HttpErrorCode, k6Result.HttpStatusCode)
            : (ErrorClassification?)null;

        // Preserve K6-specific metadata
        var metadata = new Dictionary<string, object>
        {
            ["http_status_code"] = k6Result.HttpStatusCode ?? 0,
            ["http_req_duration_ms"] = k6Result.HttpReqDurationMs,
            ["http_req_failed"] = k6Result.HttpReqFailed
        };
        if (!string.IsNullOrEmpty(k6Result.HttpErrorCode))
        {
            metadata["http_error_code"] = k6Result.HttpErrorCode;
        }

        return new Sample(
            id: Guid.NewGuid(),
            timestamp: k6Result.Timestamp,
            duration: latency,
            status: status,
            errorClassification: errorClassification,
            executionContext: context,
            metadata: metadata
        );
    }

    /// <summary>
    /// Classify K6 error codes into domain error classifications.
    /// </summary>
    private ErrorClassification ClassifyK6Error(string? errorCode, int? statusCode)
    {
        // Network errors
        if (errorCode == "ERR_K6_DIAL_SOCKET" || errorCode == "ERR_K6_SSL")
            return ErrorClassification.NetworkError;

        // Timeout errors
        if (errorCode == "ERR_K6_TIMEOUT")
            return ErrorClassification.Timeout;

        // HTTP status code errors
        if (statusCode.HasValue)
        {
            if (statusCode >= 500 || statusCode >= 400)
                return ErrorClassification.ApplicationError;
        }

        // Default
        return ErrorClassification.UnknownError;
    }

    // K6-specific data structure (not domain model)
    public class K6ResultData
    {
        public DateTime Timestamp { get; set; }
        public double HttpReqDurationMs { get; set; }
        public int? HttpStatusCode { get; set; }
        public bool HttpReqFailed { get; set; }
        public string? HttpErrorCode { get; set; }
    }
}
```

**Key Points**:
- **Engine-specific class**: `K6ResultData` is NOT a domain model
- **Error classification**: Maps K6 error codes → domain classifications
- **Metadata preservation**: Engine extras stored in generic dictionary
- **No K6 in domain**: Domain never imports K6 libraries

### Phase 3: JMeter Adapter Implementation

```csharp
// src/Infrastructure/Adapters/JMeterEngineAdapter.cs
namespace PerformanceEngine.Metrics.Domain.Domain.Infrastructure.Adapters;

/// <summary>
/// Adapter for Apache JMeter load testing engine.
/// Maps JMeter sampler results to domain Sample entities.
/// </summary>
public class JMeterEngineAdapter : IExecutionEngineAdapter
{
    public SampleCollection MapJMeterResultsToDomain(
        IEnumerable<JMeterResultData> jmeterResults,
        Guid executionId,
        string testPlanName)
    {
        if (jmeterResults == null)
            throw new ArgumentNullException(nameof(jmeterResults));
        if (string.IsNullOrWhiteSpace(testPlanName))
            throw new ArgumentException("Test plan name cannot be empty", nameof(testPlanName));

        var collection = SampleCollection.Empty;

        foreach (var result in jmeterResults)
        {
            var sample = MapJMeterResultToSample(result, executionId, testPlanName);
            collection = collection.Add(sample);
        }

        return collection;
    }

    private Sample MapJMeterResultToSample(
        JMeterResultData jmeterResult,
        Guid executionId,
        string testPlanName)
    {
        var context = new ExecutionContext(
            engineName: "JMeter",
            executionId: executionId,
            scenarioName: testPlanName
        );

        var latency = new Latency(
            value: jmeterResult.ElapsedMs,
            unit: LatencyUnit.Milliseconds
        );

        var status = jmeterResult.Success ? SampleStatus.Success : SampleStatus.Failure;
        var errorClassification = !jmeterResult.Success
            ? ClassifyJMeterError(jmeterResult.ResponseMessage, jmeterResult.ResponseCode)
            : (ErrorClassification?)null;

        var metadata = new Dictionary<string, object>
        {
            ["response_code"] = jmeterResult.ResponseCode ?? "N/A",
            ["response_message"] = jmeterResult.ResponseMessage ?? "N/A",
            ["elapsed_ms"] = jmeterResult.ElapsedMs,
            ["success"] = jmeterResult.Success
        };
        if (!string.IsNullOrEmpty(jmeterResult.SamplerLabel))
        {
            metadata["sampler_label"] = jmeterResult.SamplerLabel;
        }

        return new Sample(
            id: Guid.NewGuid(),
            timestamp: jmeterResult.Timestamp,
            duration: latency,
            status: status,
            errorClassification: errorClassification,
            executionContext: context,
            metadata: metadata
        );
    }

    /// <summary>
    /// Classify JMeter errors into domain error classifications.
    /// </summary>
    private ErrorClassification ClassifyJMeterError(string? responseMessage, string? responseCode)
    {
        if (string.IsNullOrEmpty(responseMessage))
            return ErrorClassification.UnknownError;

        // Network errors (Java exceptions)
        if (responseMessage.Contains("java.net.ConnectException") ||
            responseMessage.Contains("java.net.UnknownHostException") ||
            responseMessage.Contains("java.net.SocketException") ||
            responseMessage.Contains("SSLException"))
            return ErrorClassification.NetworkError;

        // Timeout errors
        if (responseMessage.Contains("java.net.SocketTimeoutException") ||
            responseMessage.Contains("java.util.concurrent.TimeoutException"))
            return ErrorClassification.Timeout;

        // HTTP status code errors
        if (!string.IsNullOrEmpty(responseCode) && int.TryParse(responseCode, out var code))
        {
            if (code >= 500 || code >= 400)
                return ErrorClassification.ApplicationError;
        }

        return ErrorClassification.UnknownError;
    }

    // JMeter-specific data structure
    public class JMeterResultData
    {
        public DateTime Timestamp { get; set; }
        public long ElapsedMs { get; set; }
        public string? ResponseCode { get; set; }
        public string? ResponseMessage { get; set; }
        public bool Success { get; set; }
        public string? SamplerLabel { get; set; }
    }
}
```

---

## Adapter Templates

### Template: New Engine Adapter

Use this template when adding a new execution engine (Gatling, Artillery, Locust, etc.):

```csharp
// src/Infrastructure/Adapters/[EngineName]EngineAdapter.cs
namespace PerformanceEngine.Metrics.Domain.Domain.Infrastructure.Adapters;

/// <summary>
/// Adapter for [Engine Name] load testing engine.
/// Maps [Engine] results to domain Sample entities.
/// </summary>
public class [EngineName]EngineAdapter : IExecutionEngineAdapter
{
    /// <summary>
    /// Map [Engine]-specific results to domain samples.
    /// </summary>
    public SampleCollection Map[EngineName]ResultsToDomain(
        IEnumerable<[EngineName]ResultData> results,
        Guid executionId,
        string scenarioName)
    {
        if (results == null)
            throw new ArgumentNullException(nameof(results));
        if (string.IsNullOrWhiteSpace(scenarioName))
            throw new ArgumentException("Scenario name cannot be empty", nameof(scenarioName));

        var collection = SampleCollection.Empty;

        foreach (var result in results)
        {
            var sample = MapResultToSample(result, executionId, scenarioName);
            collection = collection.Add(sample);
        }

        return collection;
    }

    private Sample MapResultToSample(
        [EngineName]ResultData result,
        Guid executionId,
        string scenarioName)
    {
        // 1. Create execution context
        var context = new ExecutionContext(
            engineName: "[Engine Name]",
            executionId: executionId,
            scenarioName: scenarioName
        );

        // 2. Map latency (adjust unit as needed)
        var latency = new Latency(
            value: result.[DurationField],  // e.g., result.ResponseTimeMs
            unit: LatencyUnit.[Unit]        // e.g., LatencyUnit.Milliseconds
        );

        // 3. Determine status
        var status = result.[SuccessField]  // e.g., result.IsSuccess
            ? SampleStatus.Success
            : SampleStatus.Failure;

        // 4. Classify errors
        var errorClassification = status == SampleStatus.Failure
            ? ClassifyError(result.[ErrorField])  // e.g., result.ErrorMessage
            : (ErrorClassification?)null;

        // 5. Preserve engine-specific metadata
        var metadata = new Dictionary<string, object>
        {
            ["[engine_specific_field_1]"] = result.[Field1],
            ["[engine_specific_field_2]"] = result.[Field2],
            // Add all relevant engine fields
        };

        // 6. Create domain sample
        return new Sample(
            id: Guid.NewGuid(),
            timestamp: result.[TimestampField],
            duration: latency,
            status: status,
            errorClassification: errorClassification,
            executionContext: context,
            metadata: metadata
        );
    }

    /// <summary>
    /// Classify engine-specific errors into domain classifications.
    /// </summary>
    private ErrorClassification ClassifyError(string? errorMessage)
    {
        if (string.IsNullOrEmpty(errorMessage))
            return ErrorClassification.UnknownError;

        // Network errors (adjust patterns for your engine)
        if (errorMessage.Contains("[connection error pattern]") ||
            errorMessage.Contains("[DNS error pattern]"))
            return ErrorClassification.NetworkError;

        // Timeout errors
        if (errorMessage.Contains("[timeout pattern]"))
            return ErrorClassification.Timeout;

        // Application errors (HTTP 4xx/5xx)
        if (errorMessage.Contains("[http error pattern]"))
            return ErrorClassification.ApplicationError;

        // Default
        return ErrorClassification.UnknownError;
    }

    /// <summary>
    /// Engine-specific result data structure.
    /// THIS IS NOT A DOMAIN MODEL.
    /// </summary>
    public class [EngineName]ResultData
    {
        public DateTime [TimestampField] { get; set; }
        public double [DurationField] { get; set; }
        public bool [SuccessField] { get; set; }
        public string? [ErrorField] { get; set; }
        // Add all engine-specific fields
    }
}
```

### Checklist for New Adapters

When implementing a new adapter, ensure:

- [ ] **Engine name** is a constant (e.g., "K6", "JMeter", "Gatling")
- [ ] **Error classification** maps ALL engine error types to domain classifications
- [ ] **Metadata** preserves engine-specific fields without leaking into domain
- [ ] **Unit conversion** correctly maps engine's time unit to `LatencyUnit`
- [ ] **Timestamp** is UTC (convert if engine uses local time)
- [ ] **Immutable collection pattern**: `collection = collection.Add(sample)`
- [ ] **Tests** verify: success mapping, failure mapping, error classification, metadata preservation

---

## Testing Strategy

### Test Pyramid

```
            ┌───────────────┐
            │  Integration  │  10 tests (end-to-end)
            │     Tests     │
        ┌───┴───────────────┴───┐
        │   Contract Tests      │  23 tests (adapter compliance)
    ┌───┴───────────────────────┴───┐
    │      Unit Tests               │  129 tests (domain logic)
    └───────────────────────────────┘
```

### Test Categories

#### 1. Domain Unit Tests (129 tests)

Test **individual domain entities and value objects** in isolation.

```csharp
// tests/Domain/LatencyTests.cs
[Fact]
public void Latency_ConvertsMillisecondsToMicroseconds()
{
    var latency = new Latency(45.5, LatencyUnit.Milliseconds);
    
    var converted = latency.ConvertTo(LatencyUnit.Microseconds);
    
    Assert.Equal(45500.0, converted.Value);
    Assert.Equal(LatencyUnit.Microseconds, converted.Unit);
}

[Fact]
public void Latency_RejectsNegativeValue()
{
    var exception = Assert.Throws<ArgumentException>(() =>
        new Latency(-10.0, LatencyUnit.Milliseconds)
    );
    
    Assert.Contains("Latency cannot be negative", exception.Message);
}
```

#### 2. Aggregation Tests (49 tests)

Test **determinism and correctness** of aggregation operations.

```csharp
// tests/Aggregations/DeterminismTests.cs
[Fact]
public void PercentileAggregation_ProducesDeterministicResults()
{
    var samples = CreateSampleCollection();
    var p95 = new PercentileAggregation(95.0);
    
    // Run 1000 times
    var results = new HashSet<double>();
    for (int i = 0; i < 1000; i++)
    {
        var result = p95.Compute(samples, new FullExecutionWindow(), DateTime.UtcNow);
        results.Add(result.Value);
    }
    
    // MUST produce identical result every time
    Assert.Single(results);
}
```

#### 3. Adapter Contract Tests (23 tests)

Test **adapter compliance** with domain expectations.

```csharp
// tests/Infrastructure/K6AdapterTests.cs
[Fact]
public void K6Adapter_MapsTimeoutError_ToTimeoutClassification()
{
    var adapter = new K6EngineAdapter();
    var results = new[]
    {
        new K6EngineAdapter.K6ResultData
        {
            Timestamp = DateTime.UtcNow.AddSeconds(-5),
            HttpReqDurationMs = 30000,
            HttpStatusCode = null,
            HttpReqFailed = true,
            HttpErrorCode = "ERR_K6_TIMEOUT"
        }
    };
    
    var samples = adapter.MapK6ResultsToDomain(results, Guid.NewGuid(), "LoadTest");
    
    Assert.Single(samples.AllSamples);
    var sample = samples.AllSamples[0];
    Assert.Equal(SampleStatus.Failure, sample.Status);
    Assert.Equal(ErrorClassification.Timeout, sample.ErrorClassification);
}
```

#### 4. Integration Tests (10 tests)

Test **end-to-end workflows** from adapter → use case → DTO.

```csharp
// tests/Application/MetricServiceIntegrationTests.cs
[Fact]
public void MetricService_ComputesP95_FromK6Results()
{
    // Arrange
    var k6Results = CreateK6Results();
    var adapter = new K6EngineAdapter();
    var samples = adapter.MapK6ResultsToDomain(k6Results, Guid.NewGuid(), "API Test");
    
    var service = new MetricService();
    var request = new AggregationRequestDto
    {
        Samples = samples.AllSamples.ToList(),
        Window = "FullExecution",
        AggregationOperation = "Percentile:95"
    };
    
    // Act
    var metricDto = service.ComputeMetric(request);
    
    // Assert
    Assert.NotNull(metricDto);
    Assert.Equal("Percentile:95", metricDto.MetricType);
    Assert.Single(metricDto.AggregatedValues);
    Assert.True(metricDto.AggregatedValues[0].Value > 0);
}
```

---

## Summary

This implementation guide covered:

1. **User Story 1**: Building immutable domain vocabulary (Sample, Latency, Metric)
2. **User Story 2**: Implementing deterministic aggregations (Average, Max, Min, Percentile)
3. **User Story 3**: Creating engine-agnostic adapters (K6, JMeter)

### Key Principles

✅ **Immutability**: All domain objects immutable after construction  
✅ **Determinism**: Same inputs → same outputs (reproducible)  
✅ **Engine-Agnostic**: Domain knows nothing about K6/JMeter/Gatling  
✅ **Clean Architecture**: Dependencies flow inward (Infrastructure → Domain)  
✅ **Comprehensive Testing**: 162 tests covering all scenarios

### Next Steps

- Add new aggregation operations (Median, Standard Deviation)
- Implement additional adapters (Gatling, Artillery, Locust)
- Add persistence layer (database repositories)
- Implement event sourcing for metric history

---

**Happy Implementing! 🚀**
