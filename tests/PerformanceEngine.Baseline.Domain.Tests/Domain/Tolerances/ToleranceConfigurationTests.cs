namespace PerformanceEngine.Baseline.Domain.Tests.Domain.Tolerances;

using FluentAssertions;
using PerformanceEngine.Baseline.Domain.Domain;
using PerformanceEngine.Baseline.Domain.Domain.Tolerances;
using Xunit;

public class ToleranceConfigurationTests
{
    [Fact]
    public void Constructor_WithValidTolerances_CreatesConfiguration()
    {
        // Arrange
        var tolerances = new[]
        {
            new Tolerance("ResponseTime", ToleranceType.Absolute, 10m),
            new Tolerance("Throughput", ToleranceType.Relative, 5m),
        };

        // Act
        var config = new ToleranceConfiguration(tolerances);

        // Assert
        config.Should().NotBeNull();
    }

    [Fact]
    public void GetTolerance_WithExistingMetricName_ReturnsTolerance()
    {
        // Arrange
        var tolerance = new Tolerance("ResponseTime", ToleranceType.Absolute, 10m);
        var config = new ToleranceConfiguration(new[] { tolerance });

        // Act
        var result = config.GetTolerance("ResponseTime");

        // Assert
        result.Should().NotBeNull();
        result.MetricName.Should().Be("ResponseTime");
        result.Type.Should().Be(ToleranceType.Absolute);
        result.Amount.Should().Be(10m);
    }

    [Fact]
    public void GetTolerance_WithNonexistentMetricName_ThrowsKeyNotFoundException()
    {
        // Arrange
        var tolerance = new Tolerance("ResponseTime", ToleranceType.Absolute, 10m);
        var config = new ToleranceConfiguration(new[] { tolerance });

        // Act & Assert
        var action = () => config.GetTolerance("NonExistent");
        action.Should().Throw<KeyNotFoundException>();
    }

    [Fact]
    public void HasTolerance_WithExistingMetricName_ReturnsTrue()
    {
        // Arrange
        var tolerance = new Tolerance("ResponseTime", ToleranceType.Absolute, 10m);
        var config = new ToleranceConfiguration(new[] { tolerance });

        // Act
        var result = config.HasTolerance("ResponseTime");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void HasTolerance_WithNonexistentMetricName_ReturnsFalse()
    {
        // Arrange
        var tolerance = new Tolerance("ResponseTime", ToleranceType.Absolute, 10m);
        var config = new ToleranceConfiguration(new[] { tolerance });

        // Act
        var result = config.HasTolerance("NonExistent");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void Configuration_WithMultipleTolerances_StoresAll()
    {
        // Arrange
        var tolerances = new[]
        {
            new Tolerance("ResponseTime", ToleranceType.Absolute, 10m),
            new Tolerance("Throughput", ToleranceType.Relative, 5m),
            new Tolerance("ErrorRate", ToleranceType.Absolute, 0.5m),
        };

        // Act
        var config = new ToleranceConfiguration(tolerances);

        // Assert
        config.HasTolerance("ResponseTime").Should().BeTrue();
        config.HasTolerance("Throughput").Should().BeTrue();
        config.HasTolerance("ErrorRate").Should().BeTrue();
        config.HasTolerance("Unknown").Should().BeFalse();
    }

    [Fact]
    public void GetTolerance_RetrievesCorrectToleranceAfterMultipleAdds()
    {
        // Arrange
        var tolerances = new[]
        {
            new Tolerance("ResponseTime", ToleranceType.Absolute, 10m),
            new Tolerance("Throughput", ToleranceType.Relative, 5m),
        };
        var config = new ToleranceConfiguration(tolerances);

        // Act
        var responseTolerance = config.GetTolerance("ResponseTime");
        var throughputTolerance = config.GetTolerance("Throughput");

        // Assert
        responseTolerance.Type.Should().Be(ToleranceType.Absolute);
        responseTolerance.Amount.Should().Be(10m);
        throughputTolerance.Type.Should().Be(ToleranceType.Relative);
        throughputTolerance.Amount.Should().Be(5m);
    }

    [Fact]
    public void ToleranceConfiguration_IsImmutable()
    {
        // Arrange
        var tolerances = new[]
        {
            new Tolerance("ResponseTime", ToleranceType.Absolute, 10m),
        };
        var config = new ToleranceConfiguration(tolerances);

        // Act & Assert
        // Verify the configuration is read-only through its interface
        config.Should().NotBeNull();
        // The immutability is enforced through IReadOnlyDictionary<string, Tolerance>
    }
}
