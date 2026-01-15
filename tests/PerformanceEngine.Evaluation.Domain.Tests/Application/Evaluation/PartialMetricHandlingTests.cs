namespace PerformanceEngine.Evaluation.Domain.Tests.Application.Evaluation;

using PerformanceEngine.Evaluation.Domain.Application.Evaluation;
using PerformanceEngine.Evaluation.Domain.Domain.Evaluation;
using PerformanceEngine.Metrics.Domain;
using PerformanceEngine.Metrics.Domain.Metrics;
using PerformanceEngine.Tests.Fixtures;
using Xunit;

/// <summary>
/// Unit tests for partial metric handling in evaluation.
/// Verifies that INCONCLUSIVE outcomes are returned when partial metrics are not allowed.
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
        var window = AggregationWindow.FullExecution();
        var metric = Metric.Create(
            samples: collection,
            window: window,
            metricType: "response-time",
            sampleCount: 1,
            requiredSampleCount: 100,
            aggregationWindow: "5m");

        Assert.Equal(CompletessStatus.PARTIAL, metric.CompletessStatus);

        // Create a deny-all policy
        var policy = new PartialMetricPolicy();
        var evaluator = new Evaluator(policy);
        var rule = new TestThresholdRule("latency-threshold", 200);

        // Act
        var result = evaluator.Evaluate(metric, rule);

        // Assert
        Assert.Equal(PerformanceEngine.Evaluation.Domain.Domain.Outcome.INCONCLUSIVE, result.EvaluationOutcome);
        Assert.Contains("PARTIAL", result.OutcomeReason);
    }

    [Fact]
    public void Evaluate_CompleteMetric_BypassesPolicy()
    {
        // Arrange: Create a COMPLETE metric
        var samples = Enumerable.Range(0, 100)
            .Select(_ => new Sample(DateTime.UtcNow, new Latency(100, LatencyUnit.Milliseconds),
                SampleStatus.Success, null, _context))
            .ToList();
        var collection = SampleCollection.Create(samples);
        var window = AggregationWindow.FullExecution();
        var metric = Metric.Create(
            samples: collection,
            window: window,
            metricType: "response-time",
            sampleCount: 100,
            requiredSampleCount: 100,
            aggregationWindow: "5m");

        var policy = new PartialMetricPolicy();
        var evaluator = new Evaluator(policy);
        var rule = new TestThresholdRule("latency-threshold", 200);

        // Act
        var result = evaluator.Evaluate(metric, rule);

        // Assert: Complete metrics bypass partial metric policy
        Assert.NotEqual(PerformanceEngine.Evaluation.Domain.Domain.Outcome.INCONCLUSIVE, result.EvaluationOutcome);
    }

    private sealed class TestThresholdRule : PerformanceEngine.Evaluation.Domain.Domain.Rules.IRule
    {
        private readonly double _threshold;
        public string Id { get; }
        public string Name { get; }
        public string Description => $"Threshold rule: {Id}";

        public TestThresholdRule(string id, double threshold)
        {
            Id = id;
            Name = id;
            _threshold = threshold;
        }

        public EvaluationResult Evaluate(Metric metric)
        {
            var metricInterface = metric as PerformanceEngine.Metrics.Domain.Ports.IMetric;
            if (metricInterface!.Value > _threshold)
            {
                return EvaluationResult.Fail(
                    ImmutableList<Violation>.Empty,
                    DateTime.UtcNow);
            }
            return EvaluationResult.Pass(DateTime.UtcNow);
        }

        public bool Equals(PerformanceEngine.Evaluation.Domain.Domain.Rules.IRule? other) =>
            other is TestThresholdRule tr && tr.Id == Id;

        public override int GetHashCode() => Id.GetHashCode();
    }
}
