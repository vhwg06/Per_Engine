namespace PerformanceEngine.Evaluation.Domain.Domain.Evaluation;

using PerformanceEngine.Evaluation.Domain.ValueObjects;

/// <summary>
/// Value object capturing complete evidence of an evaluation decision.
/// Documents which rule was applied, which metrics were used, actual values,
/// constraints, and the resulting decision outcome.
/// Immutable record and designed for auditability and reproducibility.
/// </summary>
public sealed record EvaluationEvidence : ValueObject
{
    /// <summary>
    /// Gets the unique identifier of the rule applied
    /// </summary>
    public string RuleId { get; init; }

    /// <summary>
    /// Gets the human-readable name of the rule
    /// </summary>
    public string RuleName { get; init; }

    /// <summary>
    /// Gets the immutable collection of metrics that were referenced in the evaluation
    /// </summary>
    public IReadOnlyList<MetricReference> MetricsUsed { get; init; }

    /// <summary>
    /// Gets a dictionary of actual values evaluated against (metric name to value)
    /// </summary>
    public IReadOnlyDictionary<string, double> ActualValues { get; init; }

    /// <summary>
    /// Gets a human-readable description of the constraint that was checked
    /// </summary>
    public string ExpectedConstraint { get; init; }

    /// <summary>
    /// Gets whether the constraint was satisfied
    /// </summary>
    public bool ConstraintSatisfied { get; init; }

    /// <summary>
    /// Gets the decision made based on the evaluation
    /// </summary>
    public string Decision { get; init; }

    /// <summary>
    /// Gets the timestamp when this evaluation was performed (UTC)
    /// </summary>
    public DateTime EvaluatedAt { get; init; }

    /// <summary>
    /// Parameterless constructor for record initialization.
    /// </summary>
    public EvaluationEvidence()
    {
        RuleId = string.Empty;
        RuleName = string.Empty;
        MetricsUsed = new List<MetricReference>();
        ActualValues = new Dictionary<string, double>();
        ExpectedConstraint = string.Empty;
        ConstraintSatisfied = false;
        Decision = string.Empty;
        EvaluatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Initializes a new instance of the EvaluationEvidence record.
    /// </summary>
    /// <param name="ruleId">The unique identifier of the rule</param>
    /// <param name="ruleName">The human-readable name of the rule</param>
    /// <param name="metricsUsed">The collection of metrics referenced</param>
    /// <param name="actualValues">The actual values evaluated</param>
    /// <param name="expectedConstraint">The constraint that was checked</param>
    /// <param name="constraintSatisfied">Whether the constraint was satisfied</param>
    /// <param name="decision">The decision made</param>
    /// <param name="evaluatedAt">When the evaluation occurred</param>
    public EvaluationEvidence(
        string ruleId,
        string ruleName,
        IReadOnlyList<MetricReference> metricsUsed,
        IReadOnlyDictionary<string, double> actualValues,
        string expectedConstraint,
        bool constraintSatisfied,
        string decision,
        DateTime evaluatedAt)
    {
        if (string.IsNullOrWhiteSpace(ruleId))
        {
            throw new ArgumentException("Rule ID cannot be null or empty", nameof(ruleId));
        }

        if (string.IsNullOrWhiteSpace(ruleName))
        {
            throw new ArgumentException("Rule name cannot be null or empty", nameof(ruleName));
        }

        if (metricsUsed == null)
        {
            throw new ArgumentNullException(nameof(metricsUsed));
        }

        if (actualValues == null)
        {
            throw new ArgumentNullException(nameof(actualValues));
        }

        if (string.IsNullOrWhiteSpace(expectedConstraint))
        {
            throw new ArgumentException("Expected constraint cannot be null or empty", nameof(expectedConstraint));
        }

        if (string.IsNullOrWhiteSpace(decision))
        {
            throw new ArgumentException("Decision cannot be null or empty", nameof(decision));
        }

        if (evaluatedAt == default)
        {
            throw new ArgumentException("EvaluatedAt must be set", nameof(evaluatedAt));
        }

        RuleId = ruleId.Trim();
        RuleName = ruleName.Trim();
        MetricsUsed = metricsUsed;
        ActualValues = actualValues;
        ExpectedConstraint = expectedConstraint.Trim();
        ConstraintSatisfied = constraintSatisfied;
        Decision = decision.Trim();
        EvaluatedAt = evaluatedAt;
    }

    /// <summary>
    /// Returns a string representation of this evidence.
    /// </summary>
    public override string ToString() =>
        $"Rule: {RuleId}/{RuleName}, Constraint: {ExpectedConstraint}, " +
        $"Satisfied: {ConstraintSatisfied}, Decision: {Decision}, Evaluated: {EvaluatedAt:O}";
}
