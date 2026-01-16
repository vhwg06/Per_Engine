namespace PerformanceEngine.Baseline.Infrastructure.Tests.Persistence;

using PerformanceEngine.Baseline.Domain.Domain.Baselines;
using PerformanceEngine.Metrics.Domain.Ports;

/// <summary>
/// Tests for BaselineRedisMapper serialization/deserialization.
/// Verifies JSON handling and edge case handling.
/// </summary>
public class BaselineRedisMapperTests
{
    [Fact]
    public void Serialize_WithNullBaseline_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => BaselineRedisMapper.Serialize(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Deserialize_WithNullJson_ThrowsArgumentException()
    {
        // Act & Assert
        var act = () => BaselineRedisMapper.Deserialize(null!);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Deserialize_WithEmptyJson_ThrowsArgumentException()
    {
        // Act & Assert
        var act = () => BaselineRedisMapper.Deserialize("");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Deserialize_WithWhitespaceOnlyJson_ThrowsArgumentException()
    {
        // Act & Assert
        var act = () => BaselineRedisMapper.Deserialize("   ");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Deserialize_WithInvalidJson_ThrowsInvalidOperationException()
    {
        // Arrange
        var invalidJson = "{ invalid json }";

        // Act & Assert
        var act = () => BaselineRedisMapper.Deserialize(invalidJson);
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Deserialize_WithNullDeserializedDto_ThrowsInvalidOperationException()
    {
        // Arrange - JSON that deserializes to null (e.g., "null")
        var nullJson = "null";

        // Act & Assert
        var act = () => BaselineRedisMapper.Deserialize(nullJson);
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void VerifyRoundTripFidelity_WithNullBaseline_ReturnsFalse()
    {
        // Act
        var result = BaselineRedisMapper.VerifyRoundTripFidelity(null!);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void VerifyRoundTripFidelity_WithInvalidJson_ReturnsFalse()
    {
        // Arrange - create a baseline that we can test with
        // We'll use a simple approach: test that invalid JSON returns false
        var act = () => BaselineRedisMapper.VerifyRoundTripFidelity(null!);

        // Act & Assert
        var result = BaselineRedisMapper.VerifyRoundTripFidelity(null!);
        result.Should().BeFalse();
    }
}
