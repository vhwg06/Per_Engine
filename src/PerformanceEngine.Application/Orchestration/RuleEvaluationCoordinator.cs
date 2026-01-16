namespace PerformanceEngine.Application.Orchestration;

using PerformanceEngine.Application.Models;
using PerformanceEngine.Application.Ports;
using PerformanceEngine.Metrics.Domain.Metrics;
using PerformanceEngine.Evaluation.Domain.Domain.Evaluation;
using System.Linq;

/// <summary>
/// Coordinates rule evaluation with deterministic ordering and violation collection.
/// Ensures rules are evaluated in consistent order and all violations are captured.
/// </summary>
public sealed class RuleEvaluationCoordinator
{
    /// <summary>
    /// Evaluates all rules against provided metrics in deterministic order.
    /// Collects all violations and sorts them by rule ID.
    /// </summary>
    /// <param name="rules">Collection of rules to evaluate.</param>
    /// <param name="samples">Available metric samples.</param>
    /// <param name="rulesProvider">Provider for rule evaluation logic.</param>
    /// <returns>Sorted list of violations found during evaluation.</returns>
    public IReadOnlyList<Models.Violation> EvaluateRules(
        IReadOnlyCollection<EvaluationRuleDefinition> rules,
        IReadOnlyCollection<Sample> samples,
        IEvaluationRulesProvider rulesProvider)
    {
        if (rules == null || rules.Count == 0)
        {
            return Array.Empty<Models.Violation>();
        }

        // Step 1: Sort rules deterministically by rule ID (ASCII order)
        var sortedRules = rules.OrderBy(r => r.RuleId, StringComparer.Ordinal).ToList();

        // Step 2: Evaluate each rule and collect violations
        var violations = new List<Models.Violation>();

        foreach (var rule in sortedRules)
        {
            try
            {
                // Check if required metrics are available
                var requiredMetrics = rule.RequiredMetrics ?? Array.Empty<string>();
                var availableMetrics = samples.Select(s => GetMetricNameFromSample(s)).ToHashSet();

                var missingMetrics = requiredMetrics.Except(availableMetrics).ToList();

                if (missingMetrics.Any())
                {
                    // Skip this rule - missing required metrics
                    continue;
                }

                // Evaluate rule through domain
                var evaluationResult = rulesProvider.EvaluateRule(rule, samples);

                // Convert domain violations to application violations
                if (evaluationResult.Violations != null && evaluationResult.Violations.Any())
                {
                    foreach (var domainViolation in evaluationResult.Violations)
                    {
                        var appViolation = ConvertToApplicationViolation(domainViolation, rule);
                        violations.Add(appViolation);
                    }
                }
            }
            catch (Exception ex)
            {
                // Capture evaluation errors as critical violations
                var errorViolation = new Models.Violation
                {
                    RuleId = rule.RuleId,
                    RuleName = rule.RuleName,
                    ExpectedThreshold = 0,
                    ActualValue = 0,
                    AffectedMetricName = "ERROR",
                    Severity = Models.SeverityLevel.Critical,
                    Message = $"Rule evaluation error: {ex.Message}"
                };
                violations.Add(errorViolation);
            }
        }

        // Step 3: Sort violations deterministically by rule ID
        var sortedViolations = violations
            .OrderBy(v => v.RuleId, StringComparer.Ordinal)
            .ThenBy(v => v.AffectedMetricName, StringComparer.Ordinal)
            .ToList();

        return sortedViolations;
    }

    private string GetMetricNameFromSample(Sample sample)
    {
        // Extract metric name from sample - this is a simplified implementation
        // In real scenario, this would map to actual metric types
        return "latency_p99"; // Placeholder
    }

    private Models.Violation ConvertToApplicationViolation(
        PerformanceEngine.Evaluation.Domain.Domain.Evaluation.Violation domainViolation,
        EvaluationRuleDefinition rule)
    {
        return new Models.Violation
        {
            RuleId = domainViolation.RuleId,
            RuleName = rule.RuleName,
            ExpectedThreshold = domainViolation.Threshold,
            ActualValue = domainViolation.ActualValue,
            AffectedMetricName = domainViolation.MetricName,
            Severity = ConvertSeverity(rule.Severity),
            Message = domainViolation.Message
        };
    }

    private Models.SeverityLevel ConvertSeverity(PerformanceEngine.Evaluation.Domain.Domain.Evaluation.Severity domainSeverity)
    {
        return domainSeverity == PerformanceEngine.Evaluation.Domain.Domain.Evaluation.Severity.FAIL
            ? Models.SeverityLevel.Critical
            : Models.SeverityLevel.NonCritical;
    }
}
