namespace PerformanceEngine.Baseline.Domain.Domain.Baselines;

/// <summary>
/// Immutable value object representing a baseline identifier.
/// </summary>
public class BaselineId : IEquatable<BaselineId>
{
    public string Value { get; }

    public BaselineId(string? value = null)
    {
        Value = string.IsNullOrWhiteSpace(value) ? Guid.NewGuid().ToString() : value;
    }

    public override bool Equals(object? obj) => Equals(obj as BaselineId);

    public bool Equals(BaselineId? other) =>
        other is not null && Value == other.Value;

    public override int GetHashCode() => Value.GetHashCode();

    public override string ToString() => Value;

    public static bool operator ==(BaselineId? left, BaselineId? right) =>
        (left, right) switch
        {
            (null, null) => true,
            (null, _) or (_, null) => false,
            _ => left.Equals(right)
        };

    public static bool operator !=(BaselineId? left, BaselineId? right) =>
        !(left == right);
}
