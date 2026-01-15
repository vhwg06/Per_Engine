namespace PerformanceEngine.Profile.Domain.Tests.Domain;

using PerformanceEngine.Profile.Domain.Domain;
using Xunit;

/// <summary>
/// Unit tests for ProfileState enum and state transitions.
/// Verifies enum values, lifecycle, and constraints.
/// </summary>
public class ProfileStateTests
{
    [Fact]
    public void ProfileState_HasCorrectValues()
    {
        Assert.Equal(1, (int)ProfileState.Unresolved);
        Assert.Equal(2, (int)ProfileState.Resolved);
        Assert.Equal(3, (int)ProfileState.Invalid);
    }

    [Fact]
    public void ProfileState_UnresolvedIsInitialState()
    {
        // Verifies enum value for initial state
        Assert.NotEqual(ProfileState.Resolved, ProfileState.Unresolved);
        Assert.NotEqual(ProfileState.Invalid, ProfileState.Unresolved);
    }

    [Fact]
    public void ProfileState_ResolvedIsIntermediateState()
    {
        Assert.NotEqual(ProfileState.Unresolved, ProfileState.Resolved);
        Assert.NotEqual(ProfileState.Invalid, ProfileState.Resolved);
    }

    [Fact]
    public void ProfileState_InvalidIsErrorState()
    {
        Assert.NotEqual(ProfileState.Unresolved, ProfileState.Invalid);
        Assert.NotEqual(ProfileState.Resolved, ProfileState.Invalid);
    }

    [Fact]
    public void ProfileState_AllValuesAreDistinct()
    {
        var states = new[] { ProfileState.Unresolved, ProfileState.Resolved, ProfileState.Invalid };
        var distinctStates = states.Distinct().ToList();
        
        Assert.Equal(states.Length, distinctStates.Count);
    }

    [Fact]
    public void ProfileState_ParseFromInt()
    {
        Assert.Equal(ProfileState.Unresolved, (ProfileState)1);
        Assert.Equal(ProfileState.Resolved, (ProfileState)2);
        Assert.Equal(ProfileState.Invalid, (ProfileState)3);
    }

    [Fact]
    public void ProfileState_UnresolvedValue_Is1()
    {
        Assert.Equal(1, (int)ProfileState.Unresolved);
    }

    [Fact]
    public void ProfileState_ResolvedValue_Is2()
    {
        Assert.Equal(2, (int)ProfileState.Resolved);
    }

    [Fact]
    public void ProfileState_InvalidValue_Is3()
    {
        Assert.Equal(3, (int)ProfileState.Invalid);
    }
}
