# Domain Contracts: Baseline Domain

**Status**: Design Phase 1  
**Date**: 2026-01-15  
**Purpose**: Define domain-level interfaces and contracts (no implementation)  

---

## Contract 1: Baseline Aggregate Interface

**Boundary**: Domain Layer  
**Ownership**: Baseline Domain  
**Stability**: Stable (immutability enforced; no breaking changes expected)

### Interface Definition

```csharp
namespace PerformanceEngine.Baseline.Domain.Domain.Baselines;

/// <summary>
/// Immutable snapshot of metrics and evaluation results serving as a comparison reference.
/// 
/// Invariants:
/// - Baseline is completely immutable after construction (no setters, no modifications)
/// - Must contain at least one metric (empty baselines rejected)
/// - Metric names must be unique within baseline
/// - All metrics must have corresponding tolerance rules
/// - Created timestamp captures snapshot moment; immutable thereafter
/// </summary>
public interface IBaseline
{
    /// <summary>
    /// Unique identifier for this baseline snapshot.
    /// </summary>
    BaselineId Id { get; }
    
    /// <summary>
    /// Immutable timestamp when this baseline snapshot was captured.
    /// </summary>
    DateTime CreatedAt { get; }
    
    /// <summary>
    /// Immutable collection of metrics from the baseline execution.
    /// </summary>
    IReadOnlyList<IMetric> Metrics { get; }
    
    /// <summary>
    /// Immutable collection of evaluation results (optional).
    /// </summary>
    IReadOnlyList<string> EvaluationResults { get; }
    
    /// <summary>
    /// Tolerance configuration for comparing against this baseline.
    /// </summary>
    ToleranceConfiguration ToleranceConfig { get; }
    
    /// <summary>
    /// Retrieve a specific metric from the baseline by name.
    /// </summary>
    /// <param name="metricName">Name of metric to retrieve</param>
    /// <returns>Metric if found; null if not in baseline</returns>
    IMetric? GetMetric(string metricName);
}
```

### Contract Semantics

**Creation**:
- Factory creates baseline with: ID, metrics collection, tolerance config, optional evaluation results
- Constructor validates: non-empty metrics, unique names, valid tolerance config
- Throws `DomainException` if invariants violated

**Querying**:
- `GetMetric(name)` returns metric or null (never throws for missing metric)
- All properties read-only (no setters, no mutation methods)

**Immutability Guarantee**:
- No property setter exists
- Metrics collection read-only (no Add/Remove)
- Baseline cannot be modified after construction
- Same baseline instance always has same state

**Equality Semantics**:
- Two baselines equal if BaselineId values equal
- Not by metric content (different metrics with same ID = same baseline)

---

## Contract 2: Comparison & Result

**Boundary**: Domain Layer  
**Ownership**: Baseline Domain  
**Stability**: Stable (comparison logic deterministic; outcomes immutable)

### Comparison Request

```csharp
namespace PerformanceEngine.Baseline.Domain.Domain.Comparisons;

/// <summary>
/// Input specification for a deterministic comparison operation.
/// 
/// Contract: Given identical ComparisonRequest, comparison always produces identical result.
/// </summary>
public interface IComparisonRequest
{
    /// <summary>
    /// Identifies which baseline to compare against.
    /// </summary>
    BaselineId BaselineId { get; }
    
    /// <summary>
    /// Current metrics from latest test execution.
    /// </summary>
    IReadOnlyList<IMetric> CurrentMetrics { get; }
    
    /// <summary>
    /// Tolerance configuration for this comparison.
    /// </summary>
    ToleranceConfiguration ToleranceConfig { get; }
    
    /// <summary>
    /// Minimum confidence threshold for conclusive results [0.0, 1.0].
    /// Results below threshold marked INCONCLUSIVE regardless of change magnitude.
    /// Default: 0.7 (70%)
    /// </summary>
    double ConfidenceThreshold { get; }
    
    /// <summary>
    /// Optional context info for debugging/tracing (e.g., "CI run #123").
    /// </summary>
    string? ContextInfo { get; }
}
```

