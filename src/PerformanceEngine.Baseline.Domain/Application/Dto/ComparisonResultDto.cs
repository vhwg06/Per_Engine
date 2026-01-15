namespace PerformanceEngine.Baseline.Domain.Application.Dto;

using System.Collections.Immutable;
using PerformanceEngine.Baseline.Domain.Domain.Comparisons;
using PerformanceEngine.Baseline.Domain.Domain.Confidence;

/// <summary>
/// Data transfer object for per-metric comparison results.
/// </summary>
public class ComparisonMetricDto
{
    /// <summary>
    /// Gets the metric name that was compared.
    /// </summary>
    public required string MetricName { get; init; }

    /// <summary>
    /// Gets the baseline value for this metric.
    /// </summary>
    public required decimal BaselineValue { get; init; }

    /// <summary>
    /// Gets the current value for this metric.
    /// </summary>
    public required decimal CurrentValue { get; init; }

    /// <summary>
    /// Gets the absolute change (current - baseline).
    /// </summary>
    public required decimal AbsoluteChange { get; init; }

    /// <summary>
    /// Gets the relative change as a percentage.
    /// </summary>
    public required decimal RelativeChange { get; init; }

    /// <summary>
    /// Gets the outcome of this metric comparison (Improvement, Regression, etc).
    /// </summary>
    public required ComparisonOutcome Outcome { get; init; }

    /// <summary>
    /// Gets the confidence level for this metric comparison outcome.
    /// </summary>
    public required decimal Confidence { get; init; }

    /// <summary>
    /// Creates a ComparisonMetricDto from a domain ComparisonMetric object.
    /// </summary>
    public static ComparisonMetricDto FromDomain(ComparisonMetric metric) =>
        new()
        {
            MetricName = metric.MetricName,
            BaselineValue = metric.BaselineValue,
            CurrentValue = metric.CurrentValue,
            AbsoluteChange = metric.AbsoluteChange,
            RelativeChange = metric.RelativeChange,
            Outcome = metric.Outcome,
            Confidence = metric.Confidence.Value
        };

    /// <summary>
    /// Converts this DTO back to a domain ComparisonMetric object.
    /// </summary>
    public ComparisonMetric ToDomain() =>
        new(
            MetricName,
            BaselineValue,
            CurrentValue,
            new ConfidenceLevel(Confidence),
            Outcome
        );
}

/// <summary>
/// Data transfer object for overall comparison results.
/// </summary>
public class ComparisonResultDto
{
    /// <summary>
    /// Gets the unique identifier for this comparison.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Gets the baseline ID that was compared against.
    /// </summary>
    public required string BaselineId { get; init; }

    /// <summary>
    /// Gets the timestamp when this comparison was performed.
    /// </summary>
    public required DateTime ComparedAt { get; init; }

    /// <summary>
    /// Gets the per-metric comparison results.
    /// </summary>
    public required IReadOnlyList<ComparisonMetricDto> MetricResults { get; init; }

    /// <summary>
    /// Gets the overall outcome from aggregating all metric results.
    /// </summary>
    public required ComparisonOutcome OverallOutcome { get; init; }

    /// <summary>
    /// Gets the overall confidence level from aggregating metric confidences.
    /// </summary>
    public required decimal OverallConfidence { get; init; }

    /// <summary>
    /// Creates a ComparisonResultDto from a domain ComparisonResult object.
    /// </summary>
    public static ComparisonResultDto FromDomain(ComparisonResult result) =>
        new()
        {
            Id = result.Id.Value.ToString(),
            BaselineId = result.BaselineId.Value.ToString(),
            ComparedAt = result.ComparedAt,
            MetricResults = result.MetricResults
                .Select(ComparisonMetricDto.FromDomain)
                .ToImmutableList(),
            OverallOutcome = result.OverallOutcome,
            OverallConfidence = result.OverallConfidence.Value
        };

    /// <summary>
    /// Converts this DTO back to a domain ComparisonResult object.
    /// </summary>
    public ComparisonResult ToDomain()
    {
        var baselineId = Guid.TryParse(BaselineId, out var baselineGuid)
            ? new Domain.Baselines.BaselineId(baselineGuid)
            : new Domain.Baselines.BaselineId();

        var comparisonId = Guid.TryParse(Id, out var comparisonGuid)
            ? new ComparisonResultId(comparisonGuid)
            : new ComparisonResultId();

        var metrics = MetricResults
            .Select(m => m.ToDomain())
            .ToImmutableList();

        return new ComparisonResult(
            baselineId,
            metrics,
            OverallOutcome,
            new ConfidenceLevel(OverallConfidence),
            comparisonId,
            ComparedAt
        );
    }
}
