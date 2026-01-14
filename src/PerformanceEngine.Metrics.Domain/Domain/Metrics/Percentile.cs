namespace PerformanceEngine.Metrics.Domain.Metrics;

/// <summary>
/// Represents a percentile value in a latency distribution.
/// Percentile values must be between 0 and 100 (inclusive).
/// For example, p50 represents the median, p95 represents the 95th percentile.
/// </summary>
public sealed class Percentile : ValueObject
{
    /// <summary>
    /// The percentile value (0-100)
    /// </summary>
    public double Value { get; }

    /// <summary>
    /// Initializes a new instance of the Percentile class.
    /// </summary>
    /// <param name="value">The percentile value (0-100)</param>
    /// <exception cref="ArgumentException">Thrown when value is outside the range [0, 100]</exception>
    public Percentile(double value)
    {
        if (value < 0 || value > 100)
        {
            throw new ArgumentException(
                $"Percentile value must be between 0 and 100, but was {value}",
                nameof(value));
        }

        Value = value;
    }

    /// <summary>
    /// Creates a percentile from a name string (e.g., "p95" or "P50").
    /// </summary>
    /// <param name="percentileName">The percentile name like "p95" or "P50"</param>
    /// <returns>A new Percentile instance</returns>
    /// <exception cref="ArgumentException">Thrown when the name format is invalid or value is outside range</exception>
    public static Percentile Parse(string percentileName)
    {
        if (string.IsNullOrWhiteSpace(percentileName))
        {
            throw new ArgumentException("Percentile name cannot be null or empty", nameof(percentileName));
        }

        var normalized = percentileName.ToLowerInvariant().Trim();

        if (!normalized.StartsWith("p", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException(
                "Percentile name must start with 'p' (e.g., 'p95')",
                nameof(percentileName));
        }

        var valueString = normalized[1..];

        if (!double.TryParse(valueString, System.Globalization.CultureInfo.InvariantCulture, out var value))
        {
            throw new ArgumentException(
                $"Could not parse percentile value from '{percentileName}'",
                nameof(percentileName));
        }

        return new Percentile(value);
    }

    /// <summary>
    /// Gets a human-readable name for this percentile (e.g., "p95").
    /// </summary>
    public string Name => $"p{Value:F0}";

    /// <summary>
    /// Determines if this percentile represents the minimum value (p0).
    /// </summary>
    public bool IsMinimum => Value == 0;

    /// <summary>
    /// Determines if this percentile represents the maximum value (p100).
    /// </summary>
    public bool IsMaximum => Value == 100;

    /// <summary>
    /// Determines if this percentile represents the median (p50).
    /// </summary>
    public bool IsMedian => Value == 50;

    protected override IEnumerable<object?> GetAtomicValues()
    {
        yield return Value;
    }

    public override string ToString() => Name;
}
