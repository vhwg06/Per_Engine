namespace PerformanceEngine.Metrics.Domain.Aggregations;

using System.Collections.Immutable;
using PerformanceEngine.Metrics.Domain.Metrics;

/// <summary>
/// Computes the percentile value at a specified rank (e.g., p95, p99).
/// Uses the deterministic nearest-rank algorithm to ensure byte-identical results
/// across multiple runs with identical inputs.
/// </summary>
public sealed class PercentileAggregation : IAggregationOperation
{
    private readonly Percentile _percentile;

    /// <summary>
    /// Creates a new percentile aggregation for the specified percentile rank.
    /// </summary>
    /// <param name="percentile">Percentile rank (0-100) to compute.</param>
    /// <exception cref="ArgumentException">Thrown if percentile is outside valid range.</exception>
    public PercentileAggregation(Percentile percentile)
    {
        _percentile = percentile ?? throw new ArgumentNullException(nameof(percentile));
    }

    /// <summary>
    /// Gets the operation identifier for this percentile aggregation (e.g., "p95").
    /// </summary>
    public string OperationName => $"p{_percentile.Value:F0}";

    /// <summary>
    /// Computes the percentile latency using the nearest-rank algorithm.
    /// </summary>
    /// <param name="samples">Collection of samples to compute percentile from (must not be null or empty).</param>
    /// <param name="window">Window strategy for aggregation scope.</param>
    /// <returns>AggregationResult containing the percentile latency value in milliseconds.</returns>
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
            throw new InvalidOperationException("Cannot compute percentile of empty sample collection");

        // Normalize all samples to milliseconds for consistent computation
        var normalized = AggregationNormalizer.NormalizeSamples(samples, LatencyUnit.Milliseconds);
        var normalizedSnapshot = normalized.GetSnapshot();

        // Sort latencies in ascending order
        var sortedLatencies = normalizedSnapshot
            .Select(s => (decimal)s.Duration.Value)
            .OrderBy(v => v)
            .ToImmutableList();

        // Compute percentile rank using nearest-rank algorithm
        // Formula: rank = ceil(p/100 * n), where n is sample count
        // Then return value at rank-1 (0-indexed)
        var percentileValue = _percentile.Value;
        var n = sortedLatencies.Count;
        
        // Nearest-rank algorithm: rank = ceil(percentile/100 * count)
        var rank = (int)Math.Ceiling(percentileValue / 100.0 * n);
        
        // Ensure rank is within bounds
        rank = Math.Max(1, Math.Min(rank, n));
        
        // Convert to 0-based index
        var index = rank - 1;
        var percentileLatency = sortedLatencies[index];

        return new AggregationResult(
            new Latency((double)percentileLatency, LatencyUnit.Milliseconds),
            OperationName,
            DateTime.UtcNow);
    }
}

