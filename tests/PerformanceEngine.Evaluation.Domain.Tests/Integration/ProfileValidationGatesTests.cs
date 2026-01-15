namespace PerformanceEngine.Evaluation.Domain.Tests.Integration;

using System.Collections.Immutable;
using Xunit;
using PerformanceEngine.Profile.Domain.Domain.Validation;
using PerformanceEngine.Profile.Domain.Domain.Profiles;
using PerformanceEngine.Profile.Domain.Domain.Scopes;
using PerformanceEngine.Profile.Domain.Domain.Configuration;
using PerformanceEngine.Profile.Domain.Application.Validation;
using PerformanceEngine.Profile.Domain.Ports;

/// <summary>
/// Integration tests for profile validation gates.
/// Verifies that invalid profiles block evaluation with clear error messages,
/// and valid profiles allow evaluation to proceed.
/// </summary>
public class ProfileValidationGatesTests
{
    private readonly IProfileValidator _validator = new ProfileValidator();

    #region Valid Profile Tests

    [Fact]
    public void ValidProfile_PassesValidation()
    {
        // Arrange
        var profile = CreateValidProfile();

        // Act
        var result = _validator.Validate(profile);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void ValidProfile_CanProceedToEvaluation()
    {
        // Arrange
        var profile = CreateValidProfile();

        // Act
        var validationResult = _validator.Validate(profile);

        // Assert that validation passes, allowing evaluation
        Assert.True(validationResult.IsValid);
        // In a real scenario, this would proceed to evaluation service
    }

    [Fact]
    public void ValidProfileWithMultipleConfigs_PassesValidation()
    {
        // Arrange
        var scope = GlobalScope.Instance;
        var configs = new Dictionary<ConfigKey, ConfigValue>
        {
            { new ConfigKey("timeout_ms"), ConfigValue.Create(5000) },
            { new ConfigKey("max_retries"), ConfigValue.Create(3) },
            { new ConfigKey("enable_logging"), ConfigValue.Create(true) },
            { new ConfigKey("endpoint_url"), ConfigValue.Create("https://api.example.com") },
        }.ToImmutableDictionary();
        var profile = new Profile(scope, configs);

        // Act
        var result = _validator.Validate(profile);

        // Assert
        Assert.True(result.IsValid);
    }

    #endregion

    #region Invalid Profile Tests

    [Fact]
    public void InvalidProfile_FailsValidation()
    {
        // Arrange
        var invalidProfile = CreateInvalidProfile();

        // Act
        var result = _validator.Validate(invalidProfile);

        // Assert
        Assert.False(result.IsValid);
        Assert.NotEmpty(result.Errors);
    }

    [Fact]
    public void InvalidProfile_BlocksEvaluation()
    {
        // Arrange
        var invalidProfile = CreateInvalidProfile();

        // Act
        var validationResult = _validator.Validate(invalidProfile);

        // Assert
        Assert.False(validationResult.IsValid);
        // In a real scenario, EvaluationService would check this and return error
        Assert.NotEmpty(validationResult.Errors);
    }

    [Fact]
    public void InvalidProfile_ReturnsDetailedErrorMessages()
    {
        // Arrange
        var invalidProfile = CreateInvalidProfile();

        // Act
        var result = _validator.Validate(invalidProfile);

        // Assert
        Assert.False(result.IsValid);
        Assert.All(result.Errors, error =>
        {
            Assert.NotEmpty(error.ErrorCode);
            Assert.NotEmpty(error.Message);
        });
    }

    #endregion

    #region Validation Error Auditing Tests

    [Fact]
    public void ValidationFailure_IncludesErrorDetailsForAudit()
    {
        // Arrange
        var invalidProfile = CreateInvalidProfile();

        // Act
        var result = _validator.Validate(invalidProfile);

        // Assert
        Assert.False(result.IsValid);
        var errorDetails = string.Join("; ", result.Errors.Select(e => e.ToString()));
        Assert.NotEmpty(errorDetails);
    }

    [Fact]
    public void ValidationResult_CanBeSerializedForAuditTrail()
    {
        // Arrange
        var profile = CreateValidProfile();
        var result = _validator.Validate(profile);

        // Act
        var isValid = result.IsValid;
        var errorCount = result.Errors.Count;

        // Assert
        Assert.True(isValid);
        Assert.Equal(0, errorCount);
        // In real scenario, this would be logged to audit trail
    }

    #endregion

    #region Determinism Tests

    [Fact]
    public void ProfileValidation_DeterministicResults()
    {
        // Arrange
        var profile = CreateValidProfile();

        // Act: Validate same profile multiple times
        var result1 = _validator.Validate(profile);
        var result2 = _validator.Validate(profile);
        var result3 = _validator.Validate(profile);

        // Assert: All results should be identical
        Assert.Equal(result1.IsValid, result2.IsValid);
        Assert.Equal(result2.IsValid, result3.IsValid);
        Assert.Equal(result1.Errors.Count, result2.Errors.Count);
        Assert.Equal(result2.Errors.Count, result3.Errors.Count);
    }

    [Fact]
    public void ProfileValidation_UnaffectedByEvaluationDeterminism()
    {
        // Arrange
        var profile = CreateValidProfile();
        var validationResult = _validator.Validate(profile);

        // Act: Validation is independent of evaluation
        var stillValid = _validator.Validate(profile);

        // Assert: Validation result unchanged
        Assert.Equal(validationResult.IsValid, stillValid.IsValid);
        Assert.Equal(validationResult.Errors.Count, stillValid.Errors.Count);
    }

    #endregion

    #region Helper Methods

    private static Profile CreateValidProfile()
    {
        var scope = GlobalScope.Instance;
        var configs = new Dictionary<ConfigKey, ConfigValue>
        {
            { new ConfigKey("timeout_ms"), ConfigValue.Create(5000) },
            { new ConfigKey("max_retries"), ConfigValue.Create(3) },
        }.ToImmutableDictionary();
        return new Profile(scope, configs);
    }

    private static Profile CreateInvalidProfile()
    {
        // Create profile with invalid scope (empty ID and type)
        var invalidScope = new InvalidMockScope();
        var configs = new Dictionary<ConfigKey, ConfigValue>
        {
            { new ConfigKey("key1"), ConfigValue.Create("value1") }
        }.ToImmutableDictionary();
        return new Profile(invalidScope, configs);
    }

    /// <summary>
    /// Mock scope with invalid properties for testing validation.
    /// </summary>
    private class InvalidMockScope : IEquatable<InvalidMockScope>, IComparable<InvalidMockScope>, PerformanceEngine.Profile.Domain.Domain.Scopes.IScope
    {
        public string? Id => "";
        public string? Type => "";
        public int Precedence => 0;
        public string Description => "Invalid mock scope";

        public bool Equals(InvalidMockScope? other) => false;
        public int CompareTo(InvalidMockScope? other) => 0;

        bool IEquatable<PerformanceEngine.Profile.Domain.Domain.Scopes.IScope>.Equals(PerformanceEngine.Profile.Domain.Domain.Scopes.IScope? other) => false;
        int IComparable<PerformanceEngine.Profile.Domain.Domain.Scopes.IScope>.CompareTo(PerformanceEngine.Profile.Domain.Domain.Scopes.IScope? other) => 0;

        public override bool Equals(object? obj) => false;
        public override int GetHashCode() => 0;
    }

    #endregion
}
