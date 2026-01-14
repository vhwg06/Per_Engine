namespace PerformanceEngine.Metrics.Domain.Aggregations;

using PerformanceEngine.Metrics.Domain.Metrics;

/// <summary>
/// Defines the contract for aggregating samples into a single metric result.
/// All implementations must guarantee deterministic, byte-identical results
/// when given identical sample collections and aggregation parameters.
/// </summary>
public interface IAggregationOperation
{
    /// <summary>
    /// Gets the unique name identifying this aggregation operation (e.g., "average", "p95").
    /// </summary>
    string OperationName { get; }

    /// <summary>
    /// Aggregates the provided samples using the specified window strategy.
    /// </summary>
    /// <param name="samples">Collection of samples to aggregate (must not be null).</param>
    /// <param name="window">Window strategy defining aggregation scope.</param>
    /// <returns>Aggregation result containing the computed metric value.</returns>
    /// <exception cref="ArgumentNullException">Thrown if samples or window is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown if aggregation cannot be performed on empty collection.</exception>
    AggregationResult Aggregate(SampleCollection samples, AggregationWindow window);
}
