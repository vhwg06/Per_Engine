namespace PerformanceEngine.Metrics.Domain.Aggregations;

using PerformanceEngine.Metrics.Domain.Metrics;

/// <summary>
/// Finds the minimum latency value among all samples in a collection.
/// Guarantees deterministic results by normalizing all samples to milliseconds
/// and returning the exact minimum value.
/// </summary>
public sealed class MinAggregation : IAggregationOperation
{
    /// <summary>
    /// Gets the operation identifier for minimum aggregation.
    /// </summary>
    public string OperationName => "min";

    /// <summary>
    /// Finds the minimum latency across all samples.
    /// </summary>
    /// <param name="samples">Collection of samples to find minimum from (must not be null or empty).</param>
    /// <param name="window">Window strategy for aggregation scope.</param>
    /// <returns>AggregationResult containing the minimum latency value in milliseconds.</returns>
    /// <exception cref="ArgumentNullException">Thrown if samples or window is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown if collection is empty.</exception>
    public AggregationResult Aggregate(SampleCollection samples, AggregationWindow window)
    {
        if (samples is null)
            throw new ArgumentNullException(nameof(samples), "Sample collection cannot be null");
        if (window is null)
            throw new ArgumentNullException(nameof(window), "Aggregation window cannot be null");

        var snapshot = samples.GetSnapshot();
        if (snapshot.Count == 0)
            throw new InvalidOperationException("Cannot compute minimum of empty sample collection");

        // Normalize all samples to milliseconds for consistent computation
        var normalized = AggregationNormalizer.NormalizeSamples(samples, LatencyUnit.Milliseconds);
        var normalizedSnapshot = normalized.GetSnapshot();

        // Find minimum value
        decimal minMs = decimal.MaxValue;
        foreach (var sample in normalizedSnapshot)
        {
            var value = (decimal)sample.Duration.Value;
            if (value < minMs)
                minMs = value;
        }

        return new AggregationResult(
            minMs,
            OperationName,
            DateTime.UtcNow);
    }
}
