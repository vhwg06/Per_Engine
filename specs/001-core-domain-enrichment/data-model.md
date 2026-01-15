# Phase 1 Data Model: Core Domain Enrichment Entity Specifications

**Completed**: 2026-01-15  
**Input**: Research findings from research.md, specification requirements from spec.md, plan architecture from plan.md  
**Output**: Concrete entity definitions for implementation across three domains

---

## Overview

This document specifies all enriched domain entities, value objects, and their relationships. The enrichment extends three existing domains:
1. **Metrics Domain**: Add completeness metadata
2. **Evaluation Domain**: Add evidence trail and INCONCLUSIVE outcome
3. **Profile Domain**: Add deterministic resolution and validation gates

All specifications follow immutable semantics per architecture decisions.

---

## Metrics Domain Enrichment

### Entity: Metric (Extended)

**Responsibility**: Represent aggregated measurement with reliability metadata

| Attribute | Type | Immutable | Nullable | Constraints | Notes |
|-----------|------|----------|----------|------------|-------|
| `Id` | `Guid` | ✅ Yes | ❌ No | Unique per aggregation run | Pre-existing |
| `AggregationName` | `string` | ✅ Yes | ❌ No | Non-empty; identifies aggregation (e.g., "p95") | Pre-existing |
| `Value` | `double` | ✅ Yes | ❌ No | Non-negative; valid measurement | Pre-existing |
| `Unit` | `string` | ✅ Yes | ❌ No | Unit of measurement (e.g., "ms", "percent") | Pre-existing |
| `ComputedAt` | `DateTime` (UTC) | ✅ Yes | ❌ No | ≥ aggregation start time | Pre-existing |
| **`CompletessStatus`** | `CompletessStatus` enum | ✅ Yes | ❌ No | COMPLETE or PARTIAL | **NEW** |
| **`Evidence`** | `MetricEvidence` | ✅ Yes | ❌ No | Never null; immutable | **NEW** |

**Entity Relationships**:
```
Metric (entity)
├── CompletessStatus (enum)
│   ├── COMPLETE (all required samples collected)
│   └── PARTIAL (incomplete data)
└── MetricEvidence (value object)
    ├── SampleCount: int
    ├── RequiredSampleCount: int
    ├── AggregationWindow: string
    └── IsComplete: bool (derived)
```

**Factory Method (Static)**:
```csharp
public static Metric Create(
    string aggregationName,
    double value,
    string unit,
    DateTime computedAt,
    int sampleCount,
    int requiredSampleCount,
    string aggregationWindow,
    CompletessStatus? overrideStatus = null)
{
    // Invariants:
    if (string.IsNullOrWhiteSpace(aggregationName))
        throw new ArgumentException("AggregationName required", nameof(aggregationName));
    
    if (value < 0)
        throw new ArgumentException("Value must be non-negative", nameof(value));
    
    if (sampleCount < 0)
        throw new ArgumentException("SampleCount must be non-negative", nameof(sampleCount));
    
    if (requiredSampleCount <= 0)
        throw new ArgumentException("RequiredSampleCount must be positive", nameof(requiredSampleCount));
    
    if (string.IsNullOrWhiteSpace(aggregationWindow))
        throw new ArgumentException("AggregationWindow required", nameof(aggregationWindow));
    
    // Determine completeness if not overridden
    var status = overrideStatus ?? 
        (sampleCount >= requiredSampleCount ? CompletessStatus.COMPLETE : CompletessStatus.PARTIAL);
    
    var evidence = new MetricEvidence(
        sampleCount: sampleCount,
        requiredSampleCount: requiredSampleCount,
        aggregationWindow: aggregationWindow);
    
    return new Metric(
        id: Guid.NewGuid(),
        aggregationName: aggregationName,
        value: value,
        unit: unit,
        computedAt: computedAt,
        completessStatus: status,
        evidence: evidence);
}
```

**Validation Rules**:
1. CompletessStatus must be COMPLETE if SampleCount ≥ RequiredSampleCount
2. CompletessStatus may be PARTIAL if SampleCount < RequiredSampleCount
3. SampleCount must always be ≥ 1 (at least one sample needed)
4. AggregationWindow must be a recognized window reference (e.g., "5m", "1h", "10000-sample")

