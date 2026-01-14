namespace PerformanceEngine.Metrics.Domain.Application.Services;

using PerformanceEngine.Metrics.Domain.Application.Dto;
using PerformanceEngine.Metrics.Domain.Application.UseCases;

/// <summary>
/// Application service for computing metrics from aggregation requests.
/// Serves as the facade between adapters and domain logic.
/// </summary>
public sealed class MetricService
{
    private readonly ComputeMetricUseCase _computeMetricUseCase;

    /// <summary>
    /// Initializes a new instance of MetricService.
    /// </summary>
    public MetricService()
    {
        _computeMetricUseCase = new ComputeMetricUseCase();
    }

    /// <summary>
    /// Computes a metric from aggregation request data.
    /// </summary>
    /// <param name="request">The aggregation request DTO</param>
    /// <returns>The computed metric DTO, or null if computation fails</returns>
    public MetricDto? ComputeMetric(AggregationRequestDto request)
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));

        try
        {
            var metric = _computeMetricUseCase.Execute(
                request.Samples,
                request.Window,
                request.AggregationOperation);

            if (metric is null)
                return null;

            return MetricDto.FromDomain(metric);
        }
        catch (Exception ex)
        {
            // Log exception and return null for graceful degradation
            System.Diagnostics.Debug.WriteLine($"Metric computation failed: {ex.Message}");
            return null;
        }
    }
}
