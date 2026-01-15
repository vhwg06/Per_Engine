namespace PerformanceEngine.Evaluation.Domain.Domain;

/// <summary>
/// Represents the outcome of an evaluation.
/// PASS: All constraints satisfied
/// FAIL: One or more constraints violated
/// INCONCLUSIVE: Insufficient data or partial evidence
/// </summary>
public enum Outcome
{
    /// <summary>
    /// Evaluation passed - all constraints satisfied
    /// </summary>
    PASS = 1,

    /// <summary>
    /// Evaluation failed - one or more constraints violated
    /// </summary>
    FAIL = 2,

    /// <summary>
    /// Evaluation inconclusive - insufficient data or partial metrics
    /// Allows distinguishing between "no issues found" and "insufficient data to evaluate"
    /// </summary>
    INCONCLUSIVE = 3
}
