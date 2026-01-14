namespace PerformanceEngine.Metrics.Domain.Metrics;

/// <summary>
/// Represents a latency (duration) measurement with flexible units.
/// Latency values are always non-negative and immutable.
/// </summary>
public sealed class Latency : ValueObject
{
    /// <summary>
    /// The numeric value of the latency measurement
    /// </summary>
    public double Value { get; }

    /// <summary>
    /// The unit of measurement for this latency
    /// </summary>
    public LatencyUnit Unit { get; }

    /// <summary>
    /// Initializes a new instance of the Latency class.
    /// </summary>
    /// <param name="value">The numeric value of the latency (must be non-negative)</param>
    /// <param name="unit">The unit of measurement</param>
    /// <exception cref="ArgumentException">Thrown when value is negative</exception>
    public Latency(double value, LatencyUnit unit)
    {
        if (value < 0)
        {
            throw new ArgumentException("Latency value cannot be negative", nameof(value));
        }

        if (!Enum.IsDefined(typeof(LatencyUnit), unit))
        {
            throw new ArgumentException($"Invalid latency unit: {unit}", nameof(unit));
        }

        Value = value;
        Unit = unit;
    }

    /// <summary>
    /// Converts this latency to a different unit.
    /// </summary>
    /// <param name="targetUnit">The target unit to convert to</param>
    /// <returns>A new Latency instance with the value converted to the target unit</returns>
    public Latency ConvertTo(LatencyUnit targetUnit)
    {
        if (Unit == targetUnit)
        {
            return new Latency(Value, Unit);
        }

        var convertedValue = LatencyUnitConverter.Convert(Value, Unit, targetUnit);
        return new Latency(convertedValue, targetUnit);
    }

    /// <summary>
    /// Gets the value in a specific unit.
    /// </summary>
    /// <param name="targetUnit">The unit to get the value in</param>
    /// <returns>The numeric value in the target unit</returns>
    public double GetValueIn(LatencyUnit targetUnit)
    {
        if (Unit == targetUnit)
        {
            return Value;
        }

        return LatencyUnitConverter.Convert(Value, Unit, targetUnit);
    }

    protected override IEnumerable<object?> GetAtomicValues()
    {
        yield return Value;
        yield return Unit;
    }

    public override string ToString()
    {
        return $"{Value:F2}{GetUnitSuffix()}";
    }

    private string GetUnitSuffix() => Unit switch
    {
        LatencyUnit.Nanoseconds => "ns",
        LatencyUnit.Microseconds => "µs",
        LatencyUnit.Milliseconds => "ms",
        LatencyUnit.Seconds => "s",
        _ => "?"
    };
}
