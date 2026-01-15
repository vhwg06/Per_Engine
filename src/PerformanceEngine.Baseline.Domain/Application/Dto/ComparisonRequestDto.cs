namespace PerformanceEngine.Baseline.Domain.Application.Dto;

using System.Collections.Immutable;
using PerformanceEngine.Baseline.Domain.Domain.Tolerances;

/// <summary>
/// Data transfer object for comparison request information.
/// Encapsulates all data needed to perform a baseline comparison.
/// </summary>
public class ComparisonRequestDto
{
    /// <summary>
    /// Gets the unique identifier of the baseline to compare against.
    /// </summary>
    public required string BaselineId { get; init; }

    /// <summary>
    /// Gets the collection of current metrics to compare.
    /// </summary>
    public required IReadOnlyList<MetricDto> CurrentMetrics { get; init; }

    /// <summary>
    /// Gets the collection of tolerance configurations for comparison.
    /// </summary>
    public required IReadOnlyList<ToleranceDto> ToleranceDtos { get; init; }

    /// <summary>
    /// Gets the confidence threshold required to make conclusions.
    /// Values below this threshold result in Inconclusive outcome.
    /// </summary>
    public decimal ConfidenceThreshold { get; init; } = 0.5m;

    /// <summary>
    /// Gets the optional timestamp for this comparison request.
    /// If not provided, the current UTC time is used.
    /// </summary>
    public DateTime? ComparisonTime { get; init; }

    /// <summary>
    /// Creates a ToleranceConfiguration from the tolerance DTOs.
    /// </summary>
    public ToleranceConfiguration ToToleranceConfiguration() =>
        new(ToleranceDtos.Select(t => t.ToDomain()));

    /// <summary>
    /// Validates that the request contains required data.
    /// </summary>
    public bool IsValid() =>
        !string.IsNullOrWhiteSpace(BaselineId) &&
        CurrentMetrics.Count > 0 &&
        ToleranceDtos.Count > 0 &&
        ConfidenceThreshold >= 0m &&
        ConfidenceThreshold <= 1m;
}
