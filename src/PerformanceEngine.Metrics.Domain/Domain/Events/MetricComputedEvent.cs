namespace PerformanceEngine.Metrics.Domain.Events;

using PerformanceEngine.Metrics.Domain.Aggregations;
using PerformanceEngine.Metrics.Domain.Metrics;

/// <summary>
/// Domain event published when a metric is computed from aggregated samples.
/// Enables event-driven workflows and audit trails for metric computation.
/// </summary>
public sealed class MetricComputedEvent : IDomainEvent
{
    /// <summary>
    /// Gets the computed metric
    /// </summary>
    public Metric Metric { get; }

    /// <summary>
    /// Gets the aggregation operation that was performed
    /// </summary>
    public string AggregationOperation { get; }

    /// <summary>
    /// Gets the timestamp when the metric was computed
    /// </summary>
    public DateTime ComputedAt { get; }

    /// <summary>
    /// Gets the timestamp when this event occurred (interface implementation)
    /// </summary>
    public DateTime OccurredAt => ComputedAt;

    /// <summary>
    /// Creates a new MetricComputedEvent.
    /// </summary>
    /// <param name="metric">The computed metric (must not be null)</param>
    /// <param name="aggregationOperation">Name of the aggregation operation (e.g., "average", "p95")</param>
    /// <param name="computedAt">When the metric was computed</param>
    /// <exception cref="ArgumentNullException">Thrown if metric or aggregationOperation is null</exception>
    /// <exception cref="ArgumentException">Thrown if aggregationOperation is empty</exception>
    public MetricComputedEvent(Metric metric, string aggregationOperation, DateTime computedAt)
    {
        Metric = metric ?? throw new ArgumentNullException(nameof(metric));
        AggregationOperation = string.IsNullOrWhiteSpace(aggregationOperation)
            ? throw new ArgumentException("Aggregation operation cannot be null or empty", nameof(aggregationOperation))
            : aggregationOperation.Trim();
        ComputedAt = computedAt;
    }
}
