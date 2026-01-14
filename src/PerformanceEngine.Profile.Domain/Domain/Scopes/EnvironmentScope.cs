namespace PerformanceEngine.Profile.Domain.Domain.Scopes;

/// <summary>
/// Scope for environment-specific configuration (prod, staging, dev, etc.).
/// Precedence: 15 (higher than Api).
/// </summary>
public sealed record EnvironmentScope : IScope
{
    public string EnvironmentName { get; }

    public EnvironmentScope(string environmentName)
    {
        if (string.IsNullOrWhiteSpace(environmentName))
            throw new ArgumentException("Environment name cannot be null or whitespace", nameof(environmentName));

        EnvironmentName = environmentName;
    }

    public string Id => EnvironmentName;
    public string Type => "Environment";
    public int Precedence => 15;
    public string Description => $"Environment-specific configuration for '{EnvironmentName}'";

    public bool Equals(IScope? other)
    {
        return other is EnvironmentScope env && env.EnvironmentName == EnvironmentName;
    }

    public int CompareTo(IScope? other)
    {
        if (other is null) return 1;
        return Precedence.CompareTo(other.Precedence);
    }

    public override int GetHashCode() => HashCode.Combine(Type, EnvironmentName);
}