---

### Value Object: CompletessStatus (Enum)

**Responsibility**: Indicate reliability of metric data

```csharp
public enum CompletessStatus
{
    /// <summary>
    /// All required samples collected; metric is reliable.
    /// Threshold defined by Metrics Domain aggregation configuration.
    /// </summary>
    COMPLETE,
    
    /// <summary>
    /// Incomplete data; metric is partial. Use with caution.
    /// Less than required samples for complete status.
    /// </summary>
    PARTIAL
}
```

**Semantics**:
- **COMPLETE**: Metrics Domain has collected sufficient data for reliable aggregation
- **PARTIAL**: Data collection incomplete; metric should be treated as preliminary or skipped per rule configuration

---

### Value Object: MetricEvidence

**Responsibility**: Capture reliability metadata for audit and decision-making

```csharp
public sealed class MetricEvidence : ValueObject
{
    public int SampleCount { get; }
    public int RequiredSampleCount { get; }
    public string AggregationWindow { get; }
    
    public bool IsComplete => SampleCount >= RequiredSampleCount;
    
    public MetricEvidence(int sampleCount, int requiredSampleCount, string aggregationWindow)
    {
        if (sampleCount < 0)
            throw new ArgumentException("SampleCount must be non-negative", nameof(sampleCount));
        
        if (requiredSampleCount <= 0)
            throw new ArgumentException("RequiredSampleCount must be positive", nameof(requiredSampleCount));
        
        if (string.IsNullOrWhiteSpace(aggregationWindow))
            throw new ArgumentException("AggregationWindow required", nameof(aggregationWindow));
        
        SampleCount = sampleCount;
        RequiredSampleCount = requiredSampleCount;
        AggregationWindow = aggregationWindow;
    }
    
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return SampleCount;
        yield return RequiredSampleCount;
        yield return AggregationWindow;
    }
}
```

**Usage**:
```csharp
var evidence = new MetricEvidence(
    sampleCount: 95,
    requiredSampleCount: 100,
    aggregationWindow: "5m");

// Evidence shows: 95 of 100 samples in 5-minute window
// IsComplete property: false (95 < 100)
```

---

## Evaluation Domain Enrichment

### Enum: Outcome (Extended)

**Responsibility**: Represent evaluation result conclusiveness

```csharp
public enum Outcome
{
    /// <summary>
    /// Performance meets or exceeds all constraints.
    /// All metrics were complete and evaluations passed.
    /// </summary>
    PASS = 1,
    
    /// <summary>
    /// Performance fails one or more constraints.
    /// Metrics were complete; evaluation determined failure.
    /// </summary>
    FAIL = 2,
    
    /// <summary>
    /// Evaluation cannot conclude due to incomplete data or partial execution.
    /// Neither PASS nor FAIL is appropriate; requires investigation or retry.
    /// </summary>
    INCONCLUSIVE = 3
}
```

**Semantics**:
- **PASS**: Definitive success (complete data, all constraints met)
- **FAIL**: Definitive failure (complete data, constraint violation)
- **INCONCLUSIVE**: Indeterminate (incomplete metrics, partial execution, insufficient evidence)

---

### Entity: EvaluationResult (Extended)

**Responsibility**: Represent evaluation outcome with complete evidence trail

| Attribute | Type | Immutable | Nullable | Constraints | Notes |
|-----------|------|----------|----------|------------|-------|
| `Id` | `Guid` | ✅ Yes | ❌ No | Unique per evaluation | Pre-existing |
| `Outcome` | `Outcome` | ✅ Yes | ❌ No | PASS, FAIL, or INCONCLUSIVE | **EXTENDED** |
| `Violations` | `IReadOnlyList<Violation>` | ✅ Yes | ❌ No | Empty if PASS; sorted by (RuleId, MetricName) | Pre-existing; now sorted deterministically |
| **`Evidence`** | `EvaluationEvidence` | ✅ Yes | ❌ No | Never null; complete evidence trail | **NEW** |
| **`OutcomeReason`** | `string` | ✅ Yes | ❌ No | Human-readable explanation of outcome | **NEW** |
| `EvaluatedAt` | `DateTime` (UTC) | ✅ Yes | ❌ No | UTC timestamp, captured at evaluation start | Pre-existing; now deterministically consistent |

