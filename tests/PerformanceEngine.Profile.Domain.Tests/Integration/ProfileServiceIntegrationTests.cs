using FluentAssertions;
using PerformanceEngine.Profile.Domain.Application.Services;
using PerformanceEngine.Profile.Domain.Domain.Configuration;
using PerformanceEngine.Profile.Domain.Domain.Profiles;
using PerformanceEngine.Profile.Domain.Domain.Scopes;
using System.Collections.Immutable;

namespace PerformanceEngine.Profile.Domain.Tests.Integration;

/// <summary>
/// End-to-end integration tests for ProfileService.
/// Tests full resolution pipeline with conflict handling, DTOs, and error scenarios.
/// </summary>
public class ProfileServiceIntegrationTests
{
    [Fact]
    public void ProfileService_Resolve_EndToEnd_Success()
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

        var service = new ProfileService();

        // Act
        var result = service.Resolve(profiles, new ApiScope("payment"));

        // Assert
        result.Should().NotBeNull();
        result.Configuration.Should().HaveCount(2);
        result.Configuration[new ConfigKey("timeout")].Value.Should().Be("60s");
        result.Configuration[new ConfigKey("retries")].Value.Should().Be(3);
        result.AuditTrail.Should().NotBeEmpty();
    }

    [Fact]
    public void ProfileService_DetectsConflicts_ThrowsException()
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
        var service = new ProfileService();

        // Act
        Action act = () => service.Resolve(profiles, scope);

        // Assert - ProfileService wraps ConfigurationConflictException in InvalidOperationException
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void ProfileService_ResolveMultipleTimes_Deterministic()
    {
        // Arrange
        var globalConfig = ImmutableDictionary<ConfigKey, ConfigValue>.Empty
            .Add(new ConfigKey("timeout"), ConfigValue.Create("30s"));

        var profiles = new[]
        {
            new ProfileEntity(GlobalScope.Instance, globalConfig)
        };

        var service = new ProfileService();

        // Act - Resolve 10 times
        var results = new List<ResolvedProfile>();
        for (int i = 0; i < 10; i++)
        {
            results.Add(service.Resolve(profiles, GlobalScope.Instance));
        }

        // Assert - All results should be identical (ignoring ResolvedAt timestamp)
        foreach (var result in results)
        {
            result.Configuration[new ConfigKey("timeout")].Value.Should().Be("30s");
        }
    }

    [Fact]
    public void ProfileService_EmptyProfiles_ReturnsEmptyConfiguration()
    {
        // Arrange
        var profiles = Array.Empty<ProfileEntity>();
        var service = new ProfileService();

        // Act
        var result = service.Resolve(profiles, GlobalScope.Instance);

        // Assert
        result.Configuration.Should().BeEmpty();
        result.AuditTrail.Should().BeEmpty();
    }

    [Fact]
    public void ProfileService_MultiDimensional_Integration()
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
            new ProfileEntity(new TagScope("performance"), tagConfig),
            new ProfileEntity(compositeScope, compositeConfig)
        };

        var service = new ProfileService();
        var requestedScopes = new IScope[] { apiScope, envScope, new TagScope("performance") };

        // Act
        var result = service.Resolve(profiles, requestedScopes);

        // Assert - Composite wins for timeout
        result.Configuration[new ConfigKey("timeout")].Value.Should().Be("120s");
        result.Configuration[new ConfigKey("retries")].Value.Should().Be(3);
        result.Configuration[new ConfigKey("ramp")].Value.Should().Be("2m");
        result.Configuration[new ConfigKey("workers")].Value.Should().Be(10);
    }

    [Fact]
    public void ProfileService_WithScopeFactory_Integration()
    {
        // Arrange
        var globalConfig = ImmutableDictionary<ConfigKey, ConfigValue>.Empty
            .Add(new ConfigKey("timeout"), ConfigValue.Create("30s"));

        var apiConfig = ImmutableDictionary<ConfigKey, ConfigValue>.Empty
            .Add(new ConfigKey("timeout"), ConfigValue.Create("60s"));

        var profiles = new[]
        {
            new ProfileEntity(ScopeFactory.CreateGlobal(), globalConfig),
            new ProfileEntity(ScopeFactory.CreateApi("payment"), apiConfig)
        };

        var service = new ProfileService();

        // Act
        var result = service.Resolve(profiles, ScopeFactory.CreateApi("payment"));

        // Assert
        result.Configuration[new ConfigKey("timeout")].Value.Should().Be("60s");
    }
}
