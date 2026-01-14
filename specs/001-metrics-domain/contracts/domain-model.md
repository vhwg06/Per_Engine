# Contracts: Domain Model & Ports

**Scope**: Interface contracts and behavioral expectations for domain layer and ports  
**Audience**: Developers implementing domain entities and adapters

---

## Section 1: Domain Model Contracts

### Contract: Sample Immutability

**Requirement**: Sample objects are fully immutable after construction.

**Specification**:
- No property setter can be called after construction
- Calling any mutating method throws `NotSupportedException` or compiles to error
- Sample equality determined by value, not reference (all properties equal)

**Verification**:
```csharp
// Test: Sample cannot be mutated
var sample = new Sample(...);
Assert.Throws<InvalidOperationException>(() => sample.Duration = new Latency(100, LatencyUnit.Milliseconds));

// Test: Equality by value
var sample1 = new Sample(timestamp, duration, status, error, context);
var sample2 = new Sample(timestamp, duration, status, error, context);
Assert.Equal(sample1, sample2); // Same content = equal
```

### Contract: Sample Status & Error Classification Consistency

**Requirement**: Status and ErrorClassification must be logically consistent.

**Specification**:
- If `Status == SampleStatus.Success`, then `ErrorClassification == null`
- If `Status == SampleStatus.Failure`, then `ErrorClassification != null` (one of: Timeout, NetworkError, ApplicationError, UnknownError)
- Violations throw `ArgumentException` at construction time

**Verification**:
```csharp
// Test: Success without error allowed
var sample = new Sample(..., SampleStatus.Success, errorClassification: null, ...);
Assert.NotNull(sample);

// Test: Success with error throws
Assert.Throws<ArgumentException>(() =>
    new Sample(..., SampleStatus.Success, ErrorClassification.Timeout, ...));

// Test: Failure without error throws
Assert.Throws<ArgumentException>(() =>
    new Sample(..., SampleStatus.Failure, errorClassification: null, ...));
```

### Contract: Latency Non-Negative

**Requirement**: Latency value must always be ≥ 0.

**Specification**:
- `new Latency(value, unit)` throws `ArgumentException` if value < 0
- Latency equality compares by normalized nanosecond value
- Unit conversions are loss-less (e.g., 100ms = 100,000,000ns exactly)

**Verification**:
```csharp
// Test: Negative latency rejected
Assert.Throws<ArgumentException>(() => new Latency(-100, LatencyUnit.Milliseconds));

// Test: Zero latency allowed
var zero = new Latency(0, LatencyUnit.Milliseconds);
Assert.Equal(0, zero.Value);

// Test: Equality across units
var latency1 = new Latency(1, LatencyUnit.Seconds);
var latency2 = new Latency(1_000, LatencyUnit.Milliseconds);
Assert.Equal(latency1, latency2);
```

### Contract: Percentile Range [0, 100]

**Requirement**: Percentile value must be within [0, 100] inclusive.

**Specification**:
- `new Percentile(value)` throws `ArgumentException` if value < 0 or value > 100
- Percentile equality by value (e.g., p95 == 95m exactly)
- Precision: Up to 3 decimal places (e.g., p99.9)

**Verification**:
```csharp
// Test: Valid percentiles
Assert.NotNull(new Percentile(0));
Assert.NotNull(new Percentile(50));
Assert.NotNull(new Percentile(95));
Assert.NotNull(new Percentile(99.9m));
Assert.NotNull(new Percentile(100));

// Test: Invalid percentiles
Assert.Throws<ArgumentException>(() => new Percentile(-0.1m));
Assert.Throws<ArgumentException>(() => new Percentile(100.1m));
```

### Contract: Metric Requires Samples

**Requirement**: Metric cannot be created without source samples.

**Specification**:
- `new Metric(..., sampleCollection, ...)` throws `ArgumentException` if `sampleCollection.Count == 0`
- Metric maintains immutable reference to source `SampleCollection`
- Metric.SourceSamples never changes after creation

