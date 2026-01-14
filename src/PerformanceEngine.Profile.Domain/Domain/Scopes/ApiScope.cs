namespace PerformanceEngine.Profile.Domain.Domain.Scopes;

/// <summary>
/// Scope for API-specific configuration.
/// Precedence: 10 (higher than Global).
/// </summary>
public sealed record ApiScope : IScope
{
    public string ApiName { get; }

    public ApiScope(string apiName)
    {
        if (string.IsNullOrWhiteSpace(apiName))
            throw new ArgumentException("API name cannot be null or whitespace", nameof(apiName));

        ApiName = apiName;
    }

    public string Id => ApiName;
    public string Type => "Api";
    public int Precedence => 10;
    public string Description => $"API-specific configuration for '{ApiName}'";

    public bool Equals(IScope? other)
    {
        return other is ApiScope api && api.ApiName == ApiName;
    }

    public int CompareTo(IScope? other)
    {
        if (other is null) return 1;
        return Precedence.CompareTo(other.Precedence);
    }

    public override int GetHashCode() => HashCode.Combine(Type, ApiName);
}