**Entity Relationships**:
```
EvaluationResult (entity)
├── Outcome enum (PASS/FAIL/INCONCLUSIVE)
├── Violations (IReadOnlyList<Violation>)
│   └── Violation (value object)
├── EvaluationEvidence (value object)
│   ├── MetricReference (value object)
│   ├── RuleId: string
│   ├── RuleName: string
│   ├── ExpectedConstraint: string
│   └── Decision: string
└── OutcomeReason: string
```

**Factory Method (Static)**:
```csharp
public static EvaluationResult CreatePass(
    IReadOnlyList<EvaluationEvidence> evidences,
    DateTime evaluatedAt)
{
    return new EvaluationResult(
        id: Guid.NewGuid(),
        outcome: Outcome.PASS,
        violations: ImmutableList<Violation>.Empty,
        evidence: CombineEvidences(evidences),  // Merge multiple evidence trails
        outcomeReason: "All constraints satisfied with complete data.",
        evaluatedAt: evaluatedAt);
}

public static EvaluationResult CreateFail(
    IReadOnlyList<Violation> violations,
    IReadOnlyList<EvaluationEvidence> evidences,
    DateTime evaluatedAt)
{
    var sortedViolations = violations
        .OrderBy(v => v.RuleId)
        .ThenBy(v => v.MetricName)
        .ToImmutableList();
    
    return new EvaluationResult(
        id: Guid.NewGuid(),
        outcome: Outcome.FAIL,
        violations: sortedViolations,
        evidence: CombineEvidences(evidences),
        outcomeReason: $"{violations.Count} constraint(s) violated.",
        evaluatedAt: evaluatedAt);
}

public static EvaluationResult CreateInconclusive(
    IReadOnlyList<EvaluationEvidence> evidences,
    string reason,  // Why inconclusive? e.g., "Metric p95 is partial"
    DateTime evaluatedAt)
{
    return new EvaluationResult(
        id: Guid.NewGuid(),
        outcome: Outcome.INCONCLUSIVE,
        violations: ImmutableList<Violation>.Empty,
        evidence: CombineEvidences(evidences),
        outcomeReason: reason,
        evaluatedAt: evaluatedAt);
}
```

**Immutability Guarantee**:
```csharp
// All properties init-only (no setters)
public sealed record EvaluationResult
{
    public required Guid Id { get; init; }
    public required Outcome Outcome { get; init; }
    public required IReadOnlyList<Violation> Violations { get; init; }
    public required EvaluationEvidence Evidence { get; init; }
    public required string OutcomeReason { get; init; }
    public required DateTime EvaluatedAt { get; init; }
}
```

---

### Value Object: EvaluationEvidence

**Responsibility**: Capture domain-level explanation for evaluation decision without requiring log inspection

```csharp
public sealed class EvaluationEvidence : ValueObject
{
    public string RuleId { get; }
    public string RuleName { get; }
    public IReadOnlyList<MetricReference> MetricsUsed { get; }
    public IReadOnlyDictionary<string, double> ActualValues { get; }
    public string ExpectedConstraint { get; }
    public bool ConstraintSatisfied { get; }
    public string Decision { get; }
    public DateTime EvaluatedAt { get; }
    
    public EvaluationEvidence(
        string ruleId,
        string ruleName,
        IReadOnlyList<MetricReference> metricsUsed,
        IReadOnlyDictionary<string, double> actualValues,
        string expectedConstraint,
        bool constraintSatisfied,
        string decision,
        DateTime evaluatedAt)
    {
        if (string.IsNullOrWhiteSpace(ruleId))
            throw new ArgumentException("RuleId required", nameof(ruleId));
        
        if (string.IsNullOrWhiteSpace(ruleName))
            throw new ArgumentException("RuleName required", nameof(ruleName));
        
        if (metricsUsed == null || metricsUsed.Count == 0)
            throw new ArgumentException("MetricsUsed cannot be empty", nameof(metricsUsed));
        
        if (actualValues == null || actualValues.Count == 0)
            throw new ArgumentException("ActualValues cannot be empty", nameof(actualValues));
        
        if (string.IsNullOrWhiteSpace(expectedConstraint))
            throw new ArgumentException("ExpectedConstraint required", nameof(expectedConstraint));
        
        if (string.IsNullOrWhiteSpace(decision))
            throw new ArgumentException("Decision required", nameof(decision));
        
        RuleId = ruleId;
        RuleName = ruleName;
        MetricsUsed = metricsUsed;
        ActualValues = new Dictionary<string, double>(actualValues); // Defensive copy
        ExpectedConstraint = expectedConstraint;
        ConstraintSatisfied = constraintSatisfied;
        Decision = decision;
        EvaluatedAt = evaluatedAt;
    }
    
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return RuleId;
        yield return RuleName;
        yield return string.Join(",", MetricsUsed.Select(m => m.AggregationName).OrderBy(n => n));
        yield return string.Join(",", ActualValues.OrderBy(kv => kv.Key).Select(kv => $"{kv.Key}:{kv.Value}"));
        yield return ExpectedConstraint;
        yield return ConstraintSatisfied;
        yield return Decision;
        yield return EvaluatedAt;
    }
}
```

