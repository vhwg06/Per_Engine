namespace PerformanceEngine.Application.Orchestration;

using PerformanceEngine.Application.Models;
using PerformanceEngine.Application.Ports;
using PerformanceEngine.Metrics.Domain.Metrics;

/// <summary>
/// Assesses data completeness by comparing available metrics against expected metrics.
/// Identifies missing data and rules that cannot be evaluated.
/// </summary>
public sealed class CompletenessAssessor
{
    /// <summary>
    /// Assesses completeness of available metrics against requirements.
    /// </summary>
    /// <param name="rules">Collection of evaluation rules with metric requirements.</param>
    /// <param name="samples">Available metric samples.</param>
    /// <param name="metricsProvider">Provider for checking metric availability.</param>
    /// <returns>Completeness report with availability statistics.</returns>
    public CompletenessReport AssessCompleteness(
        IReadOnlyCollection<EvaluationRuleDefinition> rules,
        IReadOnlyCollection<Sample> samples,
        IMetricsProvider metricsProvider)
    {
        // Collect all required metrics from rules
        var requiredMetrics = rules
            .SelectMany(r => r.RequiredMetrics ?? Array.Empty<string>())
            .Distinct(StringComparer.Ordinal)
            .OrderBy(m => m, StringComparer.Ordinal)
            .ToList();

        // Get available metrics
        var availableMetrics = metricsProvider.GetAvailableMetricNames()
            .OrderBy(m => m, StringComparer.Ordinal)
            .ToList();

        // Identify missing metrics
        var missingMetrics = requiredMetrics
            .Except(availableMetrics, StringComparer.Ordinal)
            .OrderBy(m => m, StringComparer.Ordinal)
            .ToList();

        // Identify unevaluated rules (rules with missing required metrics)
        var unevaluatedRules = new List<string>();
        foreach (var rule in rules.OrderBy(r => r.RuleId, StringComparer.Ordinal))
        {
            var ruleRequiredMetrics = rule.RequiredMetrics ?? Array.Empty<string>();
            var hasAllRequiredMetrics = ruleRequiredMetrics.All(m =>
                availableMetrics.Contains(m, StringComparer.Ordinal));

            if (!hasAllRequiredMetrics)
            {
                unevaluatedRules.Add(rule.RuleId);
            }
        }

        // Calculate completeness
        var expectedCount = requiredMetrics.Count;
        var providedCount = expectedCount - missingMetrics.Count;
        var completenessPercentage = expectedCount > 0
            ? (double)providedCount / expectedCount
            : 1.0; // If no metrics expected, consider 100% complete

        return new CompletenessReport
        {
            MetricsProvidedCount = providedCount,
            MetricsExpectedCount = expectedCount,
            CompletenessPercentage = completenessPercentage,
            MissingMetrics = missingMetrics,
            UnevaluatedRules = unevaluatedRules
        };
    }
}