### Comparison Result

```csharp
namespace PerformanceEngine.Baseline.Domain.Domain.Comparisons;

/// <summary>
/// Immutable outcome of comparing current metrics against a baseline.
/// 
/// Invariants:
/// - Result is completely immutable after construction
/// - Must contain at least one metric comparison
/// - Overall outcome is worst-case aggregation of metric outcomes
/// - Confidence values in [0.0, 1.0] range
/// - Timestamp captures comparison moment
/// </summary>
public interface IComparisonResult
{
    /// <summary>
    /// Unique identifier for this comparison result.
    /// </summary>
    string Id { get; }
    
    /// <summary>
    /// Identifies which baseline was used.
    /// </summary>
    BaselineId BaselineId { get; }
    
    /// <summary>
    /// Timestamp when comparison was performed.
    /// </summary>
    DateTime ComparedAt { get; }
    
    /// <summary>
    /// Overall comparison outcome (aggregated from all metrics).
    /// Priority: REGRESSION > IMPROVEMENT > NO_SIGNIFICANT_CHANGE > INCONCLUSIVE
    /// </summary>
    ComparisonOutcome OverallOutcome { get; }
    
    /// <summary>
    /// Overall confidence in the outcome [0.0, 1.0].
    /// Calculated as minimum confidence across all metrics.
    /// </summary>
    ConfidenceLevel OverallConfidence { get; }
    
    /// <summary>
    /// Per-metric comparison details.
    /// </summary>
    IReadOnlyList<IComparisonMetric> MetricResults { get; }
    
    /// <summary>
    /// Quick check: did any metric regress?
    /// </summary>
    bool HasRegression();
}

/// <summary>
/// Per-metric comparison details.
/// </summary>
public interface IComparisonMetric
{
    /// <summary>
    /// Name of this metric (e.g., "p95_latency").
    /// </summary>
    string MetricName { get; }
    
    /// <summary>
    /// Baseline value for this metric.
    /// </summary>
    double BaselineValue { get; }
    
    /// <summary>
    /// Current value for this metric.
    /// </summary>
    double CurrentValue { get; }
    
    /// <summary>
    /// Absolute change = Current - Baseline.
    /// </summary>
    double AbsoluteChange { get; }
    
    /// <summary>
    /// Relative change as percentage: (Current - Baseline) / Baseline * 100.
    /// </summary>
    double RelativeChange { get; }
    
    /// <summary>
    /// Tolerance rule applied to this metric.
    /// </summary>
    Tolerance Tolerance { get; }
    
    /// <summary>
    /// Per-metric outcome: IMPROVEMENT, REGRESSION, NO_SIGNIFICANT_CHANGE, or INCONCLUSIVE.
    /// </summary>
    ComparisonOutcome Outcome { get; }
    
    /// <summary>
    /// Confidence in this metric's outcome [0.0, 1.0].
    /// </summary>
    ConfidenceLevel Confidence { get; }
}

/// <summary>
/// Comparison outcome states.
/// </summary>
public enum ComparisonOutcome
{
    /// <summary>
    /// Metric improved beyond tolerance threshold.
    /// </summary>
    IMPROVEMENT,
    
    /// <summary>
    /// Metric regressed beyond tolerance threshold.
    /// </summary>
    REGRESSION,
    
    /// <summary>
    /// Metric changed but within tolerance threshold (no significant change).
    /// </summary>
    NO_SIGNIFICANT_CHANGE,
    
    /// <summary>
    /// Confidence insufficient to determine improvement/regression reliably.
    /// </summary>
    INCONCLUSIVE
}
```

### Contract Semantics

