namespace PerformanceEngine.Baseline.Domain.Domain.Baselines;

using PerformanceEngine.Baseline.Domain.Domain.Tolerances;

/// <summary>
/// Domain service that creates and validates baselines.
/// Encapsulates baseline creation logic and invariant enforcement.
/// </summary>
public class BaselineFactory
{
    /// <summary>
    /// Creates a new baseline with validation.
    /// </summary>
    /// <param name="metrics">Baseline metrics to capture</param>
    /// <param name="toleranceConfig">Tolerance configuration for comparisons</param>
    /// <param name="id">Baseline ID (generated if null)</param>
    /// <param name="createdAt">Creation timestamp (current time if null)</param>
    /// <returns>Validated baseline instance</returns>
    /// <exception cref="DomainInvariantViolatedException">If baseline is invalid</exception>
    public Baseline Create(
        IEnumerable<IMetric> metrics,
        ToleranceConfiguration toleranceConfig,
        BaselineId? id = null,
        DateTime? createdAt = null)
    {
        return new Baseline(metrics, toleranceConfig, id, createdAt);
    }

    /// <summary>
    /// Creates a baseline from existing metrics and generates default tolerance configuration.
    /// </summary>
    /// <param name="metrics">Baseline metrics</param>
    /// <param name="defaultToleranceAmount">Default tolerance amount for all metrics</param>
    /// <param name="defaultToleranceType">Default tolerance type for all metrics</param>
    /// <returns>Baseline with generated tolerance configuration</returns>
    public Baseline CreateWithDefaultTolerances(
        IEnumerable<IMetric> metrics,
        decimal defaultToleranceAmount,
        ToleranceType defaultToleranceType = ToleranceType.Absolute)
    {
        var metricsList = metrics?.ToList() ??
            throw new ArgumentNullException(nameof(metrics));

        if (metricsList.Count == 0)
            throw new ArgumentException("At least one metric is required.", nameof(metrics));

        // Create tolerance rules for each metric
        var tolerances = metricsList
            .Select(m => new Tolerance(m.MetricType, defaultToleranceType, defaultToleranceAmount))
            .ToList();

        var toleranceConfig = new ToleranceConfiguration(tolerances);
        return new Baseline(metricsList, toleranceConfig);
    }
}
