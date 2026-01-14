using PerformanceEngine.Metrics.Domain.Metrics;

namespace PerformanceEngine.Evaluation.Domain.Domain.Evaluation;

/// <summary>
/// Pure domain service for evaluating metrics against rules.
/// Deterministic: identical inputs always produce identical outputs.
/// No side effects: does not modify state or perform I/O.
/// </summary>
public sealed class Evaluator
{
    /// <summary>
    /// Evaluates a single metric against a single rule.
    /// </summary>
    /// <param name="metric">The metric to evaluate.</param>
    /// <param name="rule">The rule to evaluate against.</param>
    /// <returns>EvaluationResult containing outcome and any violations.</returns>
    /// <exception cref="ArgumentNullException">Thrown when metric or rule is null.</exception>
    public EvaluationResult Evaluate(Metric metric, Rules.IRule rule)
    {
        if (metric == null)
        {
            throw new ArgumentNullException(nameof(metric));
        }

        if (rule == null)
        {
            throw new ArgumentNullException(nameof(rule));
        }

        // Delegate to rule's evaluate method (strategy pattern)
        return rule.Evaluate(metric);
    }

    /// <summary>
    /// Evaluates a single metric against multiple rules.
    /// Returns a single EvaluationResult aggregating all violations.
    /// </summary>
    /// <param name="metric">The metric to evaluate.</param>
    /// <param name="rules">The rules to evaluate against.</param>
    /// <returns>Aggregated EvaluationResult with all violations.</returns>
    /// <exception cref="ArgumentNullException">Thrown when metric or rules is null.</exception>
    public EvaluationResult EvaluateMultipleRules(Metric metric, IEnumerable<Rules.IRule> rules)
    {
        if (metric == null)
        {
            throw new ArgumentNullException(nameof(metric));
        }

        if (rules == null)
        {
            throw new ArgumentNullException(nameof(rules));
        }

        var rulesList = rules.ToList();
        if (!rulesList.Any())
        {
            return EvaluationResult.Pass(DateTime.UtcNow);
        }

        // Evaluate each rule and collect violations
        var allViolations = new List<Violation>();
        var allOutcomes = new List<Severity>();
        
        foreach (var rule in rulesList.OrderBy(r => r.Id)) // Deterministic ordering by rule ID
        {
            var result = rule.Evaluate(metric);
            allOutcomes.Add(result.Outcome);
            allViolations.AddRange(result.Violations);
        }

        // Determine overall outcome (most severe)
        var overallOutcome = allOutcomes.MostSevere();
        var timestamp = DateTime.UtcNow;

        return new EvaluationResult
        {
            Outcome = overallOutcome,
            Violations = allViolations.ToImmutableList(),
            EvaluatedAt = timestamp
        };
    }

    /// <summary>
    /// Evaluates multiple metrics against multiple rules.
    /// Returns a list of EvaluationResults with deterministic ordering.
    /// </summary>
    /// <param name="metrics">The metrics to evaluate.</param>
    /// <param name="rules">The rules to evaluate against.</param>
    /// <returns>List of EvaluationResults, one per metric.</returns>
    /// <exception cref="ArgumentNullException">Thrown when metrics or rules is null.</exception>
    public IEnumerable<EvaluationResult> EvaluateMultiple(
        IEnumerable<Metric> metrics,
        IEnumerable<Rules.IRule> rules)
    {
        if (metrics == null)
        {
            throw new ArgumentNullException(nameof(metrics));
        }

        if (rules == null)
        {
            throw new ArgumentNullException(nameof(rules));
        }

        var metricsList = metrics.ToList();
        var rulesList = rules.ToList();

        if (!metricsList.Any())
        {
            return Enumerable.Empty<EvaluationResult>();
        }

        if (!rulesList.Any())
        {
            // No rules to evaluate - return passing results for all metrics
            return metricsList.Select(_ => EvaluationResult.Pass(DateTime.UtcNow));
        }

        // Preserve caller-provided ordering; metricsList already deterministic from input
        return metricsList.Select(metric => EvaluateMultipleRules(metric, rulesList));
    }
}
