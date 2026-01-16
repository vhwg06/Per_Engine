namespace PerformanceEngine.Application.Ports;

using PerformanceEngine.Evaluation.Domain.Domain.Evaluation;
using PerformanceEngine.Metrics.Domain.Metrics;

/// <summary>
/// Port abstraction for accessing and evaluating performance rules.
/// Provides rule definitions and evaluation capabilities.
/// </summary>
public interface IEvaluationRulesProvider
{
    /// <summary>
    /// Gets all evaluation rules that should be applied.
    /// </summary>
    /// <returns>Collection of evaluation rules with their configurations.</returns>
    IReadOnlyCollection<EvaluationRuleDefinition> GetRules();

    /// <summary>
    /// Evaluates a single rule against provided metrics.
    /// </summary>
    /// <param name="rule">The rule to evaluate.</param>
    /// <param name="samples">Available metric samples.</param>
    /// <returns>Evaluation result indicating pass/fail and any violations.</returns>
    EvaluationResult EvaluateRule(EvaluationRuleDefinition rule, IReadOnlyCollection<Sample> samples);

    /// <summary>
    /// Gets the required metric names for a specific rule.
    /// </summary>
    /// <param name="ruleId">Rule identifier.</param>
    /// <returns>Collection of metric names required by the rule.</returns>
    IReadOnlyCollection<string> GetRequiredMetrics(string ruleId);
}

/// <summary>
/// Represents a rule definition with its metadata and requirements.
/// </summary>
public sealed record EvaluationRuleDefinition
{
    /// <summary>
    /// Unique identifier for the rule.
    /// </summary>
    public required string RuleId { get; init; }

    /// <summary>
    /// Human-readable name of the rule.
    /// </summary>
    public required string RuleName { get; init; }

    /// <summary>
    /// Severity level of violations from this rule.
    /// </summary>
    public required Severity Severity { get; init; }

    /// <summary>
    /// Required metric names for this rule to be evaluated.
    /// </summary>
    public required IReadOnlyCollection<string> RequiredMetrics { get; init; }

    /// <summary>
    /// Optional description of what the rule evaluates.
    /// </summary>
    public string? Description { get; init; }
}
