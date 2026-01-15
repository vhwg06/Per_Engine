namespace PerformanceEngine.Profile.Domain.Tests.Application.Profile;

using PerformanceEngine.Profile.Domain.Application.Profile;
using PerformanceEngine.Profile.Domain.Domain.Profiles;
using PerformanceEngine.Profile.Domain.Domain.Scopes;
using PerformanceEngine.Profile.Domain.Domain.Configuration;
using Xunit;

/// <summary>
/// Unit tests for ProfileResolver deterministic sorting algorithm.
/// Verifies scope priority order, key alphabetical order, and order independence.
/// </summary>
public class ProfileResolverSortingTests
{
    private readonly PerformanceEngine.Profile.Domain.Application.Profile.ProfileResolver _resolver = 
        new PerformanceEngine.Profile.Domain.Application.Profile.ProfileResolver();

    [Fact]
    public void Resolve_EmptyOverrides_ReturnsEmptyDictionary()
    {
        var profileObj = Profile.Create(GlobalScope.Instance, new Dictionary<ConfigKey, ConfigValue>());
        var overrides = new List<(string, string, object)>();
        
        var result = _resolver.Resolve(profileObj, overrides);
        
        Assert.Empty(result);
    }

    [Fact]
    public void Resolve_SingleOverride_ReturnsSingleEntry()
    {
        var profileObj = Profile.Create(GlobalScope.Instance, new Dictionary<ConfigKey, ConfigValue>());
        var overrides = new[] { ("global", "key1", (object)"value1") };
        
        var result = _resolver.Resolve(profileObj, overrides);
        
        Assert.Single(result);
        Assert.Equal("value1", result["key1"]);
    }

    [Fact]
    public void Resolve_ScopePriority_GlobalLowest()
    {
        var profileObj = Profile.Create(GlobalScope.Instance, new Dictionary<ConfigKey, ConfigValue>());
        var overrides = new[]
        {
            ("endpoint", "same-key", (object)"endpoint-value"),
            ("global", "same-key", (object)"global-value"),
            ("api", "same-key", (object)"api-value")
        };

        var result = _resolver.Resolve(profileObj, overrides);

        // Endpoint should be processed last (highest priority)
        Assert.Equal("endpoint-value", result["same-key"]);
    }

    [Fact]
    public void Resolve_OrderIndependence_SameInputOrder_IdenticalOutput()
    {
        var profileObj = Profile.Create(GlobalScope.Instance, new Dictionary<ConfigKey, ConfigValue>());
        
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

        var result1 = _resolver.Resolve(profileObj, overrides1);
        var result2 = _resolver.Resolve(profileObj, overrides2);

        Assert.Equal(result1, result2);
    }
}
