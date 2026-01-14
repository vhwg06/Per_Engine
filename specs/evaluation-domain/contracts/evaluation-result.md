# EvaluationResult Contract

**Namespace**: `PerformanceEngine.Evaluation.Domain.Domain.Evaluation`  
**Type**: Sealed Record (Immutable Entity)  
**Purpose**: Represents the immutable outcome of evaluating a metric against rules

---

## Type Definition

```csharp
public sealed record EvaluationResult
{
    /// <summary>
    /// Overall evaluation outcome.
    /// </summary>
    public Severity Outcome { get; init; }

    /// <summary>
    /// List of all rule violations detected (empty if PASS).
    /// </summary>
    public ImmutableList<Violation> Violations { get; init; }

    /// <summary>
    /// Timestamp when evaluation was performed (UTC).
    /// </summary>
    public DateTime EvaluatedAt { get; init; }
}
```

---

## Factory Methods

### Pass()

Creates a PASS result with no violations.

```csharp
public static EvaluationResult Pass(DateTime evaluatedAt) =>
    new()
    {
        Outcome = Severity.PASS,
        Violations = ImmutableList<Violation>.Empty,
        EvaluatedAt = evaluatedAt
    };
```

**Usage**:
```csharp
return EvaluationResult.Pass(DateTime.UtcNow);
```

### Fail()

Creates a FAIL result with violation details.

```csharp
public static EvaluationResult Fail(
    ImmutableList<Violation> violations,
    DateTime evaluatedAt) =>
    new()
    {
        Outcome = Severity.FAIL,
        Violations = violations,
        EvaluatedAt = evaluatedAt
    };
```

**Usage**:
```csharp
var violation = Violation.Create(
    ruleId: "RULE-001",
    metricName: "api_latency",
    actualValue: 250.0,
    threshold: 200.0,
    message: "P95 250ms exceeds threshold 200ms"
);

return EvaluationResult.Fail(
    ImmutableList.Create(violation),
    DateTime.UtcNow
);
```

### Warning()

Creates a WARN result with non-critical violations.

```csharp
public static EvaluationResult Warning(
    ImmutableList<Violation> violations,
    DateTime evaluatedAt) =>
    new()
    {
        Outcome = Severity.WARN,
        Violations = violations,
        EvaluatedAt = evaluatedAt
    };
```

---

## Invariants

### 1. Immutability
- All properties use `init` accessors
- Cannot be modified after construction
- `Violations` is `ImmutableList`

### 2. Consistency
- `PASS` → `Violations` must be empty
- `FAIL` or `WARN` → `Violations` must not be empty

### 3. Determinism
- Given same inputs, produces same result
- `EvaluatedAt` is for audit/logging only; does not affect logic

---

## Usage Patterns

### Pattern 1: Single Evaluation

```csharp
var evaluator = new Evaluator();
var result = evaluator.Evaluate(metric, rule);

if (result.Outcome == Severity.PASS)
{
    Console.WriteLine("✅ Passed");
}
else
{
    Console.WriteLine($"❌ Failed with {result.Violations.Count} violations");
    foreach (var v in result.Violations)
    {
        Console.WriteLine($"  - {v.Message}");
    }
}
```

### Pattern 2: Batch Aggregation

```csharp
var results = evaluator.EvaluateMultiple(metrics, rules);

var summary = new
{
    Total = results.Count(),
    Passed = results.Count(r => r.Outcome == Severity.PASS),
    Failed = results.Count(r => r.Outcome == Severity.FAIL),
    Warnings = results.Count(r => r.Outcome == Severity.WARN)
};

Console.WriteLine($"Results: {summary.Passed} passed, {summary.Failed} failed, {summary.Warnings} warnings");
```

### Pattern 3: CI/CD Gate

```csharp
var results = evaluator.EvaluateMultiple(metrics, rules);
var hasFailures = results.Any(r => r.Outcome == Severity.FAIL);

if (hasFailures)
{
    // Report violations
    var allViolations = results
        .SelectMany(r => r.Violations)
        .ToList();

    Console.WriteLine($"Quality gate FAILED: {allViolations.Count} violations");
    foreach (var v in allViolations)
    {
        Console.WriteLine($"[{v.RuleId}] {v.Message}");
    }

    Environment.Exit(1);  // Fail the build
}
```

