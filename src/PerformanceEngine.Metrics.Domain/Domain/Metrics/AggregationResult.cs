namespace PerformanceEngine.Metrics.Domain.Metrics;

/// <summary>
/// Represents the result of an aggregation operation.
/// Contains the computed value, its unit, and when it was computed.
/// </summary>
public sealed class AggregationResult : ValueObject
{
    /// <summary>
    /// Gets the aggregated latency value
    /// </summary>
    public Latency Value { get; }

    /// <summary>
    /// Gets the timestamp when this aggregation was computed
    /// </summary>
    public DateTime ComputedAt { get; }

    /// <summary>
    /// Gets the name of the aggregation operation that produced this result
    /// (e.g., "p95", "average", "max")
    /// </summary>
    public string OperationName { get; }

    /// <summary>
    /// Initializes a new instance of the AggregationResult class.
    /// </summary>
    /// <param name="value">The aggregated latency value</param>
    /// <param name="operationName">The name of the aggregation operation</param>
    /// <param name="computedAt">When this aggregation was computed (defaults to now if not provided)</param>
    /// <exception cref="ArgumentNullException">Thrown when value is null</exception>
    /// <exception cref="ArgumentException">Thrown when operationName is null or empty</exception>
    public AggregationResult(Latency value, string operationName, DateTime? computedAt = null)
    {
        if (value == null)
        {
            throw new ArgumentNullException(nameof(value));
        }

        if (string.IsNullOrWhiteSpace(operationName))
        {
            throw new ArgumentException("Operation name cannot be null or empty", nameof(operationName));
        }

        Value = value;
        OperationName = operationName.Trim();
        ComputedAt = computedAt ?? DateTime.UtcNow;
    }

    protected override IEnumerable<object?> GetAtomicValues()
    {
        yield return Value;
        yield return OperationName;
        yield return ComputedAt;
    }

    public override string ToString()
    {
        return $"{OperationName}: {Value} (computed: {ComputedAt:O})";
    }
}
