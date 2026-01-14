# Custom Rules Guide

This guide explains how to extend the Evaluation Domain with custom rule types.

## Overview

The Evaluation Domain uses the **Strategy Pattern** via the `IRule` interface. Any class implementing `IRule` can be evaluated without modifying core evaluation logic.

## IRule Interface

```csharp
public interface IRule : IEquatable<IRule>
{
    string Id { get; }
    string Name { get; }
    string Description { get; }
    
    EvaluationResult Evaluate(Metric metric);
}
```

## Creating a Custom Rule

### Step 1: Implement IRule

```csharp
public sealed record CustomPercentileRule : IRule
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required string PercentileName { get; init; }  // e.g., "p90", "p95"
    public required double MaxValue { get; init; }
    
    public EvaluationResult Evaluate(Metric metric)
    {
        // Find the specified percentile aggregation
        var aggregation = metric.AggregatedValues
            .FirstOrDefault(a => a.OperationName == PercentileName);
        
        if (aggregation == null)
        {
            var violation = Violation.Create(
                Id,
                metric.MetricType,
                double.NaN,
                MaxValue,
                $"Aggregation '{PercentileName}' not found"
            );
            return EvaluationResult.Fail(
                ImmutableList.Create(violation),
                DateTime.UtcNow
            );
        }
        
        var actualValue = aggregation.Value.GetValueIn(LatencyUnit.Milliseconds);
        
        if (actualValue > MaxValue)
        {
            var violation = Violation.Create(
                Id,
                metric.MetricType,
                actualValue,
                MaxValue,
                $"{PercentileName} latency {actualValue:F2}ms exceeds maximum {MaxValue:F2}ms"
            );
            return EvaluationResult.Fail(
                ImmutableList.Create(violation),
                DateTime.UtcNow
            );
        }
        
        return EvaluationResult.Pass(DateTime.UtcNow);
    }
    
    public virtual bool Equals(IRule? other)
    {
        if (other is not CustomPercentileRule custom)
            return false;
            
        return Id == custom.Id &&
               PercentileName == custom.PercentileName &&
               Math.Abs(MaxValue - custom.MaxValue) < 0.001;
    }
    
    public override int GetHashCode()
    {
        return HashCode.Combine(Id, PercentileName, MaxValue);
    }
}
```

### Step 2: Use Your Custom Rule

```csharp
var customRule = new CustomPercentileRule
{
    Id = "custom-p90",
    Name = "P90 Latency Check",
    Description = "Validates P90 latency is under 150ms",
    PercentileName = "p90",
    MaxValue = 150.0
};

var evaluator = new Evaluator();
var result = evaluator.Evaluate(metric, customRule);
```

## Design Guidelines

### 1. **Immutability**
Use `record` types with `init` accessors:
```csharp
public sealed record MyCustomRule : IRule
{
    public required string Id { get; init; }
    // ... other properties with init
}
```

### 2. **Determinism**
- No random numbers, no `DateTime.Now` (use provided timestamps)
- Same inputs MUST produce same outputs
- Use `InvariantCulture` for string formatting

```csharp
// ❌ BAD - Non-deterministic
var message = DateTime.Now.ToString();

// ✅ GOOD - Deterministic
var message = $"Value {actualValue:F2} exceeds {threshold:F2}";
```

### 3. **Null Handling**
Always validate inputs:
```csharp
public EvaluationResult Evaluate(Metric metric)
{
    if (metric == null)
    {
        return EvaluationResult.Fail(
            ImmutableList.Create(Violation.Create(/*...*/)),
            DateTime.UtcNow
        );
    }
    
    // ... rest of logic
}
```

### 4. **Error Messages**
Be specific and actionable:
```csharp
// ❌ BAD
"Validation failed"

// ✅ GOOD
"P95 latency 250.50ms exceeds threshold 200.00ms (aggregation: p95)"
```

### 5. **Equality Implementation**
Required for deduplication and testing:
```csharp
public virtual bool Equals(IRule? other)
{
    if (other is not MyCustomRule custom)
        return false;
        
    return Id == custom.Id &&
           /* compare all significant properties */;
}

public override int GetHashCode()
{
    return HashCode.Combine(Id, Property1, Property2);
}
```

## Built-in Rule Examples

### ThresholdRule
Compares an aggregation value against a threshold:
```csharp
var rule = new ThresholdRule
{
    Id = "lat-p95",
    Name = "P95 Latency",
    Description = "P95 must be under 200ms",
    AggregationName = "p95",
    Threshold = 200.0,
    Operator = ComparisonOperator.LessThan
};
```

