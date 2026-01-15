namespace PerformanceEngine.Profile.Domain.Tests.Domain.Validation;

using Xunit;
using PerformanceEngine.Profile.Domain.Domain.Validation;

/// <summary>
/// Unit tests for ValidationResult value object.
/// Verifies immutability, factory methods, and non-early-exit validation.
/// </summary>
public class ValidationResultTests
{
    [Fact]
    public void Success_CreatesValidResult()
    {
        // Act
        var result = ValidationResult.Success();

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Failure_WithSingleError_CreatesInvalidResult()
    {
        // Arrange
        var error = new ValidationError("CODE", "Message");

        // Act
        var result = ValidationResult.Failure(error);

        // Assert
        Assert.False(result.IsValid);
        Assert.Single(result.Errors);
        Assert.Equal(error, result.Errors[0]);
    }

    [Fact]
    public void Failure_WithMultipleErrors_CreatesInvalidResult()
    {
        // Arrange
        var error1 = new ValidationError("CODE1", "Message1");
        var error2 = new ValidationError("CODE2", "Message2");
        var error3 = new ValidationError("CODE3", "Message3");

        // Act
        var result = ValidationResult.Failure(error1, error2, error3);

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal(3, result.Errors.Count);
        Assert.Contains(error1, result.Errors);
        Assert.Contains(error2, result.Errors);
        Assert.Contains(error3, result.Errors);
    }

    [Fact]
    public void Failure_WithErrorList_CreatesInvalidResult()
    {
        // Arrange
        var errors = new[]
        {
            new ValidationError("CODE1", "Message1"),
            new ValidationError("CODE2", "Message2"),
        };

        // Act
        var result = ValidationResult.Failure((IReadOnlyList<ValidationError>)errors);

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal(2, result.Errors.Count);
    }

    [Fact]
    public void Constructor_WithIsValidTrue_AndNoErrors_Succeeds()
    {
        // Act
        var result = new ValidationResult(isValid: true, errors: null);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Constructor_WithIsValidTrue_AndErrors_ThrowsException()
    {
        // Arrange
        var errors = new[] { new ValidationError("CODE", "Message") };

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new ValidationResult(isValid: true, errors));
    }

    [Fact]
    public void Constructor_WithIsValidFalse_AndErrors_Succeeds()
    {
        // Arrange
        var errors = new[] { new ValidationError("CODE", "Message") };

        // Act
        var result = new ValidationResult(isValid: false, errors);

        // Assert
        Assert.False(result.IsValid);
        Assert.Single(result.Errors);
    }

    [Fact]
    public void Failure_WithoutErrors_ThrowsException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => ValidationResult.Failure());
    }

    [Fact]
    public void Failure_WithNullList_ThrowsException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => ValidationResult.Failure((IReadOnlyList<ValidationError>)null));
    }

    [Fact]
    public void Failure_WithEmptyList_ThrowsException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => ValidationResult.Failure(new List<ValidationError>().AsReadOnly()));
    }

    [Fact]
    public void Constructor_WithNullInErrors_ThrowsException()
    {
        // Arrange
        var errors = new ValidationError[] { new("CODE", "Message"), null };

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new ValidationResult(isValid: false, errors));
    }

    [Fact]
    public void Equality_BothSuccess_AreEqual()
    {
        // Arrange
        var result1 = ValidationResult.Success();
        var result2 = ValidationResult.Success();

        // Act & Assert
        Assert.Equal(result1, result2);
    }

    [Fact]
    public void Equality_SameErrors_AreEqual()
    {
        // Arrange
        var error = new ValidationError("CODE", "Message");
        var result1 = ValidationResult.Failure(error);
        var result2 = ValidationResult.Failure(error);

        // Act & Assert
        Assert.Equal(result1, result2);
    }

    [Fact]
    public void Equality_DifferentErrors_NotEqual()
    {
        // Arrange
        var result1 = ValidationResult.Failure(new ValidationError("CODE1", "Message1"));
        var result2 = ValidationResult.Failure(new ValidationError("CODE2", "Message2"));

        // Act & Assert
        Assert.NotEqual(result1, result2);
    }

    [Fact]
    public void Equality_SuccessVsFailure_NotEqual()
    {
        // Arrange
        var result1 = ValidationResult.Success();
        var result2 = ValidationResult.Failure(new ValidationError("CODE", "Message"));

        // Act & Assert
        Assert.NotEqual(result1, result2);
    }

    [Fact]
    public void ToString_Success_FormatsCorrectly()
    {
        // Arrange
        var result = ValidationResult.Success();

        // Act
        var str = result.ToString();

        // Assert
        Assert.Contains("Valid", str);
    }

    [Fact]
    public void ToString_Failure_FormatsCorrectly()
    {
        // Arrange
        var result = ValidationResult.Failure(
            new ValidationError("CODE1", "Message1"),
            new ValidationError("CODE2", "Message2"));

        // Act
        var str = result.ToString();

        // Assert
        Assert.Contains("Invalid", str);
        Assert.Contains("2 error(s)", str);
    }
}
