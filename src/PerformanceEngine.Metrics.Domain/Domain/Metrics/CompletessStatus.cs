namespace PerformanceEngine.Metrics.Domain.Metrics;

/// <summary>
/// Indicates the completeness/reliability status of a metric.
/// Used by evaluation domain to decide whether to include or skip the metric.
/// </summary>
public enum CompletessStatus
{
    /// <summary>
    /// All required samples collected; metric is reliable.
    /// Threshold defined by Metrics Domain aggregation configuration.
    /// Safe for evaluation decision-making.
    /// </summary>
    COMPLETE = 1,

    /// <summary>
    /// Incomplete data; metric is partial.
    /// Less than required samples for complete status.
    /// Should be treated cautiously in evaluation rules;
    /// may result in INCONCLUSIVE outcomes per policy.
    /// </summary>
    PARTIAL = 2
}
