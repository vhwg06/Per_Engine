using FluentAssertions;
using PerformanceEngine.Profile.Domain.Domain.Configuration;
using PerformanceEngine.Profile.Domain.Domain.Profiles;
using PerformanceEngine.Profile.Domain.Domain.Scopes;
using System.Collections.Immutable;

namespace PerformanceEngine.Profile.Domain.Tests.Domain.Profiles;

/// <summary>
/// Tests multi-dimensional profile resolution (API + Environment + Tag combinations).
/// Verifies precedence ordering, most-specific-wins logic, and partial overrides.
/// </summary>
public class MultiDimensionalTests
{
    [Fact]
    public void Resolve_GlobalOnly_AppliesGlobalConfig()
    {
        // Arrange
        var globalConfig = ImmutableDictionary<ConfigKey, ConfigValue>.Empty
            .Add(new ConfigKey("timeout"), ConfigValue.Create("30s"))
            .Add(new ConfigKey("retries"), ConfigValue.Create(3));

        var profiles = new[]
        {
            new ProfileEntity(GlobalScope.Instance, globalConfig)
        };

        // Act
        var result = ProfileResolver.Resolve(profiles, GlobalScope.Instance);

        // Assert
        result.Configuration.Should().HaveCount(2);
        result.Configuration[new ConfigKey("timeout")].Value.Should().Be("30s");
        result.Configuration[new ConfigKey("retries")].Value.Should().Be(3);
    }

    [Fact]
    public void Resolve_ApiOverridesGlobal_HigherPrecedenceWins()
    {
        // Arrange
        var globalConfig = ImmutableDictionary<ConfigKey, ConfigValue>.Empty
            .Add(new ConfigKey("timeout"), ConfigValue.Create("30s"))
            .Add(new ConfigKey("retries"), ConfigValue.Create(3));

        var apiConfig = ImmutableDictionary<ConfigKey, ConfigValue>.Empty
            .Add(new ConfigKey("timeout"), ConfigValue.Create("60s"));

        var profiles = new[]
        {
            new ProfileEntity(GlobalScope.Instance, globalConfig),
            new ProfileEntity(new ApiScope("payment"), apiConfig)
        };

        // Act
        var result = ProfileResolver.Resolve(profiles, new ApiScope("payment"));

        // Assert
        result.Configuration.Should().HaveCount(2);
        result.Configuration[new ConfigKey("timeout")].Value.Should().Be("60s"); // API overrides
        result.Configuration[new ConfigKey("retries")].Value.Should().Be(3); // Global fallback
    }

    [Fact]
    public void Resolve_EnvironmentOverridesApi_PrecedenceOrder()
    {
        // Arrange
        var globalConfig = ImmutableDictionary<ConfigKey, ConfigValue>.Empty
            .Add(new ConfigKey("timeout"), ConfigValue.Create("30s"));

        var apiConfig = ImmutableDictionary<ConfigKey, ConfigValue>.Empty
            .Add(new ConfigKey("timeout"), ConfigValue.Create("60s"));

        var envConfig = ImmutableDictionary<ConfigKey, ConfigValue>.Empty
            .Add(new ConfigKey("timeout"), ConfigValue.Create("90s"));

        var profiles = new[]
        {
            new ProfileEntity(GlobalScope.Instance, globalConfig),
            new ProfileEntity(new ApiScope("payment"), apiConfig),
            new ProfileEntity(new EnvironmentScope("staging"), envConfig)
        };

        var requestedScopes = new IScope[]
        {
            new ApiScope("payment"),
            new EnvironmentScope("staging")
        };

        // Act
        var result = ProfileResolver.Resolve(profiles, requestedScopes);

        // Assert - Environment (precedence 15) beats Api (precedence 10)
        result.Configuration[new ConfigKey("timeout")].Value.Should().Be("90s");
    }

    [Fact]
    public void Resolve_CompositeScopeHighestPrecedence_OverridesAll()
    {
        // Arrange
        var globalConfig = ImmutableDictionary<ConfigKey, ConfigValue>.Empty
            .Add(new ConfigKey("timeout"), ConfigValue.Create("30s"));

        var apiConfig = ImmutableDictionary<ConfigKey, ConfigValue>.Empty
            .Add(new ConfigKey("timeout"), ConfigValue.Create("60s"));

        var envConfig = ImmutableDictionary<ConfigKey, ConfigValue>.Empty
            .Add(new ConfigKey("timeout"), ConfigValue.Create("90s"));

        var apiScope = new ApiScope("payment");
        var envScope = new EnvironmentScope("prod");
        var compositeScope = new CompositeScope(apiScope, envScope);

        var compositeConfig = ImmutableDictionary<ConfigKey, ConfigValue>.Empty
            .Add(new ConfigKey("timeout"), ConfigValue.Create("120s"));

        var profiles = new[]
        {
            new ProfileEntity(GlobalScope.Instance, globalConfig),
            new ProfileEntity(apiScope, apiConfig),
            new ProfileEntity(envScope, envConfig),
            new ProfileEntity(compositeScope, compositeConfig)
        };

        var requestedScopes = new IScope[] { apiScope, envScope };

        // Act
        var result = ProfileResolver.Resolve(profiles, requestedScopes);

        // Assert - Composite (precedence 20 = max(10,15)+5) beats all
        result.Configuration[new ConfigKey("timeout")].Value.Should().Be("120s");
    }

