namespace PerformanceEngine.Baseline.Domain.Tests.Domain.Confidence;

using FluentAssertions;
using PerformanceEngine.Baseline.Domain.Domain;
using PerformanceEngine.Baseline.Domain.Domain.Confidence;
using Xunit;

public class ConfidenceLevelTests
{
    [Fact]
    public void Constructor_ValidValue_Succeeds()
    {
        // Act
        var confidence = new ConfidenceLevel(0.75m);

        // Assert
        confidence.Value.Should().Be(0.75m);
    }

    [Fact]
    public void Constructor_ValueBelowZero_Throws()
    {
        // Act & Assert
        Assert.Throws<ConfidenceValidationException>(() =>
            new ConfidenceLevel(-0.1m));
    }

    [Fact]
    public void Constructor_ValueAboveOne_Throws()
    {
        // Act & Assert
        Assert.Throws<ConfidenceValidationException>(() =>
            new ConfidenceLevel(1.1m));
    }

    [Fact]
    public void Constructor_BoundaryValues_Succeed()
    {
        // Act & Assert
        var zero = new ConfidenceLevel(0.0m);
        zero.Value.Should().Be(0.0m);

        var one = new ConfidenceLevel(1.0m);
        one.Value.Should().Be(1.0m);
    }

    [Fact]
    public void IsConclusive_AboveThreshold_ReturnsTrue()
    {
        // Arrange
        var confidence = new ConfidenceLevel(0.75m);

        // Act & Assert
        confidence.IsConclusive(0.5m).Should().BeTrue();
        confidence.IsConclusive(0.7m).Should().BeTrue();
        confidence.IsConclusive(0.75m).Should().BeTrue(); // At threshold
    }

    [Fact]
    public void IsConclusive_BelowThreshold_ReturnsFalse()
    {
        // Arrange
        var confidence = new ConfidenceLevel(0.4m);

        // Act & Assert
        confidence.IsConclusive(0.5m).Should().BeFalse();
    }

    [Fact]
    public void IsConclusive_DefaultThreshold_Uses0Point5()
    {
        // Arrange
        var conclusive = new ConfidenceLevel(0.7m);
        var inconclusive = new ConfidenceLevel(0.3m);

        // Act & Assert
        conclusive.IsConclusive().Should().BeTrue();
        inconclusive.IsConclusive().Should().BeFalse();
    }

    [Fact]
    public void Equality_SameValue_AreEqual()
    {
        // Arrange
        var conf1 = new ConfidenceLevel(0.75m);
        var conf2 = new ConfidenceLevel(0.75m);

        // Act & Assert
        conf1.Should().Be(conf2);
        (conf1 == conf2).Should().BeTrue();
    }

    [Fact]
    public void Equality_DifferentValues_AreNotEqual()
    {
        // Arrange
        var conf1 = new ConfidenceLevel(0.75m);
        var conf2 = new ConfidenceLevel(0.80m);

        // Act & Assert
        conf1.Should().NotBe(conf2);
        (conf1 == conf2).Should().BeFalse();
    }

    [Fact]
    public void Comparison_Operators_Work()
    {
        // Arrange
        var low = new ConfidenceLevel(0.3m);
        var high = new ConfidenceLevel(0.8m);

        // Act & Assert
        (low < high).Should().BeTrue();
        (high > low).Should().BeTrue();
        (low <= high).Should().BeTrue();
        (high >= low).Should().BeTrue();
    }

    [Fact]
    public void Min_ReturnsLowerValue()
    {
        // Arrange
        var conf1 = new ConfidenceLevel(0.3m);
        var conf2 = new ConfidenceLevel(0.8m);

        // Act
        var min = ConfidenceLevel.Min(conf1, conf2);

        // Assert
        min.Should().Be(conf1);
    }

    [Fact]
    public void Max_ReturnsHigherValue()
    {
        // Arrange
        var conf1 = new ConfidenceLevel(0.3m);
        var conf2 = new ConfidenceLevel(0.8m);

        // Act
        var max = ConfidenceLevel.Max(conf1, conf2);

        // Assert
        max.Should().Be(conf2);
    }

    [Fact]
    public void ToString_FormatsAsPercentage()
    {
        // Arrange
        var confidence = new ConfidenceLevel(0.75m);

        // Act
        var str = confidence.ToString();

        // Assert
        str.Should().Contain("75");
    }
}
