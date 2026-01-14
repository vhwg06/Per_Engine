# IRule Interface Contract

**Namespace**: `PerformanceEngine.Evaluation.Domain.Domain.Rules`  
**Type**: Interface  
**Purpose**: Strategy pattern contract for all evaluation rules

---

## Interface Definition

```csharp
public interface IRule : IEquatable<IRule>
{
    /// <summary>
    /// Unique identifier for this rule instance.
    /// Used for violation tracking and rule comparison.
    /// </summary>
    string Id { get; }

    /// <summary>
    /// Human-readable name for this rule.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Detailed description of what this rule validates.
    /// Should clearly state the condition being checked.
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Evaluates the given metric against this rule's constraints.
    /// Must be deterministic: identical metric always produces identical result.
    /// </summary>
    /// <param name="metric">The metric to evaluate.</param>
    /// <returns>EvaluationResult containing outcome and any violations detected.</returns>
    EvaluationResult Evaluate(Metric metric);
}
```

---

## Contract Guarantees

### 1. Determinism
**MUST**: Given the same metric, `Evaluate()` must always return the exact same result.

**Prohibited**:
- `DateTime.Now` for logic decisions
- `Random` or probabilistic operations
- External state dependencies (databases, files, network)

**Allowed**:
- `DateTime.UtcNow` for timestamp-only purposes (e.g., `EvaluationResult.EvaluatedAt`)

### 2. Purity
**MUST**: `Evaluate()` must be a pure function with no side effects.

**Prohibited**:
- Modifying input metric
- Writing to files/databases
- Network calls
- Console output
- Modifying instance state

### 3. Immutability
**MUST**: All rule properties must be immutable after construction.

**Recommended**: Use `record` types with `init`-only properties:
```csharp
public sealed record MyRule : IRule
{
    public required string Id { get; init; }
    // ...
}
```

### 4. Equality
**MUST**: Implement `IEquatable<IRule>` for rule comparison and deduplication.

**Rules are equal if**:
- Same type (use `GetType()` check)
- Same Id
- Same logical configuration (thresholds, operators, etc.)

---

## Built-In Implementations

### ThresholdRule

Single comparison against a threshold.

**Example**:
```csharp
var rule = new ThresholdRule
{
    Id = "RULE-001",
    Name = "P95 Latency SLA",
    Description = "P95 latency must be under 200ms",
    AggregationName = "P95",
    Threshold = 200.0,
    Operator = ComparisonOperator.LessThan
};
```

**Supported Operators**:
- `LessThan` (<)
- `LessThanOrEqual` (≤)
- `GreaterThan` (>)
- `GreaterThanOrEqual` (≥)
- `Equal` (==, epsilon-based)
- `NotEqual` (!=)

### RangeRule

Value must fall within a range (exclusive bounds).

**Example**:
```csharp
var rule = new RangeRule
{
    Id = "RULE-002",
    Name = "Error Rate Range",
    Description = "Error rate must be between 0.1% and 1.0%",
    AggregationName = "ErrorRate",
    MinBound = 0.1,
    MaxBound = 1.0
};
```

### CompositeRule

Logical combination of multiple rules (AND/OR).

**Example**:
```csharp
var rule = new CompositeRule
{
    Id = "RULE-003",
    Name = "Combined SLA",
    Description = "P95 < 200ms AND error_rate < 1%",
    Operator = LogicalOperator.And,
    SubRules = ImmutableList.Create<IRule>(rule1, rule2)
};
```

---

## Custom Implementation Template

```csharp
using PerformanceEngine.Evaluation.Domain.Domain.Rules;
using PerformanceEngine.Evaluation.Domain.Domain.Evaluation;
using PerformanceEngine.Metrics.Domain.Metrics;
using System.Collections.Immutable;

namespace MyDomain.CustomRules;

/// <summary>
/// Custom rule for evaluating specific business constraints.
/// </summary>
public sealed record MyCustomRule : IRule
{
    // Required IRule properties
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string Description { get; init; }

    // Custom properties for rule logic
    public required string AggregationName { get; init; }
    public required double ThresholdValue { get; init; }

    /// <summary>
    /// Deterministic evaluation logic.
    /// </summary>
    public EvaluationResult Evaluate(Metric metric)
    {
        // 1. Null check (defensive)
        if (metric == null)
        {
            throw new ArgumentNullException(nameof(metric));
        }

        // 2. Find required aggregation
        var aggregation = metric.AggregatedValues
            .FirstOrDefault(a => a.OperationName.Equals(AggregationName, 
                StringComparison.OrdinalIgnoreCase));

        if (aggregation == null)
        {
            // Aggregation missing → FAIL
            var violation = Violation.Create(
                ruleId: Id,
                metricName: metric.MetricType,
                actualValue: double.NaN,
                threshold: ThresholdValue,
                message: $"{AggregationName} aggregation not found in metric"
            );

            return EvaluationResult.Fail(
                ImmutableList.Create(violation),
                DateTime.UtcNow
            );
        }

        // 3. Extract actual value
        var actualValue = aggregation.Value.GetValueIn(LatencyUnit.Milliseconds);

        // 4. Apply custom business logic
        if (MeetsCustomCriteria(actualValue))
        {
            return EvaluationResult.Pass(DateTime.UtcNow);
        }

        // 5. Create violation for failure
        var violation = Violation.Create(
            ruleId: Id,
            metricName: metric.MetricType,
            actualValue: actualValue,
            threshold: ThresholdValue,
            message: $"{AggregationName} {actualValue} does not meet custom criteria"
        );

        return EvaluationResult.Fail(
            ImmutableList.Create(violation),
            DateTime.UtcNow
        );
    }

    private bool MeetsCustomCriteria(double value)
    {
        // Custom deterministic logic here
        return value < ThresholdValue;
    }

    /// <summary>
    /// Equality based on configuration.
    /// </summary>
    public bool Equals(IRule? other)
    {
        if (other is not MyCustomRule customRule)
            return false;

        return Id == customRule.Id &&
               AggregationName == customRule.AggregationName &&
               Math.Abs(ThresholdValue - customRule.ThresholdValue) < 0.001;
    }

    /// <summary>
    /// Consistent hash code for equality.
    /// </summary>
    public override int GetHashCode() =>
        HashCode.Combine(Id, AggregationName, ThresholdValue);
}
```

