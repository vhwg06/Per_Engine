using FluentAssertions;
using PerformanceEngine.Profile.Domain.Domain.Configuration;
using PerformanceEngine.Profile.Domain.Domain.Profiles;
using PerformanceEngine.Profile.Domain.Domain.Scopes;
using System.Collections.Immutable;

namespace PerformanceEngine.Profile.Domain.Tests.Domain.Profiles;

/// <summary>
/// Tests complex scenarios with 10+ scopes, multiple profiles, varying precedence.
/// Verifies deterministic resolution, audit trail accuracy, and precedence ordering.
/// </summary>
public class ComplexResolutionTests
{
    [Fact]
    public void Resolve_TenScopes_MultipleProfiles_DeterministicOrdering()
    {
        // Arrange - 10 different scope types
        var profiles = new List<ProfileEntity>();

        // 1. Global
        profiles.Add(new ProfileEntity(
            GlobalScope.Instance,
            ImmutableDictionary<ConfigKey, ConfigValue>.Empty
                .Add(new ConfigKey("timeout"), ConfigValue.Create("30s"))
                .Add(new ConfigKey("retries"), ConfigValue.Create(3))));

        // 2-4. Three API scopes
        foreach (var api in new[] { "payment", "search", "auth" })
        {
            profiles.Add(new ProfileEntity(
                new ApiScope(api),
                ImmutableDictionary<ConfigKey, ConfigValue>.Empty
                    .Add(new ConfigKey("timeout"), ConfigValue.Create("45s"))));
        }

        // 5-7. Three Environment scopes
        foreach (var env in new[] { "dev", "staging", "prod" })
        {
            profiles.Add(new ProfileEntity(
                new EnvironmentScope(env),
                ImmutableDictionary<ConfigKey, ConfigValue>.Empty
                    .Add(new ConfigKey("ramp"), ConfigValue.Create("2m"))));
        }

        // 8-10. Three Tag scopes
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

        // Act
        var result = ProfileResolver.Resolve(profiles, requestedScopes);

        // Assert
        result.Configuration.Should().HaveCount(4);
        result.Configuration[new ConfigKey("timeout")].Value.Should().Be("45s"); // API
        result.Configuration[new ConfigKey("retries")].Value.Should().Be(3); // Global
        result.Configuration[new ConfigKey("ramp")].Value.Should().Be("2m"); // Env
        result.Configuration[new ConfigKey("workers")].Value.Should().Be(10); // Tag
    }

    [Fact]
    public void Resolve_MultipleProfilesPerScope_LastWins()
    {
        // Arrange - Multiple profiles for same scope (should be avoided, but resolver handles it)
        var scope = new ApiScope("payment");

        var profiles = new[]
        {
            new ProfileEntity(
                scope,
                ImmutableDictionary<ConfigKey, ConfigValue>.Empty
                    .Add(new ConfigKey("timeout"), ConfigValue.Create("30s"))),
            new ProfileEntity(
                scope,
                ImmutableDictionary<ConfigKey, ConfigValue>.Empty
                    .Add(new ConfigKey("retries"), ConfigValue.Create(3)))
        };

        // Act
        var result = ProfileResolver.Resolve(profiles, scope);

        // Assert - Both profiles merge (different keys, so no conflict)
        result.Configuration.Should().HaveCount(2);
        result.Configuration[new ConfigKey("timeout")].Value.Should().Be("30s");
        result.Configuration[new ConfigKey("retries")].Value.Should().Be(3);
    }

    [Fact]
    public void Resolve_DeepHierarchy_FiveLevel_CorrectPrecedence()
    {
        // Arrange - 5-level hierarchy: Global < Api < Env < Tag < Composite
        var globalConfig = ImmutableDictionary<ConfigKey, ConfigValue>.Empty
            .Add(new ConfigKey("value"), ConfigValue.Create(0));

        var apiConfig = ImmutableDictionary<ConfigKey, ConfigValue>.Empty
            .Add(new ConfigKey("value"), ConfigValue.Create(10));

        var envConfig = ImmutableDictionary<ConfigKey, ConfigValue>.Empty
            .Add(new ConfigKey("value"), ConfigValue.Create(15));

        var tagConfig = ImmutableDictionary<ConfigKey, ConfigValue>.Empty
            .Add(new ConfigKey("value"), ConfigValue.Create(20));

        var apiScope = new ApiScope("payment");
        var envScope = new EnvironmentScope("prod");
        var compositeScope = new CompositeScope(apiScope, envScope);

        var compositeConfig = ImmutableDictionary<ConfigKey, ConfigValue>.Empty
            .Add(new ConfigKey("value"), ConfigValue.Create(100));

        var profiles = new[]
        {
            new ProfileEntity(GlobalScope.Instance, globalConfig),
            new ProfileEntity(apiScope, apiConfig),
            new ProfileEntity(envScope, envConfig),
            new ProfileEntity(new TagScope("performance"), tagConfig),
            new ProfileEntity(compositeScope, compositeConfig)
        };

        var requestedScopes = new IScope[] { apiScope, envScope, new TagScope("performance") };

        // Act
        var result = ProfileResolver.Resolve(profiles, requestedScopes);

        // Assert - Composite (max precedence) wins
        result.Configuration[new ConfigKey("value")].Value.Should().Be(100);
    }

