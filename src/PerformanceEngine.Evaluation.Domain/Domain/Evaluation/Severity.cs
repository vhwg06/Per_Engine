namespace PerformanceEngine.Evaluation.Domain.Domain.Evaluation;

/// <summary>
/// Represents the severity level of an evaluation result.
/// Provides severity escalation logic for aggregating multiple outcomes.
/// </summary>
public enum Severity
{
    /// <summary>
    /// Evaluation passed - no violations detected.
    /// </summary>
    PASS = 0,

    /// <summary>
    /// Evaluation produced warnings - minor violations detected.
    /// </summary>
    WARN = 1,

    /// <summary>
    /// Evaluation failed - critical violations detected.
    /// </summary>
    FAIL = 2
}

/// <summary>
/// Extension methods for Severity enum.
/// </summary>
public static class SeverityExtensions
{
    /// <summary>
    /// Escalates severity level: FAIL > WARN > PASS.
    /// Returns the more severe of two severity levels.
    /// </summary>
    /// <param name="current">Current severity level.</param>
    /// <param name="other">Other severity level to compare.</param>
    /// <returns>The more severe of the two levels.</returns>
    public static Severity Escalate(this Severity current, Severity other)
    {
        return (Severity)Math.Max((int)current, (int)other);
    }

    /// <summary>
    /// Determines the overall severity from a collection of results.
    /// Returns the most severe level present.
    /// </summary>
    /// <param name="severities">Collection of severity levels.</param>
    /// <returns>The most severe level, or PASS if collection is empty.</returns>
    public static Severity MostSevere(this IEnumerable<Severity> severities)
    {
        return severities.DefaultIfEmpty(Severity.PASS).Max();
    }
}
