namespace PerformanceEngine.Metrics.Domain.Ports;

using PerformanceEngine.Metrics.Domain.Metrics;

/// <summary>
/// Port (abstraction) for metric-like entities that can be evaluated.
/// Enables engine-agnostic metric handling and enrichment integration.
/// </summary>
public interface IMetric
{
    /// <summary>
    /// Gets a unique identifier for this metric.
    /// </summary>
    Guid Id { get; }

    /// <summary>
    /// Gets the type of metric (e.g., "latency", "throughput", "error_rate").
    /// </summary>
    string MetricType { get; }

    /// <summary>
    /// Gets the aggregated value for this metric.
    /// </summary>
    double Value { get; }

    /// <summary>
    /// Gets the unit of measurement (e.g., "ms", "percent", "requests/sec").
    /// </summary>
    string Unit { get; }

    /// <summary>
    /// Gets the timestamp when this metric was computed.
    /// </summary>
    DateTime ComputedAt { get; }

    /// <summary>
    /// Gets the completeness/reliability status of this metric.
    /// COMPLETE: all required samples collected, safe for evaluation decisions
    /// PARTIAL: incomplete data, may result in INCONCLUSIVE outcomes per policy
    /// </summary>
    CompletessStatus CompletessStatus { get; }

    /// <summary>
    /// Gets the evidence metadata explaining this metric's reliability.
    /// Includes sample count, required threshold, and aggregation window reference.
    /// </summary>
    MetricEvidence Evidence { get; }
}
