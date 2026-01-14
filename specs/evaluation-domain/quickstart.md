# Quick Start: Evaluation Domain

**Goal**: Get the Evaluation Domain running in 15 minutes  
**Prerequisites**: .NET 8.0 SDK, basic C# knowledge

---

## 1. Setup (5 minutes)

### Clone & Build

```bash
# Clone repository
git clone <repository-url>
cd Per_Engine

# Build the Evaluation Domain
dotnet build src/PerformanceEngine.Evaluation.Domain/

# Verify build success
dotnet test tests/PerformanceEngine.Evaluation.Domain.Tests/
```

**Expected Output**:
```
Build succeeded.
    0 Warning(s)
    0 Error(s)

Test run successful.
Total tests: 120+
Passed: 120+
```

---

## 2. Basic Usage (5 minutes)

Create a simple console app to evaluate metrics:

```bash
# Create new console app
mkdir EvaluationExample
cd EvaluationExample
dotnet new console

# Add project reference
dotnet add reference ../src/PerformanceEngine.Evaluation.Domain/
dotnet add reference ../src/PerformanceEngine.Metrics.Domain/
```

### Program.cs

```csharp
using PerformanceEngine.Evaluation.Domain.Domain.Rules;
using PerformanceEngine.Evaluation.Domain.Domain.Evaluation;
using PerformanceEngine.Metrics.Domain.Metrics;
using System.Collections.Immutable;

// 1. Create test data (normally from Metrics Domain)
var context = new ExecutionContext(
    engineName: "test",
    executionId: Guid.NewGuid(),
    scenarioName: "quick-start"
);

var sample = new Sample(
    timestamp: DateTime.UtcNow,
    duration: new Latency(150, LatencyUnit.Milliseconds),
    status: SampleStatus.Success,
    errorClassification: null,
    executionContext: context
);

var sampleCollection = SampleCollection.Create(new[] { sample }, context.ScenarioName);

var metric = new Metric(
    samples: sampleCollection,
    window: AggregationWindow.FullExecution(),
    metricType: "api_latency",
    aggregatedValues: ImmutableList.Create(
        new AggregationResult(
            value: new Latency(150, LatencyUnit.Milliseconds),
            operationName: "P95",
            computedAt: DateTime.UtcNow
        )
    ),
    computedAt: DateTime.UtcNow
);

// 2. Define a rule
var rule = new ThresholdRule
{
    Id = "RULE-001",
    Name = "P95 Latency SLA",
    Description = "P95 must be under 200ms",
    AggregationName = "P95",
    Threshold = 200.0,
    Operator = ComparisonOperator.LessThan
};

// 3. Evaluate
var evaluator = new Evaluator();
var result = evaluator.Evaluate(metric, rule);

// 4. Check result
if (result.Outcome == Severity.PASS)
{
    Console.WriteLine("✅ Test PASSED!");
    Console.WriteLine($"   P95: {metric.AggregatedValues[0].Value.GetValueIn(LatencyUnit.Milliseconds)}ms");
}
else
{
    Console.WriteLine("❌ Test FAILED!");
    foreach (var violation in result.Violations)
    {
        Console.WriteLine($"   {violation.Message}");
        Console.WriteLine($"   Expected: {violation.Threshold}");
        Console.WriteLine($"   Actual: {violation.ActualValue}");
    }
}
```

### Run

```bash
dotnet run
```

**Expected Output**:
```
✅ Test PASSED!
   P95: 150ms
```

---

## 3. Advanced: Batch Evaluation (5 minutes)

Evaluate multiple metrics with multiple rules:

```csharp
// Define multiple rules
var rules = new IRule[]
{
    new ThresholdRule
    {
        Id = "RULE-001",
        Name = "P95 Latency",
        Description = "P95 < 200ms",
        AggregationName = "P95",
        Threshold = 200.0,
        Operator = ComparisonOperator.LessThan
    },
    new ThresholdRule
    {
        Id = "RULE-002",
        Name = "P99 Latency",
        Description = "P99 < 500ms",
        AggregationName = "P99",
        Threshold = 500.0,
        Operator = ComparisonOperator.LessThan
    },
    new RangeRule
    {
        Id = "RULE-003",
        Name = "Error Rate",
        Description = "Error rate between 0.1% and 1.0%",
        AggregationName = "ErrorRate",
        MinBound = 0.1,
        MaxBound = 1.0
    }
};

// Create multiple metrics (simplified - normally from real test data)
var metrics = new[] { metric1, metric2, metric3 };

// Batch evaluate
var evaluator = new Evaluator();
var results = evaluator.EvaluateMultiple(metrics, rules);

// Report results
var failedCount = results.Count(r => r.Outcome == Severity.FAIL);
Console.WriteLine($"Evaluated {results.Count()} metrics");
Console.WriteLine($"Failed: {failedCount}");
Console.WriteLine($"Passed: {results.Count() - failedCount}");
```

---

## 4. Custom Rules

Create domain-specific evaluation logic:

