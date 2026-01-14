namespace PerformanceEngine.Evaluation.Domain.Application.Dto;

/// <summary>
/// Data transfer object for batch evaluation requests.
/// Pairs metrics with rules for bulk evaluation.
/// </summary>
public sealed record EvaluationRequestDto
{
    /// <summary>
    /// Unique identifier for this evaluation request.
    /// </summary>
    public required string RequestId { get; init; }

    /// <summary>
    /// Identifier for the metric being evaluated.
    /// </summary>
    public required string MetricId { get; init; }

    /// <summary>
    /// Type of metric (e.g., "latency", "error_rate").
    /// </summary>
    public required string MetricType { get; init; }

    /// <summary>
    /// Collection of rule IDs to evaluate against this metric.
    /// </summary>
    public required List<string> RuleIds { get; init; }

    /// <summary>
    /// Optional metadata for this evaluation request.
    /// </summary>
    public Dictionary<string, string>? Metadata { get; init; }
}

/// <summary>
/// Data transfer object for batch evaluation responses.
/// Contains evaluation results for multiple metrics.
/// </summary>
public sealed record BatchEvaluationResultDto
{
    /// <summary>
    /// Overall batch request identifier.
    /// </summary>
    public required string BatchId { get; init; }

    /// <summary>
    /// Collection of individual evaluation results.
    /// </summary>
    public required List<MetricEvaluationResultDto> Results { get; init; }

    /// <summary>
    /// Timestamp when batch evaluation was performed (ISO 8601).
    /// </summary>
    public required string EvaluatedAt { get; init; }

    /// <summary>
    /// Summary statistics for the batch.
    /// </summary>
    public required BatchSummaryDto Summary { get; init; }
}

/// <summary>
/// Individual metric evaluation result within a batch.
/// </summary>
public sealed record MetricEvaluationResultDto
{
    /// <summary>
    /// Request ID that this result corresponds to.
    /// </summary>
    public required string RequestId { get; init; }

    /// <summary>
    /// Metric identifier.
    /// </summary>
    public required string MetricId { get; init; }

    /// <summary>
    /// The evaluation result for this metric.
    /// </summary>
    public required EvaluationResultDto EvaluationResult { get; init; }
}

/// <summary>
/// Summary statistics for batch evaluation.
/// </summary>
public sealed record BatchSummaryDto
{
    /// <summary>
    /// Total number of metrics evaluated.
    /// </summary>
    public required int TotalMetrics { get; init; }

    /// <summary>
    /// Number of metrics that passed all rules.
    /// </summary>
    public required int PassedMetrics { get; init; }

    /// <summary>
    /// Number of metrics that failed one or more rules.
    /// </summary>
    public required int FailedMetrics { get; init; }

    /// <summary>
    /// Number of metrics with warnings.
    /// </summary>
    public required int WarningMetrics { get; init; }

    /// <summary>
    /// Total number of violations detected.
    /// </summary>
    public required int TotalViolations { get; init; }
}
