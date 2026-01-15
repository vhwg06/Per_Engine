namespace PerformanceEngine.Baseline.Domain.Application.Dto;

/// <summary>
/// Data transfer object for metric information.
/// Used to transfer metric data between application and infrastructure layers.
/// </summary>
public class MetricDto
{
    /// <summary>
    /// Gets the unique identifier for this metric type.
    /// </summary>
    public required string MetricType { get; init; }

    /// <summary>
    /// Gets the numeric value of this metric.
    /// </summary>
    public required decimal Value { get; init; }

    /// <summary>
    /// Gets the unit of measurement for this metric (e.g., "ms", "MB", "req/s").
    /// </summary>
    public required string Unit { get; init; }

    /// <summary>
    /// Gets the timestamp when this metric was collected.
    /// </summary>
    public required DateTime CollectedAt { get; init; }
}