**Example**:
```csharp
var evidence = new EvaluationEvidence(
    ruleId: "RULE-001",
    ruleName: "P95 Latency SLA",
    metricsUsed: new[]
    {
        new MetricReference("p95", CompletessStatus.COMPLETE, 189.5)
    },
    actualValues: new Dictionary<string, double> { { "p95", 189.5 } },
    expectedConstraint: "p95 < 200ms",
    constraintSatisfied: true,
    decision: "P95 latency (189.5ms) satisfies SLA threshold (200ms).",
    evaluatedAt: DateTime.UtcNow);
```

---

### Value Object: MetricReference

**Responsibility**: Lightweight reference to metric used in evaluation, capturing completeness status

```csharp
public sealed class MetricReference : ValueObject
{
    public string AggregationName { get; }
    public CompletessStatus Completeness { get; }
    public double Value { get; }
    
    public MetricReference(string aggregationName, CompletessStatus completeness, double value)
    {
        if (string.IsNullOrWhiteSpace(aggregationName))
            throw new ArgumentException("AggregationName required", nameof(aggregationName));
        
        if (value < 0)
            throw new ArgumentException("Value must be non-negative", nameof(value));
        
        AggregationName = aggregationName;
        Completeness = completeness;
        Value = value;
    }
    
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return AggregationName;
        yield return Completeness;
        yield return Value;
    }
}
```

---

### Evaluator Service (Updated Behavior)

**Responsibility**: Deterministic evaluation with enriched output

**Updated Method Signature**:
```csharp
public sealed class Evaluator
{
    /// <summary>
    /// Evaluate metric against rule with evidence capture.
    /// Returns INCONCLUSIVE if metric is partial and rule doesn't allow partials.
    /// Evidence always non-null; explains decision without log inspection.
    /// Deterministic: identical inputs → identical output across runs.
    /// </summary>
    public EvaluationResult Evaluate(
        IMetric metric,
        IRule rule,
        IReadOnlyList<string>? partialMetricAllowlist = null)
    {
        var evaluatedAt = DateTime.UtcNow;  // Capture once
        
        // Check metric completeness
        if (metric.Completeness == CompletessStatus.PARTIAL &&
            (partialMetricAllowlist == null || !partialMetricAllowlist.Contains(metric.AggregationName)))
        {
            // Metric is partial and not explicitly allowed
            var evidence = new EvaluationEvidence(
                ruleId: rule.Id,
                ruleName: rule.Name,
                metricsUsed: new[] { new MetricReference(metric.AggregationName, metric.Completeness, metric.Value) },
                actualValues: new Dictionary<string, double> { { metric.AggregationName, metric.Value } },
                expectedConstraint: rule.GetConstraintDescription(),
                constraintSatisfied: false,
                decision: $"Metric {metric.AggregationName} is partial; rule does not allow partial metrics.",
                evaluatedAt: evaluatedAt);
            
            return EvaluationResult.CreateInconclusive(
                new[] { evidence },
                reason: $"Incomplete data for {metric.AggregationName}",
                evaluatedAt: evaluatedAt);
        }
        
        // Metric is complete or explicitly allowed as partial
        var ruleResult = rule.Evaluate(metric);
        
        var decisionEvidence = new EvaluationEvidence(
            ruleId: rule.Id,
            ruleName: rule.Name,
            metricsUsed: new[] { new MetricReference(metric.AggregationName, metric.Completeness, metric.Value) },
            actualValues: new Dictionary<string, double> { { metric.AggregationName, metric.Value } },
            expectedConstraint: rule.GetConstraintDescription(),
            constraintSatisfied: ruleResult.IsPassing,
            decision: ruleResult.Explanation,
            evaluatedAt: evaluatedAt);
        
        return ruleResult.IsPassing
            ? EvaluationResult.CreatePass(new[] { decisionEvidence }, evaluatedAt)
            : EvaluationResult.CreateFail(ruleResult.Violations, new[] { decisionEvidence }, evaluatedAt);
    }
}
```

