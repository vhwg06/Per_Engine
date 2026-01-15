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
            new Tolerance("ResponseTime", ToleranceType.Absolute, 10.0),
            new Tolerance("Throughput", ToleranceType.Relative, 5.0),
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
        var toleranceConfig = new ToleranceConfiguration(Array.Empty<Tolerance>());

        // Act & Assert
        var action = () => BaselineInvariants.AssertValid(emptyMetrics, toleranceConfig);
        action.Should().Throw<DomainInvariantViolatedException>()
            .WithMessage("*empty*");
    }

    [Fact]
    public void AssertValid_WithDuplicateMetricTypes_Throws()
    {
        // Arrange
        var mockMetric1 = new Mock<IMetric>();
        mockMetric1.Setup(m => m.MetricType).Returns("ResponseTime");
        mockMetric1.Setup(m => m.Value).Returns(100.0);

        var mockMetric2 = new Mock<IMetric>();
        mockMetric2.Setup(m => m.MetricType).Returns("ResponseTime"); // Duplicate
        mockMetric2.Setup(m => m.Value).Returns(200.0);

        var metrics = new[] { mockMetric1.Object, mockMetric2.Object };
        var toleranceConfig = new ToleranceConfiguration(new[]
        {
            new Tolerance("ResponseTime", ToleranceType.Absolute, 10.0),
        });

        // Act & Assert
        var action = () => BaselineInvariants.AssertValid(metrics, toleranceConfig);
        action.Should().Throw<DomainInvariantViolatedException>()
            .WithMessage("*duplicate*");
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
        var toleranceConfig = new ToleranceConfiguration(new[] // Only has ResponseTime tolerance
        {
            new Tolerance("ResponseTime", ToleranceType.Absolute, 10.0),
        });

        // Act & Assert
        var action = () => BaselineInvariants.AssertValid(metrics, toleranceConfig);
        action.Should().Throw<DomainInvariantViolatedException>()
            .WithMessage("*tolerance*");
    }

    [Fact]
    public void AssertValid_WithExtraTolerance_Succeeds()
    {
        // Arrange - Extra tolerances beyond metrics are allowed
        var mockMetric1 = new Mock<IMetric>();
        mockMetric1.Setup(m => m.MetricType).Returns("ResponseTime");
        mockMetric1.Setup(m => m.Value).Returns(100.0);

        var metrics = new[] { mockMetric1.Object };
        var toleranceConfig = new ToleranceConfiguration(new[] // Has extra Throughput tolerance
        {
            new Tolerance("ResponseTime", ToleranceType.Absolute, 10.0),
            new Tolerance("Throughput", ToleranceType.Relative, 5.0),
        });

        // Act & Assert
        var action = () => BaselineInvariants.AssertValid(metrics, toleranceConfig);
        action.Should().NotThrow(); // Extra tolerances are acceptable
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
            new Tolerance("ResponseTime", ToleranceType.Absolute, 10.0),
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

            tolerances.Add(new Tolerance($"Metric_{i}", ToleranceType.Absolute, 10.0));
        }

        var toleranceConfig = new ToleranceConfiguration(tolerances);

        // Act & Assert
        var action = () => BaselineInvariants.AssertValid(mockMetrics, toleranceConfig);
        action.Should().NotThrow();
    }

    [Fact]
    public void AssertImmutable_WithReadOnlyMetrics_Succeeds()
    {
        // Arrange
        var mockMetric = new Mock<IMetric>();
        mockMetric.Setup(m => m.MetricType).Returns("ResponseTime");
        mockMetric.Setup(m => m.Value).Returns(100.0);

        var metrics = new[] { mockMetric.Object }.ToList().AsReadOnly();

        // Act & Assert
        var action = () => BaselineInvariants.AssertImmutable(metrics);
        action.Should().NotThrow();
    }

    [Fact]
    public void AssertValid_CaseSensitiveMetricNames()
    {
        // Arrange
        var mockMetric1 = new Mock<IMetric>();
        mockMetric1.Setup(m => m.MetricType).Returns("responsetime");

        var mockMetric2 = new Mock<IMetric>();
        mockMetric2.Setup(m => m.MetricType).Returns("ResponseTime");

        var metrics = new[] { mockMetric1.Object, mockMetric2.Object };
        var toleranceConfig = new ToleranceConfiguration(new[]
        {
            new Tolerance("responsetime", ToleranceType.Absolute, 10.0),
            new Tolerance("ResponseTime", ToleranceType.Absolute, 10.0),
        });

        // Act & Assert
        var action = () => BaselineInvariants.AssertValid(metrics, toleranceConfig);
        action.Should().NotThrow(); // Different case = different names
    }

    [Fact]
    public void AssertValid_WithSpecialCharactersInMetricNames_Succeeds()
    {
        // Arrange
        var mockMetric1 = new Mock<IMetric>();
        mockMetric1.Setup(m => m.MetricType).Returns("Response_Time-Ms");
        mockMetric1.Setup(m => m.Value).Returns(100.0);

        var metrics = new[] { mockMetric1.Object };
        var toleranceConfig = new ToleranceConfiguration(new[]
        {
            new Tolerance("Response_Time-Ms", ToleranceType.Absolute, 10.0),
        });

        // Act & Assert
        var action = () => BaselineInvariants.AssertValid(metrics, toleranceConfig);
        action.Should().NotThrow();
    }

    [Fact]
    public void AssertValid_WithMetricValueBoundaries_Succeeds()
    {
        // Arrange
        var mockMetric1 = new Mock<IMetric>();
        mockMetric1.Setup(m => m.MetricType).Returns("Metric1");
        mockMetric1.Setup(m => m.Value).Returns(double.MaxValue);

        var mockMetric2 = new Mock<IMetric>();
        mockMetric2.Setup(m => m.MetricType).Returns("Metric2");
        mockMetric2.Setup(m => m.Value).Returns(double.MinValue);

        var metrics = new[] { mockMetric1.Object, mockMetric2.Object };
        var toleranceConfig = new ToleranceConfiguration(new[]
        {
            new Tolerance("Metric1", ToleranceType.Absolute, 10.0),
            new Tolerance("Metric2", ToleranceType.Absolute, 10.0),
        });

        // Act & Assert
        var action = () => BaselineInvariants.AssertValid(metrics, toleranceConfig);
        action.Should().NotThrow();
    }
}