    [Fact]
    public void Resolve_ThreeDimensions_ApiEnvironmentTag_MergesCorrectly()
    {
        // Arrange
        var globalConfig = ImmutableDictionary<ConfigKey, ConfigValue>.Empty
            .Add(new ConfigKey("timeout"), ConfigValue.Create("30s"))
            .Add(new ConfigKey("retries"), ConfigValue.Create(3))
            .Add(new ConfigKey("ramp"), ConfigValue.Create("1m"));

        var apiConfig = ImmutableDictionary<ConfigKey, ConfigValue>.Empty
            .Add(new ConfigKey("timeout"), ConfigValue.Create("60s"));

        var envConfig = ImmutableDictionary<ConfigKey, ConfigValue>.Empty
            .Add(new ConfigKey("ramp"), ConfigValue.Create("2m"));

        var tagConfig = ImmutableDictionary<ConfigKey, ConfigValue>.Empty
            .Add(new ConfigKey("workers"), ConfigValue.Create(10));

        var profiles = new[]
        {
            new ProfileEntity(GlobalScope.Instance, globalConfig),
            new ProfileEntity(new ApiScope("payment"), apiConfig),
            new ProfileEntity(new EnvironmentScope("staging"), envConfig),
            new ProfileEntity(new TagScope("performance"), tagConfig)
        };

        var requestedScopes = new IScope[]
        {
            new ApiScope("payment"),
            new EnvironmentScope("staging"),
            new TagScope("performance")
        };

        // Act
        var result = ProfileResolver.Resolve(profiles, requestedScopes);

        // Assert
        result.Configuration.Should().HaveCount(4);
        result.Configuration[new ConfigKey("timeout")].Value.Should().Be("60s"); // API override
        result.Configuration[new ConfigKey("retries")].Value.Should().Be(3); // Global fallback
        result.Configuration[new ConfigKey("ramp")].Value.Should().Be("2m"); // Environment override
        result.Configuration[new ConfigKey("workers")].Value.Should().Be(10); // Tag adds new key
    }

    [Fact]
    public void Resolve_PartialDimensionMatch_AppliesOnlyMatchingScopes()
    {
        // Arrange
        var globalConfig = ImmutableDictionary<ConfigKey, ConfigValue>.Empty
            .Add(new ConfigKey("timeout"), ConfigValue.Create("30s"));

        var apiConfig = ImmutableDictionary<ConfigKey, ConfigValue>.Empty
            .Add(new ConfigKey("timeout"), ConfigValue.Create("60s"));

        var envConfig = ImmutableDictionary<ConfigKey, ConfigValue>.Empty
            .Add(new ConfigKey("timeout"), ConfigValue.Create("90s"));

        var profiles = new[]
        {
            new ProfileEntity(GlobalScope.Instance, globalConfig),
            new ProfileEntity(new ApiScope("payment"), apiConfig),
            new ProfileEntity(new EnvironmentScope("prod"), envConfig) // prod, but we'll request staging
        };

        var requestedScopes = new IScope[]
        {
            new ApiScope("payment"),
            new EnvironmentScope("staging") // Different environment
        };

        // Act
        var result = ProfileResolver.Resolve(profiles, requestedScopes);

        // Assert - Environment profile should NOT apply (wrong env)
        result.Configuration[new ConfigKey("timeout")].Value.Should().Be("60s"); // API wins, not env
    }

    [Fact]
    public void Resolve_MultipleCompositeScopes_HighestPrecedenceWins()
    {
        // Arrange
        var globalConfig = ImmutableDictionary<ConfigKey, ConfigValue>.Empty
            .Add(new ConfigKey("timeout"), ConfigValue.Create("30s"));

        var apiScope = new ApiScope("payment");
        var envScope = new EnvironmentScope("prod");
        var tagScope = new TagScope("performance");

        // Composite 1: Api + Env (precedence = 15 + 5 = 20)
        var composite1 = new CompositeScope(apiScope, envScope);
        var composite1Config = ImmutableDictionary<ConfigKey, ConfigValue>.Empty
            .Add(new ConfigKey("timeout"), ConfigValue.Create("100s"));

        // Composite 2: Env + Tag (precedence = max(15, 20) + 5 = 25)
        var composite2 = new CompositeScope(envScope, tagScope);
        var composite2Config = ImmutableDictionary<ConfigKey, ConfigValue>.Empty
            .Add(new ConfigKey("timeout"), ConfigValue.Create("150s"));

        var profiles = new[]
        {
            new ProfileEntity(GlobalScope.Instance, globalConfig),
            new ProfileEntity(composite1, composite1Config),
            new ProfileEntity(composite2, composite2Config)
        };

        var requestedScopes = new IScope[] { apiScope, envScope, tagScope };

        // Act
        var result = ProfileResolver.Resolve(profiles, requestedScopes);

        // Assert - Composite2 (25) beats Composite1 (20)
        result.Configuration[new ConfigKey("timeout")].Value.Should().Be("150s");
    }

