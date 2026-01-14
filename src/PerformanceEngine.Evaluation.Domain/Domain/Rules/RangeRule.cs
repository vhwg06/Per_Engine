using PerformanceEngine.Metrics.Domain.Metrics;
using PerformanceEngine.Evaluation.Domain.Domain.Evaluation;

namespace PerformanceEngine.Evaluation.Domain.Domain.Rules;

/// <summary>
/// Immutable rule that evaluates whether a metric's aggregated value falls within a specified range.
/// Example: error rate between 10 and 20 (value must be between min and max, exclusive).
/// </summary>
public sealed record RangeRule : IRule
{
    /// <summary>
    /// Unique identifier for this rule instance.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Human-readable name for this rule.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Detailed description of what this rule validates.
    /// </summary>
    public required string Description { get; init; }

    /// <summary>
    /// The name of the aggregation operation to evaluate (e.g., "p95", "average", "max").
    /// Must match an OperationName in the metric's AggregatedValues.
    /// </summary>
    public required string AggregationName { get; init; }

    /// <summary>
    /// The minimum acceptable value (exclusive).
    /// </summary>
    public required double MinBound { get; init; }

    /// <summary>
    /// The maximum acceptable value (exclusive).
    /// </summary>
    public required double MaxBound { get; init; }

    /// <summary>
    /// Evaluates the metric against this range rule.
    /// Value must be between MinBound and MaxBound (exclusive on both ends).
    /// </summary>
    public EvaluationResult Evaluate(Metric metric)
    {
        if (metric == null)
        {
            throw new ArgumentNullException(nameof(metric));
        }

        if (MinBound >= MaxBound)
        {
            throw new InvalidOperationException($"MinBound ({MinBound}) must be less than MaxBound ({MaxBound})");
        }

        var timestamp = DateTime.UtcNow;

        // Find the aggregation result matching our aggregation name
        var aggregationResult = metric.AggregatedValues
            .FirstOrDefault(a => a.OperationName.Equals(AggregationName, StringComparison.OrdinalIgnoreCase));

        if (aggregationResult == null)
        {
            // Aggregation not found - create violation
            var violation = Violation.Create(
                ruleId: Id,
                metricName: metric.MetricType,
                actualValue: double.NaN,
                threshold: (MinBound + MaxBound) / 2.0, // Use midpoint as representative threshold
                message: $"Aggregation '{AggregationName}' not found in metric. Rule cannot be evaluated."
            );
            return EvaluationResult.Fail(ImmutableList.Create(violation), timestamp);
        }

        // Get actual value in milliseconds for comparison
        var actualValue = aggregationResult.Value.GetValueIn(LatencyUnit.Milliseconds);

        // Check if value is within range (exclusive)
        bool inRange = actualValue > MinBound && actualValue < MaxBound;

        if (inRange)
        {
            return EvaluationResult.Pass(timestamp);
        }

        // Determine which bound was violated
        string violationReason;
        double violatedBound;
        bool isWarning;

        if (actualValue <= MinBound)
        {
            violationReason = $"{AggregationName} not in range ({MinBound}ms - {MaxBound}ms): actual value was {actualValue:F2}ms (below minimum)";
            violatedBound = MinBound;
            isWarning = false; // below/equal minimum is always a fail
        }
        else // actualValue > MaxBound
        {
            violationReason = $"{AggregationName} not in range ({MinBound}ms - {MaxBound}ms): actual value was {actualValue:F2}ms (above maximum)";
            violatedBound = MaxBound;
            // Only Throughput aggregations that exceed max trigger a warning; all others fail
            isWarning = AggregationName.Equals("Throughput", StringComparison.OrdinalIgnoreCase);
        }

        var boundViolation = Violation.Create(
            ruleId: Id,
            metricName: metric.MetricType,
            actualValue: actualValue,
            threshold: violatedBound,
            message: violationReason
        );

        return isWarning
            ? EvaluationResult.Warning(ImmutableList.Create(boundViolation), timestamp)
            : EvaluationResult.Fail(ImmutableList.Create(boundViolation), timestamp);
    }

    /// <summary>
    /// Determines equality based on rule identity and configuration.
    /// </summary>
    public bool Equals(IRule? other)
    {
        return other is RangeRule rr &&
               Id == rr.Id &&
               AggregationName == rr.AggregationName &&
               Math.Abs(MinBound - rr.MinBound) < 0.001 &&
               Math.Abs(MaxBound - rr.MaxBound) < 0.001;
    }

    /// <summary>
    /// Calculates hash code based on rule configuration.
    /// </summary>
    public override int GetHashCode()
    {
        return HashCode.Combine(Id, AggregationName, MinBound, MaxBound);
    }
}
