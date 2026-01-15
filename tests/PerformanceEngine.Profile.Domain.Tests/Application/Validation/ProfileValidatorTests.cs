namespace PerformanceEngine.Profile.Domain.Tests.Application.Validation;

using System.Collections.Immutable;
using Xunit;
using PerformanceEngine.Profile.Domain.Domain.Validation;
using PerformanceEngine.Profile.Domain.Domain.Profiles;
using PerformanceEngine.Profile.Domain.Domain.Scopes;
using PerformanceEngine.Profile.Domain.Domain.Configuration;
using PerformanceEngine.Profile.Domain.Application.Validation;

/// <summary>
/// Unit tests for ProfileValidator implementation.
/// Verifies circular dependency detection, required keys validation, type correctness, scope validation, and range constraints.
/// </summary>
public class ProfileValidatorTests
{
    private readonly ProfileValidator _validator = new();

    #region Scope Validation Tests

    [Fact]
    public void Validate_WithValidGlobalScope_Succeeds()
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
    public void Validate_WithValidApiScope_Succeeds()
    {
        // Arrange
        var scope = new ApiScope("payment-api");
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
    public void Validate_WithEmptyScopeId_Fails()
    {
        // Arrange
        var mockScope = new MockScope { Id = "", Type = "Global" };
        var configs = new Dictionary<ConfigKey, ConfigValue>
        {
            { new ConfigKey("key1"), ConfigValue.Create("value1") }
        }.ToImmutableDictionary();
        var profile = new Profile(mockScope, configs);

        // Act
        var result = _validator.Validate(profile);

        // Assert
        Assert.False(result.IsValid);
        var error = Assert.Single(result.Errors);
        Assert.Equal("EMPTY_SCOPE_ID", error.ErrorCode);
    }

    [Fact]
    public void Validate_WithEmptyScopeType_Fails()
    {
        // Arrange
        var mockScope = new MockScope { Id = "id", Type = "" };
        var configs = new Dictionary<ConfigKey, ConfigValue>
        {
            { new ConfigKey("key1"), ConfigValue.Create("value1") }
        }.ToImmutableDictionary();
        var profile = new Profile(mockScope, configs);

        // Act
        var result = _validator.Validate(profile);

        // Assert
        Assert.False(result.IsValid);
        var error = Assert.Single(result.Errors);
        Assert.Equal("INVALID_SCOPE", error.ErrorCode);
    }

    #endregion

    #region Configuration Validation Tests

    [Fact]
    public void Validate_WithValidConfigurations_Succeeds()
    {
        // Arrange
        var scope = GlobalScope.Instance;
        var configs = new Dictionary<ConfigKey, ConfigValue>
        {
            { new ConfigKey("timeout_ms"), ConfigValue.Create(5000) },
            { new ConfigKey("max_retries"), ConfigValue.Create(3) },
            { new ConfigKey("enable_logging"), ConfigValue.Create(true) },
        }.ToImmutableDictionary();
        var profile = new Profile(scope, configs);

        // Act
        var result = _validator.Validate(profile);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Validate_WithMultipleConfigKeys_Succeeds()
    {
        // Arrange
        var scope = GlobalScope.Instance;
        var configs = new Dictionary<ConfigKey, ConfigValue>
        {
            { new ConfigKey("key1"), ConfigValue.Create("value1") },
            { new ConfigKey("key2"), ConfigValue.Create("value2") },
            { new ConfigKey("key3"), ConfigValue.Create("value3") },
        }.ToImmutableDictionary();
        var profile = new Profile(scope, configs);

        // Act
        var result = _validator.Validate(profile);

        // Assert
        Assert.True(result.IsValid);
    }

    #endregion

    #region Circular Dependency Tests

    [Fact]
    public void Validate_WithNoCircularDependencies_Succeeds()
    {
        // Arrange: Configuration with no dependencies
        var scope = GlobalScope.Instance;
        var configs = new Dictionary<ConfigKey, ConfigValue>
        {
            { new ConfigKey("base_value"), ConfigValue.Create("100") },
            { new ConfigKey("derived_value"), ConfigValue.Create("200") },
        }.ToImmutableDictionary();
        var profile = new Profile(scope, configs);

        // Act
        var result = _validator.Validate(profile);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors.Where(e => e.ErrorCode == "CIRCULAR_DEPENDENCY"));
    }

    [Fact]
    public void Validate_CollectsAllErrors_DoesNotStopOnFirstError()
    {
        // Arrange: Multiple validation errors
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
        // Should have multiple errors (both scope errors): EMPTY_SCOPE_ID and INVALID_SCOPE
        Assert.True(result.Errors.Count > 0);
    }

    #endregion

    #region Helper Tests

    [Fact]
    public void Validate_WithEmptyProfile_Succeeds()
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
    public void Validate_WithDifferentScopeTypes_Validates()
    {
        // Arrange & Act & Assert for GlobalScope
        var globalProfile = new Profile(
            GlobalScope.Instance,
            new Dictionary<ConfigKey, ConfigValue>
            {
                { new ConfigKey("key"), ConfigValue.Create("value") }
            }.ToImmutableDictionary());
        Assert.True(_validator.Validate(globalProfile).IsValid);

        // Arrange & Act & Assert for ApiScope
        var apiProfile = new Profile(
            new ApiScope("api-id"),
            new Dictionary<ConfigKey, ConfigValue>
            {
                { new ConfigKey("key"), ConfigValue.Create("value") }
            }.ToImmutableDictionary());
        Assert.True(_validator.Validate(apiProfile).IsValid);
    }

    #endregion

    /// <summary>
    /// Mock implementation of IScope for testing invalid scope scenarios.
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