**Determinism Guarantee**:
- Identical baseline + current metrics + configuration → byte-identical result
- Same timestamp (captured at result creation)
- No floating-point ambiguity (decimal precision, explicit rounding)
- No concurrent ordering effects (metric comparison order doesn't matter)

**Outcome Aggregation**:
- Overall outcome = worst-case metric outcome
- Priority: REGRESSION > IMPROVEMENT > NO_SIGNIFICANT_CHANGE > INCONCLUSIVE
- Example: If 2 metrics have NO_SIGNIFICANT_CHANGE and 1 has REGRESSION → result is REGRESSION

**Confidence Calculation**:
- Calculated from comparison magnitude relative to tolerance
- If confidence < threshold → outcome marked INCONCLUSIVE
- Confidence never negative or exceeds 1.0

**Immutability**:
- Result is frozen immediately after creation
- No mutation methods (state set once at construction)
- Safe to pass across trust boundaries without defensive copying

---

## Contract 3: Tolerance & Configuration

**Boundary**: Domain Layer  
**Ownership**: Baseline Domain  
**Stability**: Stable (tolerance rules immutable; extensible via strategy)

### Tolerance Value Object

```csharp
namespace PerformanceEngine.Baseline.Domain.Domain.Tolerances;

/// <summary>
/// Configuration specifying acceptable variance (tolerance) for a single metric.
/// 
/// Invariants:
/// - Amount must be non-negative (no negative tolerances)
/// - Amount must be reasonable for context
/// - Metric name must be non-empty
/// - Tolerance is immutable value object
/// </summary>
public interface ITolerance
{
    /// <summary>
    /// Name of metric this tolerance applies to (e.g., "p95_latency").
    /// </summary>
    string MetricName { get; }
    
    /// <summary>
    /// Type of tolerance: RELATIVE (percentage-based) or ABSOLUTE (value-based).
    /// </summary>
    ToleranceType Type { get; }
    
    /// <summary>
    /// Amount of tolerance.
    /// For RELATIVE: 0.10 = ±10% of baseline value
    /// For ABSOLUTE: 50.0 = ±50 units (same units as metric)
    /// </summary>
    double Amount { get; }
    
    /// <summary>
    /// Evaluate if a value change is within acceptable tolerance.
    /// </summary>
    /// <param name="baselineValue">Baseline metric value</param>
    /// <param name="currentValue">Current metric value</param>
    /// <returns>True if change within tolerance; false if exceeds tolerance</returns>
    bool IsWithinTolerance(double baselineValue, double currentValue);
}

/// <summary>
/// Type of tolerance: how variance is measured.
/// </summary>
public enum ToleranceType
{
    /// <summary>
    /// Relative tolerance: ±X% of baseline value.
    /// Example: 0.10 (10%) with baseline 150ms accepts [135ms, 165ms]
    /// </summary>
    RELATIVE,
    
    /// <summary>
    /// Absolute tolerance: ±X units (same units as metric).
    /// Example: 20 (20ms) with baseline 150ms accepts [130ms, 170ms]
    /// </summary>
    ABSOLUTE
}

/// <summary>
/// Collection of per-metric tolerance rules.
/// </summary>
public interface IToleranceConfiguration
{
    /// <summary>
    /// Get tolerance rule for a specific metric.
    /// </summary>
    /// <param name="metricName">Name of metric</param>
    /// <returns>Tolerance rule for metric</returns>
    /// <exception cref="KeyNotFoundException">If metric has no tolerance rule</exception>
    Tolerance GetTolerance(string metricName);
    
    /// <summary>
    /// Check if tolerance rule exists for metric.
    /// </summary>
    bool HasTolerance(string metricName);
}
```

### Contract Semantics

**Tolerance Calculation** (RELATIVE):
```
isWithinTolerance = abs((current - baseline) / baseline) <= amount
Example: baseline=150ms, tolerance=±10%, current=165ms
  change = (165 - 150) / 150 = 0.10 (10%)
  isWithin = 0.10 <= 0.10 ✓ (yes, within tolerance)
```

**Tolerance Calculation** (ABSOLUTE):
```
isWithinTolerance = abs(current - baseline) <= amount
Example: baseline=150ms, tolerance=±20ms, current=170ms
  change = 170 - 150 = 20ms
  isWithin = 20 <= 20 ✓ (yes, within tolerance)
```

**Immutability**:
- Tolerance is value object (no setters)
- Configuration collection read-only
- Cannot add/remove/modify rules after creation

---

## Contract 4: Confidence Level

**Boundary**: Domain Layer  
**Ownership**: Baseline Domain  
**Stability**: Stable (confidence semantics deterministic)

### Confidence Level Value Object

```csharp
namespace PerformanceEngine.Baseline.Domain.Domain.Confidence;

/// <summary>
/// Measure of certainty [0.0, 1.0] in a comparison outcome.
/// 
/// Semantics:
/// - 0.0 = No confidence (result on tolerance boundary, inconclusive)
/// - 0.5 = Moderate confidence (result noticeably beyond tolerance)
/// - 1.0 = High confidence (result far exceeds tolerance)
/// 
/// Invariants:
/// - Value always in range [0.0, 1.0]
/// - Immutable after construction
/// - Compared against threshold to determine INCONCLUSIVE outcomes
/// </summary>
public interface IConfidenceLevel : IEquatable<ConfidenceLevel>
{
    /// <summary>
    /// Confidence value [0.0, 1.0].
    /// </summary>
    double Value { get; }
    
    /// <summary>
    /// Check if confidence exceeds threshold.
    /// Default threshold: 0.7 (70%)
    /// </summary>
    /// <param name="threshold">Minimum confidence for conclusive result</param>
    /// <returns>True if confidence >= threshold</returns>
    bool IsConclusive(double threshold = 0.7);
}
```

### Contract Semantics

**Threshold Semantics**:
- Confidence >= threshold → Outcome is conclusive (IMPROVEMENT/REGRESSION/NO_SIGNIFICANT_CHANGE)
- Confidence < threshold → Outcome marked INCONCLUSIVE (insufficient certainty)

**Calculation** (Illustrative):
```
deviation = max(abs(change_magnitude) - tolerance, 0)
confidence = min(1.0, deviation / tolerance)

Example (RELATIVE tolerance):
- Baseline: 150ms, Tolerance: ±10%, Threshold: 0.7
- Current: 165ms → +10% (at tolerance boundary)
  deviation = 0%, confidence = 0.0 → INCONCLUSIVE
- Current: 195ms → +30% (30% beyond tolerance)
  deviation = 20% / 10% = 2.0 → confidence = 1.0 → CONCLUSIVE
```

---

## Contract 5: Repository Port (Infrastructure Boundary)

**Boundary**: Domain ↔ Infrastructure  
**Ownership**: Baseline Domain (interface); Infrastructure (implementation)  
**Stability**: Stable (baseline retrieval semantics immutable)

### Baseline Repository Interface

```csharp
namespace PerformanceEngine.Baseline.Domain.Ports;

/// <summary>
/// Port: Abstraction for baseline snapshot persistence.
/// 
/// Semantics:
/// - Storage medium is infrastructure concern (Redis, PostgreSQL, etc.)
/// - Domain defines what repository must do (create, retrieve, optional listing)
/// - Infrastructure implements how storage works (Redis adapter, SQL adapter, etc.)
/// 
/// Guarantees:
/// - Baselines are immutable once stored (no updates)
/// - Baseline may expire (TTL policy operational concern)
/// - Expired baseline returns null (graceful expiration handling)
/// - Exceptions only for infrastructure failures (connection lost, etc.)
/// </summary>
public interface IBaselineRepository
{
    /// <summary>
    /// Store new baseline snapshot.
    /// </summary>
    /// <param name="baseline">Immutable baseline aggregate to persist</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Assigned baseline ID (may be same as baseline.Id or newly generated)</returns>
    /// <exception cref="RepositoryException">If storage operation fails (infrastructure error)</exception>
    Task<BaselineId> CreateAsync(Baseline baseline, CancellationToken cancellationToken);
    
    /// <summary>
    /// Retrieve baseline by ID.
    /// </summary>
    /// <param name="id">Baseline ID to retrieve</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>
    /// Baseline if found and not expired.
    /// Null if baseline has expired (TTL elapsed) or never existed.
    /// </returns>
    /// <exception cref="RepositoryException">If retrieval operation fails (infrastructure error)</exception>
    Task<Baseline?> GetByIdAsync(BaselineId id, CancellationToken cancellationToken);
    
    /// <summary>
    /// List recent baselines (optional, for dashboards/monitoring).
    /// </summary>
    /// <param name="count">Number of recent baselines to retrieve</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of baselines ordered by creation time (newest first)</returns>
    /// <exception cref="RepositoryException">If listing operation fails</exception>
    Task<IReadOnlyList<Baseline>> ListRecentAsync(int count, CancellationToken cancellationToken);
}

/// <summary>
/// Exception thrown when repository operation fails due to infrastructure issues.
/// </summary>
public class RepositoryException : Exception
{
    public RepositoryException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}
```

### Contract Semantics

**Create Operation**:
- Accepts immutable Baseline aggregate
- Stores snapshot (implementation detail: Redis, PostgreSQL, files, etc.)
- Returns BaselineId (may be same as input or newly assigned)
- Throws RepositoryException if storage fails

**Retrieve Operation**:
- Returns baseline if stored and not expired
- Returns null if baseline not found OR expired (graceful semantics)
- Throws RepositoryException if infrastructure failure (connection lost, etc.)
- Caller responsible for handling null (cannot compare without baseline)

**TTL & Expiration**:
- Expiration is operational concern (not domain responsibility)
- Repository implements TTL per configuration
- Expired baseline treated same as never-created (return null)
- No domain logic for managing TTL

**Concurrency**:
- Multiple concurrent reads allowed (baseline is immutable)
- Multiple concurrent creates serializable (implementation detail)
- No concurrent modification semantics (baselines immutable)

---

## Exception Hierarchy

```csharp
namespace PerformanceEngine.Baseline.Domain;

/// <summary>
/// Base exception for all baseline domain errors.
/// </summary>
public class BaselineDomainException : Exception
{
    public BaselineDomainException(string message) : base(message) { }
}

/// <summary>
/// Domain invariant violated (programmer error, not runtime condition).
/// </summary>
public class DomainInvariantViolatedException : BaselineDomainException
{
    public DomainInvariantViolatedException(string message) : base(message) { }
}

/// <summary>
/// Baseline not found or expired (expected runtime condition).
/// </summary>
public class BaselineNotFoundException : BaselineDomainException
{
    public BaselineNotFoundException(string baselineId) 
        : base($"Baseline {baselineId} not found or expired") { }
}

/// <summary>
/// Tolerance validation failed (configuration error).
/// </summary>
public class ToleranceValidationException : BaselineDomainException
{
    public ToleranceValidationException(string message) : base(message) { }
}

/// <summary>
/// Confidence level validation failed (configuration error).
/// </summary>
public class ConfidenceValidationException : BaselineDomainException
{
    public ConfidenceValidationException(string message) : base(message) { }
}
```

---

## Contract Versioning & Evolution

### Backward Compatibility

- **STABLE**: Baseline, ComparisonResult, Tolerance interfaces (no breaking changes expected)
- **EXTENSIBLE**: Custom tolerance strategies, outcome aggregation strategies (extend, don't modify)
- **FORWARD-COMPATIBLE**: Baseline fields (can add fields to aggregate, existing consumers unaffected)

### Version Pinning (Phase 2+)

- BaselineId may extend to support version tuples (baseline_id:version)
- Phase 1: Single "current" baseline per suite
- Phase 2: Multiple versions with explicit pinning

---

## Testing Contracts

All domain contracts must satisfy:

1. **Determinism Test** (1000 runs with identical input → identical output)
2. **Immutability Test** (no modification of aggregate after creation)
3. **Invariant Test** (all domain rules enforced)
4. **Exception Test** (correct exceptions for constraint violations)
5. **Serialization Test** (round-trip fidelity if persistence involved)
