namespace PerformanceEngine.Application.Orchestration;

using PerformanceEngine.Application.Models;
using PerformanceEngine.Profile.Domain.Domain.Profiles;

/// <summary>
/// Constructs immutable EvaluationResult objects from orchestration outputs.
/// Assembles all components into the final result structure.
/// </summary>
public sealed class ResultConstructor
{
    /// <summary>
    /// Constructs an immutable evaluation result from all orchestration components.
    /// </summary>
    /// <param name="outcome">Overall evaluation outcome.</param>
    /// <param name="violations">Collection of rule violations.</param>
    /// <param name="completenessReport">Data completeness report.</param>
    /// <param name="profile">Profile that was applied.</param>
    /// <param name="executionContext">Execution context information.</param>
    /// <param name="dataFingerprint">Deterministic fingerprint of metrics data.</param>
    /// <param name="evaluationTimestamp">When evaluation was performed.</param>
    /// <param name="totalRulesCount">Total number of rules available.</param>
    /// <returns>Immutable evaluation result.</returns>
    public EvaluationResult ConstructResult(
        Outcome outcome,
        IReadOnlyList<Violation> violations,
        CompletenessReport completenessReport,
        ResolvedProfile profile,
        ExecutionContext executionContext,
        string dataFingerprint,
        DateTime evaluationTimestamp,
        int totalRulesCount)
    {
        // Build execution metadata
        var metadata = new ExecutionMetadata
        {
            ProfileId = profile.ToString(), // Use profile string representation
            ProfileName = "Resolved Profile", // Default name
            EvaluatedAt = evaluationTimestamp,
            RulesEvaluatedCount = totalRulesCount - completenessReport.UnevaluatedRules.Count,
            RulesSkippedCount = completenessReport.UnevaluatedRules.Count,
            ExecutionContext = executionContext
        };

        // Construct final result
        return new EvaluationResult
        {
            Outcome = outcome,
            Violations = violations,
            CompletenessReport = completenessReport,
            Metadata = metadata,
            DataFingerprint = dataFingerprint
        };
    }
}
