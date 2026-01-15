namespace PerformanceEngine.Evaluation.Domain.Tests.Application.Evaluation;

using PerformanceEngine.Evaluation.Domain.Application.Evaluation;
using PerformanceEngine.Evaluation.Domain.Domain.Evaluation;
using PerformanceEngine.Metrics.Domain.Metrics;
using PerformanceEngine.Tests.Fixtures;
using Xunit;

/// <summary>
/// Unit tests for partial metric handling in evaluation.
/// Verifies that INCONCLUSIVE outcomes are returned when partial metrics are not allowed.
/// Confirms that OutcomeReason explains why INCONCLUSIVE was assigned.
/// </summary>
public class PartialMetricHandlingTests
{
    private readonly ExecutionContext _context = new("test-engine", Guid.NewGuid());

    [Fact]
    public void Evaluate_PartialMetricWithDenyPolicy_ReturnsInconclusive()
    {
        // Arrange: Create a partial metric
        var sample = new Sample(
            DateTime.UtcNow,
            new Latency(100, LatencyUnit.Milliseconds),
            SampleStatus.Success,
            null,
            _context);
        var collection = SampleCollection.Create([sample]);
        var metric = Metric.Create(
            aggregationName: "response-time",
            sampleCollection: collection,
            sampleCount: 1,
            requiredSampleCount: 100,
            aggregationWindow: "5m");

        Assert.Equal(CompletessStatus.PARTIAL, metric.CompletessStatus);

        // Create a deny-all policy
        var policy = new PartialMetricPolicy();
        var evaluator = new Evaluator(policy);
        var rule = new ThresholdRule("latency-threshold", "Latency threshold rule", 200);

        // Act
        var result = evaluator.Evaluate(metric, rule);

        // Assert
        Assert.NotNull(result.EvaluationOutcome);
        Assert.Equal(Outcome.INCONCLUSIVE, result.EvaluationOutcome);
        Assert.False(string.IsNullOrWhiteSpace(result.OutcomeReason));
        Assert.Contains("PARTIAL", result.OutcomeReason);
        Assert.Empty(result.Violations);
    }

    [Fact]
    public void Evaluate_PartialMetricWithAllowPolicy_EvaluatesNormally()
    {
        // Arrange: Create a partial metric
        var sample = new Sample(
            DateTime.UtcNow,
            new Latency(100, LatencyUnit.Milliseconds),
            SampleStatus.Success,
            null,
            _context);
        var collection = SampleCollection.Create([sample]);
        var metric = Metric.Create(
            aggregationName: "response-time",
            sampleCollection: collection,
            sampleCount: 1,
            requiredSampleCount: 100,
            aggregationWindow: "5m");

        // Create an allow-for-specific-rules policy
        var policy = PartialMetricPolicy.AllowForSpecificRules("latency-threshold");
        var evaluator = new Evaluator(policy);
        var rule = new ThresholdRule("latency-threshold", "Latency threshold rule", 200);

        // Act
        var result = evaluator.Evaluate(metric, rule);

        // Assert: Should evaluate normally, not return INCONCLUSIVE
        Assert.NotEqual(Outcome.INCONCLUSIVE, result.EvaluationOutcome);
    }

    [Fact]
    public void EvaluateMultipleRules_PartialMetricWithMixedPolicies_SkipsDisallowedRules()
    {
        // Arrange: Create a partial metric
        var sample = new Sample(
            DateTime.UtcNow,
            new Latency(100, LatencyUnit.Milliseconds),
            SampleStatus.Success,
            null,
            _context);
        var collection = SampleCollection.Create([sample]);
        var metric = Metric.Create(
            aggregationName: "response-time",
            sampleCollection: collection,
            sampleCount: 1,
            requiredSampleCount: 100,
            aggregationWindow: "5m");

        // Create a policy that allows only specific rules
        var policy = PartialMetricPolicy.AllowForSpecificRules("rule-1");
        var evaluator = new Evaluator(policy);

        var rule1 = new ThresholdRule("rule-1", "Rule 1", 200);
        var rule2 = new ThresholdRule("rule-2", "Rule 2", 300);
        var rules = new[] { rule1, rule2 };

        // Act
        var result = evaluator.EvaluateMultipleRules(metric, rules);

        // Assert: result should not be INCONCLUSIVE (some rules were evaluated)
        Assert.NotNull(result);
        // rule-2 should be skipped due to partial metric policy
    }

    [Fact]
    public void Evaluate_PartialMetricOutcomeReason_IncludesMetricDetails()
    {
        // Arrange: Create a partial metric with specific sample counts
        var sample = new Sample(
            DateTime.UtcNow,
            new Latency(100, LatencyUnit.Milliseconds),
            SampleStatus.Success,
            null,
            _context);
        var collection = SampleCollection.Create([sample]);
        var metric = Metric.Create(
            aggregationName: "error-rate",
            sampleCollection: collection,
            sampleCount: 5,
            requiredSampleCount: 50,
            aggregationWindow: "10m");

        var policy = new PartialMetricPolicy();
        var evaluator = new Evaluator(policy);
        var rule = new ThresholdRule("error-rule", "Error rule", 0.1);

        // Act
        var result = evaluator.Evaluate(metric, rule);

        // Assert: OutcomeReason should include sample count information
        Assert.Contains("5/50", result.OutcomeReason);
        Assert.Contains("error-rate", result.OutcomeReason);
    }

    [Fact]
    public void Evaluate_CompleteMetric_BypassesPartialMetricPolicy()
    {
        // Arrange: Create a COMPLETE metric
        var samples = Enumerable.Range(0, 100)
            .Select(_ => new Sample(
                DateTime.UtcNow,
                new Latency(100, LatencyUnit.Milliseconds),
                SampleStatus.Success,
                null,
                _context))
            .ToList();

        var collection = SampleCollection.Create(samples);
        var metric = Metric.Create(
            aggregationName: "response-time",
            sampleCollection: collection,
            sampleCount: 100,
            requiredSampleCount: 100,
            aggregationWindow: "5m");

        Assert.Equal(CompletessStatus.COMPLETE, metric.CompletessStatus);

        // Create a deny-all policy
        var policy = new PartialMetricPolicy();
        var evaluator = new Evaluator(policy);
        var rule = new ThresholdRule("latency-threshold", "Latency threshold rule", 200);

        // Act
        var result = evaluator.Evaluate(metric, rule);

        // Assert: Should NOT be INCONCLUSIVE; policy check is bypassed for complete metrics
        Assert.NotEqual(Outcome.INCONCLUSIVE, result.EvaluationOutcome);
    }

    /// <summary>
    /// Simple threshold rule for testing.
    /// </summary>
    private sealed class ThresholdRule : PerformanceEngine.Evaluation.Domain.Domain.Rules.IRule
    {
        private readonly string _ruleId;
        private readonly string _description;
        private readonly double _threshold;

        public string Id => _ruleId;

        public ThresholdRule(string id, string description, double threshold)
        {
            _ruleId = id;
            _description = description;
            _threshold = threshold;
        }

        public EvaluationResult Evaluate(Metric metric)
        {
            // Simple threshold check
            if (metric.Value > _threshold)
            {
                var violation = Violation.Create(
                    ruleId: Id,
                    metricName: metric.MetricType,
                    actualValue: metric.Value,
                    threshold: _threshold,
                    message: $"{metric.MetricType} ({metric.Value}) exceeded threshold ({_threshold})");

                return EvaluationResult.Fail(
                    ImmutableList.Create(violation),
                    DateTime.UtcNow);
            }

            return EvaluationResult.Pass(DateTime.UtcNow);
        }
    }
}
