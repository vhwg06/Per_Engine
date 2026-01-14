namespace PerformanceEngine.Evaluation.Domain.Tests.Domain.Rules;

using PerformanceEngine.Metrics.Domain.Metrics;

/// <summary>
/// Demonstrates custom rule extensibility via IRule interface.
/// This example shows how to create a custom rule that evaluates percentile values.
/// </summary>
public sealed record CustomPercentileRule : IRule
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string Description { get; init; }

    /// <summary>
    /// Percentile to evaluate (e.g., 95, 99, 99.9)
    /// </summary>
    public required double Percentile { get; init; }

    /// <summary>
    /// Maximum acceptable value in milliseconds
    /// </summary>
    public required double MaxValue { get; init; }

    public EvaluationResult Evaluate(Metric metric)
    {
        if (metric == null)
        {
            throw new ArgumentNullException(nameof(metric));
        }

        var timestamp = DateTime.UtcNow;
        var aggregationName = $"P{Percentile:F1}".Replace(".0", ""); // P95, P99, etc.

        // Find the percentile aggregation
        var aggregationResult = metric.AggregatedValues
            .FirstOrDefault(a => a.OperationName.Equals(aggregationName, StringComparison.OrdinalIgnoreCase));

        if (aggregationResult == null)
        {
            var violation = Violation.Create(
                ruleId: Id,
                metricName: metric.MetricType,
                actualValue: double.NaN,
                threshold: MaxValue,
                message: $"{aggregationName} aggregation not found in metric"
            );
            return EvaluationResult.Fail(ImmutableList.Create(violation), timestamp);
        }

        var actualValue = aggregationResult.Value.GetValueIn(LatencyUnit.Milliseconds);

        if (actualValue <= MaxValue)
        {
            return EvaluationResult.Pass(timestamp);
        }

        var failureViolation = Violation.Create(
            ruleId: Id,
            metricName: metric.MetricType,
            actualValue: actualValue,
            threshold: MaxValue,
            message: $"{aggregationName} latency {actualValue:F2}ms exceeds maximum {MaxValue}ms"
        );

        return EvaluationResult.Fail(ImmutableList.Create(failureViolation), timestamp);
    }

    public bool Equals(IRule? other)
    {
        return other is CustomPercentileRule cpr &&
               Id == cpr.Id &&
               Math.Abs(Percentile - cpr.Percentile) < 0.001 &&
               Math.Abs(MaxValue - cpr.MaxValue) < 0.001;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Id, Percentile, MaxValue);
    }
}

public class CustomRuleTests
{
    [Fact]
    public void CustomPercentileRule_P95BelowThreshold_ReturnsPass()
    {
        // Arrange
        var rule = new CustomPercentileRule
        {
            Id = "custom-p95-rule",
            Name = "P95 Custom Check",
            Description = "Custom rule for P95 latency",
            Percentile = 95.0,
            MaxValue = 300.0
        };

        var metric = TestMetricFactory.CreateMetric("P95", 250.0);

        // Act
        var result = rule.Evaluate(metric);

        // Assert
        result.Outcome.Should().Be(Severity.PASS);
        result.Violations.Should().BeEmpty();
    }

    [Fact]
    public void CustomPercentileRule_P95ExceedsThreshold_ReturnsFail()
    {
        // Arrange
        var rule = new CustomPercentileRule
        {
            Id = "custom-p95-fail",
            Name = "P95 Failure Test",
            Description = "Test P95 violation",
            Percentile = 95.0,
            MaxValue = 200.0
        };

        var metric = TestMetricFactory.CreateMetric("P95", 350.0);

        // Act
        var result = rule.Evaluate(metric);

        // Assert
        result.Outcome.Should().Be(Severity.FAIL);
        result.Violations.Should().HaveCount(1);
        result.Violations[0].RuleId.Should().Be("custom-p95-fail");
        result.Violations[0].ActualValue.Should().Be(350.0);
        result.Violations[0].Threshold.Should().Be(200.0);
    }

