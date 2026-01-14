namespace PerformanceEngine.Evaluation.Domain.Tests.Domain.Rules;

public class ThresholdRuleTests
{
    [Theory]
    [InlineData(ComparisonOperator.LessThan, 150, 200, true)]    // 150 < 200 = pass
    [InlineData(ComparisonOperator.LessThan, 200, 200, false)]   // 200 < 200 = fail
    [InlineData(ComparisonOperator.LessThan, 250, 200, false)]   // 250 < 200 = fail
    [InlineData(ComparisonOperator.LessThanOrEqual, 150, 200, true)]
    [InlineData(ComparisonOperator.LessThanOrEqual, 200, 200, true)]
    [InlineData(ComparisonOperator.LessThanOrEqual, 250, 200, false)]
    [InlineData(ComparisonOperator.GreaterThan, 250, 200, true)]
    [InlineData(ComparisonOperator.GreaterThan, 200, 200, false)]
    [InlineData(ComparisonOperator.GreaterThan, 150, 200, false)]
    [InlineData(ComparisonOperator.GreaterThanOrEqual, 250, 200, true)]
    [InlineData(ComparisonOperator.GreaterThanOrEqual, 200, 200, true)]
    [InlineData(ComparisonOperator.GreaterThanOrEqual, 150, 200, false)]
    [InlineData(ComparisonOperator.Equal, 200, 200, true)]
    [InlineData(ComparisonOperator.Equal, 200.0005, 200, true)]  // Within epsilon
    [InlineData(ComparisonOperator.Equal, 201, 200, false)]
    [InlineData(ComparisonOperator.NotEqual, 201, 200, true)]
    [InlineData(ComparisonOperator.NotEqual, 200, 200, false)]
    public void Evaluate_Should_Apply_Operator_Correctly(
        ComparisonOperator op,
        double actualValue,
        double threshold,
        bool shouldPass)
    {
        // Arrange
        var metric = TestMetricFactory.CreateMetricWithAggregations(
            "latency",
            ("p95", actualValue)
        );

        var rule = new ThresholdRule
        {
            Id = "RULE-001",
            Name = "P95 Latency Rule",
            Description = "P95 latency threshold",
            AggregationName = "p95",
            Threshold = threshold,
            Operator = op
        };

        // Act
        var result = rule.Evaluate(metric);

        // Assert
        if (shouldPass)
        {
            result.IsPassing.Should().BeTrue();
            result.Violations.Should().BeEmpty();
        }
        else
        {
            result.IsFailing.Should().BeTrue();
            result.Violations.Should().HaveCount(1);
            result.Violations[0].RuleId.Should().Be("RULE-001");
            result.Violations[0].ActualValue.Should().BeApproximately(actualValue, 0.01);
        }
    }

    [Fact]
    public void Evaluate_Should_Fail_When_Aggregation_Not_Found()
    {
        // Arrange
        var metric = TestMetricFactory.CreateMetricWithAggregations(
            "latency",
            ("average", 100.0)  // Only has "average", not "p95"
        );

        var rule = new ThresholdRule
        {
            Id = "RULE-001",
            Name = "P95 Rule",
            Description = "Test",
            AggregationName = "p95",
            Threshold = 200.0,
            Operator = ComparisonOperator.LessThan
        };

        // Act
        var result = rule.Evaluate(metric);

        // Assert
        result.IsFailing.Should().BeTrue();
        result.Violations.Should().HaveCount(1);
        result.Violations[0].Message.Should().Contain("not found");
    }

    [Fact]
    public void Evaluate_Should_Throw_When_Metric_Null()
    {
        // Arrange
        var rule = new ThresholdRule
        {
            Id = "RULE-001",
            Name = "Test",
            Description = "Test",
            AggregationName = "p95",
            Threshold = 200.0,
            Operator = ComparisonOperator.LessThan
        };

        // Act
        var act = () => rule.Evaluate(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Equals_Should_Compare_By_Configuration()
    {
        // Arrange
        var rule1 = new ThresholdRule
        {
            Id = "RULE-001",
            Name = "Test",
            Description = "Test",
            AggregationName = "p95",
            Threshold = 200.0,
            Operator = ComparisonOperator.LessThan
        };

        var rule2 = new ThresholdRule
        {
            Id = "RULE-001",
            Name = "Test",
            Description = "Test",
            AggregationName = "p95",
            Threshold = 200.0,
            Operator = ComparisonOperator.LessThan
        };

        var rule3 = new ThresholdRule
        {
            Id = "RULE-002",
            Name = "Test",
            Description = "Test",
            AggregationName = "p95",
            Threshold = 200.0,
            Operator = ComparisonOperator.LessThan
        };

        // Act & Assert
        rule1.Equals(rule2 as IRule).Should().BeTrue();
        rule1.Equals(rule3 as IRule).Should().BeFalse();
    }

    [Fact]
    public void Evaluate_Should_Be_Deterministic()
    {
        // Arrange
        var metric = TestMetricFactory.CreateMetricWithAggregations(
            "latency",
            ("p95", 250.0)
        );

        var rule = new ThresholdRule
        {
            Id = "RULE-001",
            Name = "Test",
            Description = "Test",
            AggregationName = "p95",
            Threshold = 200.0,
            Operator = ComparisonOperator.LessThan
        };

        // Act - Run multiple times
        var result1 = rule.Evaluate(metric);
        var result2 = rule.Evaluate(metric);
        var result3 = rule.Evaluate(metric);

        // Assert - Same outcomes (ignoring timestamp)
        result1.Outcome.Should().Be(result2.Outcome).And.Be(result3.Outcome);
        result1.Violations.Count.Should().Be(result2.Violations.Count).And.Be(result3.Violations.Count);
    }
}
