namespace PerformanceEngine.Baseline.Domain.Application.Dto;

using System.Collections.Immutable;
using PerformanceEngine.Baseline.Domain.Domain.Baselines;
using PerformanceEngine.Baseline.Domain.Domain.Tolerances;
using PerformanceEngine.Metrics.Domain.Metrics;

/// <summary>
/// Data transfer object for baseline information.
/// Serializes a baseline snapshot for transfer between application and infrastructure layers.
/// </summary>
public class BaselineDto
{
    /// <summary>
    /// Gets the unique identifier for this baseline.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Gets the timestamp when this baseline was created.
    /// </summary>
    public required DateTime CreatedAt { get; init; }

    /// <summary>
    /// Gets the collection of metric DTOs that define this baseline.
    /// </summary>
    public required IReadOnlyList<MetricDto> MetricDtos { get; init; }

    /// <summary>
    /// Gets the collection of tolerance DTOs that apply to metric comparisons.
    /// </summary>
    public required IReadOnlyList<ToleranceDto> ToleranceDtos { get; init; }

    /// <summary>
    /// Creates a BaselineDto from a domain Baseline object.
    /// </summary>
    public static BaselineDto FromDomain(Baseline baseline) =>
        new()
        {
            Id = baseline.Id.Value,
            CreatedAt = baseline.CreatedAt,
            MetricDtos = baseline.Metrics
                .Select(m => new MetricDto
                {
                    Id = m.Id,
                    MetricType = m.MetricType,
                    Value = m.Value,
                    Unit = m.Unit,
                    ComputedAt = m.ComputedAt
                })
                .ToImmutableList(),
            ToleranceDtos = baseline.ToleranceConfig.Tolerances
                .Select(ToleranceDto.FromDomain)
                .ToImmutableList()
        };

    /// <summary>
    /// Converts this DTO back to domain objects.
    /// Note: Returns the Baseline aggregate root with reconstructed dependencies.
    /// </summary>
    public Baseline ToDomain()
    {
        var metrics = MetricDtos
            .Select(m => new DtoMetricAdapter(m))
            .Cast<IMetric>()
            .ToList();

        var toleranceConfig = new ToleranceConfiguration(
            ToleranceDtos.Select(t => t.ToDomain())
        );

        var baselineId = new BaselineId(Id);

        return new Baseline(metrics, toleranceConfig, baselineId, CreatedAt);
    }

    /// <summary>
    /// Adapter that implements IMetric from a MetricDto.
    /// </summary>
    private sealed class DtoMetricAdapter : IMetric
    {
        private readonly MetricDto _dto;

        public DtoMetricAdapter(MetricDto dto)
        {
            _dto = dto ?? throw new ArgumentNullException(nameof(dto));
        }

        public Guid Id => _dto.Id;
        public string MetricType => _dto.MetricType;
        public double Value => _dto.Value;
        public string Unit => _dto.Unit;
        public DateTime ComputedAt => _dto.ComputedAt;
        public CompletessStatus CompletessStatus => CompletessStatus.COMPLETE;
        public MetricEvidence Evidence => new() { SampleCount = 1, RequiredSampleCount = 1, AggregationWindow = "dto-reconstruction" };
    }
}
