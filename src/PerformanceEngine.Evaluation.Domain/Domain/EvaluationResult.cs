namespace PerformanceEngine.Evaluation.Domain;

/// <summary>
/// Immutable aggregate root representing a complete evaluation decision.
/// Contains all context needed for audit trails and deterministic replay.
/// 
/// Invariants:
/// - Id is unique (enforced by repository at persistence layer)
/// - Outcome severity is consistent with violations list
/// - All collections are immutable and cannot be modified after construction
/// - Timestamp is UTC-based and set at evaluation time
/// - Evidence trail is complete (no missing metric references)
/// </summary>
public record EvaluationResult(
    /// <summary>Unique identifier assigned at evaluation time.</summary>
    Guid Id,
    
    /// <summary>Overall evaluation outcome severity (Pass, Warning, Fail).</summary>
    Severity Outcome,
    
    /// <summary>Immutable list of rule violations detected during evaluation.</summary>
    ImmutableList<Violation> Violations,
    
    /// <summary>Complete audit trail of evaluation decisions and evidence.</summary>
    ImmutableList<EvaluationEvidence> Evidence,
    
    /// <summary>Human-readable rationale for the evaluation outcome.</summary>
    string OutcomeReason,
    
    /// <summary>UTC timestamp when evaluation was performed.</summary>
    DateTime EvaluatedAt
)
{
    /// <summary>
    /// Factory method to create a new EvaluationResult with validation.
    /// </summary>
    public static EvaluationResult Create(
        Severity outcome,
        IEnumerable<Violation> violations,
        IEnumerable<EvaluationEvidence> evidence,
        string outcomeReason,
        DateTime evaluatedAtUtc)
    {
        // Validate outcome consistency with violations
        var violationsList = violations.ToList();
        if (outcome == Severity.Pass && violationsList.Any())
            throw new InvalidOperationException(
                "Outcome cannot be Pass when violations are present");

        if (string.IsNullOrWhiteSpace(outcomeReason))
            throw new ArgumentException("Outcome reason must not be empty", nameof(outcomeReason));

        // Ensure timestamp is UTC
        if (evaluatedAtUtc.Kind != DateTimeKind.Utc)
            throw new ArgumentException(
                "Evaluated timestamp must be UTC", nameof(evaluatedAtUtc));

        return new EvaluationResult(
            Id: Guid.NewGuid(),
            Outcome: outcome,
            Violations: violationsList.ToImmutableList(),
            Evidence: evidence.ToImmutableList(),
            OutcomeReason: outcomeReason,
            EvaluatedAt: evaluatedAtUtc);
    }
}
