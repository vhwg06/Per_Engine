namespace PerformanceEngine.Metrics.Domain.Metrics;

/// <summary>
/// Represents an aggregated metric derived from a collection of samples.
/// A metric always has underlying samples; it cannot exist without them.
/// </summary>
public sealed class Metric
{
    /// <summary>
    /// Gets the unique identifier for this metric
    /// </summary>
    public Guid Id { get; }

    /// <summary>
    /// Gets the collection of samples this metric is computed from
    /// </summary>
    public SampleCollection Samples { get; }

    /// <summary>
    /// Gets the aggregation window specification
    /// </summary>
    public AggregationWindow Window { get; }

    /// <summary>
    /// Gets the type of metric (e.g., "latency", "throughput")
    /// </summary>
    public string MetricType { get; }

    /// <summary>
    /// Gets the timestamp when this metric was computed
    /// </summary>
    public DateTime ComputedAt { get; }

    /// <summary>
    /// Gets optional aggregation results stored with this metric
    /// </summary>
    public IReadOnlyList<AggregationResult> AggregatedValues { get; }

    /// <summary>
    /// Initializes a new instance of the Metric class.
    /// </summary>
    /// <param name="samples">The collection of samples to aggregate</param>
    /// <param name="window">The aggregation window specification</param>
    /// <param name="metricType">The type of metric</param>
    /// <param name="aggregatedValues">Optional pre-computed aggregation results</param>
    /// <param name="computedAt">When this metric was computed</param>
    /// <exception cref="ArgumentNullException">Thrown when required parameters are null</exception>
    /// <exception cref="ArgumentException">Thrown when invariants are violated</exception>
    public Metric(
        SampleCollection samples,
        AggregationWindow window,
        string metricType,
        IReadOnlyList<AggregationResult>? aggregatedValues = null,
        DateTime? computedAt = null)
    {
        if (samples == null)
        {
            throw new ArgumentNullException(nameof(samples));
        }

        // Invariant: Metric cannot exist without samples
        if (samples.IsEmpty)
        {
            throw new ArgumentException(
                "Metric cannot be created with an empty sample collection",
                nameof(samples));
        }

        if (window == null)
        {
            throw new ArgumentNullException(nameof(window));
        }

        if (string.IsNullOrWhiteSpace(metricType))
        {
            throw new ArgumentException(
                "Metric type cannot be null or empty",
                nameof(metricType));
        }

        Id = Guid.NewGuid();
        Samples = samples;
        Window = window;
        MetricType = metricType.Trim();
        ComputedAt = computedAt ?? DateTime.UtcNow;
        AggregatedValues = aggregatedValues ?? new List<AggregationResult>();
    }

    /// <summary>
    /// Gets the total number of samples in this metric
    /// </summary>
    public int SampleCount => Samples.Count;

    /// <summary>
    /// Gets the count of successful samples
    /// </summary>
    public int SuccessCount => Samples.SuccessfulSamples.Count();

    /// <summary>
    /// Gets the count of failed samples
    /// </summary>
    public int FailureCount => Samples.FailedSamples.Count();

    /// <summary>
    /// Gets the success rate as a percentage (0-100)
    /// </summary>
    public double SuccessRate => SampleCount == 0 ? 0 : (SuccessCount * 100.0 / SampleCount);

    /// <summary>
    /// Creates a copy of this metric with updated aggregated values
    /// </summary>
    public Metric WithAggregatedValues(params AggregationResult[] results)
    {
        return new Metric(Samples, Window, MetricType, results, ComputedAt);
    }

    public override string ToString()
    {
        return $"Metric[{MetricType}: {Samples.Count} samples, {SuccessRate:F1}% success]";
    }

    public override bool Equals(object? obj)
    {
        if (obj is not Metric other)
            return false;

        return Id == other.Id;
    }

    public override int GetHashCode() => Id.GetHashCode();

    public static bool operator ==(Metric? a, Metric? b)
    {
        if (a is null && b is null)
            return true;
        if (a is null || b is null)
            return false;
        return a.Equals(b);
    }

    public static bool operator !=(Metric? a, Metric? b) => !(a == b);
}
