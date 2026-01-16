# Domain Model: Baseline Domain

**Status**: Design Phase 1  
**Date**: 2026-01-15  
**Purpose**: Specification of domain entities, value objects, and services for baseline comparison logic  

---

## Ubiquitous Language

### Core Terms

- **Baseline**: An immutable snapshot of metrics and evaluation results, captured at a specific point in time, used as the reference point for performance comparisons
- **Comparison**: The deterministic operation of evaluating current metrics against a baseline to identify regressions, improvements, or no significant change
- **ComparisonResult**: The immutable outcome of a comparison, containing overall result, per-metric details, and confidence assessment
- **Tolerance**: A configuration specifying the acceptable variance (relative or absolute) for determining if a metric change is significant
- **Confidence**: A value [0.0, 1.0] representing the certainty/reliability of a comparison outcome
- **Regression**: A performance deterioration (current metric worse than baseline beyond tolerance)
- **Improvement**: A performance enhancement (current metric better than baseline beyond tolerance)
- **Inconclusive**: A comparison result where confidence is insufficient to reliably claim improvement or regression

---

## Aggregate Root: Baseline

### Definition

Baseline is the primary aggregate root, representing an immutable snapshot of performance metrics and evaluation results designated as a reference point.

### Responsibilities

- Capture and hold immutable snapshots of metrics and evaluations
- Enforce immutability: no modifications after creation
- Provide identity (BaselineId) for later retrieval

### Attributes

```
Baseline {
  id: BaselineId (unique identifier, e.g., UUID)
  createdAt: DateTime (when baseline snapshot was taken)
  metrics: IReadOnlyList<IMetric> (immutable collection from Metrics Domain)
  evaluationResults: IReadOnlyList<EvaluationResult> (optional; from Evaluation Domain)
  toleranceConfig: ToleranceConfiguration (rules for this baseline's comparisons)
  
  // Invariants:
  // - No setters; all state captured at construction
  // - Cannot be modified after creation
  // - createdAt immutable (represents capture point)
}
```

### Key Methods

- `constructor(id, metrics, evaluationResults, toleranceConfig)` - Create new baseline snapshot
- `GetMetric(name): IMetric?` - Retrieve metric by name (null if not found)
- `GetEvaluationResult(ruleId): EvaluationResult?` - Retrieve evaluation result (optional)

### Invariant Enforcement

```csharp
class BaselineInvariants
{
    // Validate immutability
    public static void AssertImmutable(Baseline baseline)
    {
        // Ensure no setters exist on Baseline properties
        // Verify collection is read-only (no add/remove after construction)
    }
    
    // Validate baseline consistency
    public static void AssertConsistent(Baseline baseline)
    {
        // Metrics must be non-empty (cannot baseline empty results)
        Assert.NotEmpty(baseline.metrics);
        
        // Metric names unique within baseline
        Assert.AllUnique(baseline.metrics.Select(m => m.MetricName));
        
        // Tolerance configuration must be valid
        ToleranceValidation.AssertValid(baseline.toleranceConfig);
    }
}
```

---

## Value Object: BaselineId

### Definition

Unique identifier for a baseline snapshot.

### Attributes

```
BaselineId {
  value: string (UUID, e.g., "550e8400-e29b-41d4-a716-446655440000")
  
  // Invariants:
  // - Not empty/null
  // - Format: valid UUID v4 or system-defined format
  // - Immutable after construction
}
```

### Equality & Hashing

- Two BaselineId equal if values equal (value semantics)
- Hashable for use in dictionaries/sets

---

## Value Object: Tolerance

### Definition

Configuration specifying acceptable variance for determining if a metric change is significant.

### Attributes

