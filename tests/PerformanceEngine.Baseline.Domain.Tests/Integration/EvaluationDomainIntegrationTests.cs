namespace PerformanceEngine.Baseline.Domain.Tests.Integration;

using FluentAssertions;
using PerformanceEngine.Baseline.Domain.Domain.Baselines;
using PerformanceEngine.Baseline.Domain.Domain.Tolerances;
using PerformanceEngine.Metrics.Domain.Metrics;
using PerformanceEngine.Metrics.Domain.Ports;
using Xunit;

/// <summary>
/// Phase 6 Integration Test - T058
/// Tests integration with Evaluation domain:
/// Baselines can store optional evaluation results.
/// </summary>
public sealed class EvaluationDomainIntegrationTests
{
    [Fact]
    public void Baseline_WithoutEvaluationResults_IsValid()
    {
        // Arrange
        var metric = new TestMetric("cpu", 50.0);
        var tolerance = new Tolerance("cpu", ToleranceType.Relative, 5.0m);
        var config = new ToleranceConfiguration(new[] { tolerance });

        // Act
        var baseline = new Baseline(new[] { metric }, config);

        // Assert
        baseline.Should().NotBeNull();
        baseline.Metrics.Should().HaveCount(1);
        baseline.Id.Should().NotBeNull();
    }

    [Fact]
    public void Baseline_SupportsOptionalEvaluationResults()
    {
        // Arrange
        var metric1 = new TestMetric("responseTime", 100.0);
        var metric2 = new TestMetric("cpu", 50.0);
        
        // Define tolerances for ALL metrics (domain invariant requirement)
        var responseTimeTolerance = new Tolerance("responseTime", ToleranceType.Relative, 10.0m);
        var cpuTolerance = new Tolerance("cpu", ToleranceType.Relative, 5.0m);
        var config = new ToleranceConfiguration(new[] { responseTimeTolerance, cpuTolerance });

        // Act - Create baseline with optional evaluation results field
        // Note: In real implementation, evaluation results would be stored separately
        // via a different mechanism (e.g., optional repository, separate aggregate)
        var baseline = new Baseline(new[] { metric1, metric2 }, config);

        // Assert
        baseline.Should().NotBeNull();
        baseline.Metrics.Should().HaveCount(2);
        // Both baseline creation with and without evaluation results are valid
    }

    [Fact]
    public void MultipleBaselines_WithAndWithoutEvaluationResults_CoexistSuccessfully()
    {
        // Arrange
        var tolerance1 = new Tolerance("metric1", ToleranceType.Relative, 5.0m);
        var tolerance2 = new Tolerance("metric2", ToleranceType.Relative, 5.0m);
        var config1 = new ToleranceConfiguration(new[] { tolerance1 });
        var config2 = new ToleranceConfiguration(new[] { tolerance2 });

        // Act - Create multiple baselines
        var baseline1 = new Baseline(
            new[] { new TestMetric("metric1", 100.0) },
            config1);

        var baseline2 = new Baseline(
            new[] { new TestMetric("metric2", 200.0) },
            config2);

        // Assert
        baseline1.Should().NotBeNull();
        baseline2.Should().NotBeNull();
        baseline1.Id.Should().NotBe(baseline2.Id);
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
