namespace PerformanceEngine.Evaluation.Domain.Domain.Evaluation;

/// <summary>
/// Immutable entity representing the result of evaluating metric(s) against rule(s).
/// Contains outcome severity, all violations detected, and timestamp for reproducibility tracking.
/// Thread-safe and suitable for serialization.
/// </summary>
public sealed record EvaluationResult
{
    /// <summary>
    /// Overall severity of the evaluation (PASS, WARN, FAIL).
    /// </summary>
    public required Severity Outcome { get; init; }

    /// <summary>
    /// Immutable collection of all violations detected during evaluation.
    /// </summary>
    public required ImmutableList<Violation> Violations { get; init; }

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
        new() { Outcome = Severity.PASS, Violations = ImmutableList<Violation>.Empty, EvaluatedAt = evaluatedAt };

    /// <summary>
    /// Creates a result with warnings.
    /// </summary>
    public static EvaluationResult Warning(ImmutableList<Violation> violations, DateTime evaluatedAt) =>
        new() { Outcome = Severity.WARN, Violations = violations, EvaluatedAt = evaluatedAt };

    /// <summary>
    /// Creates a failed result with violations.
    /// </summary>
    public static EvaluationResult Fail(ImmutableList<Violation> violations, DateTime evaluatedAt) =>
        new() { Outcome = Severity.FAIL, Violations = violations, EvaluatedAt = evaluatedAt };

    /// <summary>
    /// Creates a result with outcome determined by violations.
    /// If violations exist, outcome is FAIL; otherwise PASS.
    /// </summary>
    public static EvaluationResult FromViolations(ImmutableList<Violation> violations, DateTime evaluatedAt)
    {
        var outcome = violations.Any() ? Severity.FAIL : Severity.PASS;
        return new() { Outcome = outcome, Violations = violations, EvaluatedAt = evaluatedAt };
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
    /// Returns deterministic string representation for testing.
    /// </summary>
    public override string ToString()
    {
        var violationSummary = Violations.Any() 
            ? $"{Violations.Count} violation(s)" 
            : "no violations";
        return $"EvaluationResult: {Outcome} ({violationSummary}) at {EvaluatedAt:O}";
    }
}
