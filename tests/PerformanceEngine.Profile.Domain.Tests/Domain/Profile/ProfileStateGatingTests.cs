namespace PerformanceEngine.Profile.Domain.Tests.Domain.Profile;

using PerformanceEngine.Profile.Domain.Domain;
using Xunit;

/// <summary>
/// Unit tests for Profile state gating.
/// Verifies that ApplyOverride throws if not Unresolved, Get throws if not Resolved.
/// </summary>
public class ProfileStateGatingTests
{
    [Fact]
    public void Profile_InitialState_IsUnresolved()
    {
        var profile = new PerformanceEngine.Profile.Domain.Domain.Profile(id: "test");
        
        Assert.Equal(ProfileState.Unresolved, profile.State);
    }

    [Fact]
    public void Profile_ApplyOverride_SucceedsWhenUnresolved()
    {
        var profile = new PerformanceEngine.Profile.Domain.Domain.Profile(id: "test");
        Assert.Equal(ProfileState.Unresolved, profile.State);
        
        // Should succeed - profile is Unresolved
        profile.ApplyOverride("global", "key1", "value1");
        
        // Profile remains Unresolved after override
        Assert.Equal(ProfileState.Unresolved, profile.State);
    }

    [Fact]
    public void Profile_ApplyOverride_ThrowsWhenResolved()
    {
        var profile = new PerformanceEngine.Profile.Domain.Domain.Profile(id: "test");
        profile.ApplyOverride("global", "key1", "value1");
        
        // Resolve the profile
        profile.Resolve();
        Assert.Equal(ProfileState.Resolved, profile.State);
        
        // Should throw - profile is Resolved
        Assert.Throws<InvalidOperationException>(() =>
            profile.ApplyOverride("global", "key2", "value2"));
    }

    [Fact]
    public void Profile_ApplyOverride_ThrowsWhenInvalid()
    {
        var profile = new PerformanceEngine.Profile.Domain.Domain.Profile(id: "test");
        
        // Manually set to Invalid state (simulating validation failure)
        profile.SetState(ProfileState.Invalid);
        
        // Should throw - profile is Invalid
        Assert.Throws<InvalidOperationException>(() =>
            profile.ApplyOverride("global", "key1", "value1"));
    }

    [Fact]
    public void Profile_Get_FailsWhenUnresolved()
    {
        var profile = new PerformanceEngine.Profile.Domain.Domain.Profile(id: "test");
        Assert.Equal(ProfileState.Unresolved, profile.State);
        
        // Should throw - profile is Unresolved
        Assert.Throws<InvalidOperationException>(() =>
            profile.Get("key1"));
    }

    [Fact]
    public void Profile_Get_SucceedsWhenResolved()
    {
        var profile = new PerformanceEngine.Profile.Domain.Domain.Profile(id: "test");
        profile.ApplyOverride("global", "key1", "value1");
        profile.Resolve();
        
        Assert.Equal(ProfileState.Resolved, profile.State);
        
        // Should succeed - profile is Resolved
        var value = profile.Get("key1");
        Assert.Equal("value1", value);
    }

    [Fact]
    public void Profile_Get_FailsWhenInvalid()
    {
        var profile = new PerformanceEngine.Profile.Domain.Domain.Profile(id: "test");
        profile.SetState(ProfileState.Invalid);
        
        // Should throw - profile is Invalid
        Assert.Throws<InvalidOperationException>(() =>
            profile.Get("key1"));
    }

    [Fact]
    public void Profile_StateTransition_UnresolvedToResolved()
    {
        var profile = new PerformanceEngine.Profile.Domain.Domain.Profile(id: "test");
        Assert.Equal(ProfileState.Unresolved, profile.State);
        
        profile.Resolve();
        
        Assert.Equal(ProfileState.Resolved, profile.State);
    }

    [Fact]
    public void Profile_StateTransition_UnresolvedToInvalid()
    {
        var profile = new PerformanceEngine.Profile.Domain.Domain.Profile(id: "test");
        
        profile.SetState(ProfileState.Invalid);
        
        Assert.Equal(ProfileState.Invalid, profile.State);
    }

    [Fact]
    public void Profile_MultipleOverrides_AllSucceedBeforeResolve()
    {
        var profile = new PerformanceEngine.Profile.Domain.Domain.Profile(id: "test");
        
        profile.ApplyOverride("global", "key1", "value1");
        profile.ApplyOverride("api", "key2", "value2");
        profile.ApplyOverride("endpoint", "key3", "value3");
        
        // All should be stored
        profile.Resolve();
        Assert.Equal("value1", profile.Get("key1"));
        Assert.Equal("value2", profile.Get("key2"));
        Assert.Equal("value3", profile.Get("key3"));
    }
}