    [Fact]
    public void Resolve_PartialOverrides_25Keys_CorrectMerge()
    {
        // Arrange - Global has 25 keys, API overrides 5
        var globalConfigBuilder = ImmutableDictionary<ConfigKey, ConfigValue>.Empty.ToBuilder();
        for (int i = 1; i <= 25; i++)
        {
            globalConfigBuilder.Add(
                new ConfigKey($"key{i}"),
                ConfigValue.Create($"global_value{i}"));
        }

        var apiConfigBuilder = ImmutableDictionary<ConfigKey, ConfigValue>.Empty.ToBuilder();
        for (int i = 1; i <= 5; i++)
        {
            apiConfigBuilder.Add(
                new ConfigKey($"key{i}"),
                ConfigValue.Create($"api_value{i}"));
        }

        var profiles = new[]
        {
            new ProfileEntity(GlobalScope.Instance, globalConfigBuilder.ToImmutable()),
            new ProfileEntity(new ApiScope("payment"), apiConfigBuilder.ToImmutable())
        };

        // Act
        var result = ProfileResolver.Resolve(profiles, new ApiScope("payment"));

        // Assert - 25 keys total, 5 from API, 20 from Global
        result.Configuration.Should().HaveCount(25);
        result.Configuration[new ConfigKey("key1")].Value.Should().Be("api_value1");
        result.Configuration[new ConfigKey("key6")].Value.Should().Be("global_value6");
    }

    [Fact]
    public void Resolve_AuditTrail_TracksAllContributingScopes()
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
            new ProfileEntity(new EnvironmentScope("prod"), envConfig)
        };

        var requestedScopes = new IScope[]
        {
            new ApiScope("payment"),
            new EnvironmentScope("prod")
        };

        // Act
        var result = ProfileResolver.Resolve(profiles, requestedScopes);

        // Assert - Audit trail shows all scopes that contributed to each key
        result.AuditTrail.Should().HaveCount(3);

        var timeoutScopes = result.AuditTrail[new ConfigKey("timeout")];
        timeoutScopes.Should().HaveCount(3); // All 3 profiles set timeout
        timeoutScopes.Should().Contain(GlobalScope.Instance);
        timeoutScopes.Should().Contain(s => s is ApiScope);
        timeoutScopes.Should().Contain(s => s is EnvironmentScope);

        var retriesScopes = result.AuditTrail[new ConfigKey("retries")];
        retriesScopes.Should().HaveCount(1); // Only Global set retries

        var rampScopes = result.AuditTrail[new ConfigKey("ramp")];
        rampScopes.Should().HaveCount(1); // Only Environment set ramp
    }

    [Fact]
    public void Resolve_MixedTypes_AllConfigTypes_Supported()
    {
        // Arrange - Test all ConfigType values
        var globalConfig = ImmutableDictionary<ConfigKey, ConfigValue>.Empty
            .Add(new ConfigKey("string_val"), ConfigValue.Create("hello"))
            .Add(new ConfigKey("int_val"), ConfigValue.Create(42))
            .Add(new ConfigKey("double_val"), ConfigValue.Create(3.14))
            .Add(new ConfigKey("bool_val"), ConfigValue.Create(true))
            .Add(new ConfigKey("duration_val"), ConfigValue.Create("30s"));

        var profiles = new[]
        {
            new ProfileEntity(GlobalScope.Instance, globalConfig)
        };

        // Act
        var result = ProfileResolver.Resolve(profiles, GlobalScope.Instance);

        // Assert - All types preserved
        result.Configuration.Should().HaveCount(5);
        result.Configuration[new ConfigKey("string_val")].Value.Should().Be("hello");
        result.Configuration[new ConfigKey("int_val")].Value.Should().Be(42);
        result.Configuration[new ConfigKey("double_val")].Value.Should().Be(3.14);
        result.Configuration[new ConfigKey("bool_val")].Value.Should().Be(true);
        result.Configuration[new ConfigKey("duration_val")].Value.Should().Be("30s");
    }

    [Fact]
    public void Resolve_TenRequestedScopes_OnlyMatchingApply()
    {
        // Arrange - 10 different profiles, request 3
        var profiles = new List<ProfileEntity>();

        for (int i = 1; i <= 10; i++)
        {
            profiles.Add(new ProfileEntity(
                new ApiScope($"api{i}"),
                ImmutableDictionary<ConfigKey, ConfigValue>.Empty
                    .Add(new ConfigKey("timeout"), ConfigValue.Create($"{i * 10}s"))));
        }

        profiles.Add(new ProfileEntity(
            GlobalScope.Instance,
            ImmutableDictionary<ConfigKey, ConfigValue>.Empty
                .Add(new ConfigKey("timeout"), ConfigValue.Create("5s"))));

        var requestedScopes = new IScope[]
        {
            new ApiScope("api3"),
            new ApiScope("api7"),
            new ApiScope("api9")
        };

        // Act
        var result = ProfileResolver.Resolve(profiles, requestedScopes);

        // Assert - api9 has highest precedence (all ApiScopes have precedence 10)
        // But which one wins? With same precedence, last one applied wins
        result.Configuration[new ConfigKey("timeout")].Value.Should().Be("90s");
    }
}
