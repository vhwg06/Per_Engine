namespace PerformanceEngine.Profile.Domain.Domain.Configuration;

/// <summary>
/// Immutable value object representing a configuration key.
/// </summary>
public sealed record ConfigKey
{
    public string Name { get; }

    public ConfigKey(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Config key name cannot be null or whitespace", nameof(name));

        Name = name;
    }

    public override string ToString() => Name;
}