```csharp
public sealed record CustomLatencyPercentileRule : IRule
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required double Percentile { get; init; }
    public required double MaxMilliseconds { get; init; }

    public EvaluationResult Evaluate(Metric metric)
    {
        var aggregationName = $"P{Percentile:F1}".Replace(".0", "");
        var aggregation = metric.AggregatedValues
            .FirstOrDefault(a => a.OperationName.Equals(aggregationName, 
                StringComparison.OrdinalIgnoreCase));

        if (aggregation == null)
        {
            var violation = Violation.Create(
                ruleId: Id,
                metricName: metric.MetricType,
                actualValue: double.NaN,
                threshold: MaxMilliseconds,
                message: $"{aggregationName} not found in metric"
            );
            return EvaluationResult.Fail(
                ImmutableList.Create(violation),
                DateTime.UtcNow
            );
        }

        var actualMs = aggregation.Value.GetValueIn(LatencyUnit.Milliseconds);
        if (actualMs <= MaxMilliseconds)
        {
            return EvaluationResult.Pass(DateTime.UtcNow);
        }

        var violation = Violation.Create(
            ruleId: Id,
            metricName: metric.MetricType,
            actualValue: actualMs,
            threshold: MaxMilliseconds,
            message: $"{aggregationName} {actualMs}ms exceeds {MaxMilliseconds}ms"
        );

        return EvaluationResult.Fail(ImmutableList.Create(violation), DateTime.UtcNow);
    }

    public bool Equals(IRule? other) =>
        other is CustomLatencyPercentileRule r &&
        Id == r.Id &&
        Percentile == r.Percentile &&
        MaxMilliseconds == r.MaxMilliseconds;

    public override int GetHashCode() =>
        HashCode.Combine(Id, Percentile, MaxMilliseconds);
}
```

**Usage**:
```csharp
var customRule = new CustomLatencyPercentileRule
{
    Id = "CUSTOM-001",
    Name = "P99.9 Latency",
    Description = "P99.9 must be under 1000ms",
    Percentile = 99.9,
    MaxMilliseconds = 1000.0
};

var result = evaluator.Evaluate(metric, customRule);
```

---

## 5. Testing Your Code

### Unit Tests

```csharp
using Xunit;
using FluentAssertions;

public class MyRuleTests
{
    [Fact]
    public void MyRule_WithPassingMetric_ReturnsPass()
    {
        // Arrange
        var rule = new ThresholdRule
        {
            Id = "TEST",
            Name = "Test",
            Description = "Test",
            AggregationName = "P95",
            Threshold = 200.0,
            Operator = ComparisonOperator.LessThan
        };

        var metric = CreateTestMetric(p95: 150.0);

        // Act
        var result = rule.Evaluate(metric);

        // Assert
        result.Outcome.Should().Be(Severity.PASS);
        result.Violations.Should().BeEmpty();
    }
}
```

### Run Tests

```bash
# All tests
dotnet test

# Specific test class
dotnet test --filter "FullyQualifiedName~MyRuleTests"

# Determinism tests (1000+ iterations)
dotnet test --filter "FullyQualifiedName~Determinism"
```

---

## 6. Next Steps

1. **Read Full Documentation**: [README.md](../../src/PerformanceEngine.Evaluation.Domain/README.md)
2. **Implementation Guide**: [IMPLEMENTATION_GUIDE.md](../../src/PerformanceEngine.Evaluation.Domain/IMPLEMENTATION_GUIDE.md)
3. **Domain Specification**: [specs/evaluation-domain/spec.md](spec.md)
4. **API Contracts**: [contracts/](contracts/) directory

---

## Troubleshooting

### Build Errors

```bash
# Clean and rebuild
dotnet clean
dotnet build
```

### Missing Dependencies

```bash
# Restore NuGet packages
dotnet restore
```

### Test Failures

```bash
# Run tests with verbose output
dotnet test --logger "console;verbosity=detailed"
```

---

## Common Patterns

### Pattern 1: CI/CD Integration

```csharp
// In your CI/CD pipeline
var results = evaluator.EvaluateMultiple(metrics, rules);
var hasFailures = results.Any(r => r.Outcome == Severity.FAIL);

if (hasFailures)
{
    Console.WriteLine("❌ Quality gates FAILED");
    Environment.Exit(1);  // Fail the build
}
else
{
    Console.WriteLine("✅ Quality gates PASSED");
    Environment.Exit(0);
}
```

### Pattern 2: Dynamic Rule Loading

```csharp
// Load rules from configuration
var ruleConfigs = LoadRulesFromJson("rules.json");

var rules = ruleConfigs.Select(cfg => new ThresholdRule
{
    Id = cfg.Id,
    Name = cfg.Name,
    Description = cfg.Description,
    AggregationName = cfg.AggregationName,
    Threshold = cfg.Threshold,
    Operator = ParseOperator(cfg.Operator)
}).ToArray();
```

### Pattern 3: Detailed Reporting

```csharp
var results = evaluator.EvaluateMultiple(metrics, rules);

foreach (var result in results)
{
    Console.WriteLine($"\n{result.Outcome} @ {result.EvaluatedAt:yyyy-MM-dd HH:mm:ss}");
    
    if (result.Violations.Any())
    {
        Console.WriteLine("Violations:");
        foreach (var v in result.Violations)
        {
            Console.WriteLine($"  - {v.Message}");
            Console.WriteLine($"    Rule: {v.RuleId}");
            Console.WriteLine($"    Metric: {v.MetricName}");
            Console.WriteLine($"    Expected: {v.Threshold}");
            Console.WriteLine($"    Actual: {v.ActualValue}");
        }
    }
}
```

---

**Quick Start Complete!** ✅

You now have a working Evaluation Domain setup and understand the basic patterns.

For production use, review the full documentation and implement proper error handling, logging, and persistence.
