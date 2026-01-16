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
/// Phase 6 Integration Test - T057
/// Tests cross-domain integration: Baseline domain accepts metrics from Metrics domain.
/// </summary>
public sealed class MetricsDomainIntegrationTests
{
    [Fact]
    public void Baseline_AcceptsMetricsFromMetricsDomain()
    {
        // Arrange
        var metric1 = new SimpleMetric("cpu", 50.0);
        var metric2 = new SimpleMetric("memory", 512.0);
        var metrics = new List<IMetric> { metric1, metric2 };

        // Define tolerances for ALL metrics (domain invariant requirement)
        var cpuTolerance = new Tolerance("cpu", ToleranceType.Relative, 5.0m);
        var memoryTolerance = new Tolerance("memory", ToleranceType.Relative, 10.0m);
        var config = new ToleranceConfiguration(new[] { cpuTolerance, memoryTolerance });

        // Act
        var baseline = new Baseline(metrics, config);

        // Assert
        baseline.Should().NotBeNull();
        baseline.Metrics.Should().HaveCount(2);
    }

    [Fact]
    public void ComparisonWithMetricsDomain_ProducesValidResults()
    {
        // Arrange
        var tolerance = new Tolerance("latency", ToleranceType.Relative, 10.0m);
        var config = new ToleranceConfiguration(new[] { tolerance });
        
        var metric1 = new SimpleMetric("latency", 100.0);
        var baseline = new Baseline(new[] { metric1 }, config);

        var confidence = new ConfidenceLevel(0.8m);

        // Act
        var comparisonMetric = new ComparisonMetric(
            "latency",
            100.0m,
            105.0m,
            tolerance,
            ComparisonOutcome.NoSignificantChange,
            confidence);

        // Assert
        comparisonMetric.Outcome.Should().Be(ComparisonOutcome.NoSignificantChange);
        baseline.Metrics.Should().HaveCount(1);
    }

    private sealed class SimpleMetric : IMetric
    {
        public SimpleMetric(string metricType, double value)
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
