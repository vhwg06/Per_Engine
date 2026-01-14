namespace PerformanceEngine.Metrics.Domain.Application.UseCases;

using PerformanceEngine.Metrics.Domain.Aggregations;
using PerformanceEngine.Metrics.Domain.Metrics;

/// <summary>
/// Use case for normalizing samples to a consistent latency unit.
/// Ensures all samples in a collection use the same unit before aggregation.
/// </summary>
public sealed class NormalizeSamplesUseCase
{
    /// <summary>
    /// Normalizes all samples in a collection to the specified target unit.
    /// </summary>
    /// <param name="samples">Collection of samples with potentially mixed units</param>
    /// <param name="targetUnit">The target unit to normalize to</param>
    /// <returns>New normalized SampleCollection</returns>
    /// <exception cref="ArgumentNullException">Thrown if samples is null</exception>
    public SampleCollection Execute(SampleCollection samples, LatencyUnit targetUnit = LatencyUnit.Milliseconds)
    {
        if (samples is null)
            throw new ArgumentNullException(nameof(samples), "Sample collection cannot be null");

        return AggregationNormalizer.NormalizeSamples(samples, targetUnit);
    }
}
