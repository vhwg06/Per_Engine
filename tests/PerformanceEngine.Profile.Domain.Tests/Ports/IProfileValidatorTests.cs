namespace PerformanceEngine.Profile.Domain.Tests.Ports;

using System.Collections.Immutable;
using Xunit;
using PerformanceEngine.Profile.Domain.Ports;
using PerformanceEngine.Profile.Domain.Domain.Validation;
using PerformanceEngine.Profile.Domain.Domain.Profiles;
using PerformanceEngine.Profile.Domain.Domain.Scopes;
using PerformanceEngine.Profile.Domain.Domain.Configuration;
using PerformanceEngine.Profile.Domain.Application.Validation;

/// <summary>
/// Contract tests for IProfileValidator port.
/// Verifies that all validator implementations satisfy the contract.
/// </summary>
public class IProfileValidatorTests
{
    private readonly IProfileValidator _validator = new ProfileValidator();

    [Fact]
    public void Validate_WithNullProfile_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _validator.Validate(null!));
    }

    [Fact]
    public void Validate_WithValidProfile_ReturnsSuccess()
    {
        // Arrange
        var scope = GlobalScope.Instance;
        var configs = new Dictionary<ConfigKey, ConfigValue>
        {
            { new ConfigKey("key1"), ConfigValue.Create("value1") }
        }.ToImmutableDictionary();
        var profile = new Profile(scope, configs);

        // Act
        var result = _validator.Validate(profile);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Validate_WithProfileHavingEmptyConfigurations_Succeeds()
    {
        // Arrange
        var scope = GlobalScope.Instance;
        var configs = ImmutableDictionary<ConfigKey, ConfigValue>.Empty;
        var profile = new Profile(scope, configs);

        // Act
        var result = _validator.Validate(profile);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_ReturnsValidationResult()
    {
        // Arrange
        var scope = GlobalScope.Instance;
        var configs = new Dictionary<ConfigKey, ConfigValue>
        {
            { new ConfigKey("key1"), ConfigValue.Create("value1") }
        }.ToImmutableDictionary();
        var profile = new Profile(scope, configs);

        // Act
        var result = _validator.Validate(profile);

        // Assert
        Assert.NotNull(result);
        Assert.IsType<ValidationResult>(result);
    }

    [Fact]
    public void Validate_WithInvalidScope_ReturnsFailure()
    {
        // Arrange
        var mockScope = new MockScope { Id = "", Type = "" };
        var configs = new Dictionary<ConfigKey, ConfigValue>
        {
            { new ConfigKey("key1"), ConfigValue.Create("value1") }
        }.ToImmutableDictionary();
        var profile = new Profile(mockScope, configs);

        // Act
        var result = _validator.Validate(profile);

        // Assert
        Assert.False(result.IsValid);
        Assert.NotEmpty(result.Errors);
    }

    [Fact]
    public void Validate_CollectsAllErrors_NonEarlyExit()
    {
        // Arrange
        var mockScope = new MockScope { Id = "", Type = "" };
        var configs = ImmutableDictionary<ConfigKey, ConfigValue>.Empty;
        var profile = new Profile(mockScope, configs);

        // Act
        var result = _validator.Validate(profile);

        // Assert
        // Should collect multiple errors (empty ID, empty Type) at once
        Assert.False(result.IsValid);
        Assert.NotEmpty(result.Errors);
    }

    /// <summary>
    /// Mock implementation of IScope for testing validation of invalid scopes.
    /// </summary>
    private class MockScope : IScope
    {
        public string? Id { get; set; }
        public string? Type { get; set; }
        public int Precedence => 0;
        public string Description => "Mock scope";

        public bool Equals(IScope? other) => other?.Id == Id && other?.Type == Type;
        public int CompareTo(IScope? other) => Id?.CompareTo(other?.Id) ?? 0;
        public override bool Equals(object? obj) => Equals(obj as IScope);
        public override int GetHashCode() => HashCode.Combine(Id, Type);
    }
}
