namespace PerformanceEngine.Evaluation.Domain.Tests.PersistenceScenarios;

using PerformanceEngine.Evaluation.Domain;
using Xunit;

/// <summary>
/// Tests verifying validation rules enforced at entity construction time.
/// Domain invariants must be preserved to ensure data consistency.
/// </summary>
public class ValidationTests
{
    [Fact]
    public void EvaluationResult_Create_WithPassOutcomeAndViolations_Throws()
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
                message: "Violation")
        };

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() =>
            EvaluationResult.Create(
                outcome: Severity.Pass,  // Contradicts violations
                violations: violations,
                evidence: Array.Empty<EvaluationEvidence>(),
                outcomeReason: "Test passed",
                evaluatedAtUtc: DateTime.UtcNow));

        Assert.Contains("Pass", ex.Message);
        Assert.Contains("violations", ex.Message);
    }

    [Fact]
    public void EvaluationResult_Create_WithEmptyOutcomeReason_Throws()
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() =>
            EvaluationResult.Create(
                outcome: Severity.Pass,
                violations: Array.Empty<Violation>(),
                evidence: Array.Empty<EvaluationEvidence>(),
                outcomeReason: "",  // Empty
                evaluatedAtUtc: DateTime.UtcNow));

        Assert.Contains("Outcome reason", ex.Message);
    }

    [Fact]
    public void EvaluationResult_Create_WithLocalTimeNotUtc_Throws()
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() =>
            EvaluationResult.Create(
                outcome: Severity.Pass,
                violations: Array.Empty<Violation>(),
                evidence: Array.Empty<EvaluationEvidence>(),
                outcomeReason: "Test passed",
                evaluatedAtUtc: DateTime.Now));  // Local time, not UTC

        Assert.Contains("UTC", ex.Message);
    }

    [Fact]
    public void Violation_Create_WithEmptyRuleName_Throws()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            Violation.Create(
                ruleName: "",  // Empty
                metricName: "Metric1",
                severity: Severity.Fail,
                actualValue: "100",
                thresholdValue: "50",
                message: "Violation"));

        Assert.Contains("Rule name", ex.Message);
    }

    [Fact]
    public void Violation_Create_WithEmptyMetricName_Throws()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            Violation.Create(
                ruleName: "Rule1",
                metricName: "",  // Empty
                severity: Severity.Fail,
                actualValue: "100",
                thresholdValue: "50",
                message: "Violation"));

        Assert.Contains("Metric name", ex.Message);
    }

    [Fact]
    public void Violation_Create_WithEmptyActualValue_Throws()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            Violation.Create(
                ruleName: "Rule1",
                metricName: "Metric1",
                severity: Severity.Fail,
                actualValue: "",  // Empty
                thresholdValue: "50",
                message: "Violation"));

        Assert.Contains("Actual value", ex.Message);
    }

    [Fact]
    public void Violation_Create_WithEmptyThresholdValue_Throws()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            Violation.Create(
                ruleName: "Rule1",
                metricName: "Metric1",
                severity: Severity.Fail,
                actualValue: "100",
                thresholdValue: "",  // Empty
                message: "Violation"));

        Assert.Contains("Threshold value", ex.Message);
    }

    [Fact]
    public void Violation_Create_WithEmptyMessage_Throws()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            Violation.Create(
                ruleName: "Rule1",
                metricName: "Metric1",
                severity: Severity.Fail,
                actualValue: "100",
                thresholdValue: "50",
                message: ""));  // Empty

        Assert.Contains("Message", ex.Message);
    }

    [Fact]
    public void EvaluationEvidence_Create_WithEmptyRuleId_Throws()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            EvaluationEvidence.Create(
                ruleId: "",  // Empty
                ruleName: "Rule1",
                metrics: new[] { MetricReference.Create("Metric1", "100") },
                actualValues: new Dictionary<string, string> { ["Metric1"] = "100" },
                expectedConstraint: "Metric1 <= 50",
                constraintSatisfied: false,
                decisionOutcome: "Constraint violated",
                recordedAtUtc: DateTime.UtcNow));

        Assert.Contains("Rule ID", ex.Message);
    }

    [Fact]
    public void EvaluationEvidence_Create_WithEmptyRuleName_Throws()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            EvaluationEvidence.Create(
                ruleId: "rule-1",
                ruleName: "",  // Empty
                metrics: new[] { MetricReference.Create("Metric1", "100") },
                actualValues: new Dictionary<string, string> { ["Metric1"] = "100" },
                expectedConstraint: "Metric1 <= 50",
                constraintSatisfied: false,
                decisionOutcome: "Constraint violated",
                recordedAtUtc: DateTime.UtcNow));

        Assert.Contains("Rule name", ex.Message);
    }

    [Fact]
    public void EvaluationEvidence_Create_WithEmptyConstraint_Throws()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            EvaluationEvidence.Create(
                ruleId: "rule-1",
                ruleName: "Rule1",
                metrics: new[] { MetricReference.Create("Metric1", "100") },
                actualValues: new Dictionary<string, string> { ["Metric1"] = "100" },
                expectedConstraint: "",  // Empty
                constraintSatisfied: false,
                decisionOutcome: "Constraint violated",
                recordedAtUtc: DateTime.UtcNow));

        Assert.Contains("Expected constraint", ex.Message);
    }

    [Fact]
    public void EvaluationEvidence_Create_WithEmptyDecisionOutcome_Throws()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            EvaluationEvidence.Create(
                ruleId: "rule-1",
                ruleName: "Rule1",
                metrics: new[] { MetricReference.Create("Metric1", "100") },
                actualValues: new Dictionary<string, string> { ["Metric1"] = "100" },
                expectedConstraint: "Metric1 <= 50",
                constraintSatisfied: false,
                decisionOutcome: "",  // Empty
                recordedAtUtc: DateTime.UtcNow));

        Assert.Contains("Decision outcome", ex.Message);
    }

    [Fact]
    public void EvaluationEvidence_Create_WithLocalTimeNotUtc_Throws()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            EvaluationEvidence.Create(
                ruleId: "rule-1",
                ruleName: "Rule1",
                metrics: new[] { MetricReference.Create("Metric1", "100") },
                actualValues: new Dictionary<string, string> { ["Metric1"] = "100" },
                expectedConstraint: "Metric1 <= 50",
                constraintSatisfied: false,
                decisionOutcome: "Constraint violated",
                recordedAtUtc: DateTime.Now));  // Local time, not UTC

        Assert.Contains("UTC", ex.Message);
    }

    [Fact]
    public void MetricReference_Create_WithEmptyName_Throws()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            MetricReference.Create("", "100"));

        Assert.Contains("Metric name", ex.Message);
    }

    [Fact]
    public void MetricReference_Create_WithEmptyValue_Throws()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            MetricReference.Create("Metric1", ""));

        Assert.Contains("Metric value", ex.Message);
    }

    [Fact]
    public void EvaluationResult_Create_ValidPassResult_Succeeds()
    {
        // Act
        var result = EvaluationResult.Create(
            outcome: Severity.Pass,
            violations: Array.Empty<Violation>(),
            evidence: Array.Empty<EvaluationEvidence>(),
            outcomeReason: "All rules passed",
            evaluatedAtUtc: DateTime.UtcNow);

        // Assert
        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal(Severity.Pass, result.Outcome);
        Assert.Empty(result.Violations);
        Assert.Empty(result.Evidence);
    }

    [Fact]
    public void EvaluationResult_Create_ValidFailResult_Succeeds()
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
                message: "Violation")
        };

        // Act
        var result = EvaluationResult.Create(
            outcome: Severity.Fail,
            violations: violations,
            evidence: Array.Empty<EvaluationEvidence>(),
            outcomeReason: "Rule violated",
            evaluatedAtUtc: DateTime.UtcNow);

        // Assert
        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal(Severity.Fail, result.Outcome);
        Assert.Single(result.Violations);
    }
}
