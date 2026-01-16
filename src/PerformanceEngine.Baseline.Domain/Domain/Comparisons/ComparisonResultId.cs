namespace PerformanceEngine.Baseline.Domain.Domain.Comparisons;

/// <summary>
/// Immutable value object representing a comparison result identifier.
/// </summary>
public class ComparisonResultId : IEquatable<ComparisonResultId>
{
    public string Value { get; }

    public ComparisonResultId(string? value = null)
    {
        Value = string.IsNullOrWhiteSpace(value) ? Guid.NewGuid().ToString() : value;
    }

    public override bool Equals(object? obj) => Equals(obj as ComparisonResultId);

    public bool Equals(ComparisonResultId? other) =>
        other is not null && Value == other.Value;

    public override int GetHashCode() => Value.GetHashCode();

    public override string ToString() => Value;

    public static bool operator ==(ComparisonResultId? left, ComparisonResultId? right) =>
        (left, right) switch
        {
            (null, null) => true,
            (null, _) or (_, null) => false,
            _ => left.Equals(right)
        };

    public static bool operator !=(ComparisonResultId? left, ComparisonResultId? right) =>
        !(left == right);
}
