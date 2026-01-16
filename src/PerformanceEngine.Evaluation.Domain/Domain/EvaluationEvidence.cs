namespace PerformanceEngine.Evaluation.Domain;

/// <summary>
/// Immutable value object capturing complete evaluation context for a single rule.
/// Enables deterministic replay by preserving all inputs and decision criteria.
/// 
/// Invariants:
/// - RuleId and RuleName are non-empty
/// - Metrics collection contains all metrics used in evaluation
/// - DecisionOutcome is consistent with ConstraintSatisfied
/// - Timestamp is UTC-based
/// - Cannot be modified after construction
/// </summary>
public record EvaluationEvidence(
    /// <summary>Unique identifier of the rule that was evaluated.</summary>
    string RuleId,
    
    /// <summary>Human-readable name of the rule.</summary>
    string RuleName,
    
    /// <summary>Metrics used in this rule's evaluation.</summary>
    ImmutableList<MetricReference> Metrics,
    
    /// <summary>Actual values of metrics at evaluation time (strings for precision).</summary>
    ImmutableDictionary<string, string> ActualValues,
    
    /// <summary>Expected constraint expression that was evaluated.</summary>
    string ExpectedConstraint,
    
    /// <summary>True if constraint was satisfied, false if violated.</summary>
    bool ConstraintSatisfied,
    
    /// <summary>The evaluation decision outcome (why the rule passed or failed).</summary>
    string DecisionOutcome,
    
    /// <summary>UTC timestamp when this evidence was recorded.</summary>
    DateTime RecordedAtUtc
)
{
    /// <summary>
    /// Factory method with validation.
    /// </summary>
    public static EvaluationEvidence Create(
        string ruleId,
        string ruleName,
        IEnumerable<MetricReference> metrics,
        IDictionary<string, string> actualValues,
        string expectedConstraint,
        bool constraintSatisfied,
        string decisionOutcome,
        DateTime recordedAtUtc)
    {
        if (string.IsNullOrWhiteSpace(ruleId))
            throw new ArgumentException("Rule ID must not be empty", nameof(ruleId));

        if (string.IsNullOrWhiteSpace(ruleName))
            throw new ArgumentException("Rule name must not be empty", nameof(ruleName));

        if (string.IsNullOrWhiteSpace(expectedConstraint))
            throw new ArgumentException("Expected constraint must not be empty", nameof(expectedConstraint));

        if (string.IsNullOrWhiteSpace(decisionOutcome))
            throw new ArgumentException("Decision outcome must not be empty", nameof(decisionOutcome));

        if (recordedAtUtc.Kind != DateTimeKind.Utc)
            throw new ArgumentException("Timestamp must be UTC", nameof(recordedAtUtc));

        return new EvaluationEvidence(
            RuleId: ruleId,
            RuleName: ruleName,
            Metrics: metrics.ToImmutableList(),
            ActualValues: actualValues.ToImmutableDictionary(),
            ExpectedConstraint: expectedConstraint,
            ConstraintSatisfied: constraintSatisfied,
            DecisionOutcome: decisionOutcome,
            RecordedAtUtc: recordedAtUtc);
    }
}