### RangeRule
Validates a value is within bounds:
```csharp
var rule = new RangeRule
{
    Id = "error-range",
    Name = "Error Rate Range",
    Description = "Error rate must be between 0% and 1%",
    AggregationName = "error_rate",
    MinBound = 0.0,
    MaxBound = 1.0
};
```

### CompositeRule
Combines multiple rules with AND/OR logic:
```csharp
var composite = new CompositeRule
{
    Id = "latency-and-errors",
    Name = "Performance Gate",
    Description = "Both latency and error rate must pass",
    Operator = LogicalOperator.And,
    SubRules = new List<IRule>
    {
        new ThresholdRule { /* P95 < 200ms */ },
        new ThresholdRule { /* errors < 1% */ }
    }
};
```

## Testing Custom Rules

### Unit Test Example

```csharp
[Fact]
public void CustomRule_ValueExceedsThreshold_ReturnsFail()
{
    // Arrange
    var rule = new CustomPercentileRule
    {
        Id = "test-rule",
        Name = "Test",
        Description = "Test rule",
        PercentileName = "p90",
        MaxValue = 100.0
    };
    
    var metric = TestMetricFactory.CreateMetricWithAggregations(
        "test",
        new Dictionary<string, double> { { "p90", 150.0 } }
    );
    
    // Act
    var result = rule.Evaluate(metric);
    
    // Assert
    result.Outcome.Should().Be(Severity.FAIL);
    result.Violations.Should().HaveCount(1);
    result.Violations[0].ActualValue.Should().Be(150.0);
}
```

### Determinism Test

```csharp
[Fact]
public void CustomRule_Deterministic_IdenticalResults()
{
    var rule = new CustomPercentileRule { /* config */ };
    var metric = TestMetricFactory.CreateMetricWithAggregations(/*...*/);
    
    // Run 1000 times
    var results = Enumerable.Range(0, 1000)
        .Select(_ => rule.Evaluate(metric))
        .ToList();
    
    // All results should be identical
    results.Should().AllSatisfy(r => r.Outcome.Should().Be(Severity.FAIL));
}
```

## Using RuleFactory

For common patterns, use the built-in factory:

```csharp
// Instead of manual creation:
var rule = RuleFactory.P95LatencyRule("lat-01", 200.0);
var errorRule = RuleFactory.ErrorRateRule("err-01", 1.0);
var rangeRule = RuleFactory.Range("range-01", "metric", "agg", 10.0, 20.0);
```

## Best Practices

1. **Keep rules simple** - Each rule should validate one concept
2. **Use composition** - Combine simple rules with `CompositeRule` for complex logic
3. **Document thoroughly** - Explain what the rule validates and why
4. **Test edge cases** - Null inputs, missing aggregations, boundary values
5. **Version your rules** - Include version in ID if rules evolve (e.g., "lat-p95-v2")

## Anti-Patterns to Avoid

❌ **Don't access external resources**
```csharp
// BAD - I/O in domain logic
var threshold = File.ReadAllText("config.json");
```

❌ **Don't use non-deterministic values**
```csharp
// BAD - Random values
var jitter = Random.Shared.NextDouble();
```

❌ **Don't modify inputs**
```csharp
// BAD - Side effects
public EvaluationResult Evaluate(Metric metric)
{
    metric.SomeProperty = newValue;  // Don't do this!
}
```

❌ **Don't catch and swallow exceptions**
```csharp
// BAD - Silent failures
try {
    return ValidateMetric(metric);
}
catch {
    return EvaluationResult.Pass();  // Don't hide errors!
}
```

## FAQ

**Q: Can I use dependency injection in rules?**
A: Rules should be pure value objects with no dependencies. Put I/O logic in adapters.

**Q: How do I access external thresholds?**
A: Pass thresholds as constructor parameters. Load them outside the domain layer.

**Q: Can rules call other rules?**
A: Yes! Use `CompositeRule` or call `rule.Evaluate()` directly in your custom logic.

**Q: How do I debug rule evaluation?**
A: Check the `EvaluationResult.Violations` collection for detailed failure messages.

**Q: Can I have async rules?**
A: No. Rules must be synchronous and deterministic. Do async work before calling Evaluate().

## Examples Repository

See `tests/.../Domain/Rules/CustomRuleTests.cs` for complete working examples.

## Support

For questions or issues with custom rules:
1. Check existing built-in rules for patterns
2. Review test examples in the test suite
3. Ensure your rule follows DDD and Clean Architecture principles
