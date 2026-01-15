namespace PerformanceEngine.Baseline.Domain.Application.Services;

using System.Collections.Immutable;
using PerformanceEngine.Baseline.Domain.Application.Dto;
using PerformanceEngine.Baseline.Domain.Domain.Baselines;
using PerformanceEngine.Baseline.Domain.Domain.Ports;
using PerformanceEngine.Baseline.Domain.Domain.Tolerances;
using PerformanceEngine.Metrics.Domain.Ports;

/// <summary>
/// Implementation of IComparisonService that coordinates baseline and comparison operations.
/// Acts as a facade between infrastructure/presentation and the domain layer.
/// </summary>
public class ComparisonService : IComparisonService
{
    private readonly ComparisonOrchestrator _orchestrator;
    private readonly IBaselineRepository _repository;

    public ComparisonService(
        ComparisonOrchestrator orchestrator,
        IBaselineRepository repository)
    {
        _orchestrator = orchestrator ?? throw new ArgumentNullException(nameof(orchestrator));
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public async Task<BaselineDto> CreateBaselineAsync(
        IEnumerable<MetricDto> metrics,
        IEnumerable<ToleranceDto> tolerances)
    {
        ArgumentNullException.ThrowIfNull(metrics);
        ArgumentNullException.ThrowIfNull(tolerances);

        var metricsList = metrics.ToList();
        if (!metricsList.Any())
        {
            throw new ArgumentException("At least one metric must be provided.", nameof(metrics));
        }

        var toleranceList = tolerances.ToList();
        if (!toleranceList.Any())
        {
            throw new ArgumentException("At least one tolerance must be provided.", nameof(tolerances));
        }

        // Convert DTOs to domain objects
        var metricObjects = metricsList
            .Select(m => new DtoMetricAdapter(m))
            .Cast<IMetric>()
            .ToList();

        var toleranceConfig = new ToleranceConfiguration(
            toleranceList.Select(t => t.ToDomain())
        );

        // Create baseline through orchestrator
        var baselineId = await _orchestrator.CreateBaselineAsync(metricObjects, toleranceConfig);

        // Retrieve created baseline
        var baseline = await _repository.GetAsync(baselineId);
        if (baseline == null)
        {
            throw new BaselineDomainException("Failed to retrieve created baseline.");
        }

        return BaselineDto.FromDomain(baseline);
    }

    public async Task<ComparisonResultDto> CompareAsync(ComparisonRequestDto request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!request.IsValid())
        {
            throw new ArgumentException("Comparison request is invalid.", nameof(request));
        }

        if (!Guid.TryParse(request.BaselineId, out var baselineGuidValue))
        {
            throw new ArgumentException($"Invalid baseline ID format: {request.BaselineId}", nameof(request));
        }

        var baselineId = new BaselineId(baselineGuidValue);
        var currentMetrics = request.CurrentMetrics
            .Select(m => new DtoMetricAdapter(m))
            .Cast<IMetric>()
            .ToList();

        var toleranceConfig = request.ToToleranceConfiguration();

        // Perform comparison
        var result = await _orchestrator.CompareAsync(
            baselineId,
            currentMetrics,
            toleranceConfig,
            request.ConfidenceThreshold
        );

        return ComparisonResultDto.FromDomain(result);
    }

    public async Task<BaselineDto?> GetBaselineAsync(string baselineId)
    {
        if (string.IsNullOrWhiteSpace(baselineId))
        {
            throw new ArgumentException("Baseline ID cannot be empty.", nameof(baselineId));
        }

        if (!Guid.TryParse(baselineId, out var guidValue))
        {
            throw new ArgumentException($"Invalid baseline ID format: {baselineId}", nameof(baselineId));
        }

        var baseline = await _repository.GetAsync(new BaselineId(guidValue));
        return baseline == null ? null : BaselineDto.FromDomain(baseline);
    }

    public async Task<bool> BaselineExistsAsync(string baselineId)
    {
        if (string.IsNullOrWhiteSpace(baselineId))
        {
            throw new ArgumentException("Baseline ID cannot be empty.", nameof(baselineId));
        }

        if (!Guid.TryParse(baselineId, out var guidValue))
        {
            return false;
        }

        return await _orchestrator.BaselineExistsAsync(new BaselineId(guidValue));
    }

    public async Task DeleteBaselineAsync(string baselineId)
    {
        if (string.IsNullOrWhiteSpace(baselineId))
        {
            throw new ArgumentException("Baseline ID cannot be empty.", nameof(baselineId));
        }

        if (!Guid.TryParse(baselineId, out var guidValue))
        {
            throw new ArgumentException($"Invalid baseline ID format: {baselineId}", nameof(baselineId));
        }

        await _orchestrator.DeleteBaselineAsync(new BaselineId(guidValue));
    }

    /// <summary>
    /// Adapter that implements IMetric from a MetricDto.
    /// Used internally to convert DTOs to domain objects.
    /// </summary>
    private sealed class DtoMetricAdapter : IMetric
    {
        private readonly MetricDto _dto;

        public DtoMetricAdapter(MetricDto dto)
        {
            _dto = dto ?? throw new ArgumentNullException(nameof(dto));
        }

        public string MetricType => _dto.MetricType;
        public decimal Value => _dto.Value;
        public string Unit => _dto.Unit;
        public DateTime CollectedAt => _dto.CollectedAt;
    }
}
