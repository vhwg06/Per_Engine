using FluentAssertions;
using PerformanceEngine.Profile.Domain.Domain.Configuration;
using PerformanceEngine.Profile.Domain.Domain.Profiles;
using PerformanceEngine.Profile.Domain.Domain.Scopes;
using System.Collections.Immutable;

namespace PerformanceEngine.Profile.Domain.Tests.Domain;

/// <summary>
/// Tests edge cases:
/// - Null scopes/profiles
/// - Empty configuration dictionaries
/// - Extreme precedence values
/// - Single profile scenarios
/// - Circular composite scopes (should be prevented)
/// </summary>
public class EdgeCaseTests
{
    [Fact]
    public void Resolve_NullProfiles_ThrowsArgumentNullException()
    {
        // Act
        Action act = () => ProfileResolver.Resolve(null!, GlobalScope.Instance);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Resolve_NullScope_ThrowsArgumentNullException()
    {
        // Arrange
        var profiles = new[]
        {
            new ProfileEntity(
                GlobalScope.Instance,
                ImmutableDictionary<ConfigKey, ConfigValue>.Empty
                    .Add(new ConfigKey("timeout"), ConfigValue.Create("30s")))
        };

        // Act
        Action act = () => ProfileResolver.Resolve(profiles, (IEnumerable<IScope>)null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Resolve_EmptyProfiles_ReturnsEmptyConfiguration()
    {
        // Arrange
        var profiles = Array.Empty<ProfileEntity>();

        // Act
        var result = ProfileResolver.Resolve(profiles, GlobalScope.Instance);

        // Assert
        result.Configuration.Should().BeEmpty();
        result.AuditTrail.Should().BeEmpty();
    }

    [Fact]
    public void Resolve_EmptyConfigurationDictionary_ValidProfile()
    {
        // Arrange
        var profile = new ProfileEntity(
            GlobalScope.Instance,
            ImmutableDictionary<ConfigKey, ConfigValue>.Empty);

        var profiles = new[] { profile };

        // Act
        var result = ProfileResolver.Resolve(profiles, GlobalScope.Instance);

        // Assert
        result.Configuration.Should().BeEmpty();
    }

    [Fact]
    public void Resolve_SingleProfile_NoConflicts()
    {
        // Arrange
        var profile = new ProfileEntity(
            GlobalScope.Instance,
            ImmutableDictionary<ConfigKey, ConfigValue>.Empty
                .Add(new ConfigKey("timeout"), ConfigValue.Create("30s")));

        var profiles = new[] { profile };

        // Act
        var result = ProfileResolver.Resolve(profiles, GlobalScope.Instance);

        // Assert
        result.Configuration.Should().HaveCount(1);
        result.Configuration[new ConfigKey("timeout")].Value.Should().Be("30s");
    }

    [Fact]
    public void Resolve_VeryHighPrecedence_WorksCorrectly()
    {
        // Arrange
        var globalConfig = ImmutableDictionary<ConfigKey, ConfigValue>.Empty
            .Add(new ConfigKey("timeout"), ConfigValue.Create("30s"));

        var highPrecedenceScope = new TagScope("ultra-high", precedence: 10000);
        var highPrecedenceConfig = ImmutableDictionary<ConfigKey, ConfigValue>.Empty
            .Add(new ConfigKey("timeout"), ConfigValue.Create("999s"));

        var profiles = new[]
        {
            new ProfileEntity(GlobalScope.Instance, globalConfig),
            new ProfileEntity(highPrecedenceScope, highPrecedenceConfig)
        };

        // Act
        var result = ProfileResolver.Resolve(profiles, highPrecedenceScope);

        // Assert - High precedence wins
        result.Configuration[new ConfigKey("timeout")].Value.Should().Be("999s");
    }

    [Fact]
    public void Resolve_NegativePrecedence_WorksButUnusual()
    {
        // Arrange - Negative precedence (technically allowed, but unusual)
        var globalConfig = ImmutableDictionary<ConfigKey, ConfigValue>.Empty
            .Add(new ConfigKey("timeout"), ConfigValue.Create("30s"));

        var negativePrecedenceScope = new TagScope("negative", precedence: -10);
        var negativeConfig = ImmutableDictionary<ConfigKey, ConfigValue>.Empty
            .Add(new ConfigKey("timeout"), ConfigValue.Create("999s"));

        var profiles = new[]
        {
            new ProfileEntity(GlobalScope.Instance, globalConfig),
            new ProfileEntity(negativePrecedenceScope, negativeConfig)
        };

        // Act
        var result = ProfileResolver.Resolve(profiles, negativePrecedenceScope);

        // Assert - Global (0) beats negative (-10)
        result.Configuration[new ConfigKey("timeout")].Value.Should().Be("30s");
    }

    [Fact]
    public void CompositeScope_NestedComposite_PreventedByDesign()
    {
        // Arrange
        var apiScope = new ApiScope("payment");
        var envScope = new EnvironmentScope("prod");
        var composite1 = new CompositeScope(apiScope, envScope);

        // Act - Try to nest composites (should be prevented by design)
        Action act = () => new CompositeScope(composite1, new TagScope("test"));

        // Assert - Nesting is prevented to avoid complex precedence
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Resolve_SamePrecedence_LastAppliedWins()
    {
        // Arrange - Two API scopes (same precedence)
        var apiScope1 = new ApiScope("payment");
        var apiScope2 = new ApiScope("search");

        var config1 = ImmutableDictionary<ConfigKey, ConfigValue>.Empty
            .Add(new ConfigKey("timeout"), ConfigValue.Create("30s"));

        var config2 = ImmutableDictionary<ConfigKey, ConfigValue>.Empty
            .Add(new ConfigKey("timeout"), ConfigValue.Create("60s"));

        var profiles = new[]
        {
            new ProfileEntity(apiScope1, config1),
            new ProfileEntity(apiScope2, config2)
        };

        var requestedScopes = new IScope[] { apiScope1, apiScope2 };

        // Act
        var result = ProfileResolver.Resolve(profiles, requestedScopes);

        // Assert - Both match, same precedence, last applied wins
        result.Configuration[new ConfigKey("timeout")].Value.Should().Be("60s");
    }

    [Fact]
    public void ConfigValue_NullValue_ThrowsException()
    {
        // Act
        Action act = () => ConfigValue.Create(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ConfigKey_EmptyName_ThrowsException()
    {
        // Act
        Action act = () => new ConfigKey(string.Empty);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ConfigKey_NullName_ThrowsException()
    {
        // Act
        Action act = () => new ConfigKey(null!);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Profile_NullScope_ThrowsException()
    {
        // Arrange
        var config = ImmutableDictionary<ConfigKey, ConfigValue>.Empty
            .Add(new ConfigKey("timeout"), ConfigValue.Create("30s"));

        // Act
        Action act = () => new ProfileEntity(null!, config);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Resolve_RequestNonExistentScope_UsesOnlyGlobal()
    {
        // Arrange
        var globalConfig = ImmutableDictionary<ConfigKey, ConfigValue>.Empty
            .Add(new ConfigKey("timeout"), ConfigValue.Create("30s"));

        var apiConfig = ImmutableDictionary<ConfigKey, ConfigValue>.Empty
            .Add(new ConfigKey("timeout"), ConfigValue.Create("60s"));

        var profiles = new[]
        {
            new ProfileEntity(GlobalScope.Instance, globalConfig),
            new ProfileEntity(new ApiScope("payment"), apiConfig)
        };

        // Act - Request a different API that doesn't exist
        var result = ProfileResolver.Resolve(profiles, new ApiScope("search"));

        // Assert - Only global applies
        result.Configuration[new ConfigKey("timeout")].Value.Should().Be("30s");
    }

    [Fact]
    public void Resolve_HundredProfiles_PerformanceAcceptable()
    {
        // Arrange - 100 profiles
        var profiles = new List<ProfileEntity>();

        profiles.Add(new ProfileEntity(
            GlobalScope.Instance,
            ImmutableDictionary<ConfigKey, ConfigValue>.Empty
                .Add(new ConfigKey("timeout"), ConfigValue.Create("30s"))));

        for (int i = 1; i < 100; i++)
        {
            profiles.Add(new ProfileEntity(
                new ApiScope($"api{i}"),
                ImmutableDictionary<ConfigKey, ConfigValue>.Empty
                    .Add(new ConfigKey($"key{i}"), ConfigValue.Create($"value{i}"))));
        }

        // Act
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var result = ProfileResolver.Resolve(profiles, new ApiScope("api50"));
        stopwatch.Stop();

        // Assert - Should complete quickly
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(100);
        result.Configuration.Should().Contain(kv => kv.Key.Name == "key50");
    }
}
