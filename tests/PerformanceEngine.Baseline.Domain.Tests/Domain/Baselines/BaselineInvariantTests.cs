namespace PerformanceEngine.Baseline.Domain.Tests.Domain.Baselines;

using FluentAssertions;
using Moq;
using PerformanceEngine.Baseline.Domain.Domain;
using PerformanceEngine.Baseline.Domain.Domain.Baselines;
using PerformanceEngine.Baseline.Domain.Domain.Tolerances;
using PerformanceEngine.Metrics.Domain.Ports;
using Xunit;

public class BaselineInvariantTests
{
    [Fact]
    public void AssertValid_WithValidBaseline_Succeeds()
    {
        // Arrange
        var mockMetric1 = new Mock<IMetric>();
        mockMetric1.Setup(m => m.MetricType).Returns("ResponseTime");
        mockMetric1.Setup(m => m.Value).Returns(100.0);

        var mockMetric2 = new Mock<IMetric>();
        mockMetric2.Setup(m => m.MetricType).Returns("Throughput");
        mockMetric2.Setup(m => m.Value).Returns(1000.0);

        var metrics = new[] { mockMetric1.Object, mockMetric2.Object };
        var toleranceConfig = new ToleranceConfiguration(new[]
        {
            new Tolerance("ResponseTime", ToleranceType.Absolute, 10m),
            new Tolerance("Throughput", ToleranceType.Relative, 5m),
        });

        // Act & Assert
        var action = () => BaselineInvariants.AssertValid(metrics, toleranceConfig);
        action.Should().NotThrow();
    }

    [Fact]
    public void AssertValid_WithEmptyMetrics_Throws()
    {
        // Arrange
        var emptyMetrics = Array.Empty<IMetric>();
        var tolerance = new Tolerance("CPU", ToleranceType.Absolute, 5m);
        var toleranceConfig = new ToleranceConfiguration(new[] { tolerance });

        // Act & Assert
        var action = () => BaselineInvariants.AssertValid(emptyMetrics, toleranceConfig);
        action.Should().Throw<Exception>();
    }

    [Fact]
    public void AssertValid_WithDuplicateMetricTypes_Throws()
    {
        // Arrange
        var mockMetric1 = new Mock<IMetric>();
        mockMetric1.Setup(m => m.MetricType).Returns("ResponseTime");
        mockMetric1.Setup(m => m.Value).Returns(100.0);

        var mockMetric2 = new Mock<IMetric>();
        mockMetric2.Setup(m => m.MetricType).Returns("ResponseTime");
        mockMetric2.Setup(m => m.Value).Returns(200.0);

        var metrics = new[] { mockMetric1.Object, mockMetric2.Object };
        var toleranceConfig = new ToleranceConfiguration(new[]
        {
            new Tolerance("ResponseTime", ToleranceType.Absolute, 10m),
        });

        // Act & Assert
        var action = () => BaselineInvariants.AssertValid(metrics, toleranceConfig);
        action.Should().Throw<DomainInvariantViolatedException>();
    }

    [Fact]
    public void AssertValid_WithIncompleteTolerance_Throws()
    {
        // Arrange
        var mockMetric1 = new Mock<IMetric>();
        mockMetric1.Setup(m => m.MetricType).Returns("ResponseTime");
        mockMetric1.Setup(m => m.Value).Returns(100.0);

        var mockMetric2 = new Mock<IMetric>();
        mockMetric2.Setup(m => m.MetricType).Returns("Throughput");
        mockMetric2.Setup(m => m.Value).Returns(1000.0);

        var metrics = new[] { mockMetric1.Object, mockMetric2.Object };
        var toleranceConfig = new ToleranceConfiguration(new[]
        {
            new Tolerance("ResponseTime", ToleranceType.Absolute, 10m),
        });

        // Act & Assert
        var action = () => BaselineInvariants.AssertValid(metrics, toleranceConfig);
        action.Should().Throw<DomainInvariantViolatedException>();
    }

    [Fact]
    public void AssertValid_WithSingleMetric_Succeeds()
    {
        // Arrange
        var mockMetric = new Mock<IMetric>();
        mockMetric.Setup(m => m.MetricType).Returns("ResponseTime");
        mockMetric.Setup(m => m.Value).Returns(100.0);

        var metrics = new[] { mockMetric.Object };
        var toleranceConfig = new ToleranceConfiguration(new[]
        {
            new Tolerance("ResponseTime", ToleranceType.Absolute, 10m),
        });

        // Act & Assert
        var action = () => BaselineInvariants.AssertValid(metrics, toleranceConfig);
        action.Should().NotThrow();
    }

    [Fact]
    public void AssertValid_WithManyMetrics_Succeeds()
    {
        // Arrange
        var mockMetrics = new List<IMetric>();
        var tolerances = new List<Tolerance>();

        for (int i = 0; i < 50; i++)
        {
            var metric = new Mock<IMetric>();
            metric.Setup(m => m.MetricType).Returns($"Metric_{i}");
            metric.Setup(m => m.Value).Returns(100.0 + i);
            mockMetrics.Add(metric.Object);

            tolerances.Add(new Tolerance($"Metric_{i}", ToleranceType.Absolute, 10m));
        }

        var toleranceConfig = new ToleranceConfiguration(tolerances);

        // Act & Assert
        var action = () => BaselineInvariants.AssertValid(mockMetrics, toleranceConfig);
        action.Should().NotThrow();
    }
}
