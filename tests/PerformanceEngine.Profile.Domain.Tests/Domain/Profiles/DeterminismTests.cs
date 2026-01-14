using FluentAssertions;
using PerformanceEngine.Profile.Domain.Domain.Configuration;
using PerformanceEngine.Profile.Domain.Domain.Profiles;
using PerformanceEngine.Profile.Domain.Domain.Scopes;
using PerformanceEngine.Profile.Domain.Tests.Determinism;
using System.Collections.Immutable;

namespace PerformanceEngine.Profile.Domain.Tests.Domain.Profiles;

/// <summary>
/// Verifies deterministic behavior of scope resolution:
/// - Same inputs always produce identical outputs
/// - Resolution is independent of profile order
/// - Serialization is byte-identical across runs
/// </summary>
public class DeterminismTests : DeterminismTestBase
{
    [Fact]
    public void Resolve_WithGlobalScope_IsDeterministic()
    {
        // Arrange
        var config = ImmutableDictionary<ConfigKey, ConfigValue>.Empty
            .Add(new ConfigKey("timeout"), ConfigValue.Create("30s"))
            .Add(new ConfigKey("retries"), ConfigValue.Create(3));

        var profile = new ProfileEntity(GlobalScope.Instance, config);
        var profiles = new[] { profile };

        // Act & Assert - 1000 iterations should produce identical results
        AssertDeterministic(
            () => ProfileResolver.Resolve(profiles, GlobalScope.Instance),
            iterations: 1000,
            resultDescription: "GlobalScope resolution");
    }

    [Fact]
    public void Resolve_WithMultipleScopes_IsDeterministic()
    {
        // Arrange
        var globalConfig = ImmutableDictionary<ConfigKey, ConfigValue>.Empty
            .Add(new ConfigKey("timeout"), ConfigValue.Create("30s"))
            .Add(new ConfigKey("retries"), ConfigValue.Create(3));

        var apiConfig = ImmutableDictionary<ConfigKey, ConfigValue>.Empty
            .Add(new ConfigKey("timeout"), ConfigValue.Create("60s"));

        var envConfig = ImmutableDictionary<ConfigKey, ConfigValue>.Empty
            .Add(new ConfigKey("timeout"), ConfigValue.Create("90s"))
            .Add(new ConfigKey("ramp"), ConfigValue.Create("2m"));

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

        // Act & Assert - 1000 iterations should produce identical results
        AssertDeterministic(
            () => ProfileResolver.Resolve(profiles, requestedScopes),
            iterations: 1000,
            resultDescription: "Multi-scope resolution");
    }

    [Fact]
    public void Resolve_WithDifferentProfileOrder_ProducesIdenticalResults()
    {
        // Arrange
        var globalConfig = ImmutableDictionary<ConfigKey, ConfigValue>.Empty
            .Add(new ConfigKey("timeout"), ConfigValue.Create("30s"))
            .Add(new ConfigKey("retries"), ConfigValue.Create(3));

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

        var requestedScope = new ApiScope("payment");

        // Act & Assert - different orderings should produce identical results
        AssertOrderIndependentDeterminism(
            profiles,
            p => ProfileResolver.Resolve(p, requestedScope),
            permutations: 100,
            resultDescription: "Profile resolution");
    }

    [Fact]
    public void Resolve_WithCompositeScope_IsDeterministic()
    {
        // Arrange
        var globalConfig = ImmutableDictionary<ConfigKey, ConfigValue>.Empty
            .Add(new ConfigKey("timeout"), ConfigValue.Create("30s"));

        var compositeConfig = ImmutableDictionary<ConfigKey, ConfigValue>.Empty
            .Add(new ConfigKey("timeout"), ConfigValue.Create("120s"))
            .Add(new ConfigKey("retries"), ConfigValue.Create(5));

        var apiScope = new ApiScope("payment");
        var envScope = new EnvironmentScope("prod");
        var compositeScope = new CompositeScope(apiScope, envScope);

        var profiles = new[]
        {
            new ProfileEntity(GlobalScope.Instance, globalConfig),
            new ProfileEntity(compositeScope, compositeConfig)
        };

        var requestedScopes = new IScope[] { apiScope, envScope };

        // Act & Assert
        AssertDeterministic(
            () => ProfileResolver.Resolve(profiles, requestedScopes),
            iterations: 1000,
            resultDescription: "CompositeScope resolution");
    }

