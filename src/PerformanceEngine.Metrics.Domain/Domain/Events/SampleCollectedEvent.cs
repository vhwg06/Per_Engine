namespace PerformanceEngine.Metrics.Domain.Events;

using PerformanceEngine.Metrics.Domain.Metrics;

/// <summary>
/// Domain event published when a sample is collected from an execution engine.
/// Enables event-driven workflows and audit trails.
/// </summary>
public sealed class SampleCollectedEvent : IDomainEvent
{
    /// <summary>
    /// Gets the sample that was collected
    /// </summary>
    public Sample Sample { get; }

    /// <summary>
    /// Gets the timestamp when the sample was collected
    /// </summary>
    public DateTime CollectedAt { get; }

    /// <summary>
    /// Gets the name of the source engine that produced this sample
    /// </summary>
    public string SourceName { get; }

    /// <summary>
    /// Gets the timestamp when this event occurred (interface implementation)
    /// </summary>
    public DateTime OccurredAt => CollectedAt;

    /// <summary>
    /// Creates a new SampleCollectedEvent.
    /// </summary>
    /// <param name="sample">The collected sample (must not be null)</param>
    /// <param name="collectedAt">When the sample was collected</param>
    /// <param name="sourceName">Name of the source engine (e.g., "k6", "jmeter")</param>
    /// <exception cref="ArgumentNullException">Thrown if sample or sourceName is null</exception>
    /// <exception cref="ArgumentException">Thrown if sourceName is empty</exception>
    public SampleCollectedEvent(Sample sample, DateTime collectedAt, string sourceName)
    {
        Sample = sample ?? throw new ArgumentNullException(nameof(sample));
        CollectedAt = collectedAt;
        SourceName = string.IsNullOrWhiteSpace(sourceName) 
            ? throw new ArgumentException("Source name cannot be null or empty", nameof(sourceName))
            : sourceName.Trim();
    }
}
