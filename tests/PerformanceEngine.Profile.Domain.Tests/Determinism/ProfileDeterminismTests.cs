namespace PerformanceEngine.Profile.Domain.Tests.Determinism;

using PerformanceEngine.Profile.Domain.Application.Profile;
using PerformanceEngine.Profile.Domain.Domain;
using System.Text.Json;
using Xunit;

/// <summary>
/// Determinism verification tests for Profile resolution.
/// Verifies byte-identical JSON serialization across 1000+ iterations and order permutations.
/// </summary>
public class ProfileDeterminismTests
{
    private readonly ProfileResolver _resolver = new();

    [Fact]
    public void Resolve_OrderIndependence_SameProfileDifferentInputOrders()
    {
        var profile = new Profile(id: "determinism-test");
        var overrides = new[]
        {
            ("global", "timeout", (object)5000),
            ("api", "retries", (object)3),
            ("endpoint", "circuit-breaker", (object)true),
            ("global", "debug", (object)false)
        };

        // Resolve in original order
        var result1 = _resolver.Resolve(profile, overrides);

        // Resolve in reverse order
        var result2 = _resolver.Resolve(profile, overrides.Reverse().ToList());

        // Resolve in shuffled order
        var shuffled = overrides.OrderBy(_ => Guid.NewGuid()).ToList();
        var result3 = _resolver.Resolve(profile, shuffled);

        Assert.Equal(result1, result2);
        Assert.Equal(result1, result3);
    }

    [Fact]
    public void Resolve_JsonSerialization_DeterministicAcrosIterations()
    {
        var profile = new Profile(id: "json-test");
        var overrides = new[]
        {
            ("global", "setting-a", (object)"value-a"),
            ("api", "setting-b", (object)123),
            ("endpoint", "setting-c", (object)true)
        };

        var jsons = new List<string>();

        for (int i = 0; i < 100; i++)
        {
            var result = _resolver.Resolve(profile, overrides);
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
        var profile = new Profile(id: "1000-iteration-test");
        var overrides = new[]
        {
            ("global", "k1", (object)"v1"),
            ("global", "k2", (object)"v2"),
            ("api", "k3", (object)"v3"),
            ("api", "k4", (object)"v4"),
            ("endpoint", "k5", (object)"v5")
        };

        var firstResult = _resolver.Resolve(profile, overrides);

        for (int i = 0; i < 1000; i++)
        {
            var result = _resolver.Resolve(profile, overrides);
            Assert.Equal(firstResult, result);
        }
    }

    [Fact]
    public void VerifyOrderIndependence_AllResolutionsIdentical()
    {
        var profile = new Profile(id: "order-independence-test");
        var overrides = new[]
        {
            ("global", "g1", (object)"gv1"),
            ("api", "a1", (object)"av1"),
            ("endpoint", "e1", (object)"ev1")
        };

        var results = _resolver.VerifyOrderIndependence(profile, overrides);

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
        var profile = new Profile(id: "scope-priority-test");
        var overrides = new[]
        {
            ("global", "shared-key", (object)"global-value"),
            ("api", "shared-key", (object)"api-value"),
            ("endpoint", "shared-key", (object)"endpoint-value")
        };

        for (int i = 0; i < 1000; i++)
        {
            var result = _resolver.Resolve(profile, overrides);
            // Endpoint should always win (highest priority)
            Assert.Equal("endpoint-value", result["shared-key"]);
        }
    }

    [Fact]
    public void Resolve_KeyAlphabeticalConsistency()
    {
        var profile = new Profile(id: "key-alpha-test");
        var overrides = new[]
        {
            ("global", "zebra", (object)"z"),
            ("global", "apple", (object)"a"),
            ("global", "middle", (object)"m")
        };

        var results = new List<IReadOnlyDictionary<string, object>>();

        for (int i = 0; i < 100; i++)
        {
            var shuffled = overrides.OrderBy(_ => Guid.NewGuid()).ToList();
            var result = _resolver.Resolve(profile, shuffled);
            results.Add(result);
        }

        // All keys should maintain alphabetical order in result
        var firstKeys = results[0].Keys.ToList();
        Assert.Equal("apple", firstKeys[0]);
        Assert.Equal("middle", firstKeys[1]);
        Assert.Equal("zebra", firstKeys[2]);

        // Verify all results have same key order
        foreach (var result in results.Skip(1))
        {
            var keys = result.Keys.ToList();
            Assert.Equal(firstKeys, keys);
        }
    }

    [Fact]
    public void Resolve_ComplexScenario_DeterministicAcrossPermutations()
    {
        var profile = new Profile(id: "complex-test");
        var overrides = new[]
        {
            ("global", "timeout", (object)5000),
            ("global", "retry-count", (object)3),
            ("api", "timeout", (object)3000),
            ("api", "circuit-breaker", (object)true),
            ("endpoint", "timeout", (object)1000),
            ("endpoint", "debug", (object)false)
        };

        var results = new List<IReadOnlyDictionary<string, object>>();

        // Generate 100 permutations
        var random = new Random(seed: 42);
        for (int i = 0; i < 100; i++)
        {
            var shuffled = overrides.OrderBy(_ => random.Next()).ToList();
            var result = _resolver.Resolve(profile, shuffled);
            results.Add(result);
        }

        // All results should be identical
        var firstResult = results[0];
        foreach (var result in results.Skip(1))
        {
            Assert.Equal(firstResult, result);
        }

        // Verify priority is correctly applied
        Assert.Equal(1000, (int)firstResult["timeout"]);  // Endpoint wins
        Assert.Equal(3, (int)firstResult["retry-count"]); // Global only
        Assert.True((bool)firstResult["circuit-breaker"]); // API only
        Assert.False((bool)firstResult["debug"]);          // Endpoint only
    }
}
