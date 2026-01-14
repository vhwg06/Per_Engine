namespace PerformanceEngine.Metrics.Domain.Application.Dto;

using PerformanceEngine.Metrics.Domain.Metrics;
using System.Linq;

/// <summary>
/// Data transfer object for Metric domain entity.
/// Used for transferring metric data across application boundaries.
/// </summary>
public sealed class MetricDto
{
    /// <summary>
    /// Gets the metric unique identifier
    /// </summary>
    public Guid Id { get; }

    /// <summary>
    /// Gets the samples that were aggregated
    /// </summary>
    public IReadOnlyList<SampleDto> Samples { get; }

    /// <summary>
    /// Gets the aggregation window (as string representation)
    /// </summary>
    public string WindowName { get; }

    /// <summary>
    /// Gets the type of metric
    /// </summary>
    public string MetricType { get; }

    /// <summary>
    /// Gets the aggregation results
    /// </summary>
    public IReadOnlyList<AggregationResultDto> AggregatedValues { get; }

    /// <summary>
    /// Gets the computation timestamp
    /// </summary>
    public DateTime ComputedAt { get; }

    private MetricDto(
        Guid id,
        IReadOnlyList<SampleDto> samples,
        string windowName,
        string metricType,
        IReadOnlyList<AggregationResultDto> aggregatedValues,
        DateTime computedAt)
    {
        Id = id;
        Samples = samples;
        WindowName = windowName;
        MetricType = metricType;
        AggregatedValues = aggregatedValues;
        ComputedAt = computedAt;
    }

    /// <summary>
    /// Creates a MetricDto from a Metric domain entity.
    /// </summary>
    public static MetricDto FromDomain(Metric metric)
    {
        if (metric is null)
            throw new ArgumentNullException(nameof(metric));

        var sampleDtos = metric.Samples.AllSamples
            .Select(SampleDto.FromDomain)
            .ToList()
            .AsReadOnly();

        var resultDtos = metric.AggregatedValues
            .Select(AggregationResultDto.FromDomain)
            .ToList()
            .AsReadOnly();

        return new MetricDto(
            metric.Id,
            sampleDtos,
            metric.Window.Name,
            metric.MetricType,
            resultDtos,
            metric.ComputedAt);
    }

    /// <summary>
    /// Creates a Metric domain entity from this DTO.
    /// Note: This uses FullExecution window since window info is limited in DTO.
    /// </summary>
    public Metric ToDomain()
    {
        var samples = new SampleCollection();
        foreach (var sampleDto in Samples)
        {
            samples.Add(sampleDto.ToDomain());
        }

        var window = AggregationWindow.FullExecution();
        var results = AggregatedValues
            .Select(r => r.ToDomain())
            .ToList();

        return new Metric(samples, window, MetricType, results, ComputedAt);
    }
}

/// <summary>
/// Data transfer object for AggregationResult value object.
/// </summary>
public sealed class AggregationResultDto
{
    /// <summary>
    /// Gets the aggregated value
    /// </summary>
    public double Value { get; }

    /// <summary>
    /// Gets the latency unit
    /// </summary>
    public LatencyUnit Unit { get; }

    /// <summary>
    /// Gets the name of the operation that produced this result
    /// </summary>
    public string OperationName { get; }

    /// <summary>
    /// Gets when this result was computed
    /// </summary>
    public DateTime ComputedAt { get; }

    private AggregationResultDto(double value, LatencyUnit unit, string operationName, DateTime computedAt)
    {
        Value = value;
        Unit = unit;
        OperationName = operationName;
        ComputedAt = computedAt;
    }

    /// <summary>
    /// Creates a DTO from a domain AggregationResult.
    /// </summary>
    public static AggregationResultDto FromDomain(AggregationResult result)
    {
        if (result is null)
            throw new ArgumentNullException(nameof(result));

        return new AggregationResultDto(result.Value.Value, result.Value.Unit, result.OperationName, result.ComputedAt);
    }

    /// <summary>
    /// Creates a domain AggregationResult from this DTO.
    /// </summary>
    public AggregationResult ToDomain()
    {
        var latency = new Latency(Value, Unit);
        return new AggregationResult(latency, OperationName, ComputedAt);
    }
}

