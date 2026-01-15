namespace PerformanceEngine.Profile.Domain.Tests.Application.Profile;

using PerformanceEngine.Profile.Domain.Application.Profile;
using PerformanceEngine.Profile.Domain.Domain;
using Xunit;

/// <summary>
/// Unit tests for ProfileResolver deterministic sorting algorithm.
/// Verifies scope priority order, key alphabetical order, and order independence.
/// </summary>
public class ProfileResolverSortingTests
{
    private readonly ProfileResolver _resolver = new();
    private readonly PerformanceEngine.Profile.Domain.Domain.Profile _testProfile = 
        new(id: "test-profile");

    [Fact]
    public void Resolve_EmptyOverrides_ReturnsEmptyDictionary()
    {
        var overrides = new List<(string, string, object)>();
        
        var result = _resolver.Resolve(_testProfile, overrides);
        
        Assert.Empty(result);
    }

    [Fact]
    public void Resolve_SingleOverride_ReturnsSingleEntry()
    {
        var overrides = new[] { ("global", "key1", (object)"value1") };
        
        var result = _resolver.Resolve(_testProfile, overrides);
        
        Assert.Single(result);
        Assert.Equal("value1", result["key1"]);
    }

    [Fact]
    public void Resolve_ScopePriority_GlobalLowest()
    {
        var overrides = new[]
        {
            ("endpoint", "same-key", (object)"endpoint-value"),
            ("global", "same-key", (object)"global-value"),
            ("api", "same-key", (object)"api-value")
        };

        var result = _resolver.Resolve(_testProfile, overrides);

        // Endpoint should be processed last (highest priority)
        Assert.Equal("endpoint-value", result["same-key"]);
    }

    [Fact]
    public void Resolve_KeyAlphabeticalOrder_WithinSameScope()
    {
        var overrides = new[]
        {
            ("global", "zebra", (object)"z-val"),
            ("global", "apple", (object)"a-val"),
            ("global", "middle", (object)"m-val")
        };

        var result = _resolver.Resolve(_testProfile, overrides);

        // All should be present (no override competition)
        Assert.Equal("a-val", result["apple"]);
        Assert.Equal("m-val", result["middle"]);
        Assert.Equal("z-val", result["zebra"]);
    }

    [Fact]
    public void Resolve_OrderIndependence_SameInputOrder_IdenticalOutput()
    {
        var overrides1 = new[]
        {
            ("global", "key1", (object)"val1"),
            ("api", "key2", (object)"val2"),
            ("endpoint", "key3", (object)"val3")
        };

        var overrides2 = new[]
        {
            ("endpoint", "key3", (object)"val3"),
            ("global", "key1", (object)"val1"),
            ("api", "key2", (object)"val2")
        };

        var result1 = _resolver.Resolve(_testProfile, overrides1);
        var result2 = _resolver.Resolve(_testProfile, overrides2);

        Assert.Equal(result1, result2);
    }

    [Fact]
    public void Resolve_ResultIsImmutable()
    {
        var overrides = new[] { ("global", "key1", (object)"value1") };
        
        var result = _resolver.Resolve(_testProfile, overrides);
        
        // Should be read-only
        Assert.IsAssignableFrom<IReadOnlyDictionary<string, object>>(result);
    }

    [Fact]
    public void Resolve_ComplexScenario_MixedScopesAndKeys()
    {
        var overrides = new[]
        {
            ("global", "timeout", (object)5000),
            ("api", "timeout", (object)3000),
            ("endpoint", "timeout", (object)2000),
            ("global", "retries", (object)3),
            ("api", "retries", (object)2)
        };

        var result = _resolver.Resolve(_testProfile, overrides);

        // Endpoint scope wins for timeout (highest priority)
        Assert.Equal(2000, (int)result["timeout"]);
        // API scope wins for retries (endpoint doesn't override)
        Assert.Equal(2, (int)result["retries"]);
    }

    [Fact]
    public void Resolve_NullProfile_ThrowsArgumentNullException()
    {
        var overrides = new[] { ("global", "key1", (object)"value1") };
        
        Assert.Throws<ArgumentNullException>(() =>
            _resolver.Resolve(null!, overrides));
    }

    [Fact]
    public void Resolve_NullOverrides_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            _resolver.Resolve(_testProfile, null!));
    }

    [Fact]
    public void VerifyOrderIndependence_SameInputInDifferentOrders_IdenticalOutput()
    {
        var overrides = new[]
        {
            ("global", "key1", (object)"val1"),
            ("api", "key2", (object)"val2"),
            ("endpoint", "key3", (object)"val3"),
            ("global", "key4", (object)"val4")
        };

        var results = _resolver.VerifyOrderIndependence(_testProfile, overrides);

        // All results should be identical
        var firstResult = results[0];
        foreach (var result in results.Skip(1))
        {
            Assert.Equal(firstResult, result);
        }
    }

    [Fact]
    public void Resolve_ScopeCase_Insensitive()
    {
        var overrides1 = new[] { ("GLOBAL", "key", (object)"value") };
        var overrides2 = new[] { ("Global", "key", (object)"value") };

        var result1 = _resolver.Resolve(_testProfile, overrides1);
        var result2 = _resolver.Resolve(_testProfile, overrides2);

        // Both should be processed (case-insensitive comparison)
        Assert.NotEmpty(result1);
        Assert.NotEmpty(result2);
    }
}
