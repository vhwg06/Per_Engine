# API Contracts: Core Domain Enrichment - Metrics Domain

**Completed**: 2026-01-15  
**Scope**: Metrics Domain enrichment interfaces and data contracts  
**Format**: C# interface specifications with JSON serialization examples

---

## Port: IMetric (Extended)

**Namespace**: `PerformanceEngine.Metrics.Domain.Ports`

**Responsibility**: Engine-agnostic interface for accessing metrics with completeness metadata

### Interface Definition

```csharp
public interface IMetric
{
    /// <summary>
    /// Unique identifier for this metric.
    /// </summary>
    Guid Id { get; }
    
    /// <summary>
    /// Aggregation name (e.g., "p95", "p99", "error_rate").
    /// </summary>
    string AggregationName { get; }
    
    /// <summary>
    /// Aggregated value (e.g., 195.5 for p95 in milliseconds).
    /// </summary>
    double Value { get; }
    
    /// <summary>
    /// Unit of measurement (e.g., "ms", "percent").
    /// </summary>
    string Unit { get; }
    
    /// <summary>
    /// Timestamp when metric was computed (UTC).
    /// </summary>
    DateTime ComputedAt { get; }
    
    /// <summary>
    /// NEW: Reliability status of metric (COMPLETE or PARTIAL).
    /// </summary>
    CompletessStatus CompletessStatus { get; }
    
    /// <summary>
    /// NEW: Evidence metadata for reliability assessment (sample count, window reference).
    /// </summary>
    MetricEvidence GetEvidence();
}
```

### JSON Serialization Contract

```json
{
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "aggregationName": "p95",
  "value": 195.5,
  "unit": "ms",
  "computedAt": "2026-01-15T12:34:56.789Z",
  "completessStatus": "COMPLETE",
  "evidence": {
    "sampleCount": 100,
    "requiredSampleCount": 100,
    "aggregationWindow": "5m",
    "isComplete": true
  }
}
```

### Contract Examples

#### Example 1: Complete Metric (All Samples Collected)

```csharp
var metric = new Metric(
    id: Guid.NewGuid(),
    aggregationName: "p95",
    value: 195.5,
    unit: "ms",
    computedAt: DateTime.UtcNow,
    completessStatus: CompletessStatus.COMPLETE,
    evidence: new MetricEvidence(
        sampleCount: 100,
        requiredSampleCount: 100,
        aggregationWindow: "5m"));

Assert.Equal(CompletessStatus.COMPLETE, metric.CompletessStatus);
Assert.True(metric.GetEvidence().IsComplete);
Assert.Equal(100, metric.GetEvidence().SampleCount);
```

#### Example 2: Partial Metric (Incomplete Data)

```csharp
var metric = new Metric(
    id: Guid.NewGuid(),
    aggregationName: "error_rate",
    value: 0.025,
    unit: "percent",
    computedAt: DateTime.UtcNow,
    completessStatus: CompletessStatus.PARTIAL,
    evidence: new MetricEvidence(
        sampleCount: 45,
        requiredSampleCount: 100,
        aggregationWindow: "5m"));

Assert.Equal(CompletessStatus.PARTIAL, metric.CompletessStatus);
Assert.False(metric.GetEvidence().IsComplete);
Assert.Equal(45, metric.GetEvidence().SampleCount);
```

---

## Enum: CompletessStatus

**Namespace**: `PerformanceEngine.Metrics.Domain`

### Definition

```csharp
public enum CompletessStatus
{
    COMPLETE = 1,
    PARTIAL = 2
}
```

### Semantics

| Status | Meaning | Use Case |
|--------|---------|----------|
| **COMPLETE** | All required samples collected; metric is reliable | Safe to use in evaluation without restrictions |
| **PARTIAL** | Data incomplete; should be used with caution or skipped | Requires explicit rule allowance or evaluation returns INCONCLUSIVE |

---

## Value Object: MetricEvidence

**Namespace**: `PerformanceEngine.Metrics.Domain`

### Definition

