namespace PerformanceEngine.Evaluation.Domain.Domain.Evaluation;

using PerformanceEngine.Evaluation.Domain.ValueObjects;
using PerformanceEngine.Metrics.Domain.Metrics;

/// <summary>
/// Value object representing a reference to a metric used in evaluation.
/// Captures the metric's name, actual value, unit, and completeness status.
/// Immutable record and used for evidence trail documentation.
/// </summary>
public sealed record MetricReference : ValueObject
{
    /// <summary>
    /// Gets the name/key of the aggregation this metric was derived from
    /// </summary>
    public string AggregationName { get; init; }

    /// <summary>
    /// Gets the actual measured value
    /// </summary>
    public double Value { get; init; }

    /// <summary>
    /// Gets the unit of measurement
    /// </summary>
    public string Unit { get; init; }

    /// <summary>
    /// Gets the completeness status of this metric (COMPLETE or PARTIAL)
    /// </summary>
    public CompletessStatus CompletessStatus { get; init; }

    /// <summary>
    /// Parameterless constructor for record initialization.
    /// </summary>
    public MetricReference()
    {
        AggregationName = string.Empty;
        Value = 0.0;
        Unit = string.Empty;
        CompletessStatus = CompletessStatus.COMPLETE;
    }

    /// <summary>
    /// Initializes a new instance of the MetricReference record.
    /// </summary>
    /// <param name="aggregationName">The name of the aggregation</param>
    /// <param name="value">The actual measured value</param>
    /// <param name="unit">The unit of measurement</param>
    /// <param name="completessStatus">The completeness status</param>
    public MetricReference(
        string aggregationName,
        double value,
        string unit,
        CompletessStatus completessStatus)
    {
        if (string.IsNullOrWhiteSpace(aggregationName))
        {
            throw new ArgumentException("Aggregation name cannot be null or empty", nameof(aggregationName));
        }

        if (string.IsNullOrWhiteSpace(unit))
        {
            throw new ArgumentException("Unit cannot be null or empty", nameof(unit));
        }

        if (!Enum.IsDefined(typeof(CompletessStatus), completessStatus))
        {
            throw new ArgumentException($"Invalid completeness status: {completessStatus}", nameof(completessStatus));
        }

        AggregationName = aggregationName.Trim();
        Value = value;
        Unit = unit.Trim();
        CompletessStatus = completessStatus;
    }

    /// <summary>
    /// Returns a string representation of this metric reference.
    /// </summary>
    public override string ToString() => $"{AggregationName}={Value}{Unit} ({CompletessStatus})";
}
