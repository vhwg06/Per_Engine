namespace PerformanceEngine.Profile.Domain.Domain.Scopes;

/// <summary>
/// Composite scope for multi-dimensional contexts.
/// Combines two base scopes into a single, more specific scope.
/// Precedence: max(scopeA.precedence, scopeB.precedence) + 5
/// </summary>
public sealed record CompositeScope : IScope
{
    public IScope ScopeA { get; }
    public IScope ScopeB { get; }

    public CompositeScope(IScope scopeA, IScope scopeB)
    {
        ScopeA = scopeA ?? throw new ArgumentNullException(nameof(scopeA));
        ScopeB = scopeB ?? throw new ArgumentNullException(nameof(scopeB));

        // Prevent nesting composite scopes
        if (scopeA is CompositeScope || scopeB is CompositeScope)
        {
            throw new ArgumentException("Cannot nest CompositeScope instances");
        }
    }

    public string Id => $"{ScopeA.Id}+{ScopeB.Id}";
    public string Type => "Composite";
    public int Precedence => Math.Max(ScopeA.Precedence, ScopeB.Precedence) + 5;
    public string Description => $"Composite scope: {ScopeA.Description} AND {ScopeB.Description}";

    public bool Equals(IScope? other)
    {
        if (other is not CompositeScope composite) return false;

        // Order-independent equality
        return (ScopeA.Equals(composite.ScopeA) && ScopeB.Equals(composite.ScopeB)) ||
               (ScopeA.Equals(composite.ScopeB) && ScopeB.Equals(composite.ScopeA));
    }

    public int CompareTo(IScope? other)
    {
        if (other is null) return 1;
        return Precedence.CompareTo(other.Precedence);
    }

    public override int GetHashCode()
    {
        // Order-independent hash
        return ScopeA.GetHashCode() ^ ScopeB.GetHashCode();
    }

    /// <summary>
    /// Checks if this composite scope matches a requested scope context.
    /// Both component scopes must match for the composite to apply.
    /// </summary>
    public bool MatchesContext(IEnumerable<IScope> contextScopes)
    {
        var scopesList = contextScopes.ToList();
        return scopesList.Any(s => s.Equals(ScopeA)) && scopesList.Any(s => s.Equals(ScopeB));
    }
}