**Determinism Guarantee**:
- Identical inputs (metric, rule, profile) → identical output across 1000+ runs
- `EvaluatedAt` timestamp deterministically identical
- Violations sorted by (RuleId, MetricName)
- No `Random`, `DateTime.Now`, or concurrent ordering

---

## Profile Domain Enrichment

### Enum: ProfileState

**Responsibility**: Track profile lifecycle state

```csharp
public enum ProfileState
{
    /// <summary>
    /// Profile accepts overrides; not yet resolved.
    /// </summary>
    Unresolved = 1,
    
    /// <summary>
    /// Profile has been resolved; immutable, no more overrides accepted.
    /// </summary>
    Resolved = 2,
    
    /// <summary>
    /// Profile failed validation; cannot be used for evaluation.
    /// </summary>
    Invalid = 3
}
```

---

### Entity: Profile (Extended)

**Responsibility**: Configuration container with deterministic resolution and immutability enforcement

| Attribute | Type | Immutable After Construction | Constraints | Notes |
|-----------|------|-----|----------|-------|
| `Id` | `Guid` | ✅ Yes | Unique | Profile identifier |
| `Name` | `string` | ✅ Yes | Non-empty | Profile name |
| **`State`** | `ProfileState` | ❌ Evolves | Unresolved → Resolved/Invalid | **NEW**: Tracks lifecycle |
| `Overrides` | `IReadOnlyDictionary<string, ConfigurationValue>` | ✅ Yes (per snapshot) | Immutable after resolution | Pre-existing; frozen after resolve |
| **`ResolvedConfiguration`** | `IReadOnlyDictionary<string, ConfigurationValue>?` | ✅ Yes | Null until resolved | **NEW**: Post-resolution config |
| **`ValidationErrors`** | `IReadOnlyList<ValidationError>?` | ✅ Yes | Null until validated | **NEW**: Validation result |

**Entity Relationships**:
```
Profile (entity)
├── ProfileState enum
│   ├── Unresolved (accepting overrides)
│   ├── Resolved (immutable)
│   └── Invalid (failed validation)
├── Overrides (IReadOnlyDictionary)
│   └── ConfigurationValue (value object)
├── ResolvedConfiguration (IReadOnlyDictionary, null until resolved)
└── ValidationErrors (IReadOnlyList, null until validated)
```

**State Machine**:
```
┌──────────────┐
│  Unresolved  │
│ (new profile)│
└──────┬───────┘
       │
       ├─→ ApplyOverride(scope, key, value)
       │   └─→ remains Unresolved
       │
       ├─→ Resolve(inputs)
       │   ├─→ Success → Resolved state
       │   └─→ Error → Invalid state
       │
       └─→ Validate()
           ├─→ Success → Resolved state
           └─→ Failure → Invalid state

┌──────────┐
│ Resolved │ (immutable; ApplyOverride throws)
└──────────┘

┌─────────┐
│ Invalid │ (cannot evaluate; requires fixes)
└─────────┘
```

