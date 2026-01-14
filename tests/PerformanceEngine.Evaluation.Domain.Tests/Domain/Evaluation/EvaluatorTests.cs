namespace PerformanceEngine.Evaluation.Domain.Tests.Domain.Evaluation;

public class EvaluatorTests
{
    [Fact]
    public void Evaluate_PassingRule_ReturnsPassResult()
    {
        // Arrange
        var evaluator = new Evaluator();
        var rule = new ThresholdRule
        {
            Id = "p95-threshold",
            Name = "P95 Latency Check",
            Description = "P95 should be less than 200ms",
            AggregationName = "P95",
            Threshold = 200.0,
            Operator = ComparisonOperator.LessThan,
        };

        var metric = TestMetricFactory.CreateMetric(
            aggregationName: "P95",
            aggregationValue: 150.0 // Passes: 150 < 200
        );

        // Act
        var result = evaluator.Evaluate(metric, rule);

        // Assert
        result.Outcome.Should().Be(Severity.PASS);
        result.Violations.Should().BeEmpty();
    }

    [Fact]
    public void Evaluate_FailingRule_ReturnsFailResult()
    {
        // Arrange
        var evaluator = new Evaluator();
        var rule = new ThresholdRule
        {
            Id = "error-threshold",
            Name = "Error Rate Check",
            Description = "Error rate should be less than 1%",
            AggregationName = "ErrorRate",
            Threshold = 1.0,
            Operator = ComparisonOperator.LessThan,
        };

        var metric = TestMetricFactory.CreateMetric(
            aggregationName: "ErrorRate",
            aggregationValue: 2.5 // Fails: 2.5 >= 1.0
        );

        // Act
        var result = evaluator.Evaluate(metric, rule);

        // Assert
        result.Outcome.Should().Be(Severity.FAIL);
        result.Violations.Should().HaveCount(1);
        result.Violations[0].RuleId.Should().Be("error-threshold");
        result.Violations[0].ActualValue.Should().Be(2.5);
    }

    [Fact]
    public void Evaluate_WarningRule_ReturnsWarnResult()
    {
        // Arrange
        var evaluator = new Evaluator();
        var rule = new ThresholdRule
        {
            Id = "p99-warning",
            Name = "P99 Warning",
            Description = "P99 should ideally be less than 500ms",
            AggregationName = "P99",
            Threshold = 500.0,
            Operator = ComparisonOperator.LessThan,
        };

        var metric = TestMetricFactory.CreateMetric(
            aggregationName: "P99",
            aggregationValue: 600.0 // Violates warning threshold
        );

        // Act
        var result = evaluator.Evaluate(metric, rule);

        // Assert
        result.Outcome.Should().Be(Severity.WARN);
        result.Violations.Should().HaveCount(1);
        result.Violations[0].RuleId.Should().Be("p99-warning");
    }

    [Fact]
    public void Evaluate_RangeRule_EvaluatesCorrectly()
    {
        // Arrange
        var evaluator = new Evaluator();
        var rule = new RangeRule
        {
            Id = "throughput-range",
            Name = "Throughput Range",
            Description = "Throughput must be between 100 and 1000",
            AggregationName = "Throughput",
            MinBound = 100.0,
            MaxBound = 1000.0,
        };

        var metric = TestMetricFactory.CreateMetric(
            aggregationName: "Throughput",
            aggregationValue: 500.0 // In range
        );

        // Act
        var result = evaluator.Evaluate(metric, rule);

        // Assert
        result.Outcome.Should().Be(Severity.PASS);
        result.Violations.Should().BeEmpty();
    }

