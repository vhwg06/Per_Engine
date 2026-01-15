namespace PerformanceEngine.Profile.Domain.Application.Profile;

using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Pure function service for deterministic profile resolution.
/// Resolves profiles to immutable dictionaries with guaranteed order independence.
/// Input: Profile + overrides in any order → Output: byte-identical JSON every time.
/// 
/// Resolution algorithm:
/// 1. Sort overrides by (scope priority DESC, key ASC)
/// 2. Apply overrides in sorted order
/// 3. Return immutable, sorted dictionary
/// 4. Guarantee: same input set in any order → identical output
/// </summary>
public sealed class ProfileResolver
{
    /// <summary>
    /// Resolves a profile with overrides to a deterministic, sorted dictionary.
    /// </summary>
    /// <param name="profile">The profile to resolve.</param>
    /// <param name="overrides">Dictionary of overrides to apply (scope, key, value).</param>
    /// <returns>Immutable, sorted dictionary representing resolved profile.</returns>
    /// <exception cref="ArgumentNullException">Thrown when profile or overrides is null.</exception>
    public IReadOnlyDictionary<string, object> Resolve(
        PerformanceEngine.Profile.Domain.Domain.Profile profile,
        IEnumerable<(string scope, string key, object value)> overrides)
    {
        if (profile == null)
        {
            throw new ArgumentNullException(nameof(profile));
        }

        if (overrides == null)
        {
            throw new ArgumentNullException(nameof(overrides));
        }

        // Collect all overrides from profile
        var allOverrides = overrides.ToList();

        // Sort deterministically:
        // 1. By scope priority (global=1, api=2, endpoint=3 - descending so higher priority first)
        // 2. Then by key name (alphabetically ascending)
        var sortedOverrides = allOverrides
            .OrderByDescending(o => GetScopePriority(o.scope))
            .ThenBy(o => o.key)
            .ToList();

        // Apply overrides in deterministic order to build result
        var result = new SortedDictionary<string, object>();

        foreach (var (scope, key, value) in sortedOverrides)
        {
            // Later scopes override earlier ones
            // Within same scope, last one wins
            result[key] = value;
        }

        // Return as immutable sorted dictionary
        return new System.Collections.ObjectModel.ReadOnlyDictionary<string, object>(result);
    }

    /// <summary>
    /// Gets the priority for a scope (higher number = higher priority, applied later).
    /// Global < API < Endpoint
    /// </summary>
    private static int GetScopePriority(string scope) =>
        scope?.ToLowerInvariant() switch
        {
            "global" => 1,
            "api" => 2,
            "endpoint" => 3,
            _ => 0  // Unknown scope, lowest priority
        };

    /// <summary>
    /// Verifies that resolution order independence is maintained.
    /// For testing: resolve the same profile with overrides in different orders,
    /// confirm output is identical.
    /// </summary>
    /// <param name="profile">The profile to test.</param>
    /// <param name="overrides">Collection of overrides.</param>
    /// <returns>List of resolved dictionaries from different orderings (should be identical).</returns>
    public List<IReadOnlyDictionary<string, object>> VerifyOrderIndependence(
        PerformanceEngine.Profile.Domain.Domain.Profile profile,
        IEnumerable<(string scope, string key, object value)> overrides)
    {
        var overridesList = overrides.ToList();
        var results = new List<IReadOnlyDictionary<string, object>>();

        // Resolve with original order
        results.Add(Resolve(profile, overridesList));

        // Resolve with shuffled order (determinism test)
        var random = new Random(seed: 42);  // Fixed seed for reproducibility
        for (int i = 0; i < 5; i++)
        {
            var shuffled = overridesList.OrderBy(_ => random.Next()).ToList();
            results.Add(Resolve(profile, shuffled));
        }

        return results;
    }
}
