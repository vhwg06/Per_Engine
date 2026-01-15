namespace PerformanceEngine.Baseline.Domain.Domain.Comparisons;

using PerformanceEngine.Baseline.Domain.Domain.Confidence;

/// <summary>
/// Pure domain service that aggregates per-metric outcomes into overall comparison result.
/// Uses worst-case aggregation strategy: REGRESSION > IMPROVEMENT > NO_SIGNIFICANT_CHANGE > INCONCLUSIVE
/// </summary>
public class OutcomeAggregator
{
    /// <summary>
    /// Aggregates per-metric outcomes to determine overall comparison outcome.
    /// Worst-case strategy: any REGRESSION → result is REGRESSION, etc.
    /// </summary>
    /// <param name="metrics">List of per-metric comparisons</param>
    /// <returns>Aggregated overall outcome</returns>
    public ComparisonOutcome Aggregate(IEnumerable<ComparisonMetric> metrics)
    {
        if (metrics == null)
            throw new ArgumentNullException(nameof(metrics));

        var metricsList = metrics.ToList();

        if (metricsList.Count == 0)
            return ComparisonOutcome.Inconclusive;

        // Check in priority order
        if (metricsList.Any(m => m.Outcome == ComparisonOutcome.Regression))
            return ComparisonOutcome.Regression;

        if (metricsList.Any(m => m.Outcome == ComparisonOutcome.Improvement))
            return ComparisonOutcome.Improvement;

        if (metricsList.Any(m => m.Outcome == ComparisonOutcome.NoSignificantChange))
            return ComparisonOutcome.NoSignificantChange;

        return ComparisonOutcome.Inconclusive;
    }

    /// <summary>
    /// Aggregates per-metric confidence levels to determine overall confidence.
    /// Uses minimum confidence strategy: overall confidence = lowest metric confidence.
    /// </summary>
    /// <param name="metrics">List of per-metric comparisons</param>
    /// <returns>Aggregated overall confidence (minimum)</returns>
    public ConfidenceLevel AggregateConfidence(IEnumerable<ComparisonMetric> metrics)
    {
        if (metrics == null)
            throw new ArgumentNullException(nameof(metrics));

        var metricsList = metrics.ToList();

        if (metricsList.Count == 0)
            return new ConfidenceLevel(0.0m);

        // Return minimum confidence from all metrics
        var minConfidence = metricsList.Min(m => m.Confidence.Value);
        return new ConfidenceLevel(minConfidence);
    }
}