**Verification**:
```csharp
// Test: Empty collection rejected
var emptyCollection = new SampleCollection();
Assert.Throws<ArgumentException>(() => 
    new Metric("latency", window, AggregationOperationType.Average, emptyCollection, result));

// Test: Non-empty collection accepted
var collection = new SampleCollection().Add(sample);
var metric = new Metric("latency", window, AggregationOperationType.Average, collection, result);
Assert.NotNull(metric);
```

### Contract: AggregationWindow Constraints

**Requirement**: AggregationWindow subtypes enforce specific constraints.

**Specification**:

**FullExecutionWindow**:
- No constraints
- Single instance sufficient for entire execution

**SlidingWindow**:
- WindowSize > 0
- StepSize > 0
- StepSize < WindowSize (prevents infinite loops)

**FixedWindow**:
- WindowSize > 0

**Verification**:
```csharp
// Test: SlidingWindow constraints
Assert.Throws<ArgumentException>(() => new SlidingWindow(TimeSpan.Zero, TimeSpan.FromSeconds(1)));
Assert.Throws<ArgumentException>(() => new SlidingWindow(TimeSpan.FromSeconds(1), TimeSpan.Zero));
Assert.Throws<ArgumentException>(() => new SlidingWindow(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2)));

// Test: Valid SlidingWindow
var window = new SlidingWindow(TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(1));
Assert.NotNull(window);
```

---

## Section 2: Port Contracts (Inbound & Outbound)

### Port: IExecutionEngineAdapter (Outbound Adapter Interface)

**Purpose**: Abstract adaptation layer for mapping engine-specific results to domain Sample objects.

**Location**: Domain.Ports namespace (infrastructure implements)

**Interface Contract**:

```csharp
public interface IExecutionEngineAdapter
{
    /// <summary>
    /// Map engine-specific result format to domain Sample objects.
    /// 
    /// Responsibility of adapter:
    /// - Parse engine output format (JSON, CSV, XML, etc.)
    /// - Convert time units to domain Latency
    /// - Classify engine errors into domain ErrorClassification
    /// - Preserve engine-specific metadata
    /// 
    /// </summary>
    /// <param name="engineResultData">Raw engine output data (string/bytes)</param>
    /// <returns>Enumerable of normalized domain Sample objects</returns>
    /// <exception cref="FormatException">Engine output format invalid</exception>
    /// <exception cref="ArgumentException">Data not compatible with this adapter</exception>
    Task<IEnumerable<Sample>> MapResultsToSamplesAsync(string engineResultData);
    
    /// <summary>
    /// Verify whether this adapter can handle the given engine result format.
    /// Used by adapter registry to select appropriate mapper.
    /// </summary>
    /// <param name="engineFormat">Engine identifier (e.g., "k6", "jmeter", "gatling")</param>
    /// <returns>True if this adapter can handle the format</returns>
    bool CanHandle(string engineFormat);
    
    /// <summary>
    /// Human-readable description of engine and supported format.
    /// </summary>
    string Description { get; }
}
```

**Behavioral Contract**:
1. **Determinism**: Identical input always produces identical samples (byte-equality)
2. **Error Classification**: Every engine error maps to one of: Timeout, NetworkError, ApplicationError, UnknownError
3. **Metadata Preservation**: Original engine error code/message stored in Sample.Metadata
4. **No Data Loss**: All relevant engine fields preserved (either in Sample or Metadata)
5. **Immutability**: Returned samples are immutable

**Example Implementation Structure**:
```csharp
public class K6EngineAdapter : IExecutionEngineAdapter
{
    public bool CanHandle(string engineFormat) => engineFormat.Equals("k6", StringComparison.OrdinalIgnoreCase);
    public string Description => "k6 Engine Adapter - Maps k6 JSON results to domain models";
    
    public async Task<IEnumerable<Sample>> MapResultsToSamplesAsync(string engineResultData)
    {
        // Parse JSON
        // For each k6 sample:
        //   - Convert time units (ms → domain unit)
        //   - Map error code → ErrorClassification
        //   - Preserve k6_error_code in metadata
        // Return samples
    }
}
```

