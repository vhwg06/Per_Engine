namespace PerformanceEngine.Baseline.Domain.Domain.Tolerances;

using PerformanceEngine.Baseline.Domain.Domain;

/// <summary>
/// Immutable collection of tolerance rules for multiple metrics.
/// </summary>
public class ToleranceConfiguration : IEquatable<ToleranceConfiguration>
{
    private readonly IReadOnlyDictionary<string, Tolerance> _tolerances;

    public IReadOnlyList<Tolerance> Tolerances { get; }

    /// <param name="tolerances">Collection of tolerance rules</param>
    /// <exception cref="ToleranceValidationException">If configuration is invalid</exception>
    public ToleranceConfiguration(IEnumerable<Tolerance> tolerances)
    {
        if (tolerances == null)
            throw new ToleranceValidationException("", "Tolerances collection cannot be null.");

        var tolerancesList = tolerances.ToList();

        if (tolerancesList.Count == 0)
            throw new ToleranceValidationException("", "At least one tolerance rule must be defined.");

        // Check for duplicate metric names
        var duplicates = tolerancesList
            .GroupBy(t => t.MetricName)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        if (duplicates.Count > 0)
            throw new ToleranceValidationException(
                string.Join(", ", duplicates),
                "Duplicate tolerance rules for the same metric.");

        Tolerances = tolerancesList.AsReadOnly();
        _tolerances = Tolerances.ToDictionary(t => t.MetricName);
    }

    /// <summary>
    /// Retrieves the tolerance rule for a metric.
    /// </summary>
    /// <param name="metricName">Name of the metric</param>
    /// <returns>The tolerance rule for the metric</returns>
    /// <exception cref="KeyNotFoundException">If no rule exists for the metric</exception>
    public Tolerance GetTolerance(string metricName)
    {
        if (string.IsNullOrWhiteSpace(metricName))
            throw new KeyNotFoundException("Metric name cannot be empty.");

        if (!_tolerances.TryGetValue(metricName, out var tolerance))
            throw new KeyNotFoundException($"No tolerance rule defined for metric '{metricName}'.");

        return tolerance;
    }

    /// <summary>
    /// Checks if a tolerance rule exists for a metric.
    /// </summary>
    /// <param name="metricName">Name of the metric</param>
    /// <returns>True if a rule exists, false otherwise</returns>
    public bool HasTolerance(string metricName) =>
        !string.IsNullOrWhiteSpace(metricName) && _tolerances.ContainsKey(metricName);

    public override bool Equals(object? obj) => Equals(obj as ToleranceConfiguration);

    public bool Equals(ToleranceConfiguration? other) =>
        other is not null &&
        Tolerances.Count == other.Tolerances.Count &&
        Tolerances.All(t => other.HasTolerance(t.MetricName) && 
                           t.Equals(other.GetTolerance(t.MetricName)));

    public override int GetHashCode()
    {
        var hash = new HashCode();
        foreach (var tolerance in Tolerances.OrderBy(t => t.MetricName))
        {
            hash.Add(tolerance);
        }
        return hash.ToHashCode();
    }

    public override string ToString() =>
        $"ToleranceConfiguration: {Tolerances.Count} rules";

    public static bool operator ==(ToleranceConfiguration? left, ToleranceConfiguration? right) =>
        (left, right) switch
        {
            (null, null) => true,
            (null, _) or (_, null) => false,
            _ => left.Equals(right)
        };

    public static bool operator !=(ToleranceConfiguration? left, ToleranceConfiguration? right) =>
        !(left == right);
}
