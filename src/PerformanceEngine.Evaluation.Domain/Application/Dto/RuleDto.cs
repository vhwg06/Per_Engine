namespace PerformanceEngine.Evaluation.Domain.Application.Dto;

/// <summary>
/// Data transfer object for rules.
/// Serializable representation of domain rules.
/// </summary>
public sealed record RuleDto
{
    /// <summary>
    /// Unique identifier for the rule.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Rule name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Rule description.
    /// </summary>
    public required string Description { get; init; }

    /// <summary>
    /// Type of rule (e.g., "Threshold", "Range", "Custom").
    /// </summary>
    public required string RuleType { get; init; }

    /// <summary>
    /// Rule-specific configuration as key-value pairs.
    /// For ThresholdRule: { "aggregation": "p95", "operator": "LessThan", "threshold": "200" }
    /// For RangeRule: { "aggregation": "error_rate", "minBound": "10", "maxBound": "20" }
    /// </summary>
    public required Dictionary<string, string> Configuration { get; init; }
}
