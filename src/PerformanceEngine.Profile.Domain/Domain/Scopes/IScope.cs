namespace PerformanceEngine.Profile.Domain.Domain.Scopes;

/// <summary>
/// Defines a scope for configuration contexts.
/// All scope implementations must be immutable and comparable for precedence ordering.
/// </summary>
public interface IScope : IEquatable<IScope>, IComparable<IScope>
{
    /// <summary>
    /// Unique identifier for this scope instance.
    /// </summary>
    string Id { get; }

    /// <summary>
    /// The type of scope (e.g., "Global", "Api", "Environment").
    /// </summary>
    string Type { get; }

    /// <summary>
    /// Precedence level for conflict resolution (higher wins).
    /// </summary>
    int Precedence { get; }

    /// <summary>
    /// Human-readable description of this scope.
    /// </summary>
    string Description { get; }
}
