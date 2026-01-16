namespace PerformanceEngine.Application.Models;

/// <summary>
/// Represents the severity level of an evaluation rule.
/// Determines whether rule violations result in FAIL or WARN outcomes.
/// </summary>
public enum SeverityLevel
{
    /// <summary>
    /// Non-critical rule violation results in WARN outcome.
    /// Evaluation can still pass if only non-critical rules fail.
    /// </summary>
    NonCritical = 0,

    /// <summary>
    /// Critical rule violation results in FAIL outcome.
    /// Evaluation cannot pass if any critical rule fails.
    /// </summary>
    Critical = 1
}
