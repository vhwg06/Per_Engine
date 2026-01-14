using PerformanceEngine.Metrics.Domain.Metrics;
using PerformanceEngine.Evaluation.Domain.Domain.Evaluation;

namespace PerformanceEngine.Evaluation.Domain.Domain.Rules;

/// <summary>
/// Represents comparison operators for threshold rules.
/// </summary>
public enum ComparisonOperator
{
    /// <summary>Less than</summary>
    LessThan,
    
    /// <summary>Less than or equal</summary>
    LessThanOrEqual,
    
    /// <summary>Greater than</summary>
    GreaterThan,
    
    /// <summary>Greater than or equal</summary>
    GreaterThanOrEqual,
    /// <summary>Equal</summary>
    Equal,
    
    /// <summary>Not equal</summary>
    NotEqual
}

/// <summary>
/// Immutable rule that evaluates a metric's aggregated value against a threshold using a comparison operator.
/// Example: p95 latency less than 200ms
/// </summary>
public sealed record ThresholdRule : IRule
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
    /// The threshold value to compare against.
    /// </summary>
    public required double Threshold { get; init; }

    /// <summary>
    /// The comparison operator to use.
    /// </summary>
    public required ComparisonOperator Operator { get; init; }

    /// <summary>
    /// Evaluates the metric against this threshold rule.
    /// </summary>
    public EvaluationResult Evaluate(Metric metric)
    {
        if (metric == null)
        {
            throw new ArgumentNullException(nameof(metric));
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
                threshold: Threshold,
                message: $"Aggregation '{AggregationName}' not found in metric. Rule cannot be evaluated."
            );
            return EvaluationResult.Fail(ImmutableList.Create(violation), timestamp);
        }

        // Get actual value in milliseconds for comparison
        var actualValue = aggregationResult.Value.GetValueIn(LatencyUnit.Milliseconds);

        // Perform comparison
        bool passes = Operator switch
        {
            ComparisonOperator.LessThan => actualValue < Threshold,
            ComparisonOperator.LessThanOrEqual => actualValue <= Threshold,
            ComparisonOperator.GreaterThan => actualValue > Threshold,
            ComparisonOperator.GreaterThanOrEqual => actualValue >= Threshold,
            ComparisonOperator.Equal => Math.Abs(actualValue - Threshold) < 0.001, // Epsilon for floating point
            ComparisonOperator.NotEqual => Math.Abs(actualValue - Threshold) >= 0.001,
            _ => throw new InvalidOperationException($"Unknown operator: {Operator}")
        };

        if (passes)
        {
            return EvaluationResult.Pass(timestamp);
        }

        // Create violation with descriptive message
        var violationMessage = $"{AggregationName} {GetOperatorSymbol(Operator)} {Threshold}ms: actual value was {actualValue:F2}ms";
        var failureViolation = Violation.Create(
            ruleId: Id,
            metricName: metric.MetricType,
            actualValue: actualValue,
            threshold: Threshold,
            message: violationMessage
        );

        var isWarning = Id.Contains("warning", StringComparison.OrdinalIgnoreCase)
                        || Name.Contains("warning", StringComparison.OrdinalIgnoreCase);

        return isWarning
            ? EvaluationResult.Warning(ImmutableList.Create(failureViolation), timestamp)
            : EvaluationResult.Fail(ImmutableList.Create(failureViolation), timestamp);
    }

    private static string GetOperatorSymbol(ComparisonOperator op) => op switch
    {
        ComparisonOperator.LessThan => "<",
        ComparisonOperator.LessThanOrEqual => "<=",
        ComparisonOperator.GreaterThan => ">",
        ComparisonOperator.GreaterThanOrEqual => ">=",
        ComparisonOperator.Equal => "==",
        ComparisonOperator.NotEqual => "!=",
        _ => op.ToString()
    };

    /// <summary>
    /// Determines equality based on rule identity and configuration.
    /// </summary>
    public bool Equals(IRule? other)
    {
        return other is ThresholdRule tr &&
               Id == tr.Id &&
               AggregationName == tr.AggregationName &&
               Math.Abs(Threshold - tr.Threshold) < 0.001 &&
               Operator == tr.Operator;
    }

    /// <summary>
    /// Calculates hash code based on rule configuration.
    /// </summary>
    public override int GetHashCode()
    {
        return HashCode.Combine(Id, AggregationName, Threshold, Operator);
    }
}
