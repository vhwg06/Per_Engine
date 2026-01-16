namespace PerformanceEngine.Application.Models;

/// <summary>
/// Immutable record representing a rule violation with complete diagnostic details.
/// Captures what rule failed, why it failed, and which metric caused the failure.
/// </summary>
public sealed record Violation
{
    /// <summary>
    /// Unique identifier of the rule that was violated.
    /// </summary>
    public required string RuleId { get; init; }

    /// <summary>
    /// Human-readable name of the rule.
    /// </summary>
    public required string RuleName { get; init; }

    /// <summary>
    /// Expected threshold or constraint that was violated.
    /// </summary>
    public required double ExpectedThreshold { get; init; }

    /// <summary>
    /// Actual value observed that caused the violation.
    /// </summary>
    public required double ActualValue { get; init; }

    /// <summary>
    /// Name of the metric that caused the violation.
    /// </summary>
    public required string AffectedMetricName { get; init; }

    /// <summary>
    /// Severity level of this violation (Critical or NonCritical).
    /// </summary>
    public required SeverityLevel Severity { get; init; }

    /// <summary>
    /// Human-readable message explaining the violation.
    /// </summary>
    public string? Message { get; init; }

    /// <summary>
    /// Returns a deterministic string representation for debugging and logging.
    /// </summary>
    public override string ToString()
    {
        var severityText = Severity == SeverityLevel.Critical ? "CRITICAL" : "WARNING";
        return $"[{severityText}] {RuleId} ({RuleName}): {AffectedMetricName} = {ActualValue:F2} (expected {ExpectedThreshold:F2})";
    }
}