### Port: IPersistenceRepository (Outbound Adapter Interface)

**Purpose**: Abstract persistence layer for storing and querying metrics.

**Location**: Domain.Ports namespace (infrastructure implements)

**Interface Contract**:

```csharp
public interface IPersistenceRepository
{
    /// <summary>
    /// Persist computed metrics to storage.
    /// </summary>
    /// <param name="metrics">Enumerable of Metric objects to save</param>
    /// <exception cref="InvalidOperationException">Storage unavailable or quota exceeded</exception>
    Task SaveMetricsAsync(IEnumerable<Metric> metrics);
    
    /// <summary>
    /// Query metrics by aggregation window specification.
    /// </summary>
    /// <param name="window">Aggregation window to match</param>
    /// <returns>Enumerable of matching metrics</returns>
    Task<IEnumerable<Metric>> QueryMetricsByWindowAsync(AggregationWindow window);
    
    /// <summary>
    /// Query source samples for a specific metric (for audit trail).
    /// </summary>
    /// <param name="metricId">Metric identifier</param>
    /// <returns>Enumerable of original samples used in computation</returns>
    Task<IEnumerable<Sample>> QuerySamplesByMetricAsync(Guid metricId);
    
    /// <summary>
    /// Optional: Delete metrics matching retention policy.
    /// </summary>
    /// <param name="before">Delete metrics computed before this date (inclusive)</param>
    /// <returns>Number of metrics deleted</returns>
    Task<int> DeleteMetricsBeforeAsync(DateTime before);
}
```

**Behavioral Contract**:
1. **Atomicity**: SaveMetricsAsync saves all or none (transaction semantics)
2. **Queries are read-only**: No side effects on stored metrics
3. **Audit Trail**: Source samples always retrievable for audit
4. **Idempotency**: SaveMetricsAsync idempotent (same metrics stored twice = OK)

---

## Section 3: Aggregation Operation Contracts

### Contract: Deterministic Aggregation

**Requirement**: All aggregation operations produce identical results given identical input.

**Specification**:

```csharp
// Contract signature (internal interface)
public interface IAggregationOperation
{
    AggregationResult Aggregate(SampleCollection samples);
}
```

**Behavioral Contract**:

For any aggregation operation `op` and sample collection `samples`:

```
Aggregate(samples, op) == Aggregate(samples, op)  [across multiple runs]
Aggregate(samples, op).Value == Aggregate(samples, op).Value  [exact equality]
```

**Normalization Requirement**:
- Input samples must be normalized before aggregation (consistent time units)
- Normalization rules: All latencies converted to canonical unit (nanoseconds) before aggregation

**Verification Strategy**:
```csharp
// Test template for all aggregations
[Fact]
public void DeterminismTest_Run10000Times_ProducesIdenticalResults()
{
    var samples = CreateTestSampleCollection(1000);
    var operation = new AverageAggregation();
    
    var results = new List<AggregationResult>();
    for (int i = 0; i < 10000; i++)
    {
        results.Add(operation.Aggregate(samples));
    }
    
    // All results identical (bytes)
    var firstResult = results[0];
    foreach (var result in results.Skip(1))
    {
        Assert.Equal(firstResult.Value, result.Value, precision: 15);
        Assert.Equal(firstResult.Unit, result.Unit);
    }
}
```

### Contract: Average Aggregation

**Specification**:
- **Input**: SampleCollection with Latency values
- **Output**: AggregationResult with single mean value
- **Formula**: `mean = sum(latencies) / count`
- **Precision**: Decimal with 4 decimal places (0.0001 ms precision)

**Edge Cases**:
- Empty collection: Throws `ArgumentException`
- Single sample: Returns that sample's duration
- Large datasets (1M+ samples): Verify no numerical instability

### Contract: Max Aggregation

**Specification**:
- **Input**: SampleCollection with Latency values
- **Output**: AggregationResult with maximum value
- **Formula**: `max = maximum(latencies)`

**Edge Cases**:
- Empty collection: Throws `ArgumentException`
- Single sample: Returns that sample's duration
- All equal values: Returns any value

