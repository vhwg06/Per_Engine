using FluentAssertions;
using PerformanceEngine.Profile.Domain.Domain.Configuration;
using PerformanceEngine.Profile.Domain.Domain.Profiles;
using PerformanceEngine.Profile.Domain.Domain.Scopes;
using System.Collections.Immutable;

namespace PerformanceEngine.Profile.Domain.Tests.Domain.Configuration;

/// <summary>
/// Comprehensive tests for conflict detection:
/// - Same scope, different values = conflict
/// - Different scopes, different values = valid (precedence resolves)
/// - Clear error messages with scope details
/// </summary>
public class ConflictDetectionTests
{
    [Fact]
    public void DetectConflicts_SameScope_SameKey_DifferentValues_ThrowsConflict()
    {
        // Arrange
        var scope = new ApiScope("payment");

        var profile1 = new ProfileEntity(
            scope,
            ImmutableDictionary<ConfigKey, ConfigValue>.Empty
                .Add(new ConfigKey("timeout"), ConfigValue.Create("30s")));

        var profile2 = new ProfileEntity(
            scope,
            ImmutableDictionary<ConfigKey, ConfigValue>.Empty
                .Add(new ConfigKey("timeout"), ConfigValue.Create("60s")));

        var profiles = new[] { profile1, profile2 };

        // Act
        Action act = () => ConflictHandler.ValidateNoConflicts(profiles);

        // Assert
        var exception = act.Should().Throw<ConfigurationConflictException>().Which;
        exception.ToString().Should().Contain("timeout").And.Contain("payment");
    }

    [Fact]
    public void DetectConflicts_SameScope_SameKey_SameValue_NoConflict()
    {
        // Arrange - Same key, same value = no conflict
        var scope = new ApiScope("payment");

        var profile1 = new ProfileEntity(
            scope,
            ImmutableDictionary<ConfigKey, ConfigValue>.Empty
                .Add(new ConfigKey("timeout"), ConfigValue.Create("30s")));

        var profile2 = new ProfileEntity(
            scope,
            ImmutableDictionary<ConfigKey, ConfigValue>.Empty
                .Add(new ConfigKey("timeout"), ConfigValue.Create("30s")));

        var profiles = new[] { profile1, profile2 };

        // Act
        Action act = () => ConflictHandler.ValidateNoConflicts(profiles);

        // Assert - Should not throw
        act.Should().NotThrow();
    }

    [Fact]
    public void DetectConflicts_DifferentScopes_SameKey_DifferentValues_NoConflict()
    {
        // Arrange - Different scopes = precedence resolves, no conflict
        var profile1 = new ProfileEntity(
            new ApiScope("payment"),
            ImmutableDictionary<ConfigKey, ConfigValue>.Empty
                .Add(new ConfigKey("timeout"), ConfigValue.Create("30s")));

        var profile2 = new ProfileEntity(
            new EnvironmentScope("prod"),
            ImmutableDictionary<ConfigKey, ConfigValue>.Empty
                .Add(new ConfigKey("timeout"), ConfigValue.Create("60s")));

        var profiles = new[] { profile1, profile2 };

        // Act
        Action act = () => ConflictHandler.ValidateNoConflicts(profiles);

        // Assert - Should not throw (different scopes)
        act.Should().NotThrow();
    }

    [Fact]
    public void DetectConflicts_MultipleConflicts_ReportsAll()
    {
        // Arrange
        var scope = new ApiScope("payment");

        var profile1 = new ProfileEntity(
            scope,
            ImmutableDictionary<ConfigKey, ConfigValue>.Empty
                .Add(new ConfigKey("timeout"), ConfigValue.Create("30s"))
                .Add(new ConfigKey("retries"), ConfigValue.Create(3)));

        var profile2 = new ProfileEntity(
            scope,
            ImmutableDictionary<ConfigKey, ConfigValue>.Empty
                .Add(new ConfigKey("timeout"), ConfigValue.Create("60s"))
                .Add(new ConfigKey("retries"), ConfigValue.Create(5)));

        var profiles = new[] { profile1, profile2 };

        // Act
        Action act = () => ConflictHandler.ValidateNoConflicts(profiles);

        // Assert - Both conflicts reported
        var exception = act.Should().Throw<ConfigurationConflictException>().Which;
        exception.Conflicts.Should().HaveCount(2);
        exception.ToString().Should().Contain("timeout").And.Contain("retries");
    }

