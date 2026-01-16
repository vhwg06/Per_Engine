namespace PerformanceEngine.Application.Models;

/// <summary>
/// Represents the overall outcome of a performance evaluation.
/// Determines the final verdict based on all rules and data availability.
/// Precedence: FAIL > WARN > INCONCLUSIVE > PASS
/// </summary>
public enum Outcome
{
    /// <summary>
    /// All evaluation rules passed with complete data.
    /// </summary>
    PASS = 0,

    /// <summary>
    /// One or more non-critical rules failed, but no critical failures.
    /// </summary>
    WARN = 1,

    /// <summary>
    /// One or more critical rules failed.
    /// </summary>
    FAIL = 2,

    /// <summary>
    /// Insufficient data to determine outcome (less than 50% metrics available).
    /// </summary>
    INCONCLUSIVE = 3
}

/// <summary>
/// Extension methods for Outcome enum providing precedence rules.
/// </summary>
public static class OutcomeExtensions
{
    /// <summary>
    /// Determines the more severe outcome between two outcomes.
    /// Precedence: FAIL greater than WARN greater than INCONCLUSIVE greater than PASS
    /// </summary>
    public static Outcome MostSevere(this Outcome current, Outcome other)
    {
        return (Outcome)Math.Max((int)current, (int)other);
    }

    /// <summary>
    /// Aggregates multiple outcomes into a single outcome.
    /// Returns the most severe outcome in the collection.
    /// </summary>
    public static Outcome Aggregate(this IEnumerable<Outcome> outcomes)
    {
        return outcomes.DefaultIfEmpty(Outcome.PASS).Max();
    }
}