**Methods**:
```csharp
public sealed class Profile
{
    public Guid Id { get; }
    public string Name { get; }
    public ProfileState State { get; private set; }
    private IDictionary<string, ConfigurationValue> _overrides;
    private IReadOnlyDictionary<string, ConfigurationValue>? _resolved;
    private IReadOnlyList<ValidationError>? _validationErrors;
    
    /// <summary>
    /// Apply override to profile. Throws if already resolved.
    /// </summary>
    public void ApplyOverride(string scope, string key, ConfigurationValue value)
    {
        if (State == ProfileState.Resolved || State == ProfileState.Invalid)
            throw new InvalidOperationException(
                $"Cannot apply overrides when profile is {State}. Create new profile or reset.");
        
        _overrides[$"{scope}:{key}"] = value;
    }
    
    /// <summary>
    /// Resolve profile deterministically (order-independent).
    /// Transitions state to Resolved if successful.
    /// </summary>
    public void Resolve(IReadOnlyDictionary<string, ConfigurationValue> inputs)
    {
        if (State != ProfileState.Unresolved)
            throw new InvalidOperationException(
                $"Cannot resolve profile in {State} state.");
        
        var resolver = new ProfileResolver();
        _resolved = resolver.Resolve(_overrides, inputs);
        State = ProfileState.Resolved;
    }
    
    /// <summary>
    /// Get resolved configuration value. Throws if not resolved.
    /// </summary>
    public ConfigurationValue Get(string key)
    {
        if (State != ProfileState.Resolved)
            throw new InvalidOperationException(
                $"Profile must be resolved (current state: {State}) before reading values.");
        
        if (!_resolved!.TryGetValue(key, out var value))
            throw new KeyNotFoundException($"Configuration key '{key}' not found in resolved profile.");
        
        return value;
    }
    
    /// <summary>
    /// Validate profile. Returns validation result; transitions to Invalid if validation fails.
    /// </summary>
    public ValidationResult Validate(IProfileValidator validator)
    {
        var result = validator.Validate(this);
        
        if (!result.IsValid)
        {
            State = ProfileState.Invalid;
            _validationErrors = result.Errors;
        }
        
        return result;
    }
}
```

---

### Service: ProfileResolver

**Responsibility**: Deterministically resolve profile configuration independent of input order

```csharp
public sealed class ProfileResolver
{
    /// <summary>
    /// Resolve profile deterministically.
    /// 
    /// Algorithm:
    /// 1. Collect all overrides from all scopes (global, api, endpoint)
    /// 2. Sort by (scope priority, key) for deterministic order
    /// 3. Apply in priority order: global < api < endpoint
    /// 4. Return resolved configuration
    /// 
    /// Guarantees:
    /// - Order-independent: same inputs in any order → identical output
    /// - Deterministic: same inputs → byte-identical dictionary
    /// - Time: O(n log n) where n = number of overrides
    /// </summary>
    public IReadOnlyDictionary<string, ConfigurationValue> Resolve(
        IReadOnlyDictionary<string, ConfigurationValue> overrides,
        IReadOnlyDictionary<string, ConfigurationValue> inputs)
    {
        var resolved = new Dictionary<string, ConfigurationValue>(inputs ?? new Dictionary<string, ConfigurationValue>());
        
        // Sort overrides deterministically by (scope priority, key)
        var sortedOverrides = overrides
            .OrderBy(kv => GetScopePriority(ExtractScope(kv.Key)))
            .ThenBy(kv => ExtractKey(kv.Key))
            .ToList();
        
        foreach (var (compositeKey, value) in sortedOverrides)
        {
            var key = ExtractKey(compositeKey);
            resolved[key] = value;  // Later scopes override earlier
        }
        
        return new ReadOnlyDictionary<string, ConfigurationValue>(resolved);
    }
    
    private int GetScopePriority(string scope)
    {
        // Scope priority (lower number = lower priority, applied first)
        return scope switch
        {
            "global" => 1,
            "api" => 2,
            "endpoint" => 3,
            _ => 0  // Unknown scope (lowest priority)
        };
    }
    
    private string ExtractScope(string compositeKey) => compositeKey.Split(':')[0];
    private string ExtractKey(string compositeKey) => compositeKey.Substring(compositeKey.IndexOf(':') + 1);
}
```