    [Fact]
    public void DetectConflicts_GlobalScope_MultipleProfiles_ThrowsConflict()
    {
        // Arrange - Multiple global profiles with conflicting keys
        var profile1 = new ProfileEntity(
            GlobalScope.Instance,
            ImmutableDictionary<ConfigKey, ConfigValue>.Empty
                .Add(new ConfigKey("timeout"), ConfigValue.Create("30s")));

        var profile2 = new ProfileEntity(
            GlobalScope.Instance,
            ImmutableDictionary<ConfigKey, ConfigValue>.Empty
                .Add(new ConfigKey("timeout"), ConfigValue.Create("60s")));

        var profiles = new[] { profile1, profile2 };

        // Act
        Action act = () => ConflictHandler.ValidateNoConflicts(profiles);

        // Assert
        var exception = act.Should().Throw<ConfigurationConflictException>().Which;
        exception.ToString().Should().Contain("timeout");
    }

    [Fact]
    public void DetectConflicts_CompositeScope_ConflictsDetected()
    {
        // Arrange
        var apiScope = new ApiScope("payment");
        var envScope = new EnvironmentScope("prod");
        var compositeScope = new CompositeScope(apiScope, envScope);

        var profile1 = new ProfileEntity(
            compositeScope,
            ImmutableDictionary<ConfigKey, ConfigValue>.Empty
                .Add(new ConfigKey("timeout"), ConfigValue.Create("100s")));

        var profile2 = new ProfileEntity(
            compositeScope,
            ImmutableDictionary<ConfigKey, ConfigValue>.Empty
                .Add(new ConfigKey("timeout"), ConfigValue.Create("150s")));

        var profiles = new[] { profile1, profile2 };

        // Act
        Action act = () => ConflictHandler.ValidateNoConflicts(profiles);

        // Assert
        var exception = act.Should().Throw<ConfigurationConflictException>().Which;
        exception.ToString().Should().Contain("timeout");
    }

    [Fact]
    public void DetectConflicts_EmptyProfiles_NoConflict()
    {
        // Arrange
        var profiles = Array.Empty<ProfileEntity>();

        // Act
        Action act = () => ConflictHandler.ValidateNoConflicts(profiles);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void DetectConflicts_SingleProfile_NoConflict()
    {
        // Arrange
        var profile = new ProfileEntity(
            GlobalScope.Instance,
            ImmutableDictionary<ConfigKey, ConfigValue>.Empty
                .Add(new ConfigKey("timeout"), ConfigValue.Create("30s")));

        var profiles = new[] { profile };

        // Act
        Action act = () => ConflictHandler.ValidateNoConflicts(profiles);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void DetectConflicts_DifferentKeys_NoConflict()
    {
        // Arrange - Same scope, different keys
        var scope = new ApiScope("payment");

        var profile1 = new ProfileEntity(
            scope,
            ImmutableDictionary<ConfigKey, ConfigValue>.Empty
                .Add(new ConfigKey("timeout"), ConfigValue.Create("30s")));

        var profile2 = new ProfileEntity(
            scope,
            ImmutableDictionary<ConfigKey, ConfigValue>.Empty
                .Add(new ConfigKey("retries"), ConfigValue.Create(3)));

        var profiles = new[] { profile1, profile2 };

        // Act
        Action act = () => ConflictHandler.ValidateNoConflicts(profiles);

        // Assert - Different keys = no conflict
        act.Should().NotThrow();
    }

    [Fact]
    public void DetectConflicts_TypeMismatch_IsConflict()
    {
        // Arrange - Same key, same string value but different types
        var scope = new ApiScope("payment");

        var profile1 = new ProfileEntity(
            scope,
            ImmutableDictionary<ConfigKey, ConfigValue>.Empty
                .Add(new ConfigKey("value"), ConfigValue.Create("30")));

        var profile2 = new ProfileEntity(
            scope,
            ImmutableDictionary<ConfigKey, ConfigValue>.Empty
                .Add(new ConfigKey("value"), ConfigValue.Create(30)));

        var profiles = new[] { profile1, profile2 };

        // Act
        Action act = () => ConflictHandler.ValidateNoConflicts(profiles);

        // Assert - Different types = conflict
        act.Should().Throw<ConfigurationConflictException>();
    }
}
