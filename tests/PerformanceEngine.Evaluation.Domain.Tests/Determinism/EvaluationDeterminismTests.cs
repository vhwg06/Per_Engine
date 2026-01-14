namespace PerformanceEngine.Evaluation.Domain.Tests.Determinism;

/// <summary>
/// Tests to verify deterministic behavior of the Evaluation Domain.
/// Critical for production use: identical inputs must always produce identical outputs.
/// </summary>
public class EvaluationDeterminismTests : DeterminismTestBase
{
    [Fact]
    public void ThresholdRule_Evaluation_IsDeterministic_1000Runs()
    {
        // Arrange
        var rule = new ThresholdRule
        {
            Id = "determinism-threshold",
            Name = "Determinism Test",
            Description = "Test deterministic evaluation",
            AggregationName = "P95",
            Threshold = 200.0,
            Operator = ComparisonOperator.LessThan
        };

        var metric = TestMetricFactory.CreateMetric("P95", 250.0);

        // Act & Assert - Run 1000 times, all results must be identical
        var isDeterministic = VerifyDeterminism(() =>
        {
            var result = rule.Evaluate(metric);
            return new
            {
                result.Outcome,
                ViolationCount = result.Violations.Count,
                FirstViolationValue = result.Violations.FirstOrDefault()?.ActualValue
            };
        }, iterations: 1000);

        // Assert
        isDeterministic.Should().BeTrue("ThresholdRule evaluation must be deterministic");
    }

    [Fact]
    public void RangeRule_Evaluation_IsDeterministic_1000Runs()
    {
        // Arrange
        var rule = new RangeRule
        {
            Id = "determinism-range",
            Name = "Range Determinism Test",
            Description = "Test deterministic range evaluation",
            AggregationName = "ErrorRate",
            MinBound = 1.0,
            MaxBound = 5.0
        };

        var metric = TestMetricFactory.CreateMetric("ErrorRate", 7.5);

        // Act & Assert
        var isDeterministic = VerifyDeterminism(() =>
        {
            var result = rule.Evaluate(metric);
            return new
            {
                result.Outcome,
                ViolationCount = result.Violations.Count
            };
        }, iterations: 1000);

        // Assert
        isDeterministic.Should().BeTrue("RangeRule evaluation must be deterministic");
    }

    [Fact]
    public void Evaluator_SingleEvaluation_IsDeterministic()
    {
        // Arrange
        var evaluator = new Evaluator();
        var rule = new ThresholdRule
        {
            Id = "evaluator-determinism",
            Name = "Evaluator Test",
            Description = "Test evaluator determinism",
            AggregationName = "P99",
            Threshold = 500.0,
            Operator = ComparisonOperator.LessThan
        };

        var metric = TestMetricFactory.CreateMetric("P99", 600.0);

        // Act & Assert
        var isDeterministic = VerifyDeterminism(() =>
        {
            var result = evaluator.Evaluate(metric, rule);
            return new
            {
                result.Outcome,
                ViolationCount = result.Violations.Count,
                FirstViolationId = result.Violations.FirstOrDefault()?.RuleId
            };
        }, iterations: 1000);

        // Assert
        isDeterministic.Should().BeTrue("Evaluator.Evaluate() must be deterministic");
    }

