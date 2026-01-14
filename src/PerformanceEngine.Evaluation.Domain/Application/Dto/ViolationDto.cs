namespace PerformanceEngine.Evaluation.Domain.Application.Dto;

/// <summary>
/// Data transfer object for violations.
/// Serializable representation of domain violations.
/// </summary>
public sealed record ViolationDto
{
    /// <summary>
    /// Unique identifier of the rule that was violated.
    /// </summary>
    public required string RuleId { get; init; }

    /// <summary>
    /// Name of the metric that violated the rule.
    /// </summary>
    public required string MetricName { get; init; }

    /// <summary>
    /// Actual value observed that caused the violation.
    /// </summary>
    public required double ActualValue { get; init; }

    /// <summary>
    /// Expected threshold or constraint that was violated.
    /// </summary>
    public required double Threshold { get; init; }

    /// <summary>
    /// Human-readable diagnostic message explaining the violation.
    /// </summary>
    public required string Message { get; init; }
}
