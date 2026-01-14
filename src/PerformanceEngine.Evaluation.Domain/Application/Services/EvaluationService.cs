using PerformanceEngine.Metrics.Domain.Metrics;
using PerformanceEngine.Evaluation.Domain.Domain.Evaluation;
using PerformanceEngine.Evaluation.Domain.Domain.Rules;

namespace PerformanceEngine.Evaluation.Domain.Application.Services;

/// <summary>
/// Application facade for evaluation operations.
/// Orchestrates domain services and provides error handling.
/// </summary>
public sealed class EvaluationService
{
    private readonly Evaluator _evaluator;

    /// <summary>
    /// Initializes a new instance of EvaluationService.
    /// </summary>
    public EvaluationService()
    {
        _evaluator = new Evaluator();
    }

    /// <summary>
    /// Evaluates a single metric against a single rule.
    /// Provides graceful error handling for invalid inputs.
    /// </summary>
    /// <param name="metric">The metric to evaluate.</param>
    /// <param name="rule">The rule to evaluate against.</param>
    /// <returns>EvaluationResult, or error result if inputs are invalid.</returns>
    public EvaluationResult Evaluate(Metric? metric, IRule? rule)
    {
        if (metric == null)
        {
            var violation = Violation.Create(
                ruleId: "SYSTEM",
                metricName: "INVALID",
                actualValue: double.NaN,
                threshold: double.NaN,
                message: "Metric cannot be null"
            );
            return EvaluationResult.Fail(ImmutableList.Create(violation), DateTime.UtcNow);
        }

        if (rule == null)
        {
            var violation = Violation.Create(
                ruleId: "SYSTEM",
                metricName: metric.MetricType,
                actualValue: double.NaN,
                threshold: double.NaN,
                message: "Rule cannot be null"
            );
            return EvaluationResult.Fail(ImmutableList.Create(violation), DateTime.UtcNow);
        }

        try
        {
            return _evaluator.Evaluate(metric, rule);
        }
        catch (Exception ex)
        {
            // Gracefully handle unexpected errors
            var violation = Violation.Create(
                ruleId: rule.Id,
                metricName: metric.MetricType,
                actualValue: double.NaN,
                threshold: double.NaN,
                message: $"Evaluation failed: {ex.Message}"
            );
            return EvaluationResult.Fail(ImmutableList.Create(violation), DateTime.UtcNow);
        }
    }

    /// <summary>
    /// Evaluates a single metric against multiple rules.
    /// </summary>
    /// <param name="metric">The metric to evaluate.</param>
    /// <param name="rules">The rules to evaluate against.</param>
    /// <returns>Aggregated EvaluationResult.</returns>
    public EvaluationResult EvaluateMultipleRules(Metric? metric, IEnumerable<IRule>? rules)
    {
        if (metric == null)
        {
            var violation = Violation.Create(
                ruleId: "SYSTEM",
                metricName: "INVALID",
                actualValue: double.NaN,
                threshold: double.NaN,
                message: "Metric cannot be null"
            );
            return EvaluationResult.Fail(ImmutableList.Create(violation), DateTime.UtcNow);
        }

        if (rules == null || !rules.Any())
        {
            return EvaluationResult.Pass(DateTime.UtcNow);
        }

        try
        {
            return _evaluator.EvaluateMultipleRules(metric, rules);
        }
        catch (Exception ex)
        {
            var violation = Violation.Create(
                ruleId: "SYSTEM",
                metricName: metric.MetricType,
                actualValue: double.NaN,
                threshold: double.NaN,
                message: $"Batch evaluation failed: {ex.Message}"
            );
            return EvaluationResult.Fail(ImmutableList.Create(violation), DateTime.UtcNow);
        }
    }

    /// <summary>
    /// Evaluates multiple metrics against multiple rules.
    /// </summary>
    /// <param name="metrics">The metrics to evaluate.</param>
    /// <param name="rules">The rules to evaluate against.</param>
    /// <returns>List of EvaluationResults.</returns>
    public IEnumerable<EvaluationResult> EvaluateBatch(
        IEnumerable<Metric>? metrics,
        IEnumerable<IRule>? rules)
    {
        if (metrics == null || !metrics.Any())
        {
            return Enumerable.Empty<EvaluationResult>();
        }

        if (rules == null)
        {
            return Enumerable.Empty<EvaluationResult>();
        }

        if (!rules.Any())
        {
            return metrics.Select(_ => EvaluationResult.Pass(DateTime.UtcNow));
        }

        try
        {
            return _evaluator.EvaluateMultiple(metrics, rules);
        }
        catch (Exception ex)
        {
            // Return error result for each metric
            return metrics.Select(m =>
            {
                var violation = Violation.Create(
                    ruleId: "SYSTEM",
                    metricName: m.MetricType,
                    actualValue: double.NaN,
                    threshold: double.NaN,
                    message: $"Batch evaluation failed: {ex.Message}"
                );
                return EvaluationResult.Fail(ImmutableList.Create(violation), DateTime.UtcNow);
            });
        }
    }
}
