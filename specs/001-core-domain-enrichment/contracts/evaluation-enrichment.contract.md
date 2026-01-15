# API Contracts: Core Domain Enrichment - Evaluation Domain

**Completed**: 2026-01-15  
**Scope**: Evaluation Domain enrichment interfaces and data contracts  
**Format**: C# interface specifications with JSON serialization examples

---

## Enum: Outcome (Extended)

**Namespace**: `PerformanceEngine.Evaluation.Domain.Evaluation`

### Definition

```csharp
public enum Outcome
{
    /// <summary>
    /// Performance meets or exceeds all constraints.
    /// All evaluated metrics were complete; all rules passed.
    /// </summary>
    PASS = 1,
    
    /// <summary>
    /// Performance fails one or more constraints.
    /// Metrics were complete; evaluation determined failure.
    /// </summary>
    FAIL = 2,
    
    /// <summary>
    /// Evaluation cannot conclude due to incomplete data or partial execution.
    /// Neither PASS nor FAIL is definitively appropriate.
    /// Requires investigation, data collection retry, or manual review.
    /// </summary>
    INCONCLUSIVE = 3
}
```

### Semantics

| Outcome | When Returned | Interpretation | Typical Action |
|---------|---|---|---|
| **PASS** | All rules evaluated successfully against complete metrics | Performance is acceptable | Proceed to next stage (deploy, promote, etc.) |
| **FAIL** | One or more rules failed against complete metrics | Performance violates SLO/criteria | Investigate, fix, retry evaluation |
| **INCONCLUSIVE** | Metrics incomplete, execution partial, insufficient evidence | Result uncertain; cannot trust decision | Retry after collecting more data, manual review |

---

## Entity: EvaluationResult (Extended)

**Namespace**: `PerformanceEngine.Evaluation.Domain.Evaluation`

### Interface

```csharp
public sealed record EvaluationResult
{
    /// <summary>
    /// Unique identifier for this evaluation.
    /// </summary>
    public required Guid Id { get; init; }
    
    /// <summary>
    /// Evaluation outcome: PASS, FAIL, or INCONCLUSIVE.
    /// EXTENDED: Previously only PASS/FAIL; now includes INCONCLUSIVE.
    /// </summary>
    public required Outcome Outcome { get; init; }
    
    /// <summary>
    /// List of constraint violations (empty if PASS).
    /// Sorted deterministically by (RuleId, MetricName).
    /// </summary>
    public required IReadOnlyList<Violation> Violations { get; init; }
    
    /// <summary>
    /// NEW: Complete evidence trail explaining evaluation decision.
    /// Never null; sufficient to understand decision without log inspection.
    /// </summary>
    public required EvaluationEvidence Evidence { get; init; }
    
    /// <summary>
    /// NEW: Human-readable explanation of outcome choice.
    /// Examples:
    /// - "All constraints satisfied with complete data."
    /// - "2 constraint(s) violated."
    /// - "Metric p95 is partial; rule does not allow partial metrics."
    /// </summary>
    public required string OutcomeReason { get; init; }
    
    /// <summary>
    /// Timestamp when evaluation occurred (UTC).
    /// Captured once at evaluation start for deterministic verification.
    /// </summary>
    public required DateTime EvaluatedAt { get; init; }
}
```

### JSON Serialization Contract

```json
{
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "outcome": "PASS",
  "violations": [],
  "evidence": {
    "ruleId": "RULE-001",
    "ruleName": "P95 Latency SLA",
    "metricsUsed": [
      {
        "aggregationName": "p95",
        "completeness": "COMPLETE",
        "value": 189.5
      }
    ],
    "actualValues": {
      "p95": 189.5
    },
    "expectedConstraint": "p95 < 200ms",
    "constraintSatisfied": true,
    "decision": "P95 latency (189.5ms) satisfies SLA threshold (200ms).",
    "evaluatedAt": "2026-01-15T12:34:56.789Z"
  },
  "outcomeReason": "All constraints satisfied with complete data.",
  "evaluatedAt": "2026-01-15T12:34:56.789Z"
}
```

