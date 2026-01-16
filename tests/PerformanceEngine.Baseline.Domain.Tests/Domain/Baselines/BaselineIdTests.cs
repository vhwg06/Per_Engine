namespace PerformanceEngine.Baseline.Domain.Tests.Domain.Baselines;

using FluentAssertions;
using PerformanceEngine.Baseline.Domain.Domain.Baselines;
using PerformanceEngine.Metrics.Domain.Ports;
using Xunit;

public class BaselineIdTests
{
    [Fact]
    public void Constructor_WithoutValue_GeneratesUUID()
    {
        // Act
        var id = new BaselineId();

        // Assert
        id.Value.Should().NotBeNullOrWhiteSpace();
        Guid.TryParse(id.Value, out _).Should().BeTrue();
    }

    [Fact]
    public void Constructor_WithValue_UsesProvidedValue()
    {
        // Arrange
        var providedValue = "custom-baseline-id-123";

        // Act
        var id = new BaselineId(providedValue);

        // Assert
        id.Value.Should().Be(providedValue);
    }

    [Fact]
    public void Equality_SameValue_AreEqual()
    {
        // Arrange
        var value = "same-id";
        var id1 = new BaselineId(value);
        var id2 = new BaselineId(value);

        // Act & Assert
        id1.Should().Be(id2);
        (id1 == id2).Should().BeTrue();
        (id1 != id2).Should().BeFalse();
    }

    [Fact]
    public void Equality_DifferentValues_AreNotEqual()
    {
        // Arrange
        var id1 = new BaselineId("id-1");
        var id2 = new BaselineId("id-2");

        // Act & Assert
        id1.Should().NotBe(id2);
        (id1 == id2).Should().BeFalse();
        (id1 != id2).Should().BeTrue();
    }

    [Fact]
    public void GetHashCode_SameValue_SameHashCode()
    {
        // Arrange
        var value = "hash-test-id";
        var id1 = new BaselineId(value);
        var id2 = new BaselineId(value);

        // Act & Assert
        id1.GetHashCode().Should().Be(id2.GetHashCode());
    }

    [Fact]
    public void ToString_ReturnsValue()
    {
        // Arrange
        var value = "baseline-123";
        var id = new BaselineId(value);

        // Act & Assert
        id.ToString().Should().Be(value);
    }
}
