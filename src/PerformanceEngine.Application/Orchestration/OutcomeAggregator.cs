namespace PerformanceEngine.Application.Orchestration;

using PerformanceEngine.Application.Models;

/// <summary>
/// Determines the final outcome by aggregating violations and completeness information.
/// Applies outcome precedence rules: FAIL > WARN > INCONCLUSIVE > PASS
/// </summary>
public sealed class OutcomeAggregator
{
    private const double CompletenessThreshold = 0.5; // 50%

    /// <summary>
    /// Determines the overall outcome based on violations and data completeness.
    /// </summary>
    /// <param name="violations">Collection of rule violations.</param>
    /// <param name="completenessReport">Report on data availability.</param>
    /// <returns>Overall evaluation outcome.</returns>
    public Outcome DetermineOutcome(
        IReadOnlyList<Violation> violations,
        CompletenessReport completenessReport)
    {
        // Rule 1: If completeness below threshold → INCONCLUSIVE (highest precedence after FAIL)
        if (completenessReport.CompletenessPercentage < CompletenessThreshold)
        {
            return Outcome.INCONCLUSIVE;
        }

        // Rule 2: If any critical violation → FAIL
        var hasCriticalViolation = violations.Any(v => v.Severity == SeverityLevel.Critical);
        if (hasCriticalViolation)
        {
            return Outcome.FAIL;
        }

        // Rule 3: If any non-critical violation → WARN
        var hasNonCriticalViolation = violations.Any(v => v.Severity == SeverityLevel.NonCritical);
        if (hasNonCriticalViolation)
        {
            return Outcome.WARN;
        }

        // Rule 4: No violations and sufficient data → PASS
        return Outcome.PASS;
    }
}
