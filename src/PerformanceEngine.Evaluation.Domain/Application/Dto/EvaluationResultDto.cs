using PerformanceEngine.Evaluation.Domain.Domain.Evaluation;

namespace PerformanceEngine.Evaluation.Domain.Application.Dto;

/// <summary>
/// Data transfer object for evaluation results.
/// Serializable representation of domain evaluation results.
/// </summary>
public sealed record EvaluationResultDto
{
    /// <summary>
    /// Overall severity of the evaluation (PASS, WARN, FAIL).
    /// </summary>
    public required string Outcome { get; init; }

    /// <summary>
    /// Collection of all violations detected during evaluation.
    /// </summary>
    public required List<ViolationDto> Violations { get; init; }

    /// <summary>
    /// Timestamp when evaluation was performed (ISO 8601 format).
    /// </summary>
    public required string EvaluatedAt { get; init; }

    /// <summary>
    /// Maps from domain EvaluationResult to DTO.
    /// </summary>
    public static EvaluationResultDto FromDomain(EvaluationResult domain)
    {
        return new EvaluationResultDto
        {
            Outcome = domain.Outcome.ToString(),
            Violations = domain.Violations.Select(v => v.FromDomain()).ToList(),
            EvaluatedAt = domain.EvaluatedAt.ToString("O") // ISO 8601
        };
    }

    /// <summary>
    /// Maps from DTO to domain EvaluationResult.
    /// </summary>
    public EvaluationResult ToDomain()
    {
        var outcome = Enum.Parse<Severity>(Outcome);
        var violations = Violations.Select(v => v.ToDomain()).ToImmutableList();
        var evaluatedAt = DateTime.Parse(EvaluatedAt);

        return new EvaluationResult
        {
            Outcome = outcome,
            Violations = violations,
            EvaluatedAt = evaluatedAt
        };
    }
}

/// <summary>
/// Extension methods for ViolationDto mapping.
/// </summary>
public static class ViolationDtoExtensions
{
    /// <summary>
    /// Maps from domain Violation to DTO.
    /// </summary>
    public static ViolationDto FromDomain(this Violation domain)
    {
        return new ViolationDto
        {
            RuleId = domain.RuleId,
            MetricName = domain.MetricName,
            ActualValue = domain.ActualValue,
            Threshold = domain.Threshold,
            Message = domain.Message
        };
    }

    /// <summary>
    /// Maps from DTO to domain Violation.
    /// </summary>
    public static Violation ToDomain(this ViolationDto dto)
    {
        return Violation.Create(
            ruleId: dto.RuleId,
            metricName: dto.MetricName,
            actualValue: dto.ActualValue,
            threshold: dto.Threshold,
            message: dto.Message
        );
    }
}
