using PerformanceEngine.Metrics.Domain.Metrics;
using PerformanceEngine.Evaluation.Domain.Domain.Evaluation;

namespace PerformanceEngine.Evaluation.Domain.Domain.Rules;

/// <summary>
/// Logical operator for combining rules.
/// </summary>
public enum LogicalOperator
{
    /// <summary>All sub-rules must pass</summary>
    And,
    
    /// <summary>At least one sub-rule must pass</summary>
    Or
}

/// <summary>
/// Composite rule that combines multiple rules with logical operators.
/// Allows building complex validation logic from simpler rules.
/// </summary>
public sealed record CompositeRule : IRule
{
    /// <summary>
    /// Unique identifier for this composite rule.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Human-readable name for this composite rule.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Detailed description of what this composite rule validates.
    /// </summary>
    public required string Description { get; init; }

    /// <summary>
    /// The logical operator combining sub-rules.
    /// </summary>
    public required LogicalOperator Operator { get; init; }

    /// <summary>
    /// Collection of sub-rules to evaluate.
    /// </summary>
    public required IReadOnlyList<IRule> SubRules { get; init; }

    /// <summary>
    /// Evaluates all sub-rules and combines results according to the logical operator.
    /// </summary>
    public EvaluationResult Evaluate(Metric metric)
    {
        if (metric == null)
        {
            return EvaluationResult.Fail(
                ImmutableList.Create(Violation.Create(
                    Id,
                    "INVALID",
                    double.NaN,
                    double.NaN,
                    "Metric cannot be null"
                )),
                DateTime.UtcNow
            );
        }

        if (SubRules == null || SubRules.Count == 0)
        {
            return EvaluationResult.Pass(DateTime.UtcNow);
        }

        // Evaluate all sub-rules
        var results = SubRules.Select(r => r.Evaluate(metric)).ToList();
        var allViolations = results.SelectMany(r => r.Violations).ToList();
        var timestamp = DateTime.UtcNow;

        return Operator switch
        {
            LogicalOperator.And => EvaluateAnd(results, allViolations, timestamp),
            LogicalOperator.Or => EvaluateOr(results, allViolations, timestamp),
            _ => throw new InvalidOperationException($"Unsupported operator: {Operator}")
        };
    }

    private EvaluationResult EvaluateAnd(
        List<EvaluationResult> results,
        List<Violation> allViolations,
        DateTime timestamp)
    {
        // AND: All sub-rules must pass
        var allPass = results.All(r => r.Outcome == Severity.PASS);
        
        if (allPass)
        {
            return EvaluationResult.Pass(timestamp);
        }

        // If any fail, return all violations
        var outcome = results.Select(r => r.Outcome).MostSevere();
        return new EvaluationResult
        {
            Outcome = outcome,
            Violations = allViolations.ToImmutableList(),
            EvaluatedAt = timestamp
        };
    }

    private EvaluationResult EvaluateOr(
        List<EvaluationResult> results,
        List<Violation> allViolations,
        DateTime timestamp)
    {
        // OR: At least one sub-rule must pass
        var anyPass = results.Any(r => r.Outcome == Severity.PASS);
        
        if (anyPass)
        {
            return EvaluationResult.Pass(timestamp);
        }

        // All failed - return all violations
        var outcome = results.Select(r => r.Outcome).MostSevere();
        return new EvaluationResult
        {
            Outcome = outcome,
            Violations = allViolations.ToImmutableList(),
            EvaluatedAt = timestamp
        };
    }

    /// <summary>
    /// Equality comparison based on configuration.
    /// </summary>
    public bool Equals(IRule? other)
    {
        if (other is not CompositeRule composite)
            return false;

        return Id == composite.Id &&
               Name == composite.Name &&
               Operator == composite.Operator &&
               SubRules.SequenceEqual(composite.SubRules);
    }

    /// <summary>
    /// Hash code based on configuration.
    /// </summary>
    public override int GetHashCode()
    {
        return HashCode.Combine(Id, Name, Operator, SubRules.Count);
    }
}
