namespace PerformanceEngine.Baseline.Domain.Application.Dto;

using System.Collections.Immutable;
using PerformanceEngine.Baseline.Domain.Domain.Baselines;
using PerformanceEngine.Baseline.Domain.Domain.Tolerances;

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
            Id = baseline.Id.Value.ToString(),
            CreatedAt = baseline.CreatedAt,
            MetricDtos = baseline.Metrics
                .Select(m => new MetricDto
                {
                    MetricType = m.MetricType,
                    Value = m.Value,
                    Unit = m.Unit,
                    CollectedAt = m.CollectedAt
                })
                .ToImmutableList(),
            ToleranceDtos = baseline.ToleranceConfig
                .GetAllTolerances()
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
            .Select(m => new MockMetric(m.MetricType, m.Value, m.Unit, m.CollectedAt))
            .Cast<IMetric>()
            .ToList();

        var toleranceConfig = new ToleranceConfiguration(
            ToleranceDtos.Select(t => t.ToDomain())
        );

        var baselineId = Guid.TryParse(Id, out var guidValue)
            ? new BaselineId(guidValue)
            : new BaselineId();

        return new Baseline(metrics, toleranceConfig, baselineId, CreatedAt);
    }

    /// <summary>
    /// Mock metric implementation for DTO reconstruction.
    /// This is a helper class used internally by ToDomain() to reconstruct metrics.
    /// </summary>
    private sealed class MockMetric : IMetric
    {
        public MockMetric(string metricType, decimal value, string unit, DateTime collectedAt)
        {
            MetricType = metricType;
            Value = value;
            Unit = unit;
            CollectedAt = collectedAt;
        }

        public string MetricType { get; }
        public decimal Value { get; }
        public string Unit { get; }
        public DateTime CollectedAt { get; }
    }
}
