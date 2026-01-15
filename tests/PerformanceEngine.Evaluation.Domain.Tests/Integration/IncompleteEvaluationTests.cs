namespace PerformanceEngine.Evaluation.Domain.Tests.Integration;

using PerformanceEngine.Evaluation.Domain.Application.Evaluation;
using PerformanceEngine.Evaluation.Domain.Domain.Evaluation;
using PerformanceEngine.Metrics.Domain;
using PerformanceEngine.Metrics.Domain.Metrics;
using PerformanceEngine.Tests.Fixtures;
using Xunit;

/// <summary>
/// Integration tests for incomplete evaluation scenarios.
/// Verifies end-to-end behavior when metrics are incomplete or policies restrict evaluation.
/// </summary>
public class IncompleteEvaluationTests
{
    private readonly ExecutionContext _context = new("test-engine", Guid.NewGuid());

    [Fact]
    public void PartialMetricWithNoAllowPolicy_ReturnsInconclusive()
    {
        var metric = CreatePartialMetric(sampleCount: 10, requiredSampleCount: 100);
        var policy = new PartialMetricPolicy();
        var evaluator = new Evaluator(policy);
        var rule = new SimpleTestRule("test-rule");

        var result = evaluator.Evaluate(metric, rule);

        Assert.Equal(PerformanceEngine.Evaluation.Domain.Domain.Outcome.INCONCLUSIVE, result.EvaluationOutcome);
        Assert.Contains("PARTIAL", result.OutcomeReason);
    }

    [Fact]
    public void PartialMetricWithAllowPolicy_EvaluationProceeds()
    {
        var metric = CreatePartialMetric(sampleCount: 10, requiredSampleCount: 100);
        var policy = PartialMetricPolicy.AllowForSpecificRules("test-rule");
        var evaluator = new Evaluator(policy);
        var rule = new SimpleTestRule("test-rule");

        var result = evaluator.Evaluate(metric, rule);

        Assert.NotEqual(PerformanceEngine.Evaluation.Domain.Domain.Outcome.INCONCLUSIVE, result.EvaluationOutcome);
    }

    [Fact]
    public void CompleteMetricBypassesPartialMetricPolicies()
    {
        var metric = CreateCompleteMetric();
        Assert.Equal(CompletessStatus.COMPLETE, metric.CompletessStatus);

        var policy = new PartialMetricPolicy();
        var evaluator = new Evaluator(policy);
        var rule = new SimpleTestRule("test-rule");

        var result = evaluator.Evaluate(metric, rule);

        Assert.NotEqual(PerformanceEngine.Evaluation.Domain.Domain.Outcome.INCONCLUSIVE, result.EvaluationOutcome);
    }

    private Metric CreatePartialMetric(int sampleCount, int requiredSampleCount)
    {
        var sample = new Sample(DateTime.UtcNow, new Latency(100, LatencyUnit.Milliseconds),
            SampleStatus.Success, null, _context);
        var collection = SampleCollection.Create([sample]);
        var window = AggregationWindow.FullExecution();
        return Metric.Create(
            samples: collection,
            window: window,
            metricType: "response-time",
            sampleCount: sampleCount,
            requiredSampleCount: requiredSampleCount,
            aggregationWindow: "5m");
    }

    private Metric CreateCompleteMetric()
    {
        var samples = Enumerable.Range(0, 100)
            .Select(_ => new Sample(DateTime.UtcNow, new Latency(100, LatencyUnit.Milliseconds),
                SampleStatus.Success, null, _context))
            .ToList();
        var collection = SampleCollection.Create(samples);
        var window = AggregationWindow.FullExecution();
        return Metric.Create(
            samples: collection,
            window: window,
            metricType: "response-time",
            sampleCount: 100,
            requiredSampleCount: 100,
            aggregationWindow: "5m");
    }

    private sealed class SimpleTestRule : PerformanceEngine.Evaluation.Domain.Domain.Rules.IRule
    {
        public string Id { get; }
        public string Name { get; }
        public string Description => $"Test rule: {Id}";

        public SimpleTestRule(string id)
        {
            Id = id;
            Name = id;
        }

        public EvaluationResult Evaluate(Metric metric)
        {
            return EvaluationResult.Pass(DateTime.UtcNow);
        }

        public bool Equals(PerformanceEngine.Evaluation.Domain.Domain.Rules.IRule? other) =>
            other is SimpleTestRule str && str.Id == Id;

        public override int GetHashCode() => Id.GetHashCode();
    }
}
