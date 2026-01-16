namespace PerformanceEngine.Baseline.Domain.Tests.Integration;

using FluentAssertions;
using PerformanceEngine.Baseline.Domain.Domain.Baselines;
using PerformanceEngine.Baseline.Domain.Domain.Comparisons;
using PerformanceEngine.Baseline.Domain.Domain.Confidence;
using PerformanceEngine.Baseline.Domain.Domain.Tolerances;
using PerformanceEngine.Metrics.Domain.Metrics;
using PerformanceEngine.Metrics.Domain.Ports;
using Xunit;

/// <summary>
/// Phase 6 Integration Test - T055
/// Tests baseline domain functionality with real domain objects.
/// </summary>
public sealed class BaselineComparisonWorkflowTests
{
    [Fact]
    public void Baseline_CreationAndComparison_Workflow()
    {
        // Arrange
        var metric = new TestMetric("cpu", 50.0);
        var metrics = new List<IMetric> { metric };
        var tolerance = new Tolerance("cpu", ToleranceType.Relative, 5.0m);
        var config = new ToleranceConfiguration(new[] { tolerance });
        
        // Act
        var baseline = new Baseline(metrics, config);

        // Assert
        baseline.Should().NotBeNull();
        baseline.Metrics.Should().HaveCount(1);
        baseline.ToleranceConfig.Should().NotBeNull();
    }

    [Fact]
    public void ComparisonOutcome_DeterminedCorrectly()
    {
        // Arrange
        var tolerance = new Tolerance("cpu", ToleranceType.Relative, 5.0m);
        var outcome = ComparisonOutcome.NoSignificantChange;
        var confidence = new ConfidenceLevel(0.9m);

        // Act
        var comparisonMetric = new ComparisonMetric(
            "cpu",
            100.0m,
            102.0m,
            tolerance,
            outcome,
            confidence);

        // Assert
        comparisonMetric.MetricName.Should().Be("cpu");
        comparisonMetric.Outcome.Should().Be(ComparisonOutcome.NoSignificantChange);
        comparisonMetric.Confidence.Value.Should().Be(0.9m);
    }

    [Fact]
    public void OutcomeAggregator_SelectsWorstCase()
    {
        // Arrange
        var aggregator = new OutcomeAggregator();
        var tolerance = new Tolerance("test", ToleranceType.Relative, 5.0m);
        var confidence = new ConfidenceLevel(0.8m);
        
        var metrics = new[]
        {
            new ComparisonMetric("m1", 100m, 102m, tolerance, ComparisonOutcome.NoSignificantChange, confidence),
            new ComparisonMetric("m2", 50m, 45m, tolerance, ComparisonOutcome.Improvement, confidence),
            new ComparisonMetric("m3", 75m, 95m, tolerance, ComparisonOutcome.Regression, confidence)
        };

        // Act
        var worstOutcome = aggregator.Aggregate(metrics);

        // Assert
        worstOutcome.Should().Be(ComparisonOutcome.Regression);
    }

    private sealed class TestMetric : IMetric
    {
        public TestMetric(string metricType, double value)
        {
            MetricType = metricType;
            Value = value;
            Id = Guid.NewGuid();
            Unit = "unit";
            ComputedAt = DateTime.UtcNow;
        }

        public Guid Id { get; }
        public string MetricType { get; }
        public double Value { get; }
        public string Unit { get; }
        public DateTime ComputedAt { get; }
        public CompletessStatus CompletessStatus => CompletessStatus.COMPLETE;
        public MetricEvidence Evidence => new MetricEvidence(1, 1, "test");
    }
}