### Pattern 4: Detailed Reporting

```csharp
var result = evaluator.Evaluate(metric, rule);

var report = new
{
    Outcome = result.Outcome.ToString(),
    EvaluatedAt = result.EvaluatedAt.ToString("O"),  // ISO 8601
    ViolationCount = result.Violations.Count,
    Violations = result.Violations.Select(v => new
    {
        Rule = v.RuleId,
        Metric = v.MetricName,
        Expected = v.Threshold,
        Actual = v.ActualValue,
        Message = v.Message
    }).ToList()
};

var json = JsonSerializer.Serialize(report, new JsonSerializerOptions
{
    WriteIndented = true
});

File.WriteAllText("evaluation-report.json", json);
```

---

## JSON Serialization

### Example Output

```json
{
  "outcome": "FAIL",
  "violations": [
    {
      "ruleId": "RULE-001",
      "metricName": "api_latency",
      "actualValue": 250.0,
      "threshold": 200.0,
      "message": "P95 250ms exceeds threshold 200ms"
    },
    {
      "ruleId": "RULE-002",
      "metricName": "error_rate",
      "actualValue": 2.5,
      "threshold": 1.0,
      "message": "Error rate 2.5% exceeds threshold 1.0%"
    }
  ],
  "evaluatedAt": "2026-01-14T10:30:00.000Z"
}
```

### Serialization Settings

For deterministic JSON output:

```csharp
var options = new JsonSerializerOptions
{
    WriteIndented = false,  // Compact format
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.Never
};

var json = JsonSerializer.Serialize(result, options);
```

---

## Equality Comparison

```csharp
// EvaluationResult uses record equality (value-based)
var result1 = EvaluationResult.Pass(timestamp);
var result2 = EvaluationResult.Pass(timestamp);

Assert.Equal(result1, result2);  // True - same values

// Different timestamps → different results
var result3 = EvaluationResult.Pass(timestamp.AddSeconds(1));
Assert.NotEqual(result1, result3);  // True - different timestamp
```

---

## Error Handling

### Null Checks

```csharp
// Evaluator handles null gracefully
var result = evaluator.Evaluate(null, rule);
// Returns FAIL with "Metric is null" violation

var result = evaluator.Evaluate(metric, null);
// Returns FAIL with "Rule is null" violation
```

### Missing Aggregations

```csharp
// Rule references non-existent aggregation
var rule = new ThresholdRule
{
    AggregationName = "P99",  // Not in metric
    // ...
};

var result = rule.Evaluate(metric);
// Returns FAIL with "P99 aggregation not found" violation
```

---

## Best Practices

### ✅ DO

- Use factory methods (`Pass()`, `Fail()`, `Warning()`)
- Always provide `DateTime.UtcNow` for audit trail
- Create clear, actionable violation messages
- Serialize results for persistence/reporting
- Check `Outcome` before accessing `Violations`

### ❌ DON'T

- Mutate result after creation (it's immutable)
- Use `EvaluatedAt` for business logic
- Assume `Violations` is non-null (it's always initialized)
- Compare results by reference (use value equality)

---

## Testing

### Unit Test Template

```csharp
[Fact]
public void EvaluationResult_Pass_HasNoViolations()
{
    // Act
    var result = EvaluationResult.Pass(DateTime.UtcNow);

    // Assert
    result.Outcome.Should().Be(Severity.PASS);
    result.Violations.Should().BeEmpty();
}

[Fact]
public void EvaluationResult_Fail_ContainsViolations()
{
    // Arrange
    var violation = Violation.Create(
        ruleId: "TEST",
        metricName: "test_metric",
        actualValue: 100.0,
        threshold: 50.0,
        message: "Test violation"
    );

    // Act
    var result = EvaluationResult.Fail(
        ImmutableList.Create(violation),
        DateTime.UtcNow
    );

    // Assert
    result.Outcome.Should().Be(Severity.FAIL);
    result.Violations.Should().HaveCount(1);
    result.Violations[0].RuleId.Should().Be("TEST");
}
```

---

## See Also

- [IRule Interface](rule-interface.md)
- [Violation Value Object](violation.md)
- [Evaluator Service](evaluator-service.md)
- [Severity Enum](../spec.md#severity)
