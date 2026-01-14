namespace PerformanceEngine.Profile.Domain.Domain.Profiles;

using PerformanceEngine.Profile.Domain.Domain.Configuration;
using PerformanceEngine.Profile.Domain.Domain.Scopes;

/// <summary>
/// Immutable entity representing the result of resolving configuration profiles.
/// Contains the final configuration values and an audit trail showing which scope provided each value.
/// </summary>
public sealed record ResolvedProfile
{
    public ImmutableDictionary<ConfigKey, ConfigValue> Configuration { get; }
    public ImmutableDictionary<ConfigKey, ImmutableList<IScope>> AuditTrail { get; }
    public DateTime ResolvedAt { get; }

    public ResolvedProfile(
        ImmutableDictionary<ConfigKey, ConfigValue> configuration,
        ImmutableDictionary<ConfigKey, ImmutableList<IScope>> auditTrail,
        DateTime resolvedAt)
    {
        Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        AuditTrail = auditTrail ?? throw new ArgumentNullException(nameof(auditTrail));
        ResolvedAt = resolvedAt;
    }

    /// <summary>
    /// Gets a configuration value by key.
    /// </summary>
    public ConfigValue? Get(ConfigKey key)
    {
        return Configuration.TryGetValue(key, out var value) ? value : null;
    }

    /// <summary>
    /// Gets a configuration value by key name.
    /// </summary>
    public ConfigValue? Get(string keyName)
    {
        var key = new ConfigKey(keyName);
        return Get(key);
    }

    public override string ToString() =>
        $"ResolvedProfile with {Configuration.Count} configs (resolved at {ResolvedAt:yyyy-MM-dd HH:mm:ss})";
}
