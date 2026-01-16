namespace PerformanceEngine.Baseline.Domain.Domain.Events;

using PerformanceEngine.Baseline.Domain.Domain.Baselines;
using PerformanceEngine.Baseline.Domain.Domain.Comparisons;

/// <summary>
/// Domain event raised when a comparison is performed against a baseline.
/// </summary>
public class ComparisonPerformedEvent
{
    public ComparisonResultId ComparisonResultId { get; }
    public BaselineId BaselineId { get; }
    public ComparisonOutcome Outcome { get; }
    public DateTime PerformedAt { get; }

    public ComparisonPerformedEvent(
        ComparisonResultId comparisonResultId,
        BaselineId baselineId,
        ComparisonOutcome outcome,
        DateTime performedAt)
    {
        ComparisonResultId = comparisonResultId ?? throw new ArgumentNullException(nameof(comparisonResultId));
        BaselineId = baselineId ?? throw new ArgumentNullException(nameof(baselineId));
        Outcome = outcome;
        PerformedAt = performedAt;
    }

    public override string ToString() =>
        $"ComparisonPerformedEvent: {ComparisonResultId} vs {BaselineId} = {Outcome} at {PerformedAt:O}";
}
