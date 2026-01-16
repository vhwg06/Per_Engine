namespace PerformanceEngine.Baseline.Domain.Application.Dto;

using PerformanceEngine.Baseline.Domain.Domain.Tolerances;

/// <summary>
/// Data transfer object for tolerance information.
/// Serializes tolerance configuration for transfer between layers.
/// </summary>
public class ToleranceDto
{
    /// <summary>
    /// Gets the metric name this tolerance applies to.
    /// </summary>
    public required string MetricName { get; init; }

    /// <summary>
    /// Gets the tolerance type (Relative or Absolute).
    /// </summary>
    public required ToleranceType Type { get; init; }

    /// <summary>
    /// Gets the tolerance amount (percentage for Relative, fixed value for Absolute).
    /// </summary>
    public required decimal Amount { get; init; }

    /// <summary>
    /// Creates a ToleranceDto from a domain Tolerance object.
    /// </summary>
    public static ToleranceDto FromDomain(Tolerance tolerance) =>
        new()
        {
            MetricName = tolerance.MetricName,
            Type = tolerance.Type,
            Amount = tolerance.Amount
        };

    /// <summary>
    /// Converts this DTO back to a domain Tolerance object.
    /// </summary>
    public Tolerance ToDomain() =>
        new(MetricName, Type, Amount);
}
