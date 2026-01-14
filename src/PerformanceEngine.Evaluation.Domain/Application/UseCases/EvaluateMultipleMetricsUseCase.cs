using PerformanceEngine.Metrics.Domain.Metrics;
using PerformanceEngine.Evaluation.Domain.Domain.Evaluation;
using PerformanceEngine.Evaluation.Domain.Domain.Rules;

namespace PerformanceEngine.Evaluation.Domain.Application.UseCases;

/// <summary>
/// Use case for evaluating multiple metrics against multiple rules.
/// Coordinates batch evaluation operations.
/// </summary>
public sealed class EvaluateMultipleMetricsUseCase
{
    private readonly Evaluator _evaluator;

    /// <summary>
    /// Initializes a new instance of EvaluateMultipleMetricsUseCase.
    /// </summary>
    public EvaluateMultipleMetricsUseCase()
    {
        _evaluator = new Evaluator();
    }

    /// <summary>
    /// Executes batch evaluation of metrics against rules.
    /// </summary>
    /// <param name="metrics">Collection of metrics to evaluate.</param>
    /// <param name="rules">Collection of rules to apply.</param>
    /// <returns>Collection of evaluation results with deterministic ordering.</returns>
    /// <exception cref="ArgumentNullException">Thrown when metrics or rules is null.</exception>
    public IEnumerable<EvaluationResult> Execute(
        IEnumerable<Metric> metrics,
        IEnumerable<IRule> rules)
    {
        if (metrics == null)
        {
            throw new ArgumentNullException(nameof(metrics));
        }

        if (rules == null)
        {
            throw new ArgumentNullException(nameof(rules));
        }

        return _evaluator.EvaluateMultiple(metrics, rules);
    }

    /// <summary>
    /// Executes evaluation of a single metric against all rules.
    /// </summary>
    /// <param name="metric">The metric to evaluate.</param>
    /// <param name="rules">Collection of rules to apply.</param>
    /// <returns>Aggregated evaluation result.</returns>
    /// <exception cref="ArgumentNullException">Thrown when metric or rules is null.</exception>
    public EvaluationResult ExecuteSingle(Metric metric, IEnumerable<IRule> rules)
    {
        if (metric == null)
        {
            throw new ArgumentNullException(nameof(metric));
        }

        if (rules == null)
        {
            throw new ArgumentNullException(nameof(rules));
        }

        return _evaluator.EvaluateMultipleRules(metric, rules);
    }
}
