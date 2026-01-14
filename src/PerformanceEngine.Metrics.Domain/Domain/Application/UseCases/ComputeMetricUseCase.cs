namespace PerformanceEngine.Metrics.Domain.Application.UseCases;

using PerformanceEngine.Metrics.Domain.Aggregations;
using PerformanceEngine.Metrics.Domain.Application.Dto;
using PerformanceEngine.Metrics.Domain.Metrics;

/// <summary>
/// Main orchestration use case for computing metrics from samples.
/// Coordinates validation, normalization, aggregation, and metric creation.
/// </summary>
public sealed class ComputeMetricUseCase
{
    private readonly ValidateAggregationUseCase _validationUseCase;
    private readonly NormalizeSamplesUseCase _normalizationUseCase;

    public ComputeMetricUseCase()
    {
        _validationUseCase = new ValidateAggregationUseCase();
        _normalizationUseCase = new NormalizeSamplesUseCase();
    }

    /// <summary>
    /// Computes a metric from the provided samples and aggregation parameters.
    /// Overload that takes individual parameters instead of DTO.
    /// </summary>
    public Metric Execute(SampleCollection samples, AggregationWindow window, string operationName)
    {
        if (samples is null)
            throw new ArgumentNullException(nameof(samples));
        if (window is null)
            throw new ArgumentNullException(nameof(window));
        if (string.IsNullOrWhiteSpace(operationName))
            throw new ArgumentException("Operation name cannot be null or empty", nameof(operationName));

        // Create a temporary DTO to use with existing validation logic
        var request = new AggregationRequestDto(samples, window, operationName);

        // Step 1: Validate samples
        var validationResult = _validationUseCase.Execute(request);
        if (!validationResult.IsValid)
        {
            throw new InvalidOperationException(
                $"Aggregation validation failed: {string.Join(", ", validationResult.Errors)}");
        }

        // Step 2: Normalize to consistent units
        var normalizedSamples = _normalizationUseCase.Execute(samples);

        // Step 3: Execute aggregation
        var aggregation = CreateAggregationOperation(operationName);
        var aggregationResult = aggregation.Aggregate(normalizedSamples, window);

        // Step 4: Create Metric entity with result
        var metric = new Metric(
            normalizedSamples,
            window,
            operationName,
            new[] { aggregationResult },
            DateTime.UtcNow);

        // Step 5: Could publish MetricComputedEvent here if event publishing is implemented
        // _eventPublisher.Publish(new MetricComputedEvent(metric, operationName, DateTime.UtcNow));

        return metric;
    }

    /// <summary>
    /// Creates the appropriate aggregation operation based on the operation name.
    /// </summary>
    private static IAggregationOperation CreateAggregationOperation(string operationName)
    {
        return operationName.ToLowerInvariant() switch
        {
            "average" => new AverageAggregation(),
            "max" => new MaxAggregation(),
            "min" => new MinAggregation(),
            "p0" => new PercentileAggregation(new Percentile(0)),
            "p50" => new PercentileAggregation(new Percentile(50)),
            "p95" => new PercentileAggregation(new Percentile(95)),
            "p99" => new PercentileAggregation(new Percentile(99)),
            "p99.9" => new PercentileAggregation(new Percentile(99.9)),
            "p100" => new PercentileAggregation(new Percentile(100)),
            _ => throw new InvalidOperationException($"Unknown aggregation operation: {operationName}")
        };
    }
}