    [Fact]
    public void EvaluateMultiple_SingleMetricMultipleRules_AggregatesViolations()
    {
        // Arrange
        var evaluator = new Evaluator();
        
        var rules = new List<IRule>
        {
            new ThresholdRule
            {
                Id = "p95-rule",
                Name = "P95 Check",
                Description = "P95 < 200ms",
                AggregationName = "P95",
                Threshold = 200.0,
                Operator = ComparisonOperator.LessThan,
            },
            new ThresholdRule
            {
                Id = "p99-rule",
                Name = "P99 Check",
                Description = "P99 < 500ms",
                AggregationName = "P99",
                Threshold = 500.0,
                Operator = ComparisonOperator.LessThan,
            }
        };

        // Create metric with both P95 and P99
        var metric = TestMetricFactory.CreateMetricWithMultipleAggregations(
            new Dictionary<string, double>
            {
                { "P95", 250.0 }, // Fails (250 >= 200)
                { "P99", 600.0 }  // Warns (600 >= 500)
            }
        );

        // Act
        var results = evaluator.EvaluateMultiple(new[] { metric }, rules).ToList();

        // Assert
        results.Should().HaveCount(1);
        var result = results[0];
        result.Outcome.Should().Be(Severity.FAIL); // Most severe wins
        result.Violations.Should().HaveCount(2);
    }

    [Fact]
    public void EvaluateMultiple_MultipleMetricsSingleRule_DeterministicOrdering()
    {
        // Arrange
        var evaluator = new Evaluator();
        
        var rule = new ThresholdRule
        {
            Id = "latency-rule",
            Name = "Latency Check",
            Description = "Latency < 100ms",
            AggregationName = "Average",
            Threshold = 100.0,
            Operator = ComparisonOperator.LessThan,
        };

        var metrics = new[]
        {
            TestMetricFactory.CreateMetric("Average", 120.0, "Metric-C"),
            TestMetricFactory.CreateMetric("Average", 80.0, "Metric-A"),
            TestMetricFactory.CreateMetric("Average", 150.0, "Metric-B")
        };

        // Act - Run multiple times to verify determinism
        var results1 = evaluator.EvaluateMultiple(metrics, new[] { rule }).ToList();
        var results2 = evaluator.EvaluateMultiple(metrics, new[] { rule }).ToList();
        var results3 = evaluator.EvaluateMultiple(metrics, new[] { rule }).ToList();

        // Assert - Results should be in deterministic order (by MetricType)
        results1.Should().HaveCount(3);
        results2.Should().HaveCount(3);
        results3.Should().HaveCount(3);

        // Verify same ordering across runs
        for (int i = 0; i < 3; i++)
        {
            results1[i].Outcome.Should().Be(results2[i].Outcome);
            results2[i].Outcome.Should().Be(results3[i].Outcome);
        }
    }

    [Fact]
    public void EvaluateMultiple_EmptyMetrics_ReturnsEmptyResults()
    {
        // Arrange
        var evaluator = new Evaluator();
        var rule = new ThresholdRule
        {
            Id = "test-rule",
            Name = "Test",
            Description = "Test",
            AggregationName = "Value",
            Threshold = 100.0,
            Operator = ComparisonOperator.LessThan,
        };

        // Act
        var results = evaluator.EvaluateMultiple(Array.Empty<Metric>(), new[] { rule });

        // Assert
        results.Should().BeEmpty();
    }

    [Fact]
    public void EvaluateMultiple_EmptyRules_ReturnsAllPass()
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
    public void EvaluateMultiple_BatchScenario_ProducesConsistentResults()
    {
        // Arrange
        var evaluator = new Evaluator();
        
        var rules = new List<IRule>
        {
            new ThresholdRule
            {
                Id = "p95-fail",
                Name = "P95 Threshold",
                Description = "P95 < 200ms",
                AggregationName = "P95",
                Threshold = 200.0,
                Operator = ComparisonOperator.LessThan,
            },
            new RangeRule
            {
                Id = "error-range",
                Name = "Error Rate Range",
                Description = "Error rate between 0% and 5%",
                AggregationName = "ErrorRate",
                MinBound = 0.0,
                MaxBound = 5.0,
            }
        };

        var metrics = new[]
        {
            TestMetricFactory.CreateMetricWithMultipleAggregations(
                new Dictionary<string, double>
                {
                    { "P95", 150.0 },     // Pass
                    { "ErrorRate", 2.0 }  // Pass
                },
                "API-Payment"
            ),
            TestMetricFactory.CreateMetricWithMultipleAggregations(
                new Dictionary<string, double>
                {
                    { "P95", 250.0 },     // Fail
                    { "ErrorRate", 1.0 }  // Pass
                },
                "API-Search"
            ),
            TestMetricFactory.CreateMetricWithMultipleAggregations(
                new Dictionary<string, double>
                {
                    { "P95", 180.0 },     // Pass
                    { "ErrorRate", 10.0 } // Fail
                },
                "API-Checkout"
            )
        };

        // Act
        var results = evaluator.EvaluateMultiple(metrics, rules).ToList();

        // Assert
        results.Should().HaveCount(3);
        
        // API-Payment: All pass
        results[0].Outcome.Should().Be(Severity.PASS);
        results[0].Violations.Should().BeEmpty();
        
        // API-Search: P95 fails
        results[1].Outcome.Should().Be(Severity.FAIL);
        results[1].Violations.Should().HaveCount(1);
        results[1].Violations[0].RuleId.Should().Be("p95-fail");
        
        // API-Checkout: ErrorRate fails
        results[2].Outcome.Should().Be(Severity.FAIL);
        results[2].Violations.Should().HaveCount(1);
        results[2].Violations[0].RuleId.Should().Be("error-range");
    }