```
Tolerance {
  metricName: string (e.g., "p95_latency", "error_rate")
  type: ToleranceType (RELATIVE or ABSOLUTE)
  
  // For RELATIVE: ±10% means change within 10% of baseline value is acceptable
  amount: double (e.g., 0.10 for ±10%)
  
  // For ABSOLUTE: ±50ms means change within 50ms is acceptable
  // amount: double (e.g., 50.0)
  
  // Invariants:
  // - amount ≥ 0 (no negative tolerances)
  // - amount must be reasonable (>0 for meaningful tolerance, not arbitrary large)
  // - For RELATIVE: amount typically [0.0, 1.0] (0% to 100%)
  // - For ABSOLUTE: amount must respect metric units
}

enum ToleranceType {
  RELATIVE,    // Percentage-based: ±X% of baseline
  ABSOLUTE     // Value-based: ±X units
}
```

### Key Methods

- `constructor(metricName, type, amount)` - Create tolerance configuration
- `IsWithinTolerance(baselineValue, currentValue): bool` - Evaluate if change is acceptable
- `Validate()` - Assert invariants

### Validation Rules

```csharp
class ToleranceValidation
{
    public static void AssertValid(Tolerance tolerance)
    {
        // Amount must be non-negative
        if (tolerance.Amount < 0)
            throw new ArgumentException("Tolerance amount cannot be negative");
        
        // RELATIVE: typically [0.0, 1.0] but can exceed
        // ABSOLUTE: validate against metric units (domain-specific)
        
        // Tolerance must be for a known metric type
        if (string.IsNullOrEmpty(tolerance.MetricName))
            throw new ArgumentException("Metric name required");
    }
}
```

---

## Value Object: ConfidenceLevel

### Definition

A measure [0.0, 1.0] representing certainty/reliability of a comparison outcome.

### Attributes

```
ConfidenceLevel {
  value: double ([0.0, 1.0] inclusive)
  
  // Semantics:
  // - 0.0 = no confidence (inconclusive; on tolerance boundary)
  // - 0.5 = moderate confidence (change noticeable but not extreme)
  // - 1.0 = high confidence (change far exceeds tolerance)
  
  // Invariants:
  // - value ∈ [0.0, 1.0]
  // - Immutable after construction
}
```

### Key Methods

- `constructor(value)` - Create confidence level with validation
- `IsConclusive(threshold): bool` - Determine if confidence exceeds threshold (e.g., 0.7)
- `Validate()` - Assert invariants

### Comparison with Threshold

```csharp
class ConfidenceThreshold
{
    public const double Default = 0.7; // 70%
    
    public static ComparisonOutcome DetermineOutcome(
        ComparisonOutcome baselineOutcome,
        ConfidenceLevel confidence,
        double threshold)
    {
        // If confidence below threshold, mark as INCONCLUSIVE regardless of baseline outcome
        if (confidence.Value < threshold)
            return ComparisonOutcome.INCONCLUSIVE;
        
        return baselineOutcome;
    }
}
```

---

## Value Object: ComparisonMetric

### Definition

Per-metric details of a comparison, representing how a single metric changed between baseline and current.

### Attributes

```
ComparisonMetric {
  metricName: string (e.g., "p95_latency")
  baselineValue: double (value from baseline snapshot)
  currentValue: double (value from current execution)
  
  tolerance: Tolerance (configuration used for this metric)
  
  absoluteChange: double (current - baseline)
  relativeChange: double ((current - baseline) / baseline * 100)
  
  outcome: ComparisonOutcome (IMPROVEMENT, REGRESSION, NO_SIGNIFICANT_CHANGE, INCONCLUSIVE)
  confidence: ConfidenceLevel (certainty of this metric's outcome)
  
  // Invariants:
  // - outcome matches tolerance evaluation + confidence
  // - confidence in [0.0, 1.0]
  // - absoluteChange = currentValue - baselineValue (always)
  // - relativeChange calculation consistent across all metrics
}

enum ComparisonOutcome {
  IMPROVEMENT,              // Better than baseline (beyond tolerance)
  REGRESSION,              // Worse than baseline (beyond tolerance)
  NO_SIGNIFICANT_CHANGE,   // Within tolerance
  INCONCLUSIVE             // Confidence insufficient to determine
}
```

### Key Methods

- `constructor(...)` - Create with full validation
- `IsRegression(): bool` - Check if metric regressed
- `IsImprovement(): bool` - Check if metric improved
- `IsWithinTolerance(): bool` - Check if within acceptable variance

