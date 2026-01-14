namespace PerformanceEngine.Evaluation.Domain.Tests.Integration;

/// <summary>
/// Tests to verify that rules evaluate metrics from different execution engines identically.
/// Ensures engine-agnostic evaluation - same metric values produce same results regardless of source engine.
/// </summary>
public class CrossEngineTests
{
    [Fact]
    public void ThresholdRule_EvaluatesK6AndJMeterMetricsIdentically()
    {
        // Arrange
        var rule = new ThresholdRule
        {
            Id = "cross-engine-threshold",
            Name = "Cross Engine Threshold",
            Description = "Test cross-engine evaluation",
            AggregationName = "P95",
            Threshold = 200.0,
            Operator = ComparisonOperator.LessThan
        };

        // Create metrics from different engines with same P95 value
        var k6Metric = TestMetricFactory.CreateMetric("P95", 180.0, "K6-Metric");
        var jmeterMetric = TestMetricFactory.CreateMetric("P95", 180.0, "JMeter-Metric");
        var gatlingMetric = TestMetricFactory.CreateMetric("P95", 180.0, "Gatling-Metric");

        // Act
        var k6Result = rule.Evaluate(k6Metric);
        var jmeterResult = rule.Evaluate(jmeterMetric);
        var gatlingResult = rule.Evaluate(gatlingMetric);

        // Assert - All engines with same metric value should produce same evaluation result
        k6Result.Outcome.Should().Be(Severity.PASS);
        jmeterResult.Outcome.Should().Be(Severity.PASS);
        gatlingResult.Outcome.Should().Be(Severity.PASS);

        // All should have same violation count
        k6Result.Violations.Count.Should().Be(jmeterResult.Violations.Count);
        jmeterResult.Violations.Count.Should().Be(gatlingResult.Violations.Count);
    }

    [Fact]
    public void RangeRule_EvaluatesDifferentEnginesIdentically()
    {
        // Arrange
        var rule = new RangeRule
        {
            Id = "cross-engine-range",
            Name = "Cross Engine Range",
            Description = "Test range rule across engines",
            AggregationName = "ErrorRate",
            MinBound = 0.0,
            MaxBound = 5.0
        };

        // Different engines, same error rate (out of range)
        var k6Metric = TestMetricFactory.CreateMetric("ErrorRate", 7.5, "K6-Metric");
        var jmeterMetric = TestMetricFactory.CreateMetric("ErrorRate", 7.5, "JMeter-Metric");
        var gatlingMetric = TestMetricFactory.CreateMetric("ErrorRate", 7.5, "Gatling-Metric");

        // Act
        var k6Result = rule.Evaluate(k6Metric);
        var jmeterResult = rule.Evaluate(jmeterMetric);
        var gatlingResult = rule.Evaluate(gatlingMetric);

        // Assert
        k6Result.Outcome.Should().Be(Severity.FAIL);
        jmeterResult.Outcome.Should().Be(Severity.FAIL);
        gatlingResult.Outcome.Should().Be(Severity.FAIL);

        // All should fail with same violation
        k6Result.Violations.Should().HaveCount(1);
        jmeterResult.Violations.Should().HaveCount(1);
        gatlingResult.Violations.Should().HaveCount(1);
    }

    [Fact]
    public void BatchEvaluation_HandlesMultipleEngines()
    {
        // Arrange
        var evaluator = new Evaluator();
        var rule = new ThresholdRule
        {
            Id = "multi-engine-batch",
            Name = "Multi-Engine Batch",
            Description = "Batch evaluation across engines",
            AggregationName = "P99",
            Threshold = 500.0,
            Operator = ComparisonOperator.LessThan
        };

        // Mix of engines with varying P99 values
        var metrics = new[]
        {
            TestMetricFactory.CreateMetric("P99", 450.0, "K6-API1"),      // Pass
            TestMetricFactory.CreateMetric("P99", 600.0, "JMeter-API2"),  // Fail
            TestMetricFactory.CreateMetric("P99", 480.0, "Gatling-API3"), // Pass
            TestMetricFactory.CreateMetric("P99", 550.0, "K6-API4")       // Fail
        };

        // Act
        var results = evaluator.EvaluateMultiple(metrics, new[] { rule }).ToList();

        // Assert
        results.Should().HaveCount(4);
        
        // Verify outcomes regardless of engine
        results[0].Outcome.Should().Be(Severity.PASS);
        results[1].Outcome.Should().Be(Severity.FAIL);
        results[2].Outcome.Should().Be(Severity.PASS);
        results[3].Outcome.Should().Be(Severity.FAIL);
    }

