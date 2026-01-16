namespace PerformanceEngine.Application.Models;

/// <summary>
/// Immutable record representing the complete result of a performance evaluation.
/// Contains outcome, violations, completeness information, traceability metadata, and data integrity fingerprint.
/// Thread-safe and deterministically serializable.
/// </summary>
public sealed record EvaluationResult
{
    /// <summary>
    /// Overall outcome of the evaluation (PASS, WARN, FAIL, INCONCLUSIVE).
    /// </summary>
    public required Outcome Outcome { get; init; }

    /// <summary>
    /// All violations detected during evaluation, sorted deterministically by rule ID.
    /// </summary>
    public required IReadOnlyList<Violation> Violations { get; init; }

    /// <summary>
    /// Report on data completeness and availability.
    /// </summary>
    public required CompletenessReport CompletenessReport { get; init; }

    /// <summary>
    /// Metadata about the evaluation execution (profile used, timestamps, etc.).
    /// </summary>
    public required ExecutionMetadata Metadata { get; init; }

    /// <summary>
    /// Deterministic SHA256 fingerprint of the actual collected metrics data.
    /// Used for data integrity verification.
    /// </summary>
    public required string DataFingerprint { get; init; }

    /// <summary>
    /// Indicates whether the evaluation passed (no critical violations).
    /// </summary>
    public bool IsPassing => Outcome == Outcome.PASS;

    /// <summary>
    /// Indicates whether the evaluation failed (critical violations present).
    /// </summary>
    public bool IsFailing => Outcome == Outcome.FAIL;

    /// <summary>
    /// Indicates whether the evaluation has warnings (non-critical violations).
    /// </summary>
    public bool HasWarnings => Outcome == Outcome.WARN;

    /// <summary>
    /// Indicates whether the evaluation was inconclusive (insufficient data).
    /// </summary>
    public bool IsInconclusive => Outcome == Outcome.INCONCLUSIVE;

    /// <summary>
    /// Returns a human-readable summary of the evaluation result.
    /// </summary>
    public override string ToString()
    {
        var violationCount = Violations.Count;
        var completeness = (CompletenessReport.CompletenessPercentage * 100).ToString("F1");
        return $"EvaluationResult: {Outcome} | {violationCount} violation(s) | {completeness}% complete | Profile: {Metadata.ProfileId}";
    }
}
