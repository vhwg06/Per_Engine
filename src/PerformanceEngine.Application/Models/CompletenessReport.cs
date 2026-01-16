namespace PerformanceEngine.Application.Models;

/// <summary>
/// Immutable record providing transparency into data availability during evaluation.
/// Reports which metrics were available, which were missing, and how this affected rule evaluation.
/// </summary>
public sealed record CompletenessReport
{
    /// <summary>
    /// Number of metrics that were actually provided.
    /// </summary>
    public required int MetricsProvidedCount { get; init; }

    /// <summary>
    /// Number of metrics that were expected (based on rules requirements).
    /// </summary>
    public required int MetricsExpectedCount { get; init; }

    /// <summary>
    /// Completeness ratio (0.0 to 1.0).
    /// Calculated as MetricsProvidedCount / MetricsExpectedCount.
    /// </summary>
    public required double CompletenessPercentage { get; init; }

    /// <summary>
    /// List of specific metrics that were expected but not provided.
    /// </summary>
    public required IReadOnlyList<string> MissingMetrics { get; init; }

    /// <summary>
    /// List of rule IDs that could not be evaluated due to missing metrics.
    /// </summary>
    public required IReadOnlyList<string> UnevaluatedRules { get; init; }

    /// <summary>
    /// Indicates whether completeness meets the threshold for a conclusive evaluation.
    /// Threshold is typically 50% (completeness >= 0.5).
    /// </summary>
    public bool IsSufficientForEvaluation => CompletenessPercentage >= 0.5;

    /// <summary>
    /// Returns a human-readable summary of completeness.
    /// </summary>
    public override string ToString()
    {
        var percentage = (CompletenessPercentage * 100).ToString("F1");
        return $"Completeness: {MetricsProvidedCount}/{MetricsExpectedCount} ({percentage}%) - {MissingMetrics.Count} missing, {UnevaluatedRules.Count} rules skipped";
    }
}
