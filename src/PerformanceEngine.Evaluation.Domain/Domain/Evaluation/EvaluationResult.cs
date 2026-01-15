namespace PerformanceEngine.Evaluation.Domain.Domain.Evaluation;

/// <summary>
/// Immutable entity representing the result of evaluating metric(s) against rule(s).
/// Contains outcome severity, all violations detected, evidence trail, and timestamp for reproducibility tracking.
/// Thread-safe and suitable for serialization.
/// Enhanced to support INCONCLUSIVE outcomes and complete evidence trails.
/// </summary>
public sealed record EvaluationResult
{
    /// <summary>
    /// Overall severity of the evaluation (PASS, WARN, FAIL).
    /// </summary>
    public required Severity Outcome { get; init; }

    /// <summary>
    /// High-level outcome of the evaluation (PASS, FAIL, INCONCLUSIVE).
    /// Allows distinguishing between "no issues" and "insufficient data".
    /// </summary>
    public Outcome? EvaluationOutcome { get; init; }

    /// <summary>
    /// Immutable collection of all violations detected during evaluation.
    /// </summary>
    public required ImmutableList<Violation> Violations { get; init; }

    /// <summary>
    /// Complete evidence trail documenting the evaluation decision.
    /// Captures which rules were applied, metrics used, actual values, and constraints.
    /// </summary>
    public IReadOnlyList<EvaluationEvidence>? Evidence { get; init; }

    /// <summary>
    /// Human-readable reason for the outcome (e.g., why INCONCLUSIVE was assigned).
    /// Useful for explaining non-obvious decisions.
    /// </summary>
    public string? OutcomeReason { get; init; }

    /// <summary>
    /// Timestamp when evaluation was performed (for reproducibility tracking).
    /// </summary>
    public required DateTime EvaluatedAt { get; init; }

    /// <summary>
    /// Parameterless constructor for record initialization.
    /// </summary>
    public EvaluationResult()
    {
    }

    /// <summary>
    /// Creates a passing result with no violations.
    /// </summary>
    public static EvaluationResult Pass(DateTime evaluatedAt) =>
        new() 
        { 
            Outcome = Severity.PASS, 
            EvaluationOutcome = Domain.Outcome.PASS,
            Violations = ImmutableList<Violation>.Empty, 
            EvaluatedAt = evaluatedAt 
        };

    /// <summary>
    /// Creates a result with warnings.
    /// </summary>
    public static EvaluationResult Warning(ImmutableList<Violation> violations, DateTime evaluatedAt) =>
        new() 
        { 
            Outcome = Severity.WARN, 
            EvaluationOutcome = violations.Any() ? Domain.Outcome.FAIL : Domain.Outcome.PASS,
            Violations = violations, 
            EvaluatedAt = evaluatedAt 
        };

    /// <summary>
    /// Creates a failed result with violations.
    /// </summary>
    public static EvaluationResult Fail(ImmutableList<Violation> violations, DateTime evaluatedAt) =>
        new() 
        { 
            Outcome = Severity.FAIL, 
            EvaluationOutcome = Domain.Outcome.FAIL,
            Violations = violations, 
            EvaluatedAt = evaluatedAt 
        };

    /// <summary>
    /// Creates an inconclusive result when insufficient data exists.
    /// </summary>
    public static EvaluationResult Inconclusive(
        ImmutableList<Violation> violations,
        string reason,
        IReadOnlyList<EvaluationEvidence>? evidence,
        DateTime evaluatedAt) =>
        new()
        {
            Outcome = Severity.WARN,
            EvaluationOutcome = Domain.Outcome.INCONCLUSIVE,
            Violations = violations,
            Evidence = evidence,
            OutcomeReason = reason,
            EvaluatedAt = evaluatedAt
        };

    /// <summary>
    /// Creates a result with outcome determined by violations.
    /// If violations exist, outcome is FAIL; otherwise PASS.
    /// </summary>
    public static EvaluationResult FromViolations(ImmutableList<Violation> violations, DateTime evaluatedAt)
    {
        var outcome = violations.Any() ? Severity.FAIL : Severity.PASS;
        var evalOutcome = violations.Any() ? Domain.Outcome.FAIL : Domain.Outcome.PASS;
        return new() 
        { 
            Outcome = outcome, 
            EvaluationOutcome = evalOutcome,
            Violations = violations, 
            EvaluatedAt = evaluatedAt 
        };
    }

    /// <summary>
    /// Returns true if evaluation passed (no violations).
    /// </summary>
    public bool IsPassing => Outcome == Severity.PASS && !Violations.Any();

    /// <summary>
    /// Returns true if evaluation failed.
    /// </summary>
    public bool IsFailing => Outcome == Severity.FAIL;

    /// <summary>
    /// Returns true if evaluation is inconclusive.
    /// </summary>
    public bool IsInconclusive => EvaluationOutcome == Domain.Outcome.INCONCLUSIVE;

    /// <summary>
    /// Returns deterministic string representation for testing.
    /// </summary>
    public override string ToString()
    {
        var violationSummary = Violations.Any() 
            ? $"{Violations.Count} violation(s)" 
            : "no violations";
        var reasonPart = !string.IsNullOrEmpty(OutcomeReason) ? $", Reason: {OutcomeReason}" : "";
        return $"EvaluationResult: {Outcome} ({violationSummary}){reasonPart} at {EvaluatedAt:O}";
    }
}
