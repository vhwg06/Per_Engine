using PerformanceEngine.Evaluation.Domain.Domain.Rules;

namespace PerformanceEngine.Evaluation.Domain.Domain.Rules;

/// <summary>
/// Factory for creating built-in rule types.
/// Simplifies rule instantiation with common patterns.
/// </summary>
public static class RuleFactory
{
    /// <summary>
    /// Creates a threshold rule for less-than comparison.
    /// </summary>
    public static ThresholdRule LessThan(string id, string name, string aggregationName, double threshold)
    {
        return new ThresholdRule
        {
            Id = id,
            Name = name,
            Description = $"{aggregationName} must be less than {threshold}",
            AggregationName = aggregationName,
            Threshold = threshold,
            Operator = ComparisonOperator.LessThan
        };
    }

    /// <summary>
    /// Creates a threshold rule for less-than-or-equal comparison.
    /// </summary>
    public static ThresholdRule LessThanOrEqual(string id, string name, string aggregationName, double threshold)
    {
        return new ThresholdRule
        {
            Id = id,
            Name = name,
            Description = $"{aggregationName} must be less than or equal to {threshold}",
            AggregationName = aggregationName,
            Threshold = threshold,
            Operator = ComparisonOperator.LessThanOrEqual
        };
    }

    /// <summary>
    /// Creates a threshold rule for greater-than comparison.
    /// </summary>
    public static ThresholdRule GreaterThan(string id, string name, string aggregationName, double threshold)
    {
        return new ThresholdRule
        {
            Id = id,
            Name = name,
            Description = $"{aggregationName} must be greater than {threshold}",
            AggregationName = aggregationName,
            Threshold = threshold,
            Operator = ComparisonOperator.GreaterThan
        };
    }

    /// <summary>
    /// Creates a threshold rule for greater-than-or-equal comparison.
    /// </summary>
    public static ThresholdRule GreaterThanOrEqual(string id, string name, string aggregationName, double threshold)
    {
        return new ThresholdRule
        {
            Id = id,
            Name = name,
            Description = $"{aggregationName} must be greater than or equal to {threshold}",
            AggregationName = aggregationName,
            Threshold = threshold,
            Operator = ComparisonOperator.GreaterThanOrEqual
        };
    }

    /// <summary>
    /// Creates a range rule with inclusive bounds.
    /// </summary>
    public static RangeRule Range(string id, string name, string aggregationName, double minBound, double maxBound)
    {
        return new RangeRule
        {
            Id = id,
            Name = name,
            Description = $"{aggregationName} must be between {minBound} and {maxBound}",
            AggregationName = aggregationName,
            MinBound = minBound,
            MaxBound = maxBound
        };
    }

    /// <summary>
    /// Creates a P95 latency threshold rule (common pattern).
    /// </summary>
    public static ThresholdRule P95LatencyRule(string id, double maxMilliseconds)
    {
        return LessThan(id, "P95 Latency", "p95", maxMilliseconds);
    }

    /// <summary>
    /// Creates a P99 latency threshold rule (common pattern).
    /// </summary>
    public static ThresholdRule P99LatencyRule(string id, double maxMilliseconds)
    {
        return LessThan(id, "P99 Latency", "p99", maxMilliseconds);
    }

    /// <summary>
    /// Creates an error rate threshold rule (common pattern).
    /// </summary>
    public static ThresholdRule ErrorRateRule(string id, double maxPercentage)
    {
        return LessThan(id, "Error Rate", "error_rate", maxPercentage);
    }

    /// <summary>
    /// Creates an error rate range rule (common pattern).
    /// </summary>
    public static RangeRule ErrorRateRangeRule(string id, double minPercentage, double maxPercentage)
    {
        return Range(id, "Error Rate Range", "error_rate", minPercentage, maxPercentage);
    }
}