    [Fact]
    public void Evaluator_BatchEvaluation_IsDeterministic()
    {
        // Arrange
        var evaluator = new Evaluator();
        
        var rules = new List<IRule>
        {
            new ThresholdRule
            {
                Id = "batch-rule-1",
                Name = "Batch Rule 1",
                Description = "First batch rule",
                AggregationName = "P95",
                Threshold = 200.0,
                Operator = ComparisonOperator.LessThan
            },
            new RangeRule
            {
                Id = "batch-rule-2",
                Name = "Batch Rule 2",
                Description = "Second batch rule",
                AggregationName = "ErrorRate",
                MinBound = 0.0,
                MaxBound = 5.0
            }
        };

        var metrics = new[]
        {
            TestMetricFactory.CreateMetricWithMultipleAggregations(
                new Dictionary<string, double>
                {
                    { "P95", 250.0 },
                    { "ErrorRate", 2.0 }
                },
                "Metric-A"
            ),
            TestMetricFactory.CreateMetricWithMultipleAggregations(
                new Dictionary<string, double>
                {
                    { "P95", 150.0 },
                    { "ErrorRate", 1.0 }
                },
                "Metric-B"
            )
        };

        // Act & Assert
        var isDeterministic = VerifyDeterminism(() =>
        {
            var results = evaluator.EvaluateMultiple(metrics, rules).ToList();
            return new
            {
                ResultCount = results.Count,
                Outcomes = results.Select(r => r.Outcome).ToList(),
                TotalViolations = results.Sum(r => r.Violations.Count)
            };
        }, iterations: 1000);

        // Assert
        isDeterministic.Should().BeTrue("Batch evaluation must be deterministic");
    }

    [Fact]
    public void Evaluator_BatchEvaluation_IsOrderIndependent()
    {
        // Arrange
        var evaluator = new Evaluator();
        var rule = new ThresholdRule
        {
            Id = "order-test",
            Name = "Order Independence Test",
            Description = "Test order independence",
            AggregationName = "P95",
            Threshold = 200.0,
            Operator = ComparisonOperator.LessThan
        };

        var metrics = new[]
        {
            TestMetricFactory.CreateMetric("P95", 150.0, "Metric-C"),
            TestMetricFactory.CreateMetric("P95", 250.0, "Metric-A"),
            TestMetricFactory.CreateMetric("P95", 180.0, "Metric-B"),
            TestMetricFactory.CreateMetric("P95", 300.0, "Metric-D")
        };

        // Act & Assert - Different input orders should produce same result
        var isOrderIndependent = VerifyOrderIndependence(
            operation: (shuffledMetrics) =>
            {
                var results = evaluator.EvaluateMultiple(shuffledMetrics, new[] { rule }).ToList();
                // Results should be ordered by MetricType regardless of input order
                return new
                {
                    ResultCount = results.Count,
                    Outcomes = results.Select(r => r.Outcome).ToList(),
                    ViolationCounts = results.Select(r => r.Violations.Count).ToList()
                };
            },
            inputs: metrics,
            permutations: 20
        );

        // Assert
        isOrderIndependent.Should().BeTrue("Batch evaluation results must be independent of input order");
    }

    [Fact]
    public void EvaluationService_IsDeterministic()
    {
        // Arrange
        var service = new EvaluationService();
        var rule = new ThresholdRule
        {
            Id = "service-determinism",
            Name = "Service Test",
            Description = "Test service determinism",
            AggregationName = "P95",
            Threshold = 100.0,
            Operator = ComparisonOperator.LessThan
        };

        var metric = TestMetricFactory.CreateMetric("P95", 150.0);

        // Act & Assert
        var isDeterministic = VerifyDeterminism(() =>
        {
            var result = service.Evaluate(metric, rule);
            return new
            {
                result.Outcome,
                ViolationCount = result.Violations.Count
            };
        }, iterations: 1000);

        // Assert
        isDeterministic.Should().BeTrue("EvaluationService must be deterministic");
    }

    [Fact]
    public void CustomRule_IsDeterministic()
    {
        // Arrange
        var rule = new CustomPercentileRule
        {
            Id = "custom-determinism",
            Name = "Custom Determinism Test",
            Description = "Test custom rule determinism",
            Percentile = 95.0,
            MaxValue = 200.0
        };

        var metric = TestMetricFactory.CreateMetric("P95", 250.0);

        // Act & Assert
        var isDeterministic = VerifyDeterminism(() =>
        {
            var result = rule.Evaluate(metric);
            return new
            {
                result.Outcome,
                ViolationCount = result.Violations.Count
            };
        }, iterations: 1000);

        // Assert
        isDeterministic.Should().BeTrue("Custom rules must be deterministic");
    }

