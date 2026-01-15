namespace PerformanceEngine.Baseline.Domain.Tests.Domain.Tolerances;

using FluentAssertions;
using PerformanceEngine.Baseline.Domain.Domain;
using PerformanceEngine.Baseline.Domain.Domain.Tolerances;
using Xunit;

public class ToleranceTests
{
    [Fact]
    public void Constructor_ValidAbsoluteTolerance_Succeeds()
    {
        // Act
        var tolerance = new Tolerance("latency", ToleranceType.Absolute, 50m);

        // Assert
        tolerance.MetricName.Should().Be("latency");
        tolerance.Type.Should().Be(ToleranceType.Absolute);
        tolerance.Amount.Should().Be(50m);
    }

    [Fact]
    public void Constructor_ValidRelativeTolerance_Succeeds()
    {
        // Act
        var tolerance = new Tolerance("throughput", ToleranceType.Relative, 10m);

        // Assert
        tolerance.MetricName.Should().Be("throughput");
        tolerance.Type.Should().Be(ToleranceType.Relative);
        tolerance.Amount.Should().Be(10m);
    }

    [Fact]
    public void Constructor_NegativeAmount_Throws()
    {
        // Act & Assert
        Assert.Throws<ToleranceValidationException>(() =>
            new Tolerance("latency", ToleranceType.Absolute, -10m));
    }

    [Fact]
    public void Constructor_RelativeTolerance_GreaterThan100_Throws()
    {
        // Act & Assert
        Assert.Throws<ToleranceValidationException>(() =>
            new Tolerance("latency", ToleranceType.Relative, 101m));
    }

    [Fact]
    public void Constructor_EmptyMetricName_Throws()
    {
        // Act & Assert
        Assert.Throws<ToleranceValidationException>(() =>
            new Tolerance("", ToleranceType.Absolute, 50m));
    }

    [Fact]
    public void IsWithinTolerance_AbsoluteTolerance_WithinBounds()
    {
        // Arrange
        var tolerance = new Tolerance("latency", ToleranceType.Absolute, 50m);

        // Act & Assert
        tolerance.IsWithinTolerance(100m, 120m).Should().BeTrue();
        tolerance.IsWithinTolerance(100m, 80m).Should().BeTrue();
        tolerance.IsWithinTolerance(100m, 150m).Should().BeTrue(); // At boundary
    }

    [Fact]
    public void IsWithinTolerance_AbsoluteTolerance_OutOfBounds()
    {
        // Arrange
        var tolerance = new Tolerance("latency", ToleranceType.Absolute, 50m);

        // Act & Assert
        tolerance.IsWithinTolerance(100m, 160m).Should().BeFalse(); // 60ms over
        tolerance.IsWithinTolerance(100m, 40m).Should().BeFalse();  // 60ms under
    }

    [Fact]
    public void IsWithinTolerance_RelativeTolerance_WithinBounds()
    {
        // Arrange
        var tolerance = new Tolerance("throughput", ToleranceType.Relative, 10m); // ±10%

        // Act & Assert
        // 100 ±10% = [90, 110]
        tolerance.IsWithinTolerance(100m, 105m).Should().BeTrue();
        tolerance.IsWithinTolerance(100m, 95m).Should().BeTrue();
        tolerance.IsWithinTolerance(100m, 110m).Should().BeTrue(); // At boundary
    }

    [Fact]
    public void IsWithinTolerance_RelativeTolerance_OutOfBounds()
    {
        // Arrange
        var tolerance = new Tolerance("throughput", ToleranceType.Relative, 10m); // ±10%

        // Act & Assert
        // 100 ±10% = [90, 110]
        tolerance.IsWithinTolerance(100m, 115m).Should().BeFalse(); // 15% over
        tolerance.IsWithinTolerance(100m, 85m).Should().BeFalse();  // 15% under
    }

    [Fact]
    public void IsWithinTolerance_RelativeTolerance_BaselineZero_CurrentZero()
    {
        // Arrange
        var tolerance = new Tolerance("throughput", ToleranceType.Relative, 10m);

        // Act & Assert
        tolerance.IsWithinTolerance(0m, 0m).Should().BeTrue();
    }

    [Fact]
    public void IsWithinTolerance_RelativeTolerance_BaselineZero_CurrentNonZero()
    {
        // Arrange
        var tolerance = new Tolerance("throughput", ToleranceType.Relative, 10m);

        // Act & Assert
        tolerance.IsWithinTolerance(0m, 5m).Should().BeFalse();
    }

    [Fact]
    public void Equality_SameProperties_AreEqual()
    {
        // Arrange
        var tol1 = new Tolerance("latency", ToleranceType.Absolute, 50m);
        var tol2 = new Tolerance("latency", ToleranceType.Absolute, 50m);

        // Act & Assert
        tol1.Should().Be(tol2);
        (tol1 == tol2).Should().BeTrue();
    }

    [Fact]
    public void ToString_ReturnsFormattedString()
    {
        // Arrange
        var absolute = new Tolerance("latency", ToleranceType.Absolute, 50m);
        var relative = new Tolerance("throughput", ToleranceType.Relative, 10m);

        // Act & Assert
        absolute.ToString().Should().Contain("latency");
        absolute.ToString().Should().Contain("50");
        relative.ToString().Should().Contain("throughput");
        relative.ToString().Should().Contain("10");
    }
}