---

## Aggregate Root: ComparisonResult

### Definition

Immutable outcome of comparing current metrics against a baseline snapshot.

### Responsibilities

- Hold complete comparison outcome for single comparison operation
- Provide overall result (worst-case aggregation of metrics)
- Track per-metric details for detailed analysis
- Enable deterministic reproduction (same input → same result)

### Attributes

```
ComparisonResult {
  id: ComparisonResultId (unique identifier for this result)
  baselineId: BaselineId (which baseline was used)
  comparedAt: DateTime (when comparison was performed)
  
  overallOutcome: ComparisonOutcome (IMPROVEMENT, REGRESSION, etc.)
  overallConfidence: ConfidenceLevel (highest/lowest confidence? TBD Design)
  
  metricResults: IReadOnlyList<ComparisonMetric> (per-metric details)
  
  // Invariants:
  // - No setters; all state captured at construction
  // - overallOutcome determined by aggregating metric outcomes (worst-case)
  // - metricResults non-empty (at least one metric compared)
  // - All ComparisonMetric objects consistent with tolerances
}
```

### Key Methods

- `constructor(baselineId, metricResults, aggregationStrategy)` - Create result from metric comparisons
- `GetMetricResult(metricName): ComparisonMetric?` - Retrieve per-metric details
- `HasRegression(): bool` - Quick check if any metric regressed
- `Validate()` - Assert invariants

### Outcome Aggregation

```csharp
class OutcomeAggregator
{
    // Aggregate per-metric outcomes into overall outcome
    // Priority: REGRESSION > IMPROVEMENT > NO_SIGNIFICANT_CHANGE > INCONCLUSIVE
    public static ComparisonOutcome Aggregate(IReadOnlyList<ComparisonMetric> metrics)
    {
        if (metrics.Any(m => m.Outcome == ComparisonOutcome.REGRESSION))
            return ComparisonOutcome.REGRESSION;
        if (metrics.Any(m => m.Outcome == ComparisonOutcome.IMPROVEMENT))
            return ComparisonOutcome.IMPROVEMENT;
        if (metrics.Any(m => m.Outcome == ComparisonOutcome.NO_SIGNIFICANT_CHANGE))
            return ComparisonOutcome.NO_SIGNIFICANT_CHANGE;
        return ComparisonOutcome.INCONCLUSIVE;
    }
    
    // Aggregate per-metric confidence levels
    // Strategy: Lowest confidence (most uncertain metric)
    public static ConfidenceLevel AggregateConfidence(IReadOnlyList<ComparisonMetric> metrics)
    {
        var lowestConfidence = metrics.Min(m => m.Confidence.Value);
        return new ConfidenceLevel(lowestConfidence);
    }
}
```

---

## Entity: ComparisonRequest

### Definition

Input specification for a comparison operation, encapsulating the baseline, current metrics, and configuration.

### Attributes

```
ComparisonRequest {
  baselineId: BaselineId (which baseline to compare against)
  currentMetrics: IReadOnlyList<IMetric> (metrics from current execution)
  
  toleranceConfig: ToleranceConfiguration (per-metric tolerances)
  confidenceThreshold: double (minimum confidence for conclusive result, default 0.7)
  
  // Optional:
  contextInfo: string (e.g., "CI run #123", for debugging)
  
  // Invariants:
  // - baselineId not null
  // - currentMetrics non-empty
  // - toleranceConfig valid (all metrics have tolerance rules)
}
```

### Key Methods

- `constructor(baselineId, currentMetrics, toleranceConfig, threshold)` - Create request
- `Validate()` - Assert all preconditions met

---

## Domain Service: ComparisonCalculator

### Definition

Pure domain service implementing deterministic comparison logic.

### Responsibilities

- Calculate absolute and relative change for each metric
- Evaluate tolerance rules (is change within acceptable variance?)
- Calculate confidence level for each metric
- Determine per-metric outcome (REGRESSION/IMPROVEMENT/NO_SIGNIFICANT_CHANGE/INCONCLUSIVE)
- Aggregate per-metric outcomes into overall result

