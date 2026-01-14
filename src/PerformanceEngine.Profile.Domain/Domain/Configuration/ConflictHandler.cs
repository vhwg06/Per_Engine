namespace PerformanceEngine.Profile.Domain.Domain.Configuration;

using PerformanceEngine.Profile.Domain.Domain.Profiles;
using PerformanceEngine.Profile.Domain.Domain.Scopes;

/// <summary>
/// Exception thrown when configuration conflicts are detected.
/// </summary>
public class ConfigurationConflictException : Exception
{
    public IReadOnlyList<ConflictDetail> Conflicts { get; }

    public ConfigurationConflictException(IReadOnlyList<ConflictDetail> conflicts)
        : base($"Configuration conflicts detected: {conflicts.Count} conflict(s)")
    {
        Conflicts = conflicts;
    }

    public override string ToString()
    {
        var details = string.Join("\n", Conflicts.Select(c => $"  - {c}"));
        return $"{Message}\n{details}";
    }
}

/// <summary>
/// Details about a specific configuration conflict.
/// </summary>
public sealed record ConflictDetail(
    ConfigKey Key,
    string ScopeDescription,
    ConfigValue Value1,
    ConfigValue Value2)
{
    public override string ToString() =>
        $"Key '{Key.Name}' has conflicting values in {ScopeDescription}: {Value1.Value} vs {Value2.Value}";
}

/// <summary>
/// Domain service for detecting configuration conflicts.
/// </summary>
public static class ConflictHandler
{
    /// <summary>
    /// Detects conflicts: two profiles at the same scope with different values for the same key.
    /// </summary>
    public static List<ConflictDetail> DetectConflicts(IEnumerable<Profile> profiles)
    {
        var conflicts = new List<ConflictDetail>();
        var profilesList = profiles.ToList();

        // Group profiles by scope
        var profilesByScope = profilesList
            .GroupBy(p => p.Scope, new ScopeEqualityComparer())
            .ToList();

        foreach (var scopeGroup in profilesByScope)
        {
            var profilesInScope = scopeGroup.ToList();
            if (profilesInScope.Count <= 1)
                continue;

            // Check for conflicting keys within this scope
            var allKeys = profilesInScope
                .SelectMany(p => p.Configurations.Keys)
                .Distinct()
                .ToList();

            foreach (var key in allKeys)
            {
                var valuesForKey = profilesInScope
                    .Where(p => p.Configurations.ContainsKey(key))
                    .Select(p => p.Configurations[key])
                    .Distinct()
                    .ToList();

                if (valuesForKey.Count > 1)
                {
                    conflicts.Add(new ConflictDetail(
                        key,
                        $"scope {scopeGroup.Key.Type}:{scopeGroup.Key.Id}",
                        valuesForKey[0],
                        valuesForKey[1]
                    ));
                }
            }
        }

        return conflicts;
    }

    /// <summary>
    /// Validates profiles and throws if conflicts exist.
    /// </summary>
    public static void ValidateNoConflicts(IEnumerable<Profile> profiles)
    {
        var conflicts = DetectConflicts(profiles);
        if (conflicts.Any())
        {
            throw new ConfigurationConflictException(conflicts);
        }
    }

    private class ScopeEqualityComparer : IEqualityComparer<IScope>
    {
        public bool Equals(IScope? x, IScope? y)
        {
            if (ReferenceEquals(x, y)) return true;
            if (x is null || y is null) return false;
            return x.Equals(y);
        }

        public int GetHashCode(IScope obj) => obj.GetHashCode();
    }
}
