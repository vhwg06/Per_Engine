namespace PerformanceEngine.Baseline.Domain.Domain.Confidence;

using PerformanceEngine.Baseline.Domain.Domain;

/// <summary>
/// Immutable value object representing confidence in a comparison outcome.
/// Value is in the range [0.0, 1.0] where 0 = no confidence, 1 = absolute confidence.
/// </summary>
public class ConfidenceLevel : IEquatable<ConfidenceLevel>, IComparable<ConfidenceLevel>
{
    private const decimal Epsilon = 0.00001m; // Floating-point comparison tolerance

    public decimal Value { get; }

    /// <param name="value">Confidence level in range [0.0, 1.0]</param>
    /// <exception cref="ConfidenceValidationException">If value is outside valid range</exception>
    public ConfidenceLevel(decimal value)
    {
        if (value < 0 || value > 1)
            throw new ConfidenceValidationException(value);

        Value = value;
    }

    /// <summary>
    /// Determines if confidence level exceeds the minimum threshold for a conclusive result.
    /// </summary>
    /// <param name="threshold">Minimum confidence threshold (default 0.5)</param>
    /// <returns>True if confidence >= threshold, false otherwise</returns>
    public bool IsConclusive(decimal threshold = 0.5m) => Value >= threshold;

    public override bool Equals(object? obj) => Equals(obj as ConfidenceLevel);

    public bool Equals(ConfidenceLevel? other) =>
        other is not null &&
        Math.Abs(Value - other.Value) < Epsilon;

    public int CompareTo(ConfidenceLevel? other)
    {
        if (other is null) return 1;
        return Value.CompareTo(other.Value);
    }

    public override int GetHashCode() => Value.GetHashCode();

    public override string ToString() => $"{Value:P1}"; // Format as percentage

    public static bool operator ==(ConfidenceLevel? left, ConfidenceLevel? right) =>
        (left, right) switch
        {
            (null, null) => true,
            (null, _) or (_, null) => false,
            _ => left.Equals(right)
        };

    public static bool operator !=(ConfidenceLevel? left, ConfidenceLevel? right) =>
        !(left == right);

    public static bool operator <(ConfidenceLevel? left, ConfidenceLevel? right) =>
        left is not null && right is not null && left.Value < right.Value;

    public static bool operator <=(ConfidenceLevel? left, ConfidenceLevel? right) =>
        left is not null && right is not null && left.Value <= right.Value;

    public static bool operator >(ConfidenceLevel? left, ConfidenceLevel? right) =>
        left is not null && right is not null && left.Value > right.Value;

    public static bool operator >=(ConfidenceLevel? left, ConfidenceLevel? right) =>
        left is not null && right is not null && left.Value >= right.Value;

    public static ConfidenceLevel operator +(ConfidenceLevel left, ConfidenceLevel right) =>
        new(Math.Min(left.Value + right.Value, 1m));

    public static ConfidenceLevel operator *(ConfidenceLevel left, decimal factor) =>
        new(Math.Min(left.Value * factor, 1m));

    /// <summary>
    /// Returns the minimum of two confidence levels.
    /// </summary>
    public static ConfidenceLevel Min(ConfidenceLevel left, ConfidenceLevel right) =>
        left.Value < right.Value ? left : right;

    /// <summary>
    /// Returns the maximum of two confidence levels.
    /// </summary>
    public static ConfidenceLevel Max(ConfidenceLevel left, ConfidenceLevel right) =>
        left.Value > right.Value ? left : right;
}