---

## Value Object: EvaluationEvidence

**Namespace**: `PerformanceEngine.Evaluation.Domain.Evaluation`

### Definition

```csharp
public sealed class EvaluationEvidence : ValueObject
{
    /// <summary>
    /// Rule ID that was evaluated.
    /// </summary>
    public string RuleId { get; }
    
    /// <summary>
    /// Human-readable rule name.
    /// </summary>
    public string RuleName { get; }
    
    /// <summary>
    /// Metrics used in this evaluation (including completeness status).
    /// Sorted deterministically by AggregationName.
    /// </summary>
    public IReadOnlyList<MetricReference> MetricsUsed { get; }
    
    /// <summary>
    /// Actual metric values: aggregation name → measured value.
    /// Sorted deterministically by key.
    /// </summary>
    public IReadOnlyDictionary<string, double> ActualValues { get; }
    
    /// <summary>
    /// Expected constraint as human-readable string.
    /// Example: "p95 < 200ms" or "error_rate < 1%"
    /// </summary>
    public string ExpectedConstraint { get; }
    
    /// <summary>
    /// Whether the constraint was satisfied (true = passed, false = failed).
    /// </summary>
    public bool ConstraintSatisfied { get; }
    
    /// <summary>
    /// Human-readable explanation of evaluation decision.
    /// Example: "P95 latency (189.5ms) satisfies SLA threshold (200ms)."
    /// Must be sufficient for stakeholder understanding without log inspection.
    /// </summary>
    public string Decision { get; }
    
    /// <summary>
    /// Timestamp when evaluation occurred (UTC).
    /// Captured once at evaluation start for deterministic verification.
    /// </summary>
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
        // Validation...
    }
}
```

### JSON Serialization Contract

```json
{
  "ruleId": "RULE-001",
  "ruleName": "P95 Latency SLA",
  "metricsUsed": [
    {
      "aggregationName": "p95",
      "completeness": "COMPLETE",
      "value": 189.5
    },
    {
      "aggregationName": "p999",
      "completeness": "PARTIAL",
      "value": 310.2
    }
  ],
  "actualValues": {
    "p95": 189.5,
    "p999": 310.2
  },
  "expectedConstraint": "p95 < 200ms AND p999 < 500ms",
  "constraintSatisfied": true,
  "decision": "Both P95 (189.5ms) and P999 (310.2ms) satisfy their respective thresholds. P999 is partial data (90 samples of 100 required) but is allowed by rule configuration.",
  "evaluatedAt": "2026-01-15T12:34:56.789Z"
}
```

---

## Value Object: MetricReference

**Namespace**: `PerformanceEngine.Evaluation.Domain.Evaluation`

### Definition

```csharp
public sealed class MetricReference : ValueObject
{
    /// <summary>
    /// Name of the aggregation (e.g., "p95", "p99", "error_rate").
    /// </summary>
    public string AggregationName { get; }
    
    /// <summary>
    /// Completeness status of the metric (COMPLETE or PARTIAL).
    /// Indicates reliability for stakeholder review.
    /// </summary>
    public CompletessStatus Completeness { get; }
    
    /// <summary>
    /// Actual measured value.
    /// </summary>
    public double Value { get; }
    
    public MetricReference(string aggregationName, CompletessStatus completeness, double value)
    {
        // Validation...
    }
}
```

### JSON Serialization Contract

```json
{
  "aggregationName": "p95",
  "completeness": "COMPLETE",
  "value": 189.5
}
```

---

## Port: IEvaluationResultRecorder (Extended)

**Namespace**: `PerformanceEngine.Evaluation.Domain.Ports`

### Definition

```csharp
public interface IEvaluationResultRecorder
{
    /// <summary>
    /// Record evaluation result (pre-existing method).
    /// Now result includes Evidence field with full decision trail.
    /// </summary>
    void RecordResult(EvaluationResult result);
    
    /// <summary>
    /// NEW: Record evidence explicitly for audit/compliance systems.
    /// Allows decoupled recording of evidence from result recording.
    /// </summary>
    void RecordEvidence(EvaluationEvidence evidence);
}
```

