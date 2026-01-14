namespace PerformanceEngine.Evaluation.Domain.Tests.Integration;

/// <summary>
/// Integration tests with the Metrics Domain.
/// Verifies that evaluation works correctly with real Metric objects from the Metrics Domain.
/// </summary>
public class MetricsDomainIntegrationTests
{
    [Fact]
    public void ThresholdRule_EvaluatesRealMetricObject()
    {
        // Arrange
        var rule = new ThresholdRule
        {
            Id = "real-metric-threshold",
            Name = "Real Metric Threshold",
            Description = "Test with real Metric object",
            AggregationName = "P95",
            Threshold = 200.0,
            Operator = ComparisonOperator.LessThan
        };

        // Use real Metric object from Metrics Domain
        var metric = TestMetricFactory.CreateMetric("P95", 180.0);

        // Act
        var result = rule.Evaluate(metric);

        // Assert
        result.Should().NotBeNull();
        result.Outcome.Should().Be(Severity.PASS);
        result.Violations.Should().BeEmpty();
    }

    [Fact]
    public void RuleEvaluation_AccessesMetricAggregatedValues()
    {
        // Arrange
        var rule = new ThresholdRule
        {
            Id = "aggregation-access",
            Name = "Aggregation Access Test",
            Description = "Test aggregation value access",
            AggregationName = "P99",
            Threshold = 500.0,
            Operator = ComparisonOperator.LessThan
        };

        var metric = TestMetricFactory.CreateMetric("P99", 450.0);

        // Act
        var result = rule.Evaluate(metric);

        // Assert - Verify we can access AggregatedValues from real Metric
        metric.AggregatedValues.Should().NotBeNull();
        metric.AggregatedValues.Should().NotBeEmpty();
        
        var p99Aggregation = metric.AggregatedValues.FirstOrDefault(a => 
            a.OperationName.Equals("P99", StringComparison.OrdinalIgnoreCase));
        
        p99Aggregation.Should().NotBeNull();
        p99Aggregation!.Value.GetValueIn(LatencyUnit.Milliseconds).Should().Be(450.0);
    }

    [Fact]
    public void Evaluator_WorksWithMetricMetricType()
    {
        // Arrange
        var evaluator = new Evaluator();
        var rule = new ThresholdRule
        {
            Id = "metric-type-test",
            Name = "Metric Type Test",
            Description = "Test MetricType access",
            AggregationName = "Average",
            Threshold = 100.0,
            Operator = ComparisonOperator.LessThan
        };

        var metric = TestMetricFactory.CreateMetric("Average", 80.0, "TestMetricType");

        // Act
        var result = evaluator.Evaluate(metric, rule);

        // Assert
        result.Should().NotBeNull();
        metric.MetricType.Should().Be("TestMetricType");
    }

    [Fact]
    public void MultipleAggregations_FromMetricsDomain_EvaluateCorrectly()
    {
        // Arrange
        var p95Rule = new ThresholdRule
        {
            Id = "p95",
            Name = "P95 Rule",
            Description = "P95 check",
            AggregationName = "P95",
            Threshold = 200.0,
            Operator = ComparisonOperator.LessThan
        };

        var p99Rule = new ThresholdRule
        {
            Id = "p99",
            Name = "P99 Rule",
            Description = "P99 check",
            AggregationName = "P99",
            Threshold = 500.0,
            Operator = ComparisonOperator.LessThan
        };

        // Metric with multiple aggregations
        var metric = TestMetricFactory.CreateMetricWithMultipleAggregations(
            new Dictionary<string, double>
            {
                { "P95", 180.0 },
                { "P99", 450.0 },
                { "Average", 100.0 }
            }
        );

        var evaluator = new Evaluator();

        // Act
        var p95Result = evaluator.Evaluate(metric, p95Rule);
        var p99Result = evaluator.Evaluate(metric, p99Rule);

        // Assert
        p95Result.Outcome.Should().Be(Severity.PASS);
        p99Result.Outcome.Should().Be(Severity.PASS);
    }

