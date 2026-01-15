namespace PerformanceEngine.Metrics.Domain.Metrics;

using PerformanceEngine.Metrics.Domain.Ports;
using PerformanceEngine.Metrics.Domain.ValueObjects;

/// <summary>
/// Represents an aggregated metric derived from a collection of samples.
/// A metric always has underlying samples; it cannot exist without them.
/// Enriched with completeness metadata and evidence for evaluation domain integration.
/// </summary>
public sealed class Metric : IMetric
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
    /// Gets the completeness/reliability status of this metric.
    /// COMPLETE: all required samples collected, safe for evaluation
    /// PARTIAL: incomplete data, may result in INCONCLUSIVE per policy
    /// </summary>
    public CompletessStatus CompletessStatus { get; }

    /// <summary>
    /// Gets the evidence metadata explaining metric reliability.
    /// Includes sample count, required threshold, aggregation window.
    /// </summary>
    public MetricEvidence Evidence { get; }

    /// <summary>
    /// Gets the aggregated value for IMetric interface compliance.
    /// For now, uses first aggregated value if available; can be extended.
    /// </summary>
    double IMetric.Value 
    { 
        get
        {
            if (AggregatedValues.Count > 0 && AggregatedValues[0]?.Value != null)
            {
                return AggregatedValues[0].Value.Value;
            }
            return 0.0;
        }
    }

    /// <summary>
    /// Gets the unit of measurement for IMetric interface compliance.
    /// </summary>
    string IMetric.Unit 
    { 
        get
        {
            if (AggregatedValues.Count > 0 && AggregatedValues[0]?.Value != null)
            {
                return AggregatedValues[0].Value.Unit.ToString();
            }
            return "unknown";
        }
    }

    /// <summary>
    /// Initializes a new instance of the Metric class.
    /// </summary>
    /// <param name="samples">The collection of samples to aggregate</param>
    /// <param name="window">The aggregation window specification</param>
    /// <param name="metricType">The type of metric</param>
    /// <param name="aggregatedValues">Optional pre-computed aggregation results</param>
    /// <param name="computedAt">When this metric was computed</param>
    /// <param name="completessStatus">Completeness status (COMPLETE/PARTIAL); auto-determined if null</param>
    /// <param name="evidence">Evidence metadata; auto-created if null</param>
    /// <exception cref="ArgumentNullException">Thrown when required parameters are null</exception>
    /// <exception cref="ArgumentException">Thrown when invariants are violated</exception>
    public Metric(
        SampleCollection samples,
        AggregationWindow window,
        string metricType,
        IReadOnlyList<AggregationResult>? aggregatedValues = null,
        DateTime? computedAt = null,
        CompletessStatus? completessStatus = null,
        MetricEvidence? evidence = null)
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

        // Initialize enrichment properties
        // If evidence not provided, create default evidence
        if (evidence == null)
        {
            // Default: assume required sample count = actual sample count (COMPLETE by default)
            evidence = new MetricEvidence(
                sampleCount: samples.Count,
                requiredSampleCount: samples.Count,
                aggregationWindow: window.ToString() ?? "unknown");
        }

        Evidence = evidence;

        // Determine completeness status if not explicitly provided
        if (completessStatus == null)
        {
            completessStatus = evidence.IsComplete ? CompletessStatus.COMPLETE : CompletessStatus.PARTIAL;
        }

        CompletessStatus = completessStatus.Value;
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
    /// Factory method for creating a Metric with explicit completeness determination.
    /// Allows evaluation of when a metric should be marked COMPLETE vs PARTIAL.
    /// </summary>
    /// <param name="samples">The collection of samples</param>
    /// <param name="window">The aggregation window</param>
    /// <param name="metricType">Type of metric</param>
    /// <param name="sampleCount">Number of samples collected (for evidence)</param>
    /// <param name="requiredSampleCount">Number required for completeness threshold</param>
    /// <param name="aggregationWindow">Aggregation window reference for evidence</param>
    /// <param name="aggregatedValues">Optional pre-computed aggregation results</param>
    /// <param name="computedAt">When computed</param>
    /// <param name="overrideStatus">Optional explicit completeness status (auto-determined if null)</param>
    /// <returns>New Metric instance with completeness metadata</returns>
    public static Metric Create(
        SampleCollection samples,
        AggregationWindow window,
        string metricType,
        int sampleCount,
        int requiredSampleCount,
        string aggregationWindow,
        IReadOnlyList<AggregationResult>? aggregatedValues = null,
        DateTime? computedAt = null,
        CompletessStatus? overrideStatus = null)
    {
        // Create evidence with sample count data
        var evidence = new MetricEvidence(
            sampleCount: sampleCount,
            requiredSampleCount: requiredSampleCount,
            aggregationWindow: aggregationWindow);

        // Determine status if not explicitly overridden
        var status = overrideStatus ?? 
            (evidence.IsComplete ? CompletessStatus.COMPLETE : CompletessStatus.PARTIAL);

        return new Metric(
            samples: samples,
            window: window,
            metricType: metricType,
            aggregatedValues: aggregatedValues,
            computedAt: computedAt,
            completessStatus: status,
            evidence: evidence);
    }

    /// <summary>
    /// Creates a copy of this metric with updated aggregated values.
    /// Preserves enrichment properties (completeness, evidence).
    /// </summary>
    public Metric WithAggregatedValues(params AggregationResult[] results)
    {
        return new Metric(
            Samples,
            Window,
            MetricType,
            results,
            ComputedAt,
            CompletessStatus,
            Evidence);
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
