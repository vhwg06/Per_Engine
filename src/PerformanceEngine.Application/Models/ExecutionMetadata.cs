namespace PerformanceEngine.Application.Models;

/// <summary>
/// Immutable record containing metadata about the evaluation execution.
/// Provides traceability: which profile was used, which data was evaluated, and when.
/// </summary>
public sealed record ExecutionMetadata
{
    /// <summary>
    /// Identifier of the profile that was applied during evaluation.
    /// </summary>
    public required string ProfileId { get; init; }

    /// <summary>
    /// Name of the profile that was applied.
    /// </summary>
    public required string ProfileName { get; init; }

    /// <summary>
    /// Timestamp when the evaluation was performed (UTC).
    /// </summary>
    public required DateTime EvaluatedAt { get; init; }

    /// <summary>
    /// Number of evaluation rules that were executed.
    /// </summary>
    public required int RulesEvaluatedCount { get; init; }

    /// <summary>
    /// Number of evaluation rules that were skipped due to missing data.
    /// </summary>
    public required int RulesSkippedCount { get; init; }

    /// <summary>
    /// Execution context information.
    /// </summary>
    public required ExecutionContext ExecutionContext { get; init; }

    /// <summary>
    /// Optional additional metadata about thresholds or configuration used.
    /// </summary>
    public IReadOnlyDictionary<string, string>? AdditionalMetadata { get; init; }
}
