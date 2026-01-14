namespace PerformanceEngine.Metrics.Domain.Metrics;

/// <summary>
/// Utility class for converting latency values between different units.
/// </summary>
public static class LatencyUnitConverter
{
    /// <summary>
    /// Conversion factors from each unit to nanoseconds (base unit)
    /// </summary>
    private static readonly Dictionary<LatencyUnit, double> FactorsToNanoseconds = new()
    {
        { LatencyUnit.Nanoseconds, 1.0 },
        { LatencyUnit.Microseconds, 1_000.0 },
        { LatencyUnit.Milliseconds, 1_000_000.0 },
        { LatencyUnit.Seconds, 1_000_000_000.0 }
    };

    /// <summary>
    /// Converts a latency value from one unit to another.
    /// </summary>
    /// <param name="value">The value to convert</param>
    /// <param name="sourceUnit">The source unit</param>
    /// <param name="targetUnit">The target unit</param>
    /// <returns>The converted value</returns>
    /// <exception cref="ArgumentException">Thrown if units are invalid</exception>
    public static double Convert(double value, LatencyUnit sourceUnit, LatencyUnit targetUnit)
    {
        if (!FactorsToNanoseconds.ContainsKey(sourceUnit))
        {
            throw new ArgumentException($"Invalid source unit: {sourceUnit}", nameof(sourceUnit));
        }

        if (!FactorsToNanoseconds.ContainsKey(targetUnit))
        {
            throw new ArgumentException($"Invalid target unit: {targetUnit}", nameof(targetUnit));
        }

        if (sourceUnit == targetUnit)
        {
            return value;
        }

        // Convert to nanoseconds (base unit), then to target unit
        var nanoseconds = value * FactorsToNanoseconds[sourceUnit];
        var result = nanoseconds / FactorsToNanoseconds[targetUnit];

        return result;
    }

    /// <summary>
    /// Gets the conversion factor from source unit to target unit.
    /// </summary>
    public static double GetConversionFactor(LatencyUnit sourceUnit, LatencyUnit targetUnit)
    {
        if (sourceUnit == targetUnit)
        {
            return 1.0;
        }

        var sourceToNano = FactorsToNanoseconds[sourceUnit];
        var targetToNano = FactorsToNanoseconds[targetUnit];

        return sourceToNano / targetToNano;
    }
}
