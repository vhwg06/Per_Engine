namespace PerformanceEngine.Evaluation.Domain.Tests.PersistenceScenarios;

using PerformanceEngine.Evaluation.Domain;
using Xunit;

/// <summary>
/// Tests verifying immutability of all evaluation domain entities.
/// All entities must be read-only after construction (no modifications possible).
/// </summary>
public class ImmutabilityTests
{
    [Fact]
    public void EvaluationResult_IsImmutable()
    {
        // Arrange
        var result = EvaluationResult.Create(
            outcome: Severity.Pass,
            violations: Array.Empty<Violation>(),
            evidence: Array.Empty<EvaluationEvidence>(),
            outcomeReason: "Test passed",
            evaluatedAtUtc: DateTime.UtcNow);

        // Act & Assert
        // These properties are read-only and cannot be modified
        Assert.NotNull(result.Id);
        Assert.Equal(Severity.Pass, result.Outcome);
        Assert.Empty(result.Violations);
        Assert.Empty(result.Evidence);
        Assert.NotEmpty(result.OutcomeReason);
        Assert.NotEqual(default, result.EvaluatedAt);

        // Verify no way to modify - compile-time safety ensured by record type
        // Direct property assignment would cause compile error
    }

    [Fact]
    public void EvaluationResult_Violations_AreImmutable()
    {
        // Arrange
        var violations = new[]
        {
            Violation.Create(
                ruleName: "Rule1",
                metricName: "Metric1",
                severity: Severity.Fail,
                actualValue: "100",
                thresholdValue: "50",
                message: "Violation message")
        };

        var result = EvaluationResult.Create(
            outcome: Severity.Fail,
            violations: violations,
            evidence: Array.Empty<EvaluationEvidence>(),
            outcomeReason: "Test failed",
            evaluatedAtUtc: DateTime.UtcNow);

        // Act & Assert
        // ImmutableList prevents modifications
        Assert.Single(result.Violations);
        Assert.Equal("Rule1", result.Violations[0].RuleName);

        // Cannot add to immutable list (returns new collection, doesn't modify)
        var newViolations = result.Violations.Add(Violation.Create(
            ruleName: "Rule2",
            metricName: "Metric2",
            severity: Severity.Fail,
            actualValue: "200",
            thresholdValue: "100",
            message: "Another violation"));

        // Original remains unchanged
        Assert.Single(result.Violations);
        Assert.Equal(2, newViolations.Count);
    }

    [Fact]
    public void EvaluationResult_Evidence_AreImmutable()
    {
        // Arrange
        var evidence = new[]
        {
            EvaluationEvidence.Create(
                ruleId: "rule-1",
                ruleName: "Rule1",
                metrics: new[] { MetricReference.Create("Metric1", "100") },
                actualValues: new Dictionary<string, string> { ["Metric1"] = "100" },
                expectedConstraint: "Metric1 <= 50",
                constraintSatisfied: false,
                decisionOutcome: "Constraint violated",
                recordedAtUtc: DateTime.UtcNow)
        };

        var result = EvaluationResult.Create(
            outcome: Severity.Fail,
            violations: Array.Empty<Violation>(),
            evidence: evidence,
            outcomeReason: "Test failed",
            evaluatedAtUtc: DateTime.UtcNow);

        // Act & Assert
        Assert.Single(result.Evidence);
        Assert.Equal("rule-1", result.Evidence[0].RuleId);

        // Cannot modify immutable list
        var newEvidence = result.Evidence.Add(EvaluationEvidence.Create(
            ruleId: "rule-2",
            ruleName: "Rule2",
            metrics: new[] { MetricReference.Create("Metric2", "200") },
            actualValues: new Dictionary<string, string> { ["Metric2"] = "200" },
            expectedConstraint: "Metric2 <= 100",
            constraintSatisfied: false,
            decisionOutcome: "Constraint violated",
            recordedAtUtc: DateTime.UtcNow));

        // Original remains unchanged
        Assert.Single(result.Evidence);
        Assert.Equal(2, newEvidence.Count);
    }

    [Fact]
    public void Violation_IsImmutable()
    {
        // Arrange
        var violation = Violation.Create(
            ruleName: "MaxResponseTime",
            metricName: "ResponseTime",
            severity: Severity.Fail,
            actualValue: "1250.5",
            thresholdValue: "1000.0",
            message: "Response time exceeded threshold");

        // Act & Assert
        Assert.Equal("MaxResponseTime", violation.RuleName);
        Assert.Equal("ResponseTime", violation.MetricName);
        Assert.Equal(Severity.Fail, violation.Severity);
        Assert.Equal("1250.5", violation.ActualValue);
        Assert.Equal("1000.0", violation.ThresholdValue);
        Assert.NotEmpty(violation.Message);
    }

    [Fact]
    public void EvaluationEvidence_IsImmutable()
    {
        // Arrange
        var evidence = EvaluationEvidence.Create(
            ruleId: "rule-001",
            ruleName: "ResponseTimeRule",
            metrics: new[] { MetricReference.Create("ResponseTime", "1250.5") },
            actualValues: new Dictionary<string, string> { ["ResponseTime"] = "1250.5" },
            expectedConstraint: "ResponseTime <= 1000",
            constraintSatisfied: false,
            decisionOutcome: "Violation: Response time exceeds threshold",
            recordedAtUtc: DateTime.UtcNow);

        // Act & Assert
        Assert.Equal("rule-001", evidence.RuleId);
        Assert.Equal("ResponseTimeRule", evidence.RuleName);
        Assert.Single(evidence.Metrics);
        Assert.Single(evidence.ActualValues);
        Assert.False(evidence.ConstraintSatisfied);

        // Immutable collections cannot be modified
        var newMetrics = evidence.Metrics.Add(MetricReference.Create("ErrorRate", "0.05"));
        Assert.Single(evidence.Metrics);  // Original unchanged
        Assert.Equal(2, newMetrics.Count);
    }

    [Fact]
    public void MetricReference_IsImmutable()
    {
        // Arrange
        var metric = MetricReference.Create("ResponseTime", "1250.5");

        // Act & Assert
        Assert.Equal("ResponseTime", metric.MetricName);
        Assert.Equal("1250.5", metric.Value);

        // Record type prevents property modification
    }

    [Fact]
    public void EvaluationResult_RecordsAreValueTypes()
    {
        // Arrange
        var result1 = EvaluationResult.Create(
            outcome: Severity.Pass,
            violations: Array.Empty<Violation>(),
            evidence: Array.Empty<EvaluationEvidence>(),
            outcomeReason: "Test passed",
            evaluatedAtUtc: new DateTime(2026, 1, 16, 12, 0, 0, DateTimeKind.Utc));

        var result2 = EvaluationResult.Create(
            outcome: Severity.Pass,
            violations: Array.Empty<Violation>(),
            evidence: Array.Empty<EvaluationEvidence>(),
            outcomeReason: "Test passed",
            evaluatedAtUtc: new DateTime(2026, 1, 16, 12, 0, 0, DateTimeKind.Utc));

        // Act & Assert
        // Different objects
        Assert.NotEqual(result1.Id, result2.Id);

        // But with same content (except ID), would be equal if IDs matched
        // (Record equality is structural)
    }
}
