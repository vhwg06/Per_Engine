namespace PerformanceEngine.Profile.Domain.Domain.Scopes;

/// <summary>
/// Scope for tag-specific configuration.
/// Precedence: 20 (configurable, higher than Environment by default).
/// </summary>
public sealed record TagScope : IScope
{
    public string TagName { get; }
    private readonly int _precedence;

    public TagScope(string tagName, int precedence = 20)
    {
        if (string.IsNullOrWhiteSpace(tagName))
            throw new ArgumentException("Tag name cannot be null or whitespace", nameof(tagName));

        TagName = tagName;
        _precedence = precedence;
    }

    public string Id => TagName;
    public string Type => "Tag";
    public int Precedence => _precedence;
    public string Description => $"Tag-specific configuration for '{TagName}'";

    public bool Equals(IScope? other)
    {
        return other is TagScope tag && tag.TagName == TagName;
    }

    public int CompareTo(IScope? other)
    {
        if (other is null) return 1;
        return Precedence.CompareTo(other.Precedence);
    }

    public override int GetHashCode() => HashCode.Combine(Type, TagName);
}