### Contract

**Implementations** must:
1. Accept `EvaluationResult` with all fields populated (including Evidence)
2. Accept `EvaluationEvidence` as standalone value object
3. Preserve determinism: same input → same persistence (idempotent within epoch)
4. Support audit trail queries: retrieve evidence by RuleId, time range, outcome

---

## Service: Evaluator (Updated Behavior)

**Namespace**: `PerformanceEngine.Evaluation.Domain.Evaluation`

### Updated Method Signature

```csharp
public sealed class Evaluator
{
    /// <summary>
    /// Evaluate metric against rule with evidence capture.
    /// 
    /// Behavior:
    /// - If metric is PARTIAL and rule doesn't allow partials → INCONCLUSIVE
    /// - If metric is complete (or allowed partial) and passes rule → PASS
    /// - If metric is complete (or allowed partial) and fails rule → FAIL
    /// 
    /// Evidence:
    /// - Always populated; sufficient for decision explanation
    /// - Includes metric completeness status for transparency
    /// 
    /// Determinism:
    /// - Same inputs → identical output across 1000+ runs
    /// - Same EvaluatedAt timestamp
    /// - Violations sorted deterministically
    /// </summary>
    public EvaluationResult Evaluate(
        IMetric metric,
        IRule rule,
        IReadOnlyList<string>? partialMetricAllowlist = null)
    {
        var evaluatedAt = DateTime.UtcNow;  // Captured once
        
        // Check metric completeness
        if (metric.CompletessStatus == CompletessStatus.PARTIAL &&
            (partialMetricAllowlist == null || !partialMetricAllowlist.Contains(metric.AggregationName)))
        {
            // Metric is partial and not explicitly allowed
            var evidence = new EvaluationEvidence(
                ruleId: rule.Id,
                ruleName: rule.Name,
                metricsUsed: new[] { new MetricReference(metric.AggregationName, metric.CompletessStatus, metric.Value) },
                actualValues: new Dictionary<string, double> { { metric.AggregationName, metric.Value } },
                expectedConstraint: rule.GetConstraintDescription(),
                constraintSatisfied: false,
                decision: $"Metric {metric.AggregationName} is partial; rule does not allow partial metrics.",
                evaluatedAt: evaluatedAt);
            
            return EvaluationResult.CreateInconclusive(
                evidences: new[] { evidence },
                reason: $"Incomplete data for {metric.AggregationName}",
                evaluatedAt: evaluatedAt);
        }
        
        // Metric is complete or explicitly allowed as partial
        var ruleResult = rule.Evaluate(metric);
        
        var decisionEvidence = new EvaluationEvidence(
            ruleId: rule.Id,
            ruleName: rule.Name,
            metricsUsed: new[] { new MetricReference(metric.AggregationName, metric.CompletessStatus, metric.Value) },
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

---

## Contract Examples

### Example 1: PASS Outcome (Complete Metrics)

```csharp
var metric = new Metric(
    aggregationName: "p95",
    value: 189.5,
    completessStatus: CompletessStatus.COMPLETE,
    evidence: new MetricEvidence(100, 100, "5m"));

var rule = new ThresholdRule(
    id: "RULE-001",
    name: "P95 Latency SLA",
    aggregationName: "p95",
    threshold: 200,
    operator: ComparisonOperator.LessThan);

var result = evaluator.Evaluate(metric, rule);

Assert.Equal(Outcome.PASS, result.Outcome);
Assert.Empty(result.Violations);
Assert.Equal("All constraints satisfied with complete data.", result.OutcomeReason);
Assert.NotNull(result.Evidence);
Assert.True(result.Evidence.ConstraintSatisfied);
```

### Example 2: FAIL Outcome (Complete Metrics)

```csharp
var metric = new Metric(
    aggregationName: "p95",
    value: 215.5,
    completessStatus: CompletessStatus.COMPLETE,
    evidence: new MetricEvidence(100, 100, "5m"));

