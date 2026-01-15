namespace PerformanceEngine.Metrics.Domain.Metrics;

using PerformanceEngine.Metrics.Domain.ValueObjects;

/// <summary>
/// Immutable value object capturing reliability metadata for a metric.
/// Used by evaluation domain to understand data completeness and make informed decisions.
/// </summary>
public sealed record MetricEvidence : ValueObject
{
    /// <summary>
    /// Number of samples actually collected and aggregated for this metric.
    /// </summary>
    public int SampleCount { get; init; }

    /// <summary>
    /// Number of samples required for a metric to be considered COMPLETE.
    /// </summary>
    public int RequiredSampleCount { get; init; }

    /// <summary>
    /// Reference to the aggregation window (e.g., "5m", "1h", "10000-sample").
    /// Helps evaluation rules understand the sampling period.
    /// </summary>
    public string AggregationWindow { get; init; }

    /// <summary>
    /// Computed property: metric is complete if all required samples collected.
    /// </summary>
    public bool IsComplete => SampleCount >= RequiredSampleCount;

    /// <summary>
    /// Initializes a new instance of MetricEvidence.
    /// </summary>
    public MetricEvidence()
    {
        SampleCount = 0;
        RequiredSampleCount = 1;
        AggregationWindow = string.Empty;
    }

    /// <summary>
    /// Initializes a new instance of MetricEvidence with validation.
    /// </summary>
    /// <param name="sampleCount">Number of samples collected</param>
    /// <param name="requiredSampleCount">Number of samples required for completeness</param>
    /// <param name="aggregationWindow">Aggregation window reference</param>
    /// <exception cref="ArgumentException">Thrown when invariants are violated</exception>
    public MetricEvidence(int sampleCount, int requiredSampleCount, string aggregationWindow)
    {
        // Invariant: Sample counts must be non-negative
        if (sampleCount < 0)
        {
            throw new ArgumentException("SampleCount must be non-negative", nameof(sampleCount));
        }

        // Invariant: Required sample count must be positive
        if (requiredSampleCount <= 0)
        {
            throw new ArgumentException("RequiredSampleCount must be positive", nameof(requiredSampleCount));
        }

        // Invariant: Aggregation window must not be empty
        if (string.IsNullOrWhiteSpace(aggregationWindow))
        {
            throw new ArgumentException("AggregationWindow must not be empty", nameof(aggregationWindow));
        }

        // Initialize via init accessors
        SampleCount = sampleCount;
        RequiredSampleCount = requiredSampleCount;
        AggregationWindow = aggregationWindow.Trim();
    }

    /// <summary>
    /// Returns a string representation suitable for debugging.
    /// </summary>
    public override string ToString() =>
        $"MetricEvidence: {SampleCount}/{RequiredSampleCount} samples, window: {AggregationWindow}, Complete: {IsComplete}";
}
