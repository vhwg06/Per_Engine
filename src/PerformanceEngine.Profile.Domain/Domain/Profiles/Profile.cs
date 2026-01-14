namespace PerformanceEngine.Profile.Domain.Domain.Profiles;

using PerformanceEngine.Profile.Domain.Domain.Configuration;
using PerformanceEngine.Profile.Domain.Domain.Scopes;

/// <summary>
/// Immutable entity representing a configuration profile for a specific scope.
/// </summary>
public sealed record Profile
{
    public IScope Scope { get; }
    public ImmutableDictionary<ConfigKey, ConfigValue> Configurations { get; }

    public Profile(IScope scope, ImmutableDictionary<ConfigKey, ConfigValue> configurations)
    {
        Scope = scope ?? throw new ArgumentNullException(nameof(scope));
        Configurations = configurations ?? throw new ArgumentNullException(nameof(configurations));
    }

    /// <summary>
    /// Creates a profile from a regular dictionary.
    /// </summary>
    public static Profile Create(IScope scope, IDictionary<ConfigKey, ConfigValue> configurations)
    {
        return new Profile(scope, configurations.ToImmutableDictionary());
    }

    public override string ToString() => $"Profile[{Scope.Type}:{Scope.Id}] with {Configurations.Count} configs";
}
