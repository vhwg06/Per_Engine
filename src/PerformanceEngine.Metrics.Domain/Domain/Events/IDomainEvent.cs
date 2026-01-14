namespace PerformanceEngine.Metrics.Domain.Events;

/// <summary>
/// Marker interface for all domain events.
/// Domain events represent something that happened in the domain that is of interest to other parts of the system.
/// </summary>
public interface IDomainEvent
{
    /// <summary>
    /// Gets the timestamp when this event occurred.
    /// </summary>
    DateTime OccurredAt { get; }
}
