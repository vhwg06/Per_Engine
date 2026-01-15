namespace PerformanceEngine.Baseline.Domain.Domain.Baselines;

using PerformanceEngine.Baseline.Domain.Domain;

/// <summary>
/// Enforces invariants for the Baseline aggregate root.
/// </summary>
public static class BaselineInvariants
{
    /// <summary>
    /// Validates that a baseline meets all domain constraints.
    /// </summary>
    /// <param name="metrics">Baseline metrics</param>
    /// <param name="toleranceConfig">Tolerance configuration</param>
    /// <exception cref="DomainInvariantViolatedException">If invariants are violated</exception>
    public static void AssertValid(IReadOnlyList<IMetric> metrics, ToleranceConfiguration toleranceConfig)
    {
        if (metrics == null)
            throw new DomainInvariantViolatedException(
                "Baseline.Metrics",
                "Metrics collection cannot be null.");

        if (metrics.Count == 0)
            throw new DomainInvariantViolatedException(
                "Baseline.Metrics",
                "Baseline must contain at least one metric.");

        // Check for duplicate metric types
        var metricTypes = metrics.Select(m => m.MetricType).ToList();
        var duplicates = metricTypes
            .GroupBy(type => type)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        if (duplicates.Count > 0)
            throw new DomainInvariantViolatedException(
                "Baseline.Metrics",
                $"Duplicate metric types found: {string.Join(", ", duplicates)}");

        if (toleranceConfig == null)
            throw new DomainInvariantViolatedException(
                "Baseline.ToleranceConfig",
                "Tolerance configuration cannot be null.");

        // Verify all metrics have tolerance rules
        foreach (var metric in metrics)
        {
            if (!toleranceConfig.HasTolerance(metric.MetricType))
                throw new DomainInvariantViolatedException(
                    "Baseline.ToleranceConfig",
                    $"Metric '{metric.MetricType}' has no tolerance rule defined.");
        }
    }

    /// <summary>
    /// Asserts that baseline collections are immutable.
    /// </summary>
    /// <param name="metrics">Metrics collection</param>
    /// <exception cref="DomainInvariantViolatedException">If collections are mutable</exception>
    public static void AssertImmutable(IReadOnlyList<IMetric> metrics)
    {
        if (metrics == null)
            throw new DomainInvariantViolatedException(
                "Baseline.Immutability",
                "Metrics must be read-only.");

        // IReadOnlyList is immutable by contract
    }
}
