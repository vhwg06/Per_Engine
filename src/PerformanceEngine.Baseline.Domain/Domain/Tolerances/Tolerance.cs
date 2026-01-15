namespace PerformanceEngine.Baseline.Domain.Domain.Tolerances;

using PerformanceEngine.Baseline.Domain.Domain;

/// <summary>
/// Immutable value object representing a tolerance rule for a single metric.
/// </summary>
public class Tolerance : IEquatable<Tolerance>
{
    public string MetricName { get; }
    public ToleranceType Type { get; }
    public decimal Amount { get; }

    /// <param name="metricName">Name of the metric this tolerance applies to</param>
    /// <param name="type">Tolerance evaluation strategy</param>
    /// <param name="amount">Tolerance amount (absolute or percentage)</param>
    /// <exception cref="ToleranceValidationException">If parameters are invalid</exception>
    public Tolerance(string metricName, ToleranceType type, decimal amount)
    {
        if (string.IsNullOrWhiteSpace(metricName))
            throw new ToleranceValidationException(metricName ?? "null", "Metric name cannot be empty.");

        if (amount < 0)
            throw new ToleranceValidationException(
                metricName,
                $"Tolerance amount cannot be negative. Got: {amount}");

        if (type == ToleranceType.Relative && amount > 100)
            throw new ToleranceValidationException(
                metricName,
                $"Relative tolerance cannot exceed 100%. Got: {amount}%");

        MetricName = metricName;
        Type = type;
        Amount = amount;
    }

    /// <summary>
    /// Evaluates whether a current metric value is within tolerance of the baseline.
    /// </summary>
    /// <param name="baseline">Baseline metric value</param>
    /// <param name="current">Current metric value</param>
    /// <returns>True if within tolerance, false otherwise</returns>
    public bool IsWithinTolerance(decimal baseline, decimal current)
    {
        var absoluteDifference = Math.Abs(current - baseline);

        return Type switch
        {
            ToleranceType.Absolute => absoluteDifference <= Amount,
            ToleranceType.Relative when baseline == 0 => 
                // For baseline = 0, relative tolerance cannot apply
                // Treat as within tolerance if difference is 0
                current == 0,
            ToleranceType.Relative => 
                absoluteDifference <= (Math.Abs(baseline) * Amount / 100),
            _ => throw new InvalidOperationException($"Unknown tolerance type: {Type}")
        };
    }

    public override bool Equals(object? obj) => Equals(obj as Tolerance);

    public bool Equals(Tolerance? other) =>
        other is not null &&
        MetricName == other.MetricName &&
        Type == other.Type &&
        Amount == other.Amount;

    public override int GetHashCode() => 
        HashCode.Combine(MetricName, Type, Amount);

    public override string ToString() => 
        Type == ToleranceType.Relative
            ? $"{MetricName}: ±{Amount}%"
            : $"{MetricName}: ±{Amount}";

    public static bool operator ==(Tolerance? left, Tolerance? right) =>
        (left, right) switch
        {
            (null, null) => true,
            (null, _) or (_, null) => false,
            _ => left.Equals(right)
        };

    public static bool operator !=(Tolerance? left, Tolerance? right) =>
        !(left == right);
}
