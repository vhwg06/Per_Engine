namespace PerformanceEngine.Baseline.Domain.Domain.Baselines;

using PerformanceEngine.Baseline.Domain.Domain.Tolerances;

/// <summary>
/// Aggregate root representing an immutable snapshot of performance baseline metrics.
/// Once created, a baseline cannot be modified (immutability enforced at domain level).
/// </summary>
public class Baseline : IEquatable<Baseline>
{
    public BaselineId Id { get; }
    public DateTime CreatedAt { get; }
    public IReadOnlyList<IMetric> Metrics { get; }
    public ToleranceConfiguration ToleranceConfig { get; }

    /// <param name="metrics">Baseline metrics (will be stored immutably)</param>
    /// <param name="toleranceConfig">Tolerance rules for comparisons</param>
    /// <param name="id">Baseline identifier (generated if null)</param>
    /// <param name="createdAt">Creation timestamp (uses current time if default)</param>
    /// <exception cref="DomainInvariantViolatedException">If baseline violates invariants</exception>
    public Baseline(
        IEnumerable<IMetric> metrics,
        ToleranceConfiguration toleranceConfig,
        BaselineId? id = null,
        DateTime? createdAt = null)
    {
        var metricsList = metrics?.ToList().AsReadOnly() ?? 
            throw new ArgumentNullException(nameof(metrics));

        // Validate invariants
        BaselineInvariants.AssertValid(metricsList, toleranceConfig);
        BaselineInvariants.AssertImmutable(metricsList);

        Id = id ?? new BaselineId();
        Metrics = metricsList;
        ToleranceConfig = toleranceConfig;
        CreatedAt = createdAt ?? DateTime.UtcNow;
    }

    /// <summary>
    /// Retrieves a metric by type.
    /// </summary>
    /// <param name="metricType">Metric type</param>
    /// <returns>Metric if found, null otherwise</returns>
    public IMetric? GetMetric(string metricType) =>
        Metrics.FirstOrDefault(m => m.MetricType == metricType);

    public override bool Equals(object? obj) => Equals(obj as Baseline);

    public bool Equals(Baseline? other) =>
        other is not null &&
        Id == other.Id &&
        CreatedAt == other.CreatedAt &&
        Metrics.Count == other.Metrics.Count &&
        Metrics.SequenceEqual(other.Metrics) &&
        ToleranceConfig == other.ToleranceConfig;

    public override int GetHashCode() =>
        HashCode.Combine(Id, CreatedAt, Metrics.Count, ToleranceConfig);

    public override string ToString() =>
        $"Baseline {Id} (created: {CreatedAt:O}, metrics: {Metrics.Count})";

    public static bool operator ==(Baseline? left, Baseline? right) =>
        (left, right) switch
        {
            (null, null) => true,
            (null, _) or (_, null) => false,
            _ => left.Equals(right)
        };

    public static bool operator !=(Baseline? left, Baseline? right) =>
        !(left == right);
}
