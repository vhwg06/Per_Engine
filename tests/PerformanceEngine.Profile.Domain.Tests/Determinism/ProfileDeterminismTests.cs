namespace PerformanceEngine.Profile.Domain.Tests.Determinism;

using PerformanceEngine.Profile.Domain.Application.Profile;
using PerformanceEngine.Profile.Domain.Domain.Profiles;
using PerformanceEngine.Profile.Domain.Domain.Scopes;
using PerformanceEngine.Profile.Domain.Domain.Configuration;
using System.Text.Json;
using Xunit;

/// <summary>
/// Determinism verification tests for Profile resolution.
/// Verifies byte-identical JSON serialization across 1000+ iterations and order permutations.
/// </summary>
public class ProfileDeterminismTests
{
    private readonly PerformanceEngine.Profile.Domain.Application.Profile.ProfileResolver _resolver = 
        new PerformanceEngine.Profile.Domain.Application.Profile.ProfileResolver();

    [Fact]
    public void Resolve_OrderIndependence_SameProfileDifferentInputOrders()
    {
        var profileObj = Profile.Create(GlobalScope.Instance, new Dictionary<ConfigKey, ConfigValue>());
        var overrides = new[]
        {
            ("global", "timeout", (object)5000),
            ("api", "retries", (object)3),
            ("endpoint", "circuit-breaker", (object)true),
            ("global", "debug", (object)false)
        };

        // Resolve in original order
        var result1 = _resolver.Resolve(profileObj, overrides);

        // Resolve in reverse order
        var result2 = _resolver.Resolve(profileObj, overrides.AsEnumerable().Reverse().ToList());

        // Resolve in shuffled order
        var shuffled = overrides.OrderBy(_ => Guid.NewGuid()).ToList();
        var result3 = _resolver.Resolve(profileObj, shuffled);

        Assert.Equal(result1, result2);
        Assert.Equal(result1, result3);
    }

    [Fact]
    public void Resolve_JsonSerialization_DeterministicAcrosIterations()
    {
        var profileObj = Profile.Create(GlobalScope.Instance, new Dictionary<ConfigKey, ConfigValue>());
        var overrides = new[]
        {
            ("global", "setting-a", (object)"value-a"),
            ("api", "setting-b", (object)123),
            ("endpoint", "setting-c", (object)true)
        };

        var jsons = new List<string>();

        for (int i = 0; i < 100; i++)
        {
            var result = _resolver.Resolve(profileObj, overrides);
            var json = JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = false });
            jsons.Add(json);
        }

        // All JSONs should be identical
        var firstJson = jsons[0];
        foreach (var json in jsons.Skip(1))
        {
            Assert.Equal(firstJson, json);
        }
    }

    [Fact]
    public void Resolve_1000Iterations_IdenticalOutput()
    {
        var profileObj = Profile.Create(GlobalScope.Instance, new Dictionary<ConfigKey, ConfigValue>());
        var overrides = new[]
        {
            ("global", "k1", (object)"v1"),
            ("global", "k2", (object)"v2"),
            ("api", "k3", (object)"v3"),
            ("api", "k4", (object)"v4"),
            ("endpoint", "k5", (object)"v5")
        };

        var firstResult = _resolver.Resolve(profileObj, overrides);

        for (int i = 0; i < 1000; i++)
        {
            var result = _resolver.Resolve(profileObj, overrides);
            Assert.Equal(firstResult, result);
        }
    }

    [Fact]
    public void VerifyOrderIndependence_AllResolutionsIdentical()
    {
        var profileObj = Profile.Create(GlobalScope.Instance, new Dictionary<ConfigKey, ConfigValue>());
        var overrides = new[]
        {
            ("global", "g1", (object)"gv1"),
            ("api", "a1", (object)"av1"),
            ("endpoint", "e1", (object)"ev1")
        };

        var results = _resolver.VerifyOrderIndependence(profileObj, overrides);

        // All results from different orderings should be identical
        var firstResult = results[0];
        foreach (var result in results.Skip(1))
        {
            Assert.Equal(firstResult, result);
        }
    }

    [Fact]
    public void Resolve_ScopePriorityConsistency_1000Iterations()
    {
        var profileObj = Profile.Create(GlobalScope.Instance, new Dictionary<ConfigKey, ConfigValue>());
        var overrides = new[]
        {
            ("global", "shared-key", (object)"global-value"),
            ("api", "shared-key", (object)"api-value"),
            ("endpoint", "shared-key", (object)"endpoint-value")
        };

        for (int i = 0; i < 1000; i++)
        {
            var result = _resolver.Resolve(profileObj, overrides);
            // Endpoint should always win (highest priority)
            Assert.Equal("endpoint-value", result["shared-key"]);
        }
    }
}