    [Fact]
    public void CustomPercentileRule_P99Evaluation_WorksCorrectly()
    {
        // Arrange
        var rule = new CustomPercentileRule
        {
            Id = "custom-p99",
            Name = "P99 Custom Rule",
            Description = "Custom P99 check",
            Percentile = 99.0,
            MaxValue = 500.0
        };

        var metric = TestMetricFactory.CreateMetric("P99", 450.0);

        // Act
        var result = rule.Evaluate(metric);

        // Assert
        result.Outcome.Should().Be(Severity.PASS);
    }

    [Fact]
    public void CustomPercentileRule_MissingPercentile_ReturnsFail()
    {
        // Arrange
        var rule = new CustomPercentileRule
        {
            Id = "custom-missing",
            Name = "Missing Percentile Test",
            Description = "Test missing percentile",
            Percentile = 99.9,
            MaxValue = 1000.0
        };

        var metric = TestMetricFactory.CreateMetric("P95", 100.0); // Only has P95, not P99.9

        // Act
        var result = rule.Evaluate(metric);

        // Assert
        result.Outcome.Should().Be(Severity.FAIL);
        result.Violations.Should().HaveCount(1);
        result.Violations[0].Message.Should().Contain("not found");
    }

    [Fact]
    public void CustomPercentileRule_WorksWithEvaluator()
    {
        // Arrange - Demonstrates that custom rules work with standard Evaluator
        var evaluator = new Evaluator();
        var rule = new CustomPercentileRule
        {
            Id = "custom-evaluator-test",
            Name = "Custom Rule with Evaluator",
            Description = "Test custom rule integration",
            Percentile = 95.0,
            MaxValue = 200.0
        };

        var passingMetric = TestMetricFactory.CreateMetric("P95", 150.0);
        var failingMetric = TestMetricFactory.CreateMetric("P95", 300.0);

        // Act
        var passResult = evaluator.Evaluate(passingMetric, rule);
        var failResult = evaluator.Evaluate(failingMetric, rule);

        // Assert
        passResult.Outcome.Should().Be(Severity.PASS);
        failResult.Outcome.Should().Be(Severity.FAIL);
    }

    [Fact]
    public void CustomPercentileRule_Equality_WorksCorrectly()
    {
        // Arrange
        var rule1 = new CustomPercentileRule
        {
            Id = "rule-eq-1",
            Name = "Rule 1",
            Description = "Test rule",
            Percentile = 95.0,
            MaxValue = 200.0
        };

        var rule2 = new CustomPercentileRule
        {
            Id = "rule-eq-1",
            Name = "Rule 1",
            Description = "Test rule",
            Percentile = 95.0,
            MaxValue = 200.0
        };

        var rule3 = new CustomPercentileRule
        {
            Id = "rule-eq-2",
            Name = "Rule 2",
            Description = "Different rule",
            Percentile = 99.0,
            MaxValue = 500.0
        };

        // Act & Assert
        rule1.Equals(rule2).Should().BeTrue();
        rule1.Equals(rule3).Should().BeFalse();
        rule1.GetHashCode().Should().Be(rule2.GetHashCode());
    }

    [Fact]
    public void CustomPercentileRule_DemonstratesStrategyPattern()
    {
        // Arrange - Mix of built-in and custom rules
        var rules = new List<IRule>
        {
            new ThresholdRule
            {
                Id = "builtin-threshold",
                Name = "Built-in Threshold",
                Description = "Standard threshold rule",
                AggregationName = "P95",
                Threshold = 250.0,
                Operator = ComparisonOperator.LessThan
            },
            new CustomPercentileRule
            {
                Id = "custom-percentile",
                Name = "Custom Percentile",
                Description = "Custom percentile rule",
                Percentile = 99.0,
                MaxValue = 500.0
            }
        };

        var metric = TestMetricFactory.CreateMetricWithMultipleAggregations(
            new Dictionary<string, double>
            {
                { "P95", 200.0 },  // Pass threshold rule
                { "P99", 600.0 }   // Fail custom rule
            }
        );

        var evaluator = new Evaluator();

        // Act
        var results = rules.Select(r => evaluator.Evaluate(metric, r)).ToList();

        // Assert - Strategy pattern allows treating all rules uniformly
        results.Should().HaveCount(2);
        results[0].Outcome.Should().Be(Severity.PASS);  // Threshold rule passes
        results[1].Outcome.Should().Be(Severity.FAIL);  // Custom rule fails
    }
}
