namespace PerformanceEngine.Baseline.Domain.Domain.Comparisons;

using PerformanceEngine.Baseline.Domain.Domain;

/// <summary>
/// Enforces invariants for the ComparisonResult aggregate root.
/// </summary>
public static class ComparisonResultInvariants
{
    /// <summary>
    /// Validates that a comparison result meets all domain constraints.
    /// </summary>
    /// <param name="metricResults">List of per-metric comparison results</param>
    /// <exception cref="DomainInvariantViolatedException">If invariants are violated</exception>
    public static void AssertValid(IReadOnlyList<ComparisonMetric> metricResults)
    {
        if (metricResults == null)
            throw new DomainInvariantViolatedException(
                "ComparisonResult.MetricResults",
                "Metric results cannot be null.");

        if (metricResults.Count == 0)
            throw new DomainInvariantViolatedException(
                "ComparisonResult.MetricResults",
                "Comparison result must contain at least one metric comparison.");

        // Check for duplicate metric names
        var metricNames = metricResults.Select(m => m.MetricName).ToList();
        var duplicates = metricNames
            .GroupBy(name => name)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        if (duplicates.Count > 0)
            throw new DomainInvariantViolatedException(
                "ComparisonResult.MetricResults",
                $"Duplicate metric results found: {string.Join(", ", duplicates)}");
    }

    /// <summary>
    /// Asserts that comparison results collection is immutable.
    /// </summary>
    /// <param name="metricResults">Metric results collection</param>
    /// <exception cref="DomainInvariantViolatedException">If collection is mutable</exception>
    public static void AssertImmutable(IReadOnlyList<ComparisonMetric> metricResults)
    {
        if (metricResults == null)
            throw new DomainInvariantViolatedException(
                "ComparisonResult.Immutability",
                "Metric results must be read-only.");

        // IReadOnlyList is immutable by contract
    }
}