### Key Methods

```csharp
interface IComparisonCalculator
{
    // Calculate per-metric comparison details
    ComparisonMetric CalculateMetric(
        IMetric baselineMetric,
        IMetric currentMetric,
        Tolerance tolerance,
        double confidenceThreshold);
    
    // Aggregate metrics into final comparison result
    ComparisonResult CalculateOverallResult(
        BaselineId baselineId,
        IReadOnlyList<ComparisonMetric> metricResults);
}
```

### Pure Function Semantics

- No side effects (no database access, logging, events)
- Deterministic (identical inputs → identical outputs)
- Commutative for metrics (order doesn't matter)

### Algorithm (High-Level)

```
For each currentMetric compared against baselineMetric:
  1. Calculate absoluteChange = currentMetric.Value - baselineMetric.Value
  
  2. Calculate relativeChange = (absoluteChange / baselineMetric.Value) * 100
  
  3. Evaluate tolerance:
     if RELATIVE tolerance:
       withinTolerance = abs(relativeChange) <= tolerance.Amount
     else (ABSOLUTE):
       withinTolerance = abs(absoluteChange) <= tolerance.Amount
  
  4. Calculate confidence:
     deviation = max(abs(relativeChange) - tolerance.Amount, 0) / tolerance.Amount
     confidence = min(1.0, deviation) if withinTolerance else 1.0 - min(1.0, 1.0 / deviation)
  
  5. Determine metric outcome:
     if confidence < threshold:
       outcome = INCONCLUSIVE
     else if baselineMetric.Direction == LowerIsBetter:
       if currentValue < baselineValue: outcome = IMPROVEMENT
       else if currentValue > baselineValue: outcome = REGRESSION
       else: outcome = NO_SIGNIFICANT_CHANGE
     else (HigherIsBetter):
       if currentValue > baselineValue: outcome = IMPROVEMENT
       else if currentValue < baselineValue: outcome = REGRESSION
       else: outcome = NO_SIGNIFICANT_CHANGE

After all metrics processed:
  6. Aggregate outcomes: overall = worst(all metric outcomes)
  7. Create ComparisonResult with all details
```

---

## Domain Service: BaselineFactory

### Definition

Factory service for creating baseline snapshots with validation.

### Key Methods

```csharp
interface IBaselineFactory
{
    // Create new baseline from current metrics
    Baseline CreateBaseline(
        IReadOnlyList<IMetric> metrics,
        ToleranceConfiguration toleranceConfig,
        IReadOnlyList<EvaluationResult>? evaluationResults = null);
}
```

### Responsibilities

- Validate metrics before creating baseline
- Assign BaselineId
- Capture creation timestamp
- Ensure immutability constraints enforced

---

## Value Object: ToleranceConfiguration

### Definition

Collection of per-metric tolerance rules.

### Attributes

```
ToleranceConfiguration {
  rules: IReadOnlyDictionary<string, Tolerance> (metricName → tolerance rule)
  
  // Invariants:
  // - All metric names in baseline/comparison must have rules
  // - No orphaned tolerance rules (rules for non-existent metrics okay, but not required)
  // - All tolerance rules valid (ToleranceValidation.AssertValid)
}
```

### Key Methods

- `GetTolerance(metricName): Tolerance` - Retrieve rule for metric (throws if not found)
- `HasTolerance(metricName): bool` - Check if tolerance rule exists
- `Validate()` - Assert all invariants

---

## Data Model Diagram

```
┌─────────────────────────────────┐
│       Baseline (Aggregate)      │
├─────────────────────────────────┤
│ - id: BaselineId                │
│ - createdAt: DateTime           │
│ - metrics: IMetric[]            │
│ - evaluationResults: Result[]   │
│ - toleranceConfig: TolerConfig  │
└────────────┬────────────────────┘
             │
             │ compares to
             ↓
┌─────────────────────────────────┐
│   ComparisonRequest (Entity)    │
├─────────────────────────────────┤
│ - baselineId: BaselineId        │
│ - currentMetrics: IMetric[]     │
│ - toleranceConfig: TolerConfig  │
│ - confidenceThreshold: double   │
└────────────┬────────────────────┘
             │
             │ produces
             ↓
┌─────────────────────────────────┐
│  ComparisonResult (Aggregate)   │
├─────────────────────────────────┤
│ - id: ComparisonResultId        │
│ - baselineId: BaselineId        │
│ - comparedAt: DateTime          │
│ - overallOutcome: Outcome       │
│ - overallConfidence: Confidence │
│ - metricResults: Metric[]       │
└─────────────────────────────────┘
             ↑
             │ contains
             │
┌─────────────────────────────────┐
│    ComparisonMetric (VO)        │
├─────────────────────────────────┤
│ - metricName: string            │
│ - baselineValue: double         │
│ - currentValue: double          │
│ - absoluteChange: double        │
│ - relativeChange: double        │
│ - tolerance: Tolerance          │
│ - outcome: Outcome              │
│ - confidence: Confidence        │
└─────────────────────────────────┘
```

---

## Validation & Invariant Enforcement

### BaselineInvariants

```csharp
static class BaselineInvariants
{
    public static void AssertValid(Baseline baseline)
    {
        // Baseline must have at least one metric
        if (baseline.Metrics.Count == 0)
            throw new DomainException("Baseline must contain at least one metric");
        
        // Metric names must be unique
        var names = baseline.Metrics.Select(m => m.MetricName).ToList();
        if (names.Distinct().Count() != names.Count)
            throw new DomainException("Duplicate metric names in baseline");
        
        // Tolerance configuration must be valid
        ToleranceValidation.AssertValid(baseline.ToleranceConfig);
        
        // Tolerance rules should cover all metrics (or be configurable)
        foreach (var metric in baseline.Metrics)
        {
            if (!baseline.ToleranceConfig.HasTolerance(metric.MetricName))
                throw new DomainException($"No tolerance rule for metric: {metric.MetricName}");
        }
    }
}
```

### ComparisonResultInvariants

```csharp
static class ComparisonResultInvariants
{
    public static void AssertValid(ComparisonResult result)
    {
        // Result must have at least one metric comparison
        if (result.MetricResults.Count == 0)
            throw new DomainException("Comparison result must contain at least one metric");
        
        // Overall outcome must match aggregation of metric outcomes
        var expectedOutcome = OutcomeAggregator.Aggregate(result.MetricResults);
        if (result.OverallOutcome != expectedOutcome)
            throw new DomainException("Overall outcome does not match metric aggregation");
        
        // All metric outcomes valid
        foreach (var metric in result.MetricResults)
        {
            if (!IsValidOutcome(metric.Outcome))
                throw new DomainException($"Invalid outcome for metric: {metric.MetricName}");
        }
        
        // Confidence levels valid
        if (result.OverallConfidence.Value < 0.0 || result.OverallConfidence.Value > 1.0)
            throw new DomainException("Confidence level out of range [0.0, 1.0]");
    }
}
```

---

## Related Domains

### Dependency: Metrics Domain

Baseline domain depends on Metrics Domain for:
- `IMetric` interface (Value objects: MetricName, Value, Direction)
- `IReadOnlyList<IMetric>` collections
- Metric value semantics (units, valid ranges)

### Dependency: Evaluation Domain (Optional)

Baseline domain optionally references Evaluation Domain for:
- `EvaluationResult` entities (stored in baseline snapshot)
- Not required for core comparison logic

### Repository Port (Infrastructure Boundary)

Baseline domain defines repository abstraction:
- `IBaselineRepository` interface
- Implemented by Redis adapter (infrastructure layer)
- Enables in-memory testing; swappable storage

---

## Extension Points (Phase 2+)

1. **Tolerance Strategies**: Custom tolerance rules (weighted metrics, statistical bounds)
2. **Baseline Versioning**: Multi-version support with explicit version pinning
3. **Outcome Customization**: Per-organization outcome aggregation rules
4. **Confidence Models**: Statistical confidence if historical data available (Analytics domain)
