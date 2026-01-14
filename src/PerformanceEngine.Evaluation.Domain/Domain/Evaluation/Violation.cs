namespace PerformanceEngine.Evaluation.Domain.Domain.Evaluation;

/// <summary>
/// Immutable value object representing a rule violation.
/// Contains complete context about what failed: rule, metric, threshold, and diagnostic message.
/// </summary>
public sealed record Violation
{
    /// <summary>
    /// Unique identifier of the rule that was violated.
    /// </summary>
    public required string RuleId { get; init; }

    /// <summary>
    /// Name of the metric that violated the rule.
    /// </summary>
    public required string MetricName { get; init; }

    /// <summary>
    /// Actual value observed that caused the violation.
    /// </summary>
    public required double ActualValue { get; init; }

    /// <summary>
    /// Expected threshold or constraint that was violated.
    /// </summary>
    public required double Threshold { get; init; }

    /// <summary>
    /// Human-readable diagnostic message explaining the violation.
    /// </summary>
    public required string Message { get; init; }

    /// <summary>
    /// Initializes a new violation record with validation.
    /// </summary>
    public Violation()
    {
    }

    /// <summary>
    /// Factory method to create a violation with validation.
    /// </summary>
    public static Violation Create(string ruleId, string metricName, double actualValue, double threshold, string message)
    {
        if (string.IsNullOrWhiteSpace(ruleId))
            throw new ArgumentException("RuleId cannot be null or whitespace.", nameof(ruleId));
        
        if (string.IsNullOrWhiteSpace(metricName))
            throw new ArgumentException("MetricName cannot be null or whitespace.", nameof(metricName));
        
        if (string.IsNullOrWhiteSpace(message))
            throw new ArgumentException("Message cannot be null or whitespace.", nameof(message));

        return new Violation
        {
            RuleId = ruleId,
            MetricName = metricName,
            ActualValue = actualValue,
            Threshold = threshold,
            Message = message
        };
    }

    /// <summary>
    /// Returns a deterministic string representation for testing and debugging.
    /// Format: "{RuleId}: {MetricName} = {ActualValue} (expected {Threshold}) - {Message}"
    /// </summary>
    public override string ToString()
    {
        return $"{RuleId}: {MetricName} = {ActualValue.ToString("F2", System.Globalization.CultureInfo.InvariantCulture)} (expected {Threshold.ToString("F2", System.Globalization.CultureInfo.InvariantCulture)}) - {Message}";
    }
}
