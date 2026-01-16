namespace PerformanceEngine.Baseline.Domain.Domain.Comparisons;

using PerformanceEngine.Baseline.Domain.Domain.Baselines;
using PerformanceEngine.Baseline.Domain.Domain.Confidence;

/// <summary>
/// Aggregate root representing the immutable result of comparing current metrics against a baseline.
/// </summary>
public class ComparisonResult : IEquatable<ComparisonResult>
{
    public ComparisonResultId Id { get; }
    public BaselineId BaselineId { get; }
    public DateTime ComparedAt { get; }
    public ComparisonOutcome OverallOutcome { get; }
    public ConfidenceLevel OverallConfidence { get; }
    public IReadOnlyList<ComparisonMetric> MetricResults { get; }

    /// <param name="baselineId">ID of the baseline used for comparison</param>
    /// <param name="metricResults">Per-metric comparison results</param>
    /// <param name="overallOutcome">Aggregated overall outcome</param>
    /// <param name="overallConfidence">Aggregated overall confidence</param>
    /// <param name="id">Comparison result ID (generated if null)</param>
    /// <param name="comparedAt">Comparison timestamp (uses current time if default)</param>
    /// <exception cref="DomainInvariantViolatedException">If result violates invariants</exception>
    public ComparisonResult(
        BaselineId baselineId,
        IEnumerable<ComparisonMetric> metricResults,
        ComparisonOutcome overallOutcome,
        ConfidenceLevel overallConfidence,
        ComparisonResultId? id = null,
        DateTime? comparedAt = null)
    {
        var metricsList = metricResults?.ToList().AsReadOnly() ??
            throw new ArgumentNullException(nameof(metricResults));

        // Validate invariants
        ComparisonResultInvariants.AssertValid(metricsList);
        ComparisonResultInvariants.AssertImmutable(metricsList);

        Id = id ?? new ComparisonResultId();
        BaselineId = baselineId ?? throw new ArgumentNullException(nameof(baselineId));
        MetricResults = metricsList;
        OverallOutcome = overallOutcome;
        OverallConfidence = overallConfidence ?? throw new ArgumentNullException(nameof(overallConfidence));
        ComparedAt = comparedAt ?? DateTime.UtcNow;
    }

    /// <summary>
    /// Determines if the comparison result contains any regression.
    /// </summary>
    /// <returns>True if overall outcome is REGRESSION, false otherwise</returns>
    public bool HasRegression() => OverallOutcome == ComparisonOutcome.Regression;

    public override bool Equals(object? obj) => Equals(obj as ComparisonResult);

    public bool Equals(ComparisonResult? other) =>
        other is not null &&
        Id == other.Id &&
        BaselineId == other.BaselineId &&
        ComparedAt == other.ComparedAt &&
        OverallOutcome == other.OverallOutcome &&
        OverallConfidence == other.OverallConfidence &&
        MetricResults.Count == other.MetricResults.Count &&
        MetricResults.SequenceEqual(other.MetricResults);

    public override int GetHashCode() =>
        HashCode.Combine(Id, BaselineId, ComparedAt, OverallOutcome, OverallConfidence);

    public override string ToString() =>
        $"ComparisonResult {Id} (baseline: {BaselineId}, outcome: {OverallOutcome}, confidence: {OverallConfidence})";

    public static bool operator ==(ComparisonResult? left, ComparisonResult? right) =>
        (left, right) switch
        {
            (null, null) => true,
            (null, _) or (_, null) => false,
            _ => left.Equals(right)
        };

    public static bool operator !=(ComparisonResult? left, ComparisonResult? right) =>
        !(left == right);
}