    [Fact]
    public void Resolve_AuditTrail_TracksAllAppliedScopes()
    {
        // Arrange
        var globalConfig = ImmutableDictionary<ConfigKey, ConfigValue>.Empty
            .Add(new ConfigKey("timeout"), ConfigValue.Create("30s"))
            .Add(new ConfigKey("retries"), ConfigValue.Create(3));

        var apiConfig = ImmutableDictionary<ConfigKey, ConfigValue>.Empty
            .Add(new ConfigKey("timeout"), ConfigValue.Create("60s"));

        var profiles = new[]
        {
            new ProfileEntity(GlobalScope.Instance, globalConfig),
            new ProfileEntity(new ApiScope("payment"), apiConfig)
        };

        // Act
        var result = ProfileResolver.Resolve(profiles, new ApiScope("payment"));

        // Assert - Audit trail should show which scope provided each key
        result.AuditTrail.Should().HaveCount(2);
        
        var timeoutScopes = result.AuditTrail[new ConfigKey("timeout")];
        timeoutScopes.Should().HaveCount(2); // Global and Api both set timeout
        timeoutScopes.Should().Contain(GlobalScope.Instance);
        timeoutScopes.Should().Contain(s => s is ApiScope);

        var retriesScopes = result.AuditTrail[new ConfigKey("retries")];
        retriesScopes.Should().HaveCount(1); // Only Global set retries
        retriesScopes.Should().Contain(GlobalScope.Instance);
    }

    [Fact]
    public void Resolve_TenDimensions_CorrectPrecedenceOrder()
    {
        // Arrange - Create 10 different scope types with varying precedence
        var profiles = new List<ProfileEntity>();

        var globalConfig = ImmutableDictionary<ConfigKey, ConfigValue>.Empty
            .Add(new ConfigKey("value"), ConfigValue.Create(0));
        profiles.Add(new ProfileEntity(GlobalScope.Instance, globalConfig));

        var api1Config = ImmutableDictionary<ConfigKey, ConfigValue>.Empty
            .Add(new ConfigKey("value"), ConfigValue.Create(10));
        profiles.Add(new ProfileEntity(new ApiScope("api1"), api1Config));

        var api2Config = ImmutableDictionary<ConfigKey, ConfigValue>.Empty
            .Add(new ConfigKey("value"), ConfigValue.Create(10));
        profiles.Add(new ProfileEntity(new ApiScope("api2"), api2Config));

        var envConfig = ImmutableDictionary<ConfigKey, ConfigValue>.Empty
            .Add(new ConfigKey("value"), ConfigValue.Create(15));
        profiles.Add(new ProfileEntity(new EnvironmentScope("prod"), envConfig));

        var tag1Config = ImmutableDictionary<ConfigKey, ConfigValue>.Empty
            .Add(new ConfigKey("value"), ConfigValue.Create(20));
        profiles.Add(new ProfileEntity(new TagScope("tag1"), tag1Config));

        var tag2Config = ImmutableDictionary<ConfigKey, ConfigValue>.Empty
            .Add(new ConfigKey("value"), ConfigValue.Create(20));
        profiles.Add(new ProfileEntity(new TagScope("tag2"), tag2Config));

        // Composite scopes (highest precedence)
        var composite1 = new CompositeScope(new ApiScope("api1"), new EnvironmentScope("prod"));
        var composite1Config = ImmutableDictionary<ConfigKey, ConfigValue>.Empty
            .Add(new ConfigKey("value"), ConfigValue.Create(20)); // max(10, 15) + 5 = 20
        profiles.Add(new ProfileEntity(composite1, composite1Config));

        var composite2 = new CompositeScope(new ApiScope("api2"), new TagScope("tag1"));
        var composite2Config = ImmutableDictionary<ConfigKey, ConfigValue>.Empty
            .Add(new ConfigKey("value"), ConfigValue.Create(25)); // max(10, 20) + 5 = 25
        profiles.Add(new ProfileEntity(composite2, composite2Config));

        var requestedScopes = new IScope[]
        {
            new ApiScope("api1"),
            new ApiScope("api2"),
            new EnvironmentScope("prod"),
            new TagScope("tag1"),
            new TagScope("tag2")
        };

        // Act
        var result = ProfileResolver.Resolve(profiles, requestedScopes);

        // Assert - Composite2 (precedence 25) should win
        result.Configuration[new ConfigKey("value")].Value.Should().Be(25);
    }

    [Fact]
    public void Resolve_EmptyRequestedScopes_UsesOnlyGlobal()
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

        // Act - No requested scopes means only Global applies
        var result = ProfileResolver.Resolve(profiles, Array.Empty<IScope>());

        // Assert - Only global config applied
        result.Configuration[new ConfigKey("timeout")].Value.Should().Be("30s");
    }
}
