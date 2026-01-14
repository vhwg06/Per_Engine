using PerformanceEngine.Metrics.Domain.Metrics;
using PerformanceEngine.Evaluation.Domain.Domain.Evaluation;

namespace PerformanceEngine.Evaluation.Domain.Domain.Rules;

/// <summary>
/// Strategy pattern interface for rule types.
/// All rules must implement this contract to be evaluated by the Evaluator.
/// Rules are immutable and must be deterministic - given the same metric, always produce the same result.
/// </summary>
public interface IRule : IEquatable<IRule>
{
    /// <summary>
    /// Unique identifier for this rule instance.
    /// Used for violation tracking and rule comparison.
    /// </summary>
    string Id { get; }

    /// <summary>
    /// Human-readable name for this rule.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Detailed description of what this rule validates.
    /// Should clearly state the condition being checked.
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Evaluates the given metric against this rule's constraints.
    /// Must be deterministic: identical metric always produces identical result.
    /// </summary>
    /// <param name="metric">The metric to evaluate.</param>
    /// <returns>EvaluationResult containing outcome and any violations detected.</returns>
    EvaluationResult Evaluate(Metric metric);
}