```csharp
public sealed class MetricEvidence : ValueObject
{
    /// <summary>
    /// Number of samples collected.
    /// </summary>
    public int SampleCount { get; }
    
    /// <summary>
    /// Required sample count for COMPLETE status.
    /// Defined by aggregation configuration; Metrics Domain owns this threshold.
    /// </summary>
    public int RequiredSampleCount { get; }
    
    /// <summary>
    /// Aggregation window reference (e.g., "5m", "1h", "10000-sample").
    /// Identifies the time period or sample batch used for aggregation.
    /// </summary>
    public string AggregationWindow { get; }
    
    /// <summary>
    /// Convenience property: true if SampleCount >= RequiredSampleCount.
    /// </summary>
    public bool IsComplete => SampleCount >= RequiredSampleCount;
    
    public MetricEvidence(int sampleCount, int requiredSampleCount, string aggregationWindow)
    {
        // Validation...
    }
}
```

### JSON Serialization Contract

```json
{
  "sampleCount": 95,
  "requiredSampleCount": 100,
  "aggregationWindow": "5m",
  "isComplete": false
}
```

### Equality Contract

Two `MetricEvidence` objects are equal if:
```csharp
evidence1.SampleCount == evidence2.SampleCount &&
evidence1.RequiredSampleCount == evidence2.RequiredSampleCount &&
evidence1.AggregationWindow == evidence2.AggregationWindow
```

---

## Port: IMetricProvider (Unchanged)

**Namespace**: `PerformanceEngine.Metrics.Domain.Ports`

**Note**: Existing port; no changes required. Returns `IMetric` instances which now include enrichment data.

```csharp
public interface IMetricProvider
{
    IMetric GetMetric(string aggregationName);
    IReadOnlyList<IMetric> GetAllMetrics();
}
```

---

## Backward Compatibility

### Migration Path

**Before Enrichment**:
```csharp
public interface IMetric
{
    Guid Id { get; }
    string AggregationName { get; }
    double Value { get; }
    string Unit { get; }
    DateTime ComputedAt { get; }
}
```

**After Enrichment**:
```csharp
public interface IMetric
{
    // All previous properties (unchanged)
    Guid Id { get; }
    string AggregationName { get; }
    double Value { get; }
    string Unit { get; }
    DateTime ComputedAt { get; }
    
    // NEW properties
    CompletessStatus CompletessStatus { get; }
    MetricEvidence GetEvidence();
}
```

**Compatibility Note**: Existing implementations of `IMetric` must be updated to provide these new properties. Default behavior can be:
- `CompletessStatus` = `COMPLETE` (assume all data is complete by default)
- `GetEvidence()` → `MetricEvidence` with `SampleCount = RequiredSampleCount` (perfect collection)

---

## Contract Verification Tests

### Test: Completeness Status Immutability

```csharp
[Fact]
public void CompletessStatus_IsImmutable_AfterConstruction()
{
    var metric = new Metric(/* ... */);
    
    // Verify no setter exists
    var property = typeof(Metric).GetProperty(nameof(Metric.CompletessStatus));
    Assert.Null(property?.SetMethod);
}
```

### Test: Evidence Consistency

```csharp
[Fact]
public void Evidence_IsConsistent_WithCompletessStatus()
{
    // Metric with COMPLETE status must have SampleCount >= RequiredSampleCount
    var metric = new Metric(
        aggregationName: "p95",
        value: 100,
        completessStatus: CompletessStatus.COMPLETE,
        evidence: new MetricEvidence(95, 100, "5m"));
    
    // This should throw or be prevented (invariant violation)
}
```

### Test: JSON Serialization Roundtrip

```csharp
[Fact]
public void MetricEvidence_SerializesAndDeserializes_Deterministically()
{
    var evidence = new MetricEvidence(95, 100, "5m");
    var json = JsonConvert.SerializeObject(evidence);
    var deserialized = JsonConvert.DeserializeObject<MetricEvidence>(json);
    
    Assert.Equal(evidence, deserialized);
}
```

---

**Status**: ✅ Metrics Domain Contract Complete
