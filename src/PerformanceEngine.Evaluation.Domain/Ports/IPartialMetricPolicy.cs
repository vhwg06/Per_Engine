namespace PerformanceEngine.Evaluation.Domain.Ports;

/// <summary>
/// Port defining policies for handling partial metrics in evaluation.
/// Allows configuration of which rules can accept partial metrics vs. requiring complete metrics.
/// Supports per-rule and global policies for flexibility.
/// </summary>
public interface IPartialMetricPolicy
{
    /// <summary>
    /// Determines whether a partial metric can be used for evaluation under a specific rule.
    /// </summary>
    /// <param name="ruleId">The ID of the rule being evaluated.</param>
    /// <returns>true if partial metrics are allowed for this rule; false if only complete metrics are allowed.</returns>
    bool IsPartialMetricAllowed(string ruleId);

    /// <summary>
    /// Determines whether a partial metric can be used for evaluation globally.
    /// Used when rule-specific policies don't apply or as a default policy.
    /// </summary>
    /// <returns>true if partial metrics are allowed by default; false otherwise.</returns>
    bool IsPartialMetricAllowedByDefault();
}
