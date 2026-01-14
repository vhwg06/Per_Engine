namespace PerformanceEngine.Profile.Domain.Domain.Profiles;

using PerformanceEngine.Profile.Domain.Domain.Configuration;
using PerformanceEngine.Profile.Domain.Domain.Scopes;

/// <summary>
/// Pure domain service for resolving configuration profiles.
/// Given a set of profiles and a requested scope, produces a deterministic ResolvedProfile.
/// </summary>
public static class ProfileResolver
{
    /// <summary>
    /// Resolves profiles for a requested scope (single dimension).
    /// Merges profiles based on scope precedence (higher precedence wins).
    /// </summary>
    public static ResolvedProfile Resolve(
        IEnumerable<Profile> profiles,
        IScope requestedScope)
    {
        return Resolve(profiles, new[] { requestedScope });
    }

    /// <summary>
    /// Resolves profiles for multiple requested scopes (multi-dimensional).
    /// Merges profiles based on scope precedence (higher precedence wins).
    /// </summary>
    public static ResolvedProfile Resolve(
        IEnumerable<Profile> profiles,
        IEnumerable<IScope> requestedScopes)
    {
        if (profiles == null)
            throw new ArgumentNullException(nameof(profiles));
        if (requestedScopes == null)
            throw new ArgumentNullException(nameof(requestedScopes));

        var profilesList = profiles.ToList();
        var scopesList = requestedScopes.ToList();

        // Validate no conflicts first
        ConflictHandler.ValidateNoConflicts(profilesList);

        // Filter profiles that apply to the requested scopes
        var applicableProfiles = GetApplicableProfiles(profilesList, scopesList);

        // Sort by precedence (lowest first, so higher precedence overwrites)
        var sortedProfiles = applicableProfiles
            .OrderBy(p => p.Scope.Precedence)
            .ToList();

        // Merge configurations
        var mergedConfig = new Dictionary<ConfigKey, ConfigValue>();
        var auditTrail = new Dictionary<ConfigKey, List<IScope>>();

        foreach (var profile in sortedProfiles)
        {
            foreach (var (key, value) in profile.Configurations)
            {
                mergedConfig[key] = value;

                if (!auditTrail.ContainsKey(key))
                {
                    auditTrail[key] = new List<IScope>();
                }
                auditTrail[key].Add(profile.Scope);
            }
        }

        // Convert to immutable structures
        var immutableConfig = mergedConfig.ToImmutableDictionary();
        var immutableAudit = auditTrail.ToImmutableDictionary(
            kvp => kvp.Key,
            kvp => kvp.Value.ToImmutableList()
        );

        return new ResolvedProfile(
            immutableConfig,
            immutableAudit,
            DateTime.UtcNow
        );
    }

    private static List<Profile> GetApplicableProfiles(
        List<Profile> profiles,
        List<IScope> requestedScopes)
    {
        var applicable = new List<Profile>();

        foreach (var profile in profiles)
        {
            // Global scope applies to everything
            if (profile.Scope is GlobalScope)
            {
                applicable.Add(profile);
            }
            // Exact scope match with any requested scope
            else if (requestedScopes.Any(rs => profile.Scope.Equals(rs)))
            {
                applicable.Add(profile);
            }
            // Composite scope: check if it matches the context
            else if (profile.Scope is CompositeScope composite &&
                     composite.MatchesContext(requestedScopes))
            {
                applicable.Add(profile);
            }
        }

        return applicable;
    }
}