### Contract: Min Aggregation

**Specification**:
- **Input**: SampleCollection with Latency values
- **Output**: AggregationResult with minimum value
- **Formula**: `min = minimum(latencies)`

**Edge Cases**:
- Empty collection: Throws `ArgumentException`
- Single sample: Returns that sample's duration
- All equal values: Returns any value

### Contract: Percentile Aggregation

**Specification**:
- **Input**: SampleCollection + Percentile value (e.g., p95)
- **Output**: AggregationResult with percentile value
- **Algorithm**: Nearest-rank method
  - Rank = ceil(P/100 * N) where P=percentile, N=sample count
  - Select value at position Rank in sorted array

**Edge Cases**:
- Empty collection: Throws `ArgumentException`
- Single sample with any percentile: Returns that sample
- p0 (minimum): Returns smallest value
- p100 (maximum): Returns largest value
- Duplicate values: Handles gracefully (sorted order)

**Example**:
```
Samples: [10, 20, 30, 40, 50] (sorted)
p50 (50%): Rank = ceil(50/100 * 5) = 3 → value[3-1] = 30 (median) ✅
p95 (95%): Rank = ceil(95/100 * 5) = 5 → value[5-1] = 50
```

---

## Section 4: Use Case Contracts (Application Layer)

### Use Case: ComputeMetricUseCase

**Input**:
- SampleCollection
- AggregationWindow
- AggregationOperationType
- Optional: Percentile (if operation is Percentile)

**Output**:
- Metric (domain entity with computed result)

**Behavioral Contract**:
1. **Validation**: Input samples normalized (unit consistency check)
2. **Determinism**: Same inputs produce identical metric
3. **Immutability**: Returned metric is fully immutable
4. **Composition**: Does not call external systems (pure function)

**Error Handling**:
- Empty samples → `ArgumentException`
- Invalid aggregation parameters → `ArgumentException`
- Aggregation failure → `InvalidOperationException`

### Use Case: NormalizeSamplesUseCase

**Input**:
- SampleCollection (potentially with mixed units)

**Output**:
- SampleCollection (all latencies normalized to canonical unit)

**Behavioral Contract**:
1. **Non-destructive**: Original samples unchanged (new collection created)
2. **Determinism**: Same input produces identical normalized output
3. **Lossless**: No data loss in unit conversion

---

## Section 5: Domain Event Contracts

### Event: SampleCollectedEvent

**Purpose**: Raised when a sample is created (for audit trail).

**Attributes**:
- `SampleId`: Guid
- `Timestamp`: DateTime UTC
- `Duration`: Latency
- `Status`: SampleStatus
- `ExecutionContext`: ExecutionContext
- `SourceEngine`: string
- `RaisedAt`: DateTime UTC

### Event: MetricComputedEvent

**Purpose**: Raised when a metric is computed (for downstream systems).

**Attributes**:
- `MetricId`: Guid
- `MetricType`: string
- `AggregationWindow`: AggregationWindow
- `Result`: AggregationResult
- `SampleCount`: int
- `ComputedAt`: DateTime UTC

---

## Verification Matrix

| Contract | Test Type | Expected Result |
|----------|-----------|---|
| Sample Immutability | Unit | ✅ No mutations allowed |
| Status/Error Consistency | Unit | ✅ Enforced at construction |
| Latency Non-Negative | Unit | ✅ Negative rejected |
| Percentile [0,100] | Unit | ✅ Out-of-range rejected |
| Metric Requires Samples | Unit | ✅ Empty rejected |
| Deterministic Aggregation | Contract | ✅ 10k runs identical |
| Adapter Determinism | Contract | ✅ Identical samples from same input |
| Port Isolation | Integration | ✅ Domain works without implementations |

---

## Summary

All contracts are:
- ✅ Technology-agnostic (no C#-specific keywords in specification)
- ✅ Testable (explicit verification strategies)
- ✅ Independent (no circular dependencies)
- ✅ Deterministic (reproducible, no randomness)
- ✅ Domain-pure (no infrastructure leakage)
