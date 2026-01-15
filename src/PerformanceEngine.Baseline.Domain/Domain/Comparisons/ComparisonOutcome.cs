namespace PerformanceEngine.Baseline.Domain.Domain.Comparisons;

/// <summary>
/// Enumeration of possible comparison outcomes between current metrics and baseline.
/// </summary>
public enum ComparisonOutcome
{
    /// <summary>
    /// Current performance is better than baseline (within configured tolerance).
    /// </summary>
    Improvement = 0,

    /// <summary>
    /// Current performance is worse than baseline (exceeds configured tolerance).
    /// </summary>
    Regression = 1,

    /// <summary>
    /// Current performance is within configured tolerance; no significant change detected.
    /// </summary>
    NoSignificantChange = 2,

    /// <summary>
    /// Confidence level is below threshold; result is inconclusive.
    /// </summary>
    Inconclusive = 3
}
