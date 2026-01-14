namespace PerformanceEngine.Profile.Domain.Domain.Scopes;

/// <summary>
/// Global scope that applies to all contexts by default.
/// Lowest precedence (0).
/// </summary>
public sealed record GlobalScope : IScope
{
    private static readonly GlobalScope _instance = new();
    public static GlobalScope Instance => _instance;

    private GlobalScope() { }

    public string Id => "global";
    public string Type => "Global";
    public int Precedence => 0;
    public string Description => "Global configuration applying to all contexts";

    public bool Equals(IScope? other)
    {
        return other is GlobalScope;
    }

    public int CompareTo(IScope? other)
    {
        if (other is null) return 1;
        return Precedence.CompareTo(other.Precedence);
    }

    public override int GetHashCode() => Type.GetHashCode();
}
