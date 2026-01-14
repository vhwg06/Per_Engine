namespace PerformanceEngine.Metrics.Domain.Aggregations;

using PerformanceEngine.Metrics.Domain.Metrics;

/// <summary>
/// Computes the average (mean) latency of all samples in a collection.
/// Guarantees deterministic results by normalizing all samples to milliseconds
/// before computation and maintaining exact decimal precision.
/// </summary>
public sealed class AverageAggregation : IAggregationOperation
{
    /// <summary>
    /// Gets the operation identifier for average aggregation.
    /// </summary>
    public string OperationName => "average";

    /// <summary>
    /// Computes the average latency across all samples.
    /// </summary>
    /// <param name="samples">Collection of samples to average (must not be null or empty).</param>
    /// <param name="window">Window strategy for aggregation scope.</param>
    /// <returns>AggregationResult containing the computed average latency in milliseconds.</returns>
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
            throw new InvalidOperationException("Cannot compute average of empty sample collection");

        // Normalize all samples to milliseconds for consistent computation
        var normalized = AggregationNormalizer.NormalizeSamples(samples, LatencyUnit.Milliseconds);
        var normalizedSnapshot = normalized.GetSnapshot();

        // Sum all latencies in milliseconds
        decimal totalMilliseconds = 0;
        foreach (var sample in normalizedSnapshot)
        {
            totalMilliseconds += (decimal)sample.Duration.Value;
        }

        // Compute average with exact decimal precision
        var averageMs = totalMilliseconds / normalizedSnapshot.Count;

        return new AggregationResult(
            new Latency((double)averageMs, LatencyUnit.Milliseconds),
            OperationName,
            DateTime.UtcNow);
    }
}
