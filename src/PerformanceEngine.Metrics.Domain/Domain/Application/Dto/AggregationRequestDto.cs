namespace PerformanceEngine.Metrics.Domain.Application.Dto;

using PerformanceEngine.Metrics.Domain.Metrics;

/// <summary>
/// Data transfer object for requesting metric computation.
/// Contains all parameters needed to compute a metric.
/// </summary>
public sealed class AggregationRequestDto
{
    /// <summary>
    /// Gets the samples to aggregate
    /// </summary>
    public SampleCollection Samples { get; }

    /// <summary>
    /// Gets the aggregation window strategy
    /// </summary>
    public AggregationWindow Window { get; }

    /// <summary>
    /// Gets the name of the aggregation operation to perform
    /// </summary>
    public string AggregationOperation { get; }

    /// <summary>
    /// Creates a new aggregation request.
    /// </summary>
    /// <param name="samples">Collection of samples (must not be null or empty)</param>
    /// <param name="window">Aggregation window strategy (must not be null)</param>
    /// <param name="aggregationOperation">Operation name like "average", "p95" (must not be empty)</param>
    /// <exception cref="ArgumentNullException">Thrown if samples, window, or operation is null</exception>
    /// <exception cref="ArgumentException">Thrown if operation is empty or samples is empty</exception>
    public AggregationRequestDto(SampleCollection samples, AggregationWindow window, string aggregationOperation)
    {
        if (samples is null)
            throw new ArgumentNullException(nameof(samples));
        if (samples.IsEmpty)
            throw new ArgumentException("Samples cannot be empty", nameof(samples));
        if (window is null)
            throw new ArgumentNullException(nameof(window));
        if (string.IsNullOrWhiteSpace(aggregationOperation))
            throw new ArgumentException("Aggregation operation cannot be null or empty", nameof(aggregationOperation));

        Samples = samples;
        Window = window;
        AggregationOperation = aggregationOperation.Trim();
    }
}
