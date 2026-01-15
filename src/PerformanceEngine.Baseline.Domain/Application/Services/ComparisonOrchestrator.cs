namespace PerformanceEngine.Baseline.Domain.Application.Services;

using System.Collections.Immutable;
using PerformanceEngine.Baseline.Domain.Domain.Baselines;
using PerformanceEngine.Baseline.Domain.Domain.Comparisons;
using PerformanceEngine.Baseline.Domain.Domain.Confidence;
using PerformanceEngine.Baseline.Domain.Domain.Ports;
using PerformanceEngine.Baseline.Domain.Domain.Tolerances;

/// <summary>
/// Orchestrator service that coordinates baseline management and performance comparison operations.
/// Bridges the domain layer with the application/infrastructure layers.
/// </summary>
public class ComparisonOrchestrator
{
    private readonly IBaselineRepository _baselineRepository;

    public ComparisonOrchestrator(IBaselineRepository baselineRepository)
    {
        _baselineRepository = baselineRepository ?? throw new ArgumentNullException(nameof(baselineRepository));
    }

    /// <summary>
    /// Creates a new baseline from the provided metrics and tolerance configuration.
    /// </summary>
    /// <param name="metrics">The metrics to include in the baseline.</param>
    /// <param name="toleranceConfig">The tolerance configuration for metric comparisons.</param>
    /// <returns>The ID of the created baseline.</returns>
    /// <exception cref="ArgumentNullException">Thrown if metrics or toleranceConfig is null.</exception>
    /// <exception cref="BaselineDomainException">Thrown if baseline creation fails due to domain invariant violations.</exception>
    public async Task<BaselineId> CreateBaselineAsync(
        IEnumerable<IMetric> metrics,
        ToleranceConfiguration toleranceConfig)
    {
        ArgumentNullException.ThrowIfNull(metrics);
        ArgumentNullException.ThrowIfNull(toleranceConfig);

        // Create baseline using domain factory
        var baseline = new Baseline(metrics, toleranceConfig);

        // Persist to repository
        await _baselineRepository.SaveAsync(baseline);

        return baseline.Id;
    }

    /// <summary>
    /// Performs a comparison between an existing baseline and current metrics.
    /// </summary>
    /// <param name="baselineId">The ID of the baseline to compare against.</param>
    /// <param name="currentMetrics">The current metrics to compare.</param>
    /// <param name="toleranceConfig">The tolerance configuration for this comparison.</param>
    /// <param name="confidenceThreshold">The confidence threshold for drawing conclusions (default 0.5).</param>
    /// <returns>The comparison result with per-metric and overall outcomes.</returns>
    /// <exception cref="BaselineNotFoundException">Thrown if the baseline ID doesn't exist.</exception>
    /// <exception cref="MetricNotFoundException">Thrown if metric names don't match between baseline and current metrics.</exception>
    /// <exception cref="ArgumentNullException">Thrown if required parameters are null.</exception>
    public async Task<ComparisonResult> CompareAsync(
        BaselineId baselineId,
        IEnumerable<IMetric> currentMetrics,
        ToleranceConfiguration toleranceConfig,
        decimal confidenceThreshold = 0.5m)
    {
        ArgumentNullException.ThrowIfNull(baselineId);
        ArgumentNullException.ThrowIfNull(currentMetrics);
        ArgumentNullException.ThrowIfNull(toleranceConfig);

        // Retrieve baseline from repository
        var baseline = await _baselineRepository.GetAsync(baselineId);
        if (baseline == null)
        {
            throw new BaselineNotFoundException($"Baseline with ID '{baselineId}' not found.");
        }

        // Convert to list for repeated access
        var currentMetricsList = currentMetrics.ToList();
        var baselineMetricsList = baseline.Metrics.ToList();

        // Verify metric alignment
        var baselineMetricNames = new HashSet<string>(baselineMetricsList.Select(m => m.MetricType));
        var currentMetricNames = new HashSet<string>(currentMetricsList.Select(m => m.MetricType));

        // Check for metric mismatches
        var missingInCurrent = baselineMetricNames.Except(currentMetricNames).ToList();
        if (missingInCurrent.Any())
        {
            throw new MetricNotFoundException($"Metrics missing in current data: {string.Join(", ", missingInCurrent)}");
        }

        var extraInCurrent = currentMetricNames.Except(baselineMetricNames).ToList();
        if (extraInCurrent.Any())
        {
            throw new MetricNotFoundException($"Extra metrics in current data: {string.Join(", ", extraInCurrent)}");
        }

        // Perform metric comparisons
        var calculator = new ComparisonCalculator();
        var comparisonMetrics = new List<ComparisonMetric>();

        foreach (var currentMetric in currentMetricsList)
        {
            var baselineMetric = baselineMetricsList.FirstOrDefault(m => m.MetricType == currentMetric.MetricType);
            if (baselineMetric == null)
            {
                continue; // Should not happen due to earlier checks
            }

            var tolerance = toleranceConfig.GetTolerance(currentMetric.MetricType);
            var comparisonMetric = calculator.CalculateMetric(baselineMetric.Value, currentMetric.Value, tolerance);
            comparisonMetrics.Add(comparisonMetric);
        }

        // Aggregate results
        var aggregator = new OutcomeAggregator();
        var overallOutcome = aggregator.Aggregate(comparisonMetrics.Select(m => m.Outcome));
        var overallConfidence = aggregator.AggregateConfidence(comparisonMetrics.Select(m => m.Confidence));

        // Create and return comparison result
        var result = new ComparisonResult(
            baselineId,
            comparisonMetrics,
            overallOutcome,
            overallConfidence
        );

        return result;
    }

    /// <summary>
    /// Checks if a baseline still exists (not expired).
    /// </summary>
    /// <param name="baselineId">The ID of the baseline to check.</param>
    /// <returns>True if the baseline exists; false otherwise.</returns>
    public async Task<bool> BaselineExistsAsync(BaselineId baselineId)
    {
        ArgumentNullException.ThrowIfNull(baselineId);
        var baseline = await _baselineRepository.GetAsync(baselineId);
        return baseline != null;
    }

    /// <summary>
    /// Deletes a baseline from the repository.
    /// </summary>
    /// <param name="baselineId">The ID of the baseline to delete.</param>
    public async Task DeleteBaselineAsync(BaselineId baselineId)
    {
        ArgumentNullException.ThrowIfNull(baselineId);
        await _baselineRepository.DeleteAsync(baselineId);
    }
}