    [Fact]
    public void Evaluate_Determinism_SameInputsProduceSameOutput()
    {
        // Arrange
        var evaluator = new Evaluator();
        var rule = new ThresholdRule
        {
            Id = "determinism-test",
            Name = "Determinism Test",
            Description = "Test deterministic behavior",
            AggregationName = "Value",
            Threshold = 100.0,
            Operator = ComparisonOperator.LessThan,
        };

        var metric = TestMetricFactory.CreateMetric("Value", 150.0);

        // Act - Run 100 times
        var results = Enumerable.Range(0, 100)
            .Select(_ => evaluator.Evaluate(metric, rule))
            .ToList();

        // Assert - All results should be identical
        var first = results.First();
        results.Should().AllSatisfy(r =>
        {
            r.Outcome.Should().Be(first.Outcome);
            r.Violations.Count.Should().Be(first.Violations.Count);
            if (r.Violations.Any())
            {
                r.Violations[0].ActualValue.Should().Be(first.Violations[0].ActualValue);
                r.Violations[0].RuleId.Should().Be(first.Violations[0].RuleId);
            }
        });
    }

    [Fact]
    public void Evaluate_MissingAggregation_ReturnsFail()
    {
        // Arrange
        var evaluator = new Evaluator();
        var rule = new ThresholdRule
        {
            Id = "missing-agg",
            Name = "Missing Aggregation Test",
            Description = "Test missing aggregation handling",
            AggregationName = "NonExistent",
            Threshold = 100.0,
            Operator = ComparisonOperator.LessThan,
        };

        var metric = TestMetricFactory.CreateMetric("P95", 50.0);

        // Act
        var result = evaluator.Evaluate(metric, rule);

        // Assert
        result.Outcome.Should().Be(Severity.FAIL);
        result.Violations.Should().HaveCount(1);
        result.Violations[0].Message.Should().Contain("not found");
    }

    [Fact]
    public void EvaluateMultiple_AllRulesPass_ReturnsAllPassResults()
    {
        // Arrange
        var evaluator = new Evaluator();
        
        var rules = new List<IRule>
        {
            new ThresholdRule
            {
                Id = "rule1",
                Name = "Rule 1",
                Description = "Test rule 1",
                AggregationName = "P95",
                Threshold = 200.0,
                Operator = ComparisonOperator.LessThan,
            },
            new ThresholdRule
            {
                Id = "rule2",
                Name = "Rule 2",
                Description = "Test rule 2",
                AggregationName = "P99",
                Threshold = 500.0,
                Operator = ComparisonOperator.LessThan,
            }
        };

        var metric = TestMetricFactory.CreateMetricWithMultipleAggregations(
            new Dictionary<string, double>
            {
                { "P95", 150.0 }, // Pass
                { "P99", 400.0 }  // Pass
            }
        );

        // Act
        var results = evaluator.EvaluateMultiple(new[] { metric }, rules).ToList();

        // Assert
        results.Should().HaveCount(1);
        results[0].Outcome.Should().Be(Severity.PASS);
        results[0].Violations.Should().BeEmpty();
    }
}
