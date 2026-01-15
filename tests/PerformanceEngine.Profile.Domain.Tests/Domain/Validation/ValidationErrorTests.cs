namespace PerformanceEngine.Profile.Domain.Tests.Domain.Validation;

using Xunit;
using PerformanceEngine.Profile.Domain.Domain.Validation;

/// <summary>
/// Unit tests for ValidationError value object.
/// Verifies immutability, invariants, and equality semantics.
/// </summary>
public class ValidationErrorTests
{
    [Fact]
    public void Constructor_WithValidData_CreatesError()
    {
        // Arrange & Act
        var error = new ValidationError("TEST_ERROR", "Test message", "TestField");

        // Assert
        Assert.Equal("TEST_ERROR", error.ErrorCode);
        Assert.Equal("Test message", error.Message);
        Assert.Equal("TestField", error.FieldName);
    }

    [Fact]
    public void Constructor_WithoutFieldName_AllowsNull()
    {
        // Arrange & Act
        var error = new ValidationError("TEST_ERROR", "Test message");

        // Assert
        Assert.Equal("TEST_ERROR", error.ErrorCode);
        Assert.Equal("Test message", error.Message);
        Assert.Null(error.FieldName);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithInvalidErrorCode_ThrowsArgumentException(string invalidCode)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new ValidationError(invalidCode, "Message"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithInvalidMessage_ThrowsArgumentException(string invalidMessage)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new ValidationError("CODE", invalidMessage));
    }

    [Fact]
    public void Equality_SameValues_AreEqual()
    {
        // Arrange
        var error1 = new ValidationError("CODE", "Message", "Field");
        var error2 = new ValidationError("CODE", "Message", "Field");

        // Act & Assert
        Assert.Equal(error1, error2);
    }

    [Fact]
    public void Equality_DifferentErrorCode_NotEqual()
    {
        // Arrange
        var error1 = new ValidationError("CODE1", "Message", "Field");
        var error2 = new ValidationError("CODE2", "Message", "Field");

        // Act & Assert
        Assert.NotEqual(error1, error2);
    }

    [Fact]
    public void Equality_DifferentMessage_NotEqual()
    {
        // Arrange
        var error1 = new ValidationError("CODE", "Message1", "Field");
        var error2 = new ValidationError("CODE", "Message2", "Field");

        // Act & Assert
        Assert.NotEqual(error1, error2);
    }

    [Fact]
    public void Equality_DifferentFieldName_NotEqual()
    {
        // Arrange
        var error1 = new ValidationError("CODE", "Message", "Field1");
        var error2 = new ValidationError("CODE", "Message", "Field2");

        // Act & Assert
        Assert.NotEqual(error1, error2);
    }

    [Fact]
    public void Equality_OneNullFieldName_NotEqual()
    {
        // Arrange
        var error1 = new ValidationError("CODE", "Message", "Field");
        var error2 = new ValidationError("CODE", "Message", null);

        // Act & Assert
        Assert.NotEqual(error1, error2);
    }

    [Fact]
    public void GetHashCode_SameValues_SameHashCode()
    {
        // Arrange
        var error1 = new ValidationError("CODE", "Message", "Field");
        var error2 = new ValidationError("CODE", "Message", "Field");

        // Act & Assert
        Assert.Equal(error1.GetHashCode(), error2.GetHashCode());
    }

    [Fact]
    public void ToString_FormatsCorrectly()
    {
        // Arrange
        var error = new ValidationError("TEST_CODE", "Test message", "TestField");

        // Act
        var result = error.ToString();

        // Assert
        Assert.Contains("TEST_CODE", result);
        Assert.Contains("Test message", result);
        Assert.Contains("TestField", result);
    }

    [Fact]
    public void ToString_WithoutFieldName_FormatsWithoutField()
    {
        // Arrange
        var error = new ValidationError("TEST_CODE", "Test message");

        // Act
        var result = error.ToString();

        // Assert
        Assert.Contains("TEST_CODE", result);
        Assert.Contains("Test message", result);
        Assert.DoesNotContain("Field", result);
    }
}
