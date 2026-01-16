namespace PerformanceEngine.Evaluation.Domain;

/// <summary>
/// Immutable value object representing a rule violation detected during evaluation.
/// Preserves all information needed for audit trails and debugging.
/// 
/// Invariants:
/// - RuleName, MetricName, and Message are non-empty strings
/// - Severity is a defined enum value
/// - Actual and Threshold values preserve metric precision (stored as strings)
/// - Violation cannot be modified after construction
/// </summary>
public record Violation(
    /// <summary>Name of the rule that was violated.</summary>
    string RuleName,
    
    /// <summary>Name of the metric that violated the rule.</summary>
    string MetricName,
    
    /// <summary>Severity level of the violation.</summary>
    Severity Severity,
    
    /// <summary>Actual metric value that caused the violation (string to preserve precision).</summary>
    string ActualValue,
    
    /// <summary>Threshold value that was exceeded (string to preserve precision).</summary>
    string ThresholdValue,
    
    /// <summary>Human-readable violation message.</summary>
    string Message
)
{
    /// <summary>
    /// Factory method with validation.
    /// </summary>
    public static Violation Create(
        string ruleName,
        string metricName,
        Severity severity,
        string actualValue,
        string thresholdValue,
        string message)
    {
        if (string.IsNullOrWhiteSpace(ruleName))
            throw new ArgumentException("Rule name must not be empty", nameof(ruleName));

        if (string.IsNullOrWhiteSpace(metricName))
            throw new ArgumentException("Metric name must not be empty", nameof(metricName));

        if (string.IsNullOrWhiteSpace(actualValue))
            throw new ArgumentException("Actual value must not be empty", nameof(actualValue));

        if (string.IsNullOrWhiteSpace(thresholdValue))
            throw new ArgumentException("Threshold value must not be empty", nameof(thresholdValue));

        if (string.IsNullOrWhiteSpace(message))
            throw new ArgumentException("Message must not be empty", nameof(message));

        return new Violation(
            RuleName: ruleName,
            MetricName: metricName,
            Severity: severity,
            ActualValue: actualValue,
            ThresholdValue: thresholdValue,
            Message: message);
    }
}