    [Fact]
    public void CustomRule_WorksAcrossEngines()
    {
        // Arrange
        var rule = new CustomPercentileRule
        {
            Id = "custom-cross-engine",
            Name = "Custom Cross Engine",
            Description = "Custom rule across engines",
            Percentile = 95.0,
            MaxValue = 300.0
        };

        var k6Metric = TestMetricFactory.CreateMetric("P95", 280.0, "K6-Test");
        var jmeterMetric = TestMetricFactory.CreateMetric("P95", 350.0, "JMeter-Test");

        // Act
        var k6Result = rule.Evaluate(k6Metric);
        var jmeterResult = rule.Evaluate(jmeterMetric);

        // Assert
        k6Result.Outcome.Should().Be(Severity.PASS);   // 280 < 300
        jmeterResult.Outcome.Should().Be(Severity.FAIL); // 350 > 300
    }

    [Fact]
    public void EvaluationService_HandlesEngineAgnosticMetrics()
    {
        // Arrange
        var service = new EvaluationService();
        var rules = new List<IRule>
        {
            new ThresholdRule
            {
                Id = "p95-rule",
                Name = "P95 Check",
                Description = "P95 < 200ms",
                AggregationName = "P95",
                Threshold = 200.0,
                Operator = ComparisonOperator.LessThan
            },
            new ThresholdRule
            {
                Id = "p99-rule",
                Name = "P99 Check",
                Description = "P99 < 500ms",
                AggregationName = "P99",
                Threshold = 500.0,
                Operator = ComparisonOperator.LessThan
            }
        };

        // Metrics from different engines
        var metrics = new[]
        {
            TestMetricFactory.CreateMetricWithMultipleAggregations(
                new Dictionary<string, double> { { "P95", 180.0 }, { "P99", 450.0 } },
                "K6-Payment"
            ),
            TestMetricFactory.CreateMetricWithMultipleAggregations(
                new Dictionary<string, double> { { "P95", 250.0 }, { "P99", 600.0 } },
                "JMeter-Search"
            ),
            TestMetricFactory.CreateMetricWithMultipleAggregations(
                new Dictionary<string, double> { { "P95", 190.0 }, { "P99", 480.0 } },
                "Gatling-Checkout"
            )
        };

        // Act
        var results = service.EvaluateBatch(metrics, rules).ToList();

        // Assert
        results.Should().HaveCount(3);
        
        // K6-Payment: Both pass
        results[0].Outcome.Should().Be(Severity.PASS);
        
        // JMeter-Search: Both fail
        results[1].Outcome.Should().Be(Severity.FAIL);
        results[1].Violations.Should().HaveCount(2);
        
        // Gatling-Checkout: Both pass
        results[2].Outcome.Should().Be(Severity.PASS);
    }

    [Fact]
    public void CompositeRule_EvaluatesAcrossEngines()
    {
        // Arrange
        var compositeRule = new CompositeRule
        {
            Id = "composite-cross-engine",
            Name = "Composite Cross Engine",
            Description = "Composite rule test",
            Operator = LogicalOperator.And,
            SubRules = ImmutableList.Create<IRule>(
                new ThresholdRule
                {
                    Id = "sub-p95",
                    Name = "P95 Sub Rule",
                    Description = "P95 check",
                    AggregationName = "P95",
                    Threshold = 200.0,
                    Operator = ComparisonOperator.LessThan
                },
                new ThresholdRule
                {
                    Id = "sub-error",
                    Name = "Error Sub Rule",
                    Description = "Error check",
                    AggregationName = "ErrorRate",
                    Threshold = 1.0,
                    Operator = ComparisonOperator.LessThan
                }
            )
        };

        var k6Metric = TestMetricFactory.CreateMetricWithMultipleAggregations(
            new Dictionary<string, double> { { "P95", 180.0 }, { "ErrorRate", 0.5 } },
            "K6-Test"
        );

        var jmeterMetric = TestMetricFactory.CreateMetricWithMultipleAggregations(
            new Dictionary<string, double> { { "P95", 250.0 }, { "ErrorRate", 2.0 } },
            "JMeter-Test"
        );

        // Act
        var k6Result = compositeRule.Evaluate(k6Metric);
        var jmeterResult = compositeRule.Evaluate(jmeterMetric);

        // Assert
        k6Result.Outcome.Should().Be(Severity.PASS);  // Both sub-rules pass
        jmeterResult.Outcome.Should().Be(Severity.FAIL); // Both sub-rules fail
    }
}
