namespace PerformanceEngine.Baseline.Domain.Tests.Domain.Baselines;

using FluentAssertions;
using Moq;
using PerformanceEngine.Baseline.Domain.Domain;
using PerformanceEngine.Baseline.Domain.Domain.Baselines;
using PerformanceEngine.Baseline.Domain.Domain.Tolerances;
using PerformanceEngine.Metrics.Domain.Ports;
using Xunit;

public class BaselineTests
{
    private readonly Mock<IMetric> _mockMetric1;
    private readonly Mock<IMetric> _mockMetric2;
    private readonly ToleranceConfiguration _toleranceConfig;

    public BaselineTests()
    {
        _mockMetric1 = new Mock<IMetric>();
        _mockMetric1.Setup(m => m.MetricType).Returns("ResponseTime");
        _mockMetric1.Setup(m => m.Value).Returns(100.0);

        _mockMetric2 = new Mock<IMetric>();
        _mockMetric2.Setup(m => m.MetricType).Returns("Throughput");
        _mockMetric2.Setup(m => m.Value).Returns(1000.0);

        _toleranceConfig = new ToleranceConfiguration(new[]
        {
            new Tolerance("ResponseTime", ToleranceType.Absolute, 10m),
            new Tolerance("Throughput", ToleranceType.Relative, 5m),
        });
    }

    [Fact]
    public void Constructor_WithValidInputs_CreatesBaseline()
    {
        // Arrange
        var metrics = new[] { _mockMetric1.Object, _mockMetric2.Object };

        // Act
        var baseline = new Baseline(metrics, _toleranceConfig);

        // Assert
        baseline.Metrics.Should().HaveCount(2);
        baseline.Metrics[0].MetricType.Should().Be("ResponseTime");
        baseline.Metrics[1].MetricType.Should().Be("Throughput");
    }

    [Fact]
    public void Constructor_WithEmptyMetrics_Throws()
    {
        // Arrange
        var emptyMetrics = Array.Empty<IMetric>();

        // Act & Assert
        var action = () => new Baseline(emptyMetrics, _toleranceConfig);
        action.Should().Throw<DomainInvariantViolatedException>();
    }

    [Fact]
    public void Constructor_WithDuplicateMetricTypes_Throws()
    {
        // Arrange
        var mockMetric1Dup = new Mock<IMetric>();
        mockMetric1Dup.Setup(m => m.MetricType).Returns("ResponseTime");
        mockMetric1Dup.Setup(m => m.Value).Returns(100.0);

        var mockMetric2Dup = new Mock<IMetric>();
        mockMetric2Dup.Setup(m => m.MetricType).Returns("ResponseTime");
        mockMetric2Dup.Setup(m => m.Value).Returns(200.0);

        var metrics = new[] { mockMetric1Dup.Object, mockMetric2Dup.Object };

        // Act & Assert
        var action = () => new Baseline(metrics, _toleranceConfig);
        action.Should().Throw<DomainInvariantViolatedException>();
    }

    [Fact]
    public void GetMetric_WithExistingMetricName_ReturnsMetric()
    {
        // Arrange
        var metrics = new[] { _mockMetric1.Object, _mockMetric2.Object };
        var baseline = new Baseline(metrics, _toleranceConfig);

        // Act
        var result = baseline.GetMetric("ResponseTime");

        // Assert
        result.Should().NotBeNull();
        result!.MetricType.Should().Be("ResponseTime");
        result.Value.Should().Be(100.0);
    }

    [Fact]
    public void GetMetric_WithNonexistentMetricName_ReturnsNull()
    {
        // Arrange
        var metrics = new[] { _mockMetric1.Object, _mockMetric2.Object };
        var baseline = new Baseline(metrics, _toleranceConfig);

        // Act
        var result = baseline.GetMetric("NonExistent");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void Metrics_IsReadOnly()
    {
        // Arrange
        var metrics = new[] { _mockMetric1.Object, _mockMetric2.Object };
        var baseline = new Baseline(metrics, _toleranceConfig);

        // Act & Assert
        // Verify the Metrics property returns a read-only collection
        baseline.Metrics.Should().HaveCount(2);
        baseline.Metrics[0].MetricType.Should().Be("ResponseTime");
    }

    [Fact]
    public void CreatedAt_IsSetToCurrentTime()
    {
        // Arrange
        var metrics = new[] { _mockMetric1.Object };
        var beforeCreation = DateTime.UtcNow;

        // Act
        var baseline = new Baseline(metrics, _toleranceConfig);
        var afterCreation = DateTime.UtcNow;

        // Assert
        baseline.CreatedAt.Should().BeOnOrAfter(beforeCreation);
        baseline.CreatedAt.Should().BeOnOrBefore(afterCreation);
    }

    [Fact]
    public void ToleranceConfiguration_IsStoredCorrectly()
    {
        // Arrange
        var metrics = new[] { _mockMetric1.Object };

        // Act
        var baseline = new Baseline(metrics, _toleranceConfig);

        // Assert
        baseline.ToleranceConfig.Should().NotBeNull();
        baseline.ToleranceConfig.HasTolerance("ResponseTime").Should().BeTrue();
        baseline.ToleranceConfig.HasTolerance("Throughput").Should().BeTrue();
        baseline.ToleranceConfig.HasTolerance("NonExistent").Should().BeFalse();
    }
}
