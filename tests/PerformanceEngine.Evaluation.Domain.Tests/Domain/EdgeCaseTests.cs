namespace PerformanceEngine.Evaluation.Domain.Tests.Domain;

/// <summary>
/// Tests for edge cases, boundary conditions, and exceptional scenarios.
/// </summary>
public class EdgeCaseTests
{
    [Fact]
    public void ThresholdRule_NullMetric_ThrowsArgumentNullException()
    {
        // Arrange
        var rule = new ThresholdRule
        {
            Id = "null-test",
            Name = "Null Test",
            Description = "Test null handling",
            AggregationName = "P95",
            Threshold = 100.0,
            Operator = ComparisonOperator.LessThan
        };

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => rule.Evaluate(null!));
    }

    [Fact]
    public void RangeRule_NullMetric_ThrowsArgumentNullException()
    {
        // Arrange
        var rule = new RangeRule
        {
            Id = "null-range",
            Name = "Null Range Test",
            Description = "Test null handling",
            AggregationName = "Value",
            MinBound = 10.0,
            MaxBound = 20.0
        };

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => rule.Evaluate(null!));
    }

    [Fact]
    public void RangeRule_MinGreaterThanMax_ThrowsInvalidOperationException()
    {
        // Arrange
        var rule = new RangeRule
        {
            Id = "invalid-range",
            Name = "Invalid Range",
            Description = "Min > Max",
            AggregationName = "Value",
            MinBound = 20.0,
            MaxBound = 10.0 // Invalid: min > max
        };

        var metric = TestMetricFactory.CreateMetric("Value", 15.0);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => rule.Evaluate(metric));
    }

    [Fact]
    public void ThresholdRule_ExtremelyLargeValue_HandlesCorrectly()
    {
        // Arrange
        var rule = new ThresholdRule
        {
            Id = "large-value",
            Name = "Large Value Test",
            Description = "Test extreme values",
            AggregationName = "Value",
            Threshold = 1000000.0,
            Operator = ComparisonOperator.LessThan
        };

        var metric = TestMetricFactory.CreateMetric("Value", double.MaxValue);

        // Act
        var result = rule.Evaluate(metric);

        // Assert
        result.Outcome.Should().Be(Severity.FAIL);
        result.Violations.Should().HaveCount(1);
    }

    [Fact]
    public void ThresholdRule_ExtremelySmallValue_HandlesCorrectly()
    {
        // Arrange
        var rule = new ThresholdRule
        {
            Id = "small-value",
            Name = "Small Value Test",
            Description = "Test very small values",
            AggregationName = "Value",
            Threshold = 0.001,
            Operator = ComparisonOperator.GreaterThan
        };

        var metric = TestMetricFactory.CreateMetric("Value", 0.0000001);

        // Act
        var result = rule.Evaluate(metric);

        // Assert
        result.Outcome.Should().Be(Severity.FAIL);
    }

    [Fact]
    public void ThresholdRule_ZeroThreshold_WorksCorrectly()
    {
        // Arrange
        var rule = new ThresholdRule
        {
            Id = "zero-threshold",
            Name = "Zero Threshold",
            Description = "Test zero threshold",
            AggregationName = "Value",
            Threshold = 0.0,
            Operator = ComparisonOperator.GreaterThan
        };

        var positiveMetric = TestMetricFactory.CreateMetric("Value", 10.0);
        var zeroMetric = TestMetricFactory.CreateMetric("Value", 0.0);
        var negativeMetric = TestMetricFactory.CreateMetric("Value", -5.0);

        // Act
        var positiveResult = rule.Evaluate(positiveMetric);
        var zeroResult = rule.Evaluate(zeroMetric);
        var negativeResult = rule.Evaluate(negativeMetric);

        // Assert
        positiveResult.Outcome.Should().Be(Severity.PASS);  // 10 > 0
        zeroResult.Outcome.Should().Be(Severity.FAIL);      // 0 !> 0
        negativeResult.Outcome.Should().Be(Severity.FAIL);  // -5 !> 0
    }

    [Fact]
    public void ThresholdRule_NegativeValues_HandlesCorrectly()
    {
        // Arrange
        var rule = new ThresholdRule
        {
            Id = "negative-test",
            Name = "Negative Test",
            Description = "Test negative values",
            AggregationName = "Value",
            Threshold = -10.0,
            Operator = ComparisonOperator.GreaterThan
        };

        var metric = TestMetricFactory.CreateMetric("Value", -5.0);

        // Act
        var result = rule.Evaluate(metric);

        // Assert
        result.Outcome.Should().Be(Severity.PASS); // -5 > -10
    }

    [Fact]
    public void RangeRule_BoundaryValues_ExclusiveBounds()
    {
        // Arrange
        var rule = new RangeRule
        {
            Id = "boundary-test",
            Name = "Boundary Test",
            Description = "Test exclusive bounds",
            AggregationName = "Value",
            MinBound = 10.0,
            MaxBound = 20.0
        };

        var minMetric = TestMetricFactory.CreateMetric("Value", 10.0);
        var maxMetric = TestMetricFactory.CreateMetric("Value", 20.0);
        var insideMetric = TestMetricFactory.CreateMetric("Value", 15.0);

        // Act
        var minResult = rule.Evaluate(minMetric);
        var maxResult = rule.Evaluate(maxMetric);
        var insideResult = rule.Evaluate(insideMetric);

        // Assert - Bounds are exclusive
        minResult.Outcome.Should().Be(Severity.FAIL);  // 10 == MinBound
        maxResult.Outcome.Should().Be(Severity.FAIL);  // 20 == MaxBound
        insideResult.Outcome.Should().Be(Severity.PASS); // 10 < 15 < 20
    }

    [Fact]
    public void Evaluator_EmptyRuleList_ReturnsPassResult()
    {
        // Arrange
        var evaluator = new Evaluator();
        var metric = TestMetricFactory.CreateMetric("P95", 100.0);

        // Act
        var results = evaluator.EvaluateMultiple(new[] { metric }, Array.Empty<IRule>()).ToList();

        // Assert
        results.Should().HaveCount(1);
        results[0].Outcome.Should().Be(Severity.PASS);
        results[0].Violations.Should().BeEmpty();
    }

    [Fact]
    public void Evaluator_EmptyMetricList_ReturnsEmptyResults()
    {
        // Arrange
        var evaluator = new Evaluator();
        var rule = new ThresholdRule
        {
            Id = "empty-test",
            Name = "Empty Test",
            Description = "Test empty metrics",
            AggregationName = "P95",
            Threshold = 100.0,
            Operator = ComparisonOperator.LessThan
        };

        // Act
        var results = evaluator.EvaluateMultiple(Array.Empty<Metric>(), new[] { rule });

        // Assert
        results.Should().BeEmpty();
    }

    [Fact]
    public void ThresholdRule_FloatingPointPrecision_UsesEpsilon()
    {
        // Arrange - Test that equality uses epsilon tolerance
        var rule = new ThresholdRule
        {
            Id = "epsilon-test",
            Name = "Epsilon Test",
            Description = "Test floating point equality",
            AggregationName = "Value",
            Threshold = 100.0,
            Operator = ComparisonOperator.Equal
        };

        var exactMetric = TestMetricFactory.CreateMetric("Value", 100.0);
        var slightlyHigherMetric = TestMetricFactory.CreateMetric("Value", 100.0005); // Within epsilon
        var tooHighMetric = TestMetricFactory.CreateMetric("Value", 100.002); // Beyond epsilon

        // Act
        var exactResult = rule.Evaluate(exactMetric);
        var slightlyHigherResult = rule.Evaluate(slightlyHigherMetric);
        var tooHighResult = rule.Evaluate(tooHighMetric);

        // Assert - Epsilon = 0.001
        exactResult.Outcome.Should().Be(Severity.PASS);
        slightlyHigherResult.Outcome.Should().Be(Severity.PASS); // Within epsilon
        tooHighResult.Outcome.Should().Be(Severity.FAIL); // Beyond epsilon
    }

    [Fact]
    public void CompositeRule_EmptySubRules_ReturnsPass()
    {
        // Arrange
        var compositeRule = new CompositeRule
        {
            Id = "empty-composite",
            Name = "Empty Composite",
            Description = "Test empty sub-rules",
            Operator = LogicalOperator.And,
            SubRules = ImmutableList<IRule>.Empty
        };

        var metric = TestMetricFactory.CreateMetric("P95", 100.0);

        // Act
        var result = compositeRule.Evaluate(metric);

        // Assert - No sub-rules means no violations
        result.Outcome.Should().Be(Severity.PASS);
        result.Violations.Should().BeEmpty();
    }

    [Fact]
    public void CustomRule_MissingPercentile_HandlesGracefully()
    {
        // Arrange
        var rule = new CustomPercentileRule
        {
            Id = "missing-percentile",
            Name = "Missing Percentile",
            Description = "Test missing percentile aggregation",
            Percentile = 99.9,
            MaxValue = 1000.0
        };

        var metric = TestMetricFactory.CreateMetric("P95", 100.0); // Only has P95

        // Act
        var result = rule.Evaluate(metric);

        // Assert
        result.Outcome.Should().Be(Severity.FAIL);
        result.Violations.Should().HaveCount(1);
        result.Violations[0].Message.Should().Contain("not found");
        result.Violations[0].ActualValue.Should().Be(double.NaN);
    }

    [Fact]
    public void EvaluationService_NullInputs_ReturnsGracefully()
    {
        // Arrange
        var service = new EvaluationService();
        var rule = new ThresholdRule
        {
            Id = "null-service-test",
            Name = "Null Service Test",
            Description = "Test null inputs",
            AggregationName = "P95",
            Threshold = 100.0,
            Operator = ComparisonOperator.LessThan
        };

        var metric = TestMetricFactory.CreateMetric("P95", 50.0);

        // Act
        var nullMetricResult = service.Evaluate(null!, rule);
        var nullRuleResult = service.Evaluate(metric, null!);

        // Assert - Should return error results, not throw
        nullMetricResult.Outcome.Should().Be(Severity.FAIL);
        nullMetricResult.Violations.Should().HaveCount(1);
        
        nullRuleResult.Outcome.Should().Be(Severity.FAIL);
        nullRuleResult.Violations.Should().HaveCount(1);
    }

    [Fact]
    public void BatchEvaluation_NullCollections_HandlesGracefully()
    {
        // Arrange
        var service = new EvaluationService();
        var rules = new List<IRule>
        {
            new ThresholdRule
            {
                Id = "test",
                Name = "Test",
                Description = "Test",
                AggregationName = "P95",
                Threshold = 100.0,
                Operator = ComparisonOperator.LessThan
            }
        };

        var metrics = new[] { TestMetricFactory.CreateMetric("P95", 50.0) };

        // Act
        var nullMetricsResult = service.EvaluateBatch(null!, rules);
        var nullRulesResult = service.EvaluateBatch(metrics, null!);

        // Assert
        nullMetricsResult.Should().BeEmpty();
        nullRulesResult.Should().BeEmpty();
    }

    [Fact]
    public void RangeRule_IdenticalBounds_ThrowsException()
    {
        // Arrange
        var rule = new RangeRule
        {
            Id = "identical-bounds",
            Name = "Identical Bounds",
            Description = "Min == Max",
            AggregationName = "Value",
            MinBound = 10.0,
            MaxBound = 10.0 // Invalid: min == max
        };

        var metric = TestMetricFactory.CreateMetric("Value", 10.0);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => rule.Evaluate(metric));
    }

    [Fact]
    public void ThresholdRule_InfinityValues_HandlesCorrectly()
    {
        // Arrange
        var rule = new ThresholdRule
        {
            Id = "infinity-test",
            Name = "Infinity Test",
            Description = "Test infinity values",
            AggregationName = "Value",
            Threshold = 1000.0,
            Operator = ComparisonOperator.LessThan
        };

        var infiniteMetric = TestMetricFactory.CreateMetric("Value", double.PositiveInfinity);

        // Act
        var result = rule.Evaluate(infiniteMetric);

        // Assert
        result.Outcome.Should().Be(Severity.FAIL); // Infinity is not < 1000
    }

    [Fact]
    public void CompositeRule_NestedComposition_WorksCorrectly()
    {
        // Arrange - Nested composite rules (AND of ORs)
        var innerComposite1 = new CompositeRule
        {
            Id = "inner1",
            Name = "Inner 1",
            Description = "Inner OR",
            Operator = LogicalOperator.Or,
            SubRules = ImmutableList.Create<IRule>(
                new ThresholdRule
                {
                    Id = "sub1",
                    Name = "Sub 1",
                    Description = "P95 < 200",
                    AggregationName = "P95",
                    Threshold = 200.0,
                    Operator = ComparisonOperator.LessThan
                },
                new ThresholdRule
                {
                    Id = "sub2",
                    Name = "Sub 2",
                    Description = "P95 > 300",
                    AggregationName = "P95",
                    Threshold = 300.0,
                    Operator = ComparisonOperator.GreaterThan
                }
            )
        };

        var metric = TestMetricFactory.CreateMetric("P95", 250.0);

        // Act
        var result = innerComposite1.Evaluate(metric);

        // Assert - 250 fails both P95 < 200 AND P95 > 300, so OR fails
        result.Outcome.Should().Be(Severity.FAIL);
    }
}
