namespace PerformanceEngine.Baseline.Domain.Domain.Events;

using PerformanceEngine.Baseline.Domain.Domain.Baselines;

/// <summary>
/// Domain event raised when a new baseline is created.
/// </summary>
public class BaselineCreatedEvent
{
    public BaselineId BaselineId { get; }
    public DateTime CreatedAt { get; }
    public int MetricCount { get; }

    public BaselineCreatedEvent(BaselineId baselineId, DateTime createdAt, int metricCount)
    {
        BaselineId = baselineId ?? throw new ArgumentNullException(nameof(baselineId));
        CreatedAt = createdAt;
        MetricCount = metricCount;
    }

    public override string ToString() =>
        $"BaselineCreatedEvent: {BaselineId} at {CreatedAt:O} with {MetricCount} metrics";
}
