namespace PerformanceEngine.Profile.Domain.Domain.Scopes;

/// <summary>
/// Utility class for comparing and ranking scopes.
/// </summary>
public static class ScopeComparison
{
    /// <summary>
    /// Checks if a scope is the most specific among a collection of scopes.
    /// </summary>
    public static bool IsMostSpecific(IScope scope, IEnumerable<IScope> otherScopes)
    {
        return otherScopes.All(other => scope.Precedence >= other.Precedence);
    }

    /// <summary>
    /// Ranks scopes by precedence (highest first).
    /// </summary>
    public static List<IScope> RankByPrecedence(IEnumerable<IScope> scopes)
    {
        return scopes.OrderByDescending(s => s.Precedence).ToList();
    }

    /// <summary>
    /// Gets the most specific scope from a collection.
    /// </summary>
    public static IScope? GetMostSpecific(IEnumerable<IScope> scopes)
    {
        return RankByPrecedence(scopes).FirstOrDefault();
    }
}
