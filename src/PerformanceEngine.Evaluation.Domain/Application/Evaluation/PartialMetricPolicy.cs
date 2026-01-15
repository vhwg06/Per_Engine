namespace PerformanceEngine.Evaluation.Domain.Application.Evaluation;

using PerformanceEngine.Evaluation.Domain.Ports;

/// <summary>
/// Default implementation of IPartialMetricPolicy.
/// By default, denies partial metrics across all rules unless explicitly configured otherwise.
/// Provides a conservative, fail-safe approach: partial data is treated as insufficient for decision-making.
/// Rules can be explicitly whitelisted to allow partial metrics.
/// </summary>
public sealed class PartialMetricPolicy : IPartialMetricPolicy
{
    private readonly ISet<string> _allowedRuleIds;
    private readonly bool _defaultAllowPartial;

    /// <summary>
    /// Initializes a new instance of PartialMetricPolicy with default deny-by-default behavior.
    /// </summary>
    public PartialMetricPolicy()
        : this(allowedRuleIds: new HashSet<string>(), defaultAllowPartial: false)
    {
    }

    /// <summary>
    /// Initializes a new instance of PartialMetricPolicy with custom configuration.
    /// </summary>
    /// <param name="allowedRuleIds">Set of rule IDs that explicitly allow partial metrics.</param>
    /// <param name="defaultAllowPartial">Default behavior for rules not in allowedRuleIds.</param>
    public PartialMetricPolicy(ISet<string> allowedRuleIds, bool defaultAllowPartial = false)
    {
        if (allowedRuleIds == null)
        {
            throw new ArgumentNullException(nameof(allowedRuleIds));
        }

        _allowedRuleIds = new HashSet<string>(allowedRuleIds);
        _defaultAllowPartial = defaultAllowPartial;
    }

    /// <summary>
    /// Determines whether a partial metric is allowed for a specific rule.
    /// Checks if the rule is in the explicitly allowed set.
    /// Falls back to default policy if not explicitly configured.
    /// </summary>
    /// <param name="ruleId">The rule ID to check.</param>
    /// <returns>true if partial metrics are allowed for this rule; false otherwise.</returns>
    public bool IsPartialMetricAllowed(string ruleId)
    {
        if (string.IsNullOrWhiteSpace(ruleId))
        {
            throw new ArgumentException("Rule ID cannot be null or empty.", nameof(ruleId));
        }

        // If rule is explicitly in allowed set, allow partial metrics
        if (_allowedRuleIds.Contains(ruleId))
        {
            return true;
        }

        // Otherwise, return default policy
        return _defaultAllowPartial;
    }

    /// <summary>
    /// Returns the default global policy for partial metrics.
    /// Used when no rule-specific policy applies.
    /// </summary>
    /// <returns>true if partial metrics are allowed by default; false otherwise.</returns>
    public bool IsPartialMetricAllowedByDefault() => _defaultAllowPartial;

    /// <summary>
    /// Creates a policy that allows partial metrics for specific rules.
    /// Useful factory method for common scenario: most rules reject partial metrics, but specific rules allow them.
    /// </summary>
    /// <param name="allowedRuleIds">Rules that should allow partial metrics.</param>
    /// <returns>A new PartialMetricPolicy with default deny, specific rules allow.</returns>
    public static IPartialMetricPolicy AllowForSpecificRules(params string[] allowedRuleIds) =>
        new PartialMetricPolicy(new HashSet<string>(allowedRuleIds), defaultAllowPartial: false);

    /// <summary>
    /// Creates a policy that denies partial metrics for specific rules.
    /// Useful factory method for common scenario: most rules allow partial metrics, but specific rules reject them.
    /// </summary>
    /// <param name="deniedRuleIds">Rules that should deny partial metrics.</param>
    /// <returns>A new PartialMetricPolicy with default allow, specific rules deny.</returns>
    public static IPartialMetricPolicy DenyForSpecificRules(params string[] deniedRuleIds) =>
        new PartialMetricPolicy(
            allowedRuleIds: new HashSet<string>(), 
            defaultAllowPartial: true);  // Allow by default, denial handled via inverted logic in IsPartialMetricAllowed
}
