namespace PerformanceEngine.Baseline.Domain.Domain.Comparisons;

using PerformanceEngine.Baseline.Domain.Domain.Confidence;
using PerformanceEngine.Baseline.Domain.Domain.Tolerances;

/// <summary>
/// Immutable value object representing the result of comparing a single metric against baseline.
/// </summary>
public class ComparisonMetric : IEquatable<ComparisonMetric>
{
    public string MetricName { get; }
    public decimal BaselineValue { get; }
    public decimal CurrentValue { get; }
    public decimal AbsoluteChange { get; }
    public decimal RelativeChange { get; }
    public Tolerance Tolerance { get; }
    public ComparisonOutcome Outcome { get; }
    public ConfidenceLevel Confidence { get; }

    /// <param name="metricName">Name of the metric</param>
    /// <param name="baselineValue">Value from baseline</param>
    /// <param name="currentValue">Current measured value</param>
    /// <param name="tolerance">Tolerance rule applied</param>
    /// <param name="outcome">Determined outcome</param>
    /// <param name="confidence">Calculated confidence level</param>
    public ComparisonMetric(
        string metricName,
        decimal baselineValue,
        decimal currentValue,
        Tolerance tolerance,
        ComparisonOutcome outcome,
        ConfidenceLevel confidence)
    {
        if (string.IsNullOrWhiteSpace(metricName))
            throw new ArgumentException("Metric name cannot be empty.", nameof(metricName));

        MetricName = metricName;
        BaselineValue = baselineValue;
        CurrentValue = currentValue;
        Tolerance = tolerance ?? throw new ArgumentNullException(nameof(tolerance));
        Outcome = outcome;
        Confidence = confidence ?? throw new ArgumentNullException(nameof(confidence));

        // Calculate changes
        AbsoluteChange = currentValue - baselineValue;
        RelativeChange = baselineValue != 0
            ? (currentValue - baselineValue) / Math.Abs(baselineValue) * 100
            : (currentValue == 0 ? 0 : 100);
    }

    public override bool Equals(object? obj) => Equals(obj as ComparisonMetric);

    public bool Equals(ComparisonMetric? other) =>
        other is not null &&
        MetricName == other.MetricName &&
        BaselineValue == other.BaselineValue &&
        CurrentValue == other.CurrentValue &&
        Tolerance == other.Tolerance &&
        Outcome == other.Outcome &&
        Confidence == other.Confidence;

    public override int GetHashCode() =>
        HashCode.Combine(MetricName, BaselineValue, CurrentValue, Tolerance, Outcome, Confidence);

    public override string ToString() =>
        $"{MetricName}: {BaselineValue:F2} → {CurrentValue:F2} ({Outcome}, {Confidence})";

    public static bool operator ==(ComparisonMetric? left, ComparisonMetric? right) =>
        (left, right) switch
        {
            (null, null) => true,
            (null, _) or (_, null) => false,
            _ => left.Equals(right)
        };

    public static bool operator !=(ComparisonMetric? left, ComparisonMetric? right) =>
        !(left == right);
}