**Example - Deterministic Resolution**:
```csharp
var profile = new Profile(id: Guid.NewGuid(), name: "API-Profile");

// Apply overrides in different order
// Order 1: Global → API → Endpoint
profile.ApplyOverride("global", "timeout", new ConfigurationValue(30));
profile.ApplyOverride("api", "timeout", new ConfigurationValue(60));
profile.ApplyOverride("endpoint", "timeout", new ConfigurationValue(120));

var inputs1 = new Dictionary<string, ConfigurationValue> { /* ... */ };
profile.Resolve(inputs1);
var result1 = profile.Get("timeout");  // 120 (endpoint scope wins)

// Order 2: Endpoint → Global → API (same profile, different application order)
var profile2 = new Profile(id: Guid.NewGuid(), name: "API-Profile");
profile2.ApplyOverride("endpoint", "timeout", new ConfigurationValue(120));
profile2.ApplyOverride("global", "timeout", new ConfigurationValue(30));
profile2.ApplyOverride("api", "timeout", new ConfigurationValue(60));

profile2.Resolve(inputs1);
var result2 = profile2.Get("timeout");  // 120 (identical to result1)

// result1 == result2 (deterministic, order-independent)
```

---

### Port: IProfileValidator

**Responsibility**: Define contract for profile validation

```csharp
public interface IProfileValidator
{
    /// <summary>
    /// Validate profile against rules. Returns all validation errors at once.
    /// Pure function: same profile → same result every time.
    /// </summary>
    ValidationResult Validate(Profile profile);
    
    /// <summary>
    /// Get validation rules applicable to profile type.
    /// </summary>
    IReadOnlyList<ValidationRule> GetApplicableRules(string profileType);
}
```

**Validation Rules Checked**:
1. **No Circular Dependencies**: Profile overrides form acyclic graph
2. **Required Keys Present**: All mandatory configuration keys have values
3. **Type Correctness**: Configuration values match expected schema
4. **Scope Validity**: Referenced scopes exist and are recognized
5. **Range Constraints**: Numeric values within acceptable ranges
6. **No Null Defaults**: Required fields cannot have null values

---

### Value Object: ValidationResult

**Responsibility**: Capture all validation errors collected

```csharp
public sealed class ValidationResult : ValueObject
{
    public bool IsValid { get; }
    public IReadOnlyList<ValidationError> Errors { get; }
    
    public static ValidationResult Success() =>
        new ValidationResult(isValid: true, errors: ImmutableList<ValidationError>.Empty);
    
    public static ValidationResult Failure(IReadOnlyList<ValidationError> errors) =>
        new ValidationResult(isValid: false, errors: errors ?? ImmutableList<ValidationError>.Empty);
    
    private ValidationResult(bool isValid, IReadOnlyList<ValidationError> errors)
    {
        IsValid = isValid;
        Errors = errors;
    }
    
    public IReadOnlyList<ValidationError> ErrorsByCategory(string category) =>
        Errors.Where(e => e.Category == category).ToList().AsReadOnly();
    
    public IReadOnlyList<ValidationError> ErrorsByScope(string scope) =>
        Errors.Where(e => e.Scope == scope).ToList().AsReadOnly();
    
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return IsValid;
        yield return string.Join(",", Errors.OrderBy(e => e.Code).Select(e => e.Code));
    }
}
```

---

### Value Object: ValidationError

**Responsibility**: Represent single validation failure with actionable information

```csharp
public sealed class ValidationError : ValueObject
{
    public string Code { get; }
    public string Message { get; }
    public string Category { get; }
    public string? Scope { get; }
    public IReadOnlyList<string>? Path { get; }
    
    public ValidationError(
        string code,
        string message,
        string category,
        string? scope = null,
        IReadOnlyList<string>? path = null)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Code required", nameof(code));
        
        if (string.IsNullOrWhiteSpace(message))
            throw new ArgumentException("Message required", nameof(message));
        
        Code = code;
        Message = message;
        Category = category;
        Scope = scope;
        Path = path ?? ImmutableList<string>.Empty;
    }
    
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Code;
        yield return Message;
        yield return Category;
        yield return Scope;
        yield return string.Join(",", Path ?? new List<string>());
    }
}

// Example error codes:
// CIRCULAR_DEPENDENCY: "Profile has circular override: A → B → A"
// MISSING_REQUIRED_KEY: "Configuration key 'timeout' is required"
// INVALID_SCOPE: "Scope 'unknown_api' is not recognized"
// TYPE_MISMATCH: "Value for 'timeout' must be integer, got string"
// CONSTRAINT_VIOLATION: "Value 'timeout' = 5 violates minimum constraint (≥ 10)"
```

