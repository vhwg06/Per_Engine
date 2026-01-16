namespace PerformanceEngine.Evaluation.Domain;

/// <summary>
/// Immutable value object representing a reference to a metric and its value
/// at the time of evaluation.
/// 
/// Preserves exact metric values (including decimal precision) needed for
/// deterministic replay. Values stored as strings to avoid floating-point
/// precision loss.
/// 
/// Invariants:
/// - MetricName is non-empty
/// - Value string is non-empty and represents the exact original value
/// - Cannot be modified after construction
/// </summary>
public record MetricReference(
    /// <summary>Name of the referenced metric (e.g., "ResponseTime", "ErrorRate").</summary>
    string MetricName,
    
    /// <summary>Exact value of the metric at evaluation time (stored as string for precision).</summary>
    string Value
)
{
    /// <summary>
    /// Factory method with validation.
    /// </summary>
    public static MetricReference Create(string metricName, string value)
    {
        if (string.IsNullOrWhiteSpace(metricName))
            throw new ArgumentException("Metric name must not be empty", nameof(metricName));

        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Metric value must not be empty", nameof(value));

        return new MetricReference(MetricName: metricName, Value: value);
    }

    /// <summary>
    /// Overload for convenient creation from decimal values.
    /// </summary>
    public static MetricReference Create(string metricName, decimal value)
        => Create(metricName, value.ToString("F"));

    /// <summary>
    /// Overload for convenient creation from double values.
    /// </summary>
    public static MetricReference Create(string metricName, double value)
        => Create(metricName, value.ToString("F"));
}