    [Fact]
    public void Resolve_MultipleRuns_ProduceByteIdenticalResults()
    {
        // Arrange
        var globalConfig = ImmutableDictionary<ConfigKey, ConfigValue>.Empty
            .Add(new ConfigKey("timeout"), ConfigValue.Create("30s"))
            .Add(new ConfigKey("retries"), ConfigValue.Create(3))
            .Add(new ConfigKey("ramp"), ConfigValue.Create("1m"));

        var apiConfig = ImmutableDictionary<ConfigKey, ConfigValue>.Empty
            .Add(new ConfigKey("timeout"), ConfigValue.Create("60s"));

        var profiles = new[]
        {
            new ProfileEntity(GlobalScope.Instance, globalConfig),
            new ProfileEntity(new ApiScope("payment"), apiConfig)
        };

        var requestedScope = new ApiScope("payment");

        // Act
        var result1 = ProfileResolver.Resolve(profiles, requestedScope);
        var result2 = ProfileResolver.Resolve(profiles, requestedScope);

        // Assert
        AssertByteIdentical(result1, result2, "Consecutive resolutions");
    }

    [Fact]
    public void Resolve_WithComplexHierarchy_MaintainsDeterminism()
    {
        // Arrange - 10 different scopes
        var profiles = new List<ProfileEntity>();

        // Global
        profiles.Add(new ProfileEntity(
            GlobalScope.Instance,
            ImmutableDictionary<ConfigKey, ConfigValue>.Empty
                .Add(new ConfigKey("timeout"), ConfigValue.Create("30s"))
                .Add(new ConfigKey("retries"), ConfigValue.Create(3))));

        // 3 API scopes
        foreach (var api in new[] { "payment", "search", "auth" })
        {
            profiles.Add(new ProfileEntity(
                new ApiScope(api),
                ImmutableDictionary<ConfigKey, ConfigValue>.Empty
                    .Add(new ConfigKey("timeout"), ConfigValue.Create("45s"))));
        }

        // 3 Environment scopes
        foreach (var env in new[] { "dev", "staging", "prod" })
        {
            profiles.Add(new ProfileEntity(
                new EnvironmentScope(env),
                ImmutableDictionary<ConfigKey, ConfigValue>.Empty
                    .Add(new ConfigKey("ramp"), ConfigValue.Create("2m"))));
        }

        // 3 Tag scopes
        foreach (var tag in new[] { "performance", "stress", "load" })
        {
            profiles.Add(new ProfileEntity(
                new TagScope(tag),
                ImmutableDictionary<ConfigKey, ConfigValue>.Empty
                    .Add(new ConfigKey("workers"), ConfigValue.Create(10))));
        }

        var requestedScopes = new IScope[]
        {
            new ApiScope("payment"),
            new EnvironmentScope("prod"),
            new TagScope("performance")
        };

        // Act & Assert - complex hierarchy should still be deterministic
        AssertDeterministic(
            () => ProfileResolver.Resolve(profiles, requestedScopes),
            iterations: 1000,
            resultDescription: "Complex hierarchy resolution");
    }

    [Fact]
    public void Resolve_WithPartialOverrides_IsDeterministic()
    {
        // Arrange - some keys only in global, some overridden
        var globalConfig = ImmutableDictionary<ConfigKey, ConfigValue>.Empty
            .Add(new ConfigKey("timeout"), ConfigValue.Create("30s"))
            .Add(new ConfigKey("retries"), ConfigValue.Create(3))
            .Add(new ConfigKey("ramp"), ConfigValue.Create("1m"))
            .Add(new ConfigKey("workers"), ConfigValue.Create(5))
            .Add(new ConfigKey("duration"), ConfigValue.Create("10m"));

        var apiConfig = ImmutableDictionary<ConfigKey, ConfigValue>.Empty
            .Add(new ConfigKey("timeout"), ConfigValue.Create("60s"))
            .Add(new ConfigKey("retries"), ConfigValue.Create(5));

        var profiles = new[]
        {
            new ProfileEntity(GlobalScope.Instance, globalConfig),
            new ProfileEntity(new ApiScope("payment"), apiConfig)
        };

        var requestedScope = new ApiScope("payment");

        // Act & Assert
        AssertDeterministic(
            () => ProfileResolver.Resolve(profiles, requestedScope),
            iterations: 1000,
            resultDescription: "Partial override resolution");
    }
}