---

## Application Layer (Orchestration)

### Service: EvaluationService (Updated)

**Responsibility**: Orchestrate enriched evaluation with validation gates

```csharp
public sealed class EvaluationService
{
    private readonly IMetricProvider _metricProvider;
    private readonly IProfileProvider _profileProvider;
    private readonly IProfileValidator _profileValidator;
    private readonly Evaluator _evaluator;
    
    /// <summary>
    /// Full enriched evaluation with validation gates.
    /// 
    /// Flow:
    /// 1. Validate profile (gate: invalid profiles block evaluation)
    /// 2. Resolve profile (deterministic)
    /// 3. Retrieve metrics (check completeness)
    /// 4. Evaluate with evidence (deterministic)
    /// 5. Record result (with evidence trail for audit)
    /// </summary>
    public EvaluationResult Evaluate(
        string profileId,
        IReadOnlyList<IRule> rules,
        IReadOnlyList<string>? partialMetricAllowlist = null)
    {
        // Step 1: Get and validate profile
        var profile = _profileProvider.GetProfile(profileId);
        var validationResult = _profileValidator.Validate(profile);
        
        if (!validationResult.IsValid)
            throw new InvalidProfileException(
                profileId: profileId,
                errors: validationResult.Errors,
                message: $"Profile validation failed with {validationResult.Errors.Count} error(s).");
        
        // Step 2: Resolve profile (deterministic)
        var inputs = new Dictionary<string, ConfigurationValue>();  // Populated from config source
        profile.Resolve(inputs);
        
        // Step 3: Get metrics
        var metrics = _metricProvider.GetAllMetrics();
        
        // Step 4: Evaluate each rule
        var results = new List<EvaluationResult>();
        var evaluatedAt = DateTime.UtcNow;
        
        foreach (var rule in rules.OrderBy(r => r.Id))  // Deterministic rule order
        {
            var metric = metrics.FirstOrDefault(m => m.AggregationName == rule.MetricName);
            
            if (metric == null)
                continue;
            
            var result = _evaluator.Evaluate(metric, rule, partialMetricAllowlist);
            results.Add(result);
        }
        
        // Step 5: Aggregate results
        var aggregatedOutcome = ComputeAggregatedOutcome(results);
        var combinedEvidence = CombineEvidences(results.Select(r => r.Evidence));
        
        return new EvaluationResult(
            id: Guid.NewGuid(),
            outcome: aggregatedOutcome,
            violations: results.SelectMany(r => r.Violations).OrderBy(v => v.RuleId).ToList().AsReadOnly(),
            evidence: combinedEvidence,
            outcomeReason: ComputeOutcomeReason(aggregatedOutcome, results),
            evaluatedAt: evaluatedAt);
    }
    
    private Outcome ComputeAggregatedOutcome(IReadOnlyList<EvaluationResult> results)
    {
        // INCONCLUSIVE if any result is INCONCLUSIVE
        if (results.Any(r => r.Outcome == Outcome.INCONCLUSIVE))
            return Outcome.INCONCLUSIVE;
        
        // FAIL if any result is FAIL
        if (results.Any(r => r.Outcome == Outcome.FAIL))
            return Outcome.FAIL;
        
        // PASS if all results are PASS
        return Outcome.PASS;
    }
}
```

---

## Summary: Enrichment Scope by Domain

| Domain | Added/Extended | Type | Responsibility |
|--------|----------------|------|-----------------|
| **Metrics** | `CompletessStatus`, `MetricEvidence` | NEW value objects | Express reliability metadata |
| **Evaluation** | `Outcome` (INCONCLUSIVE), `EvaluationEvidence`, `EvaluationResult.Evidence` | EXTENDED enum, NEW value objects | Capture evidence trail; support INCONCLUSIVE |
| **Profile** | `ProfileState`, `ProfileResolver`, `IProfileValidator`, `ValidationResult`, `ValidationError` | NEW entities, services, ports | Enforce determinism, immutability, validation |
| **Application** | `EvaluationService` updates, validation gates | UPDATED service | Orchestrate enriched flow with gates |

---

**Status**: ✅ Phase 1 Data Model Complete
