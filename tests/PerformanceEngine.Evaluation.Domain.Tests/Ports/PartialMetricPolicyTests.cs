namespace PerformanceEngine.Evaluation.Domain.Tests.Ports;

using PerformanceEngine.Evaluation.Domain.Application.Evaluation;
using PerformanceEngine.Evaluation.Domain.Ports;
using Xunit;

/// <summary>
/// Unit tests for IPartialMetricPolicy interface and default implementation.
/// Verifies allow/deny decision logic, rule-specific policies, and default behaviors.
/// </summary>
public class PartialMetricPolicyTests
{
    [Fact]
    public void DefaultPolicy_IsPartialMetricAllowedByDefault_ReturnsFalse()
    {
        var policy = new PartialMetricPolicy();
        Assert.False(policy.IsPartialMetricAllowedByDefault());
    }

    [Fact]
    public void DefaultPolicy_IsPartialMetricAllowed_ReturnsFalse()
    {
        var policy = new PartialMetricPolicy();
        Assert.False(policy.IsPartialMetricAllowed("rule-1"));
        Assert.False(policy.IsPartialMetricAllowed("rule-2"));
    }

    [Fact]
    public void WithAllowedRules_IsPartialMetricAllowed_ReturnsTrueForAllowedRules()
    {
        var policy = new PartialMetricPolicy(
            allowedRuleIds: new HashSet<string> { "rule-1", "rule-2" },
            defaultAllowPartial: false);

        Assert.True(policy.IsPartialMetricAllowed("rule-1"));
        Assert.True(policy.IsPartialMetricAllowed("rule-2"));
        Assert.False(policy.IsPartialMetricAllowed("rule-3"));
    }

    [Fact]
    public void WithDefaultAllow_IsPartialMetricAllowedByDefault_ReturnsTrue()
    {
        var policy = new PartialMetricPolicy(
            allowedRuleIds: new HashSet<string>(),
            defaultAllowPartial: true);

        Assert.True(policy.IsPartialMetricAllowedByDefault());
        Assert.True(policy.IsPartialMetricAllowed("rule-1"));
        Assert.True(policy.IsPartialMetricAllowed("rule-2"));
    }

    [Fact]
    public void FactoryAllowForSpecificRules_CreatesCorrectPolicy()
    {
        var policy = PartialMetricPolicy.AllowForSpecificRules("rule-1", "rule-2");

        Assert.True(policy.IsPartialMetricAllowed("rule-1"));
        Assert.True(policy.IsPartialMetricAllowed("rule-2"));
        Assert.False(policy.IsPartialMetricAllowed("rule-3"));
        Assert.False(policy.IsPartialMetricAllowedByDefault());
    }

    [Fact]
    public void IsPartialMetricAllowed_NullRuleId_ThrowsArgumentException()
    {
        var policy = new PartialMetricPolicy();
        Assert.Throws<ArgumentException>(() => policy.IsPartialMetricAllowed(null!));
        Assert.Throws<ArgumentException>(() => policy.IsPartialMetricAllowed(""));
        Assert.Throws<ArgumentException>(() => policy.IsPartialMetricAllowed("   "));
    }

    [Fact]
    public void Constructor_NullAllowedRuleIds_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new PartialMetricPolicy(allowedRuleIds: null!, defaultAllowPartial: false));
    }

    [Fact]
    public void MultipleRules_ComplexPolicy_AllowsSomeRules()
    {
        var policy = new PartialMetricPolicy(
            allowedRuleIds: new HashSet<string> { "latency-threshold", "success-rate" },
            defaultAllowPartial: false);

        // Allowed rules
        Assert.True(policy.IsPartialMetricAllowed("latency-threshold"));
        Assert.True(policy.IsPartialMetricAllowed("success-rate"));

        // Denied rules
        Assert.False(policy.IsPartialMetricAllowed("error-rate"));
        Assert.False(policy.IsPartialMetricAllowed("availability"));
        Assert.False(policy.IsPartialMetricAllowedByDefault());
    }

    [Fact]
    public void EmptyAllowedRulesSet_WithDefaultDeny_DeniesAllRules()
    {
        var policy = new PartialMetricPolicy(
            allowedRuleIds: new HashSet<string>(),
            defaultAllowPartial: false);

        Assert.False(policy.IsPartialMetricAllowed("any-rule"));
        Assert.False(policy.IsPartialMetricAllowedByDefault());
    }
}