---

## Testing Custom Rules

### Unit Test Template

```csharp
using Xunit;
using FluentAssertions;

public class MyCustomRuleTests
{
    [Fact]
    public void MyCustomRule_WithPassingMetric_ReturnsPass()
    {
        // Arrange
        var rule = new MyCustomRule
        {
            Id = "CUSTOM-001",
            Name = "Custom Test",
            Description = "Tests custom logic",
            AggregationName = "P95",
            ThresholdValue = 200.0
        };

        var metric = TestMetricFactory.CreateMetric("P95", 150.0);

        // Act
        var result = rule.Evaluate(metric);

        // Assert
        result.Outcome.Should().Be(Severity.PASS);
        result.Violations.Should().BeEmpty();
    }

    [Fact]
    public void MyCustomRule_WithFailingMetric_ReturnsFail()
    {
        // Arrange
        var rule = new MyCustomRule
        {
            Id = "CUSTOM-001",
            Name = "Custom Test",
            Description = "Tests custom logic",
            AggregationName = "P95",
            ThresholdValue = 200.0
        };

        var metric = TestMetricFactory.CreateMetric("P95", 250.0);

        // Act
        var result = rule.Evaluate(metric);

        // Assert
        result.Outcome.Should().Be(Severity.FAIL);
        result.Violations.Should().HaveCount(1);
        result.Violations[0].ActualValue.Should().BeApproximately(250.0, 0.01);
    }

    [Fact]
    public void MyCustomRule_Determinism_1000Runs()
    {
        // Arrange
        var rule = new MyCustomRule { /* ... */ };
        var metric = TestMetricFactory.CreateMetric("P95", 150.0);

        // Act - evaluate 1000 times
        var results = Enumerable.Range(0, 1000)
            .Select(_ => rule.Evaluate(metric))
            .ToList();

        // Assert - all results identical
        var firstResult = results.First();
        results.Should().AllSatisfy(r =>
        {
            r.Outcome.Should().Be(firstResult.Outcome);
            r.Violations.Count.Should().Be(firstResult.Violations.Count);
        });
    }
}
```

---

## Integration with Evaluator

Custom rules work seamlessly with the standard `Evaluator`:

```csharp
var evaluator = new Evaluator();
var customRule = new MyCustomRule { /* ... */ };
var metric = /* from Metrics Domain */;

// Single evaluation
var result = evaluator.Evaluate(metric, customRule);

// Batch evaluation with mixed rule types
var rules = new IRule[]
{
    new ThresholdRule { /* ... */ },
    new RangeRule { /* ... */ },
    customRule
};

var results = evaluator.EvaluateMultiple(new[] { metric }, rules);
```

---

## Contract Violations

### ❌ Non-Deterministic Code

```csharp
// WRONG: Using DateTime.Now in logic
public EvaluationResult Evaluate(Metric metric)
{
    if (DateTime.Now.Hour >= 9 && DateTime.Now.Hour < 17)
    {
        // Different behavior during business hours
        return EvaluationResult.Pass(DateTime.UtcNow);
    }
    // ...
}
```

### ❌ Side Effects

```csharp
// WRONG: Writing to console
public EvaluationResult Evaluate(Metric metric)
{
    Console.WriteLine($"Evaluating {metric.MetricType}");  // Side effect!
    // ...
}
```

### ❌ External Dependencies

```csharp
// WRONG: Database access
public EvaluationResult Evaluate(Metric metric)
{
    var threshold = _database.GetThreshold(metric.MetricType);  // External state!
    // ...
}
```

---

## Best Practices

1. **Keep Rules Simple**: Each rule should check one concern
2. **Use Composition**: Combine simple rules with `CompositeRule` for complex logic
3. **Clear Messages**: Violation messages should be actionable
4. **Test Determinism**: Always include 1000+ iteration tests
5. **Document Intent**: `Description` property should explain the "why"

---

## See Also

- [EvaluationResult Contract](evaluation-result.md)
- [Evaluator Service](evaluator-service.md)
- [Custom Rules Guide](../../src/PerformanceEngine.Evaluation.Domain/README.md#custom-rules)