    [Fact]
    public void CompositeRule_IsDeterministic()
    {
        // Arrange
        var compositeRule = new CompositeRule
        {
            Id = "composite-determinism",
            Name = "Composite Determinism Test",
            Description = "Test composite rule determinism",
            Operator = LogicalOperator.And,
            SubRules = ImmutableList.Create<IRule>(
                new ThresholdRule
                {
                    Id = "sub1",
                    Name = "Sub Rule 1",
                    Description = "First sub rule",
                    AggregationName = "P95",
                    Threshold = 200.0,
                    Operator = ComparisonOperator.LessThan
                },
                new ThresholdRule
                {
                    Id = "sub2",
                    Name = "Sub Rule 2",
                    Description = "Second sub rule",
                    AggregationName = "P99",
                    Threshold = 500.0,
                    Operator = ComparisonOperator.LessThan
                }
            )
        };

        var metric = TestMetricFactory.CreateMetricWithMultipleAggregations(
            new Dictionary<string, double>
            {
                { "P95", 250.0 },
                { "P99", 600.0 }
            }
        );

        // Act & Assert
        var isDeterministic = VerifyDeterminism(() =>
        {
            var result = compositeRule.Evaluate(metric);
            return new
            {
                result.Outcome,
                ViolationCount = result.Violations.Count
            };
        }, iterations: 1000);

        // Assert
        isDeterministic.Should().BeTrue("Composite rules must be deterministic");
    }

    [Fact]
    public void FloatingPointComparison_IsDeterministic()
    {
        // Arrange - Test epsilon-based equality
        var rule = new ThresholdRule
        {
            Id = "floating-point",
            Name = "Floating Point Test",
            Description = "Test floating point determinism",
            AggregationName = "Value",
            Threshold = 100.0,
            Operator = ComparisonOperator.Equal
        };

        var metric = TestMetricFactory.CreateMetric("Value", 100.0);

        // Act & Assert
        var isDeterministic = VerifyDeterminism(() =>
        {
            var result = rule.Evaluate(metric);
            return new { result.Outcome };
        }, iterations: 1000);

        // Assert
        isDeterministic.Should().BeTrue("Floating point comparisons must be deterministic");
    }

    [Fact]
    public void FullPipeline_EndToEnd_IsDeterministic()
    {
        // Arrange - Complete evaluation pipeline
        var service = new EvaluationService();
        
        var metrics = new[]
        {
            TestMetricFactory.CreateMetricWithMultipleAggregations(
                new Dictionary<string, double>
                {
                    { "P95", 180.0 },
                    { "P99", 450.0 },
                    { "ErrorRate", 2.0 }
                },
                "API-Payment"
            ),
            TestMetricFactory.CreateMetricWithMultipleAggregations(
                new Dictionary<string, double>
                {
                    { "P95", 250.0 },
                    { "P99", 600.0 },
                    { "ErrorRate", 5.0 }
                },
                "API-Search"
            )
        };

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
            },
            new RangeRule
            {
                Id = "error-range",
                Name = "Error Rate Range",
                Description = "Error rate 0-5%",
                AggregationName = "ErrorRate",
                MinBound = 0.0,
                MaxBound = 5.0
            }
        };

        // Act & Assert
        var isDeterministic = VerifyDeterminism(() =>
        {
            var results = service.EvaluateBatch(metrics, rules).ToList();
            return new
            {
                ResultCount = results.Count,
                Outcomes = results.Select(r => r.Outcome).ToList(),
                TotalViolations = results.Sum(r => r.Violations.Count),
                ViolationRuleIds = results.SelectMany(r => r.Violations).Select(v => v.RuleId).OrderBy(id => id).ToList()
            };
        }, iterations: 1000);

        // Assert
        isDeterministic.Should().BeTrue("Full evaluation pipeline must be deterministic");
    }
}