var result = evaluator.Evaluate(metric, rule);

Assert.Equal(Outcome.FAIL, result.Outcome);
Assert.Single(result.Violations);
Assert.Equal("1 constraint(s) violated.", result.OutcomeReason);
Assert.False(result.Evidence.ConstraintSatisfied);
```

### Example 3: INCONCLUSIVE Outcome (Partial Metrics)

```csharp
var metric = new Metric(
    aggregationName: "p95",
    value: 189.5,
    completessStatus: CompletessStatus.PARTIAL,
    evidence: new MetricEvidence(45, 100, "5m"));

var result = evaluator.Evaluate(metric, rule);  // No partialMetricAllowlist

Assert.Equal(Outcome.INCONCLUSIVE, result.Outcome);
Assert.Empty(result.Violations);
Assert.Contains("partial", result.OutcomeReason, StringComparison.OrdinalIgnoreCase);
Assert.Equal("Metric p95 is partial; rule does not allow partial metrics.", result.Evidence.Decision);
```

---

## Backward Compatibility

### Migration Path

**Before Enrichment**:
```csharp
public class EvaluationResult
{
    public Guid Id { get; }
    public bool IsPassing { get; }  // Simple boolean
    public IReadOnlyList<Violation> Violations { get; }
    public DateTime EvaluatedAt { get; }
}
```

**After Enrichment**:
```csharp
public record EvaluationResult
{
    public Guid Id { get; init; }
    public Outcome Outcome { get; init; }  // Extended enum
    public IReadOnlyList<Violation> Violations { get; init; }
    public EvaluationEvidence Evidence { get; init; }  // NEW
    public string OutcomeReason { get; init; }  // NEW
    public DateTime EvaluatedAt { get; init; }
}
```

**Compatibility Bridge**:
```csharp
// Old code using IsPassing can migrate via extension method:
public static bool IsPassing(this EvaluationResult result) =>
    result.Outcome == Outcome.PASS;

// Old code treating FAIL/PASS still works; INCONCLUSIVE is new path
```

---

## Contract Verification Tests

### Test: Deterministic Evidence

```csharp
[Fact]
public void Evaluation_ProducesDeterministicEvidence_Across1000Runs()
{
    var metric = new Metric(/* ... */);
    var rule = new ThresholdRule(/* ... */);
    
    var results = new List<string>();
    
    for (int i = 0; i < 1000; i++)
    {
        var result = evaluator.Evaluate(metric, rule);
        var json = JsonConvert.SerializeObject(result.Evidence);
        results.Add(json);
    }
    
    // All 1000 JSONs should be identical
    var first = results[0];
    Assert.All(results, json => Assert.Equal(first, json));
}
```

### Test: INCONCLUSIVE with Partial Metrics

```csharp
[Fact]
public void Evaluation_ReturnsInconclusive_WhenMetricPartialAndNotAllowed()
{
    var partialMetric = new Metric(
        completessStatus: CompletessStatus.PARTIAL,
        evidence: new MetricEvidence(50, 100, "5m"));
    
    var result = evaluator.Evaluate(partialMetric, rule);
    
    Assert.Equal(Outcome.INCONCLUSIVE, result.Outcome);
    Assert.Contains("partial", result.OutcomeReason);
}
```

### Test: Violation Sorting Determinism

```csharp
[Fact]
public void EvaluationResult_Violations_SortedDeterministically()
{
    var result1 = EvaluationResult.CreateFail(
        violations: new[] { violation3, violation1, violation2 },
        evidences: evidences,
        evaluatedAt: now);
    
    var result2 = EvaluationResult.CreateFail(
        violations: new[] { violation2, violation3, violation1 },
        evidences: evidences,
        evaluatedAt: now);
    
    // Both should have same sorted order
    Assert.Equal(
        result1.Violations.Select(v => v.RuleId),
        result2.Violations.Select(v => v.RuleId));
}
```

---

**Status**: ✅ Evaluation Domain Contract Complete