    [Fact]
    public void MissingAggregation_HandledGracefully()
    {
        // Arrange
        var rule = new ThresholdRule
        {
            Id = "missing-agg",
            Name = "Missing Aggregation",
            Description = "Test missing aggregation",
            AggregationName = "P99",
            Threshold = 500.0,
            Operator = ComparisonOperator.LessThan
        };

        // Metric only has P95, not P99
        var metric = TestMetricFactory.CreateMetric("P95", 180.0);

        // Act
        var result = rule.Evaluate(metric);

        // Assert
        result.Outcome.Should().Be(Severity.FAIL);
        result.Violations.Should().HaveCount(1);
        result.Violations[0].Message.Should().Contain("not found");
    }

    [Fact]
    public void LatencyUnitConversion_WorksCorrectly()
    {
        // Arrange
        var rule = new ThresholdRule
        {
            Id = "latency-conversion",
            Name = "Latency Conversion Test",
            Description = "Test latency unit conversion",
            AggregationName = "P95",
            Threshold = 200.0, // milliseconds
            Operator = ComparisonOperator.LessThan
        };

        // Create metric with latency in milliseconds
        var metric = TestMetricFactory.CreateMetric("P95", 180.0);

        // Act
        var result = rule.Evaluate(metric);

        // Assert - Verify conversion to milliseconds works
        result.Outcome.Should().Be(Severity.PASS);
        
        var aggregation = metric.AggregatedValues.First();
        var valueInMs = aggregation.Value.GetValueIn(LatencyUnit.Milliseconds);
        valueInMs.Should().Be(180.0);
    }

    [Fact]
    public void RealWorldScenario_FullIntegration()
    {
        // Arrange - Simulate real-world scenario with multiple metrics and rules
        var service = new EvaluationService();

        var metrics = new[]
        {
            TestMetricFactory.CreateMetricWithMultipleAggregations(
                new Dictionary<string, double>
                {
                    { "P50", 100.0 },
                    { "P95", 180.0 },
                    { "P99", 450.0 },
                    { "Average", 120.0 },
                    { "Max", 800.0 }
                },
                "API-GetUser"
            ),
            TestMetricFactory.CreateMetricWithMultipleAggregations(
                new Dictionary<string, double>
                {
                    { "P50", 150.0 },
                    { "P95", 300.0 },
                    { "P99", 700.0 },
                    { "Average", 200.0 },
                    { "Max", 1200.0 }
                },
                "API-CreateOrder"
            )
        };

        var rules = new List<IRule>
        {
            new ThresholdRule
            {
                Id = "p95-sla",
                Name = "P95 SLA",
                Description = "P95 latency SLA",
                AggregationName = "P95",
                Threshold = 200.0,
                Operator = ComparisonOperator.LessThan
            },
            new ThresholdRule
            {
                Id = "p99-sla",
                Name = "P99 SLA",
                Description = "P99 latency SLA",
                AggregationName = "P99",
                Threshold = 500.0,
                Operator = ComparisonOperator.LessThan
            },
            new ThresholdRule
            {
                Id = "avg-target",
                Name = "Average Target",
                Description = "Average latency target",
                AggregationName = "Average",
                Threshold = 150.0,
                Operator = ComparisonOperator.LessThan
            }
        };

        // Act
        var results = service.EvaluateBatch(metrics, rules).ToList();

        // Assert
        results.Should().HaveCount(2);

        // API-GetUser: Should pass all checks (P95=180, P99=450, Avg=120)
        var getUserResult = results[0];
        getUserResult.Outcome.Should().Be(Severity.PASS);

        // API-CreateOrder: Should fail some checks (P95=300, P99=700, Avg=200)
        var createOrderResult = results[1];
        createOrderResult.Outcome.Should().Be(Severity.FAIL);
        createOrderResult.Violations.Should().HaveCountGreaterThan(0);
    }

    [Fact]
    public void MetricsDomain_Metric_IsUsable()
    {
        // Arrange
        var metric = TestMetricFactory.CreateMetric("P95", 100.0);

        // Assert
        metric.Should().NotBeNull();
        metric.MetricType.Should().Be("TestMetric");
        metric.AggregatedValues.Should().NotBeNull();
    }
}
