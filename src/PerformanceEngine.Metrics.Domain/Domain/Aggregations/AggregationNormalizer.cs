namespace PerformanceEngine.Metrics.Domain.Aggregations;

using System.Collections.Immutable;
using PerformanceEngine.Metrics.Domain.Metrics;

/// <summary>
/// Normalizes samples to a consistent latency unit for aggregation operations.
/// Ensures all samples are converted to the target unit while preserving metadata
/// and guaranteeing no precision loss during conversion.
/// </summary>
public sealed class AggregationNormalizer
{
    /// <summary>
    /// Normalizes all samples in the collection to a target latency unit.
    /// </summary>
    /// <param name="samples">Collection of samples with potentially mixed units.</param>
    /// <param name="targetUnit">The unit to normalize all samples to.</param>
    /// <returns>New SampleCollection with all samples converted to the target unit.</returns>
    /// <exception cref="ArgumentNullException">Thrown if samples is null.</exception>
    public static SampleCollection NormalizeSamples(SampleCollection samples, LatencyUnit targetUnit)
    {
        if (samples is null)
            throw new ArgumentNullException(nameof(samples), "Sample collection cannot be null");

        var normalized = ImmutableList.CreateBuilder<Sample>();

        foreach (var sample in samples.GetSnapshot())
        {
            var convertedLatency = sample.Duration.ConvertTo(targetUnit);
            
            var normalizedSample = new Sample(
                sample.Timestamp,
                convertedLatency,
                sample.Status,
                sample.ErrorClassification,
                sample.ExecutionContext,
                sample.Metadata);

            normalized.Add(normalizedSample);
        }

        return new SampleCollection(normalized.ToImmutable());
    }
}
