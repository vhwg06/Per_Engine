namespace PerformanceEngine.Evaluation.Domain.Tests.Domain.Evaluation;

using PerformanceEngine.Evaluation.Domain.Domain.Evaluation;
using PerformanceEngine.Metrics.Domain.Metrics;
using Xunit;

/// <summary>
/// Unit tests for EvaluationEvidence value object.
/// Verifies invariants, immutability, and complete evidence capture.
/// </summary>
public class EvaluationEvidenceTests
{
    [Fact]
    public void Constructor_ValidParameters_CreatesInstance()
    {
        var metrics = new List<MetricReference>
        {
            new("latency", 150.0, "ms", CompletessStatus.COMPLETE)
        };
        var values = new Dictionary<string, double> { { "p95", 150.0 } };

        var evidence = new EvaluationEvidence(
            ruleId: "rule-001",
            ruleName: "Max Latency Check",
            metricsUsed: metrics,
            actualValues: values,
            expectedConstraint: "p95 latency < 200ms",
            constraintSatisfied: true,
            decision: "PASS",
            evaluatedAt: DateTime.UtcNow);

        Assert.Equal("rule-001", evidence.RuleId);
        Assert.Equal("Max Latency Check", evidence.RuleName);
        Assert.Single(evidence.MetricsUsed);
        Assert.True(evidence.ConstraintSatisfied);
        Assert.Equal("PASS", evidence.Decision);
    }

    [Fact]
    public void Constructor_NullRuleId_ThrowsException()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            new EvaluationEvidence(
                ruleId: string.Empty,
                ruleName: "Test",
                metricsUsed: new List<MetricReference>(),
                actualValues: new Dictionary<string, double>(),
                expectedConstraint: "test",
                constraintSatisfied: false,
                decision: "FAIL",
                evaluatedAt: DateTime.UtcNow));
        Assert.Contains("null or empty", ex.Message);
    }

    [Fact]
    public void Constructor_NullMetricsUsed_ThrowsException()
    {
        var ex = Assert.Throws<ArgumentNullException>(() =>
            new EvaluationEvidence(
                ruleId: "rule-001",
                ruleName: "Test",
                metricsUsed: null!,
                actualValues: new Dictionary<string, double>(),
                expectedConstraint: "test",
                constraintSatisfied: false,
                decision: "FAIL",
                evaluatedAt: DateTime.UtcNow));
        Assert.Equal("metricsUsed", ex.ParamName);
    }

    [Fact]
    public void Constructor_EmptyConstraint_ThrowsException()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            new EvaluationEvidence(
                ruleId: "rule-001",
                ruleName: "Test",
                metricsUsed: new List<MetricReference>(),
                actualValues: new Dictionary<string, double>(),
                expectedConstraint: string.Empty,
                constraintSatisfied: false,
                decision: "FAIL",
                evaluatedAt: DateTime.UtcNow));
        Assert.Contains("null or empty", ex.Message);
    }

    [Fact]
    public void Constructor_EmptyDecision_ThrowsException()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            new EvaluationEvidence(
                ruleId: "rule-001",
                ruleName: "Test",
                metricsUsed: new List<MetricReference>(),
                actualValues: new Dictionary<string, double>(),
                expectedConstraint: "test",
                constraintSatisfied: false,
                decision: string.Empty,
                evaluatedAt: DateTime.UtcNow));
        Assert.Contains("null or empty", ex.Message);
    }

    [Fact]
    public void Constructor_DefaultEvaluatedAt_ThrowsException()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            new EvaluationEvidence(
                ruleId: "rule-001",
                ruleName: "Test",
                metricsUsed: new List<MetricReference>(),
                actualValues: new Dictionary<string, double>(),
                expectedConstraint: "test",
                constraintSatisfied: false,
                decision: "FAIL",
                evaluatedAt: default));
        Assert.Contains("must be set", ex.Message);
    }

    [Fact]
    public void Constructor_MultipleMetrics_CapturesAll()
    {
        var metrics = new List<MetricReference>
        {
            new("p50", 100.0, "ms", CompletessStatus.COMPLETE),
            new("p95", 150.0, "ms", CompletessStatus.COMPLETE),
            new("p99", 200.0, "ms", CompletessStatus.PARTIAL)
        };

        var evidence = new EvaluationEvidence(
            ruleId: "rule-001",
            ruleName: "Latency Profile",
            metricsUsed: metrics,
            actualValues: new Dictionary<string, double> { { "p50", 100.0 }, { "p95", 150.0 } },
            expectedConstraint: "All percentiles within targets",
            constraintSatisfied: false,
            decision: "FAIL",
            evaluatedAt: DateTime.UtcNow);

        Assert.Equal(3, evidence.MetricsUsed.Count);
    }

    [Fact]
    public void ToString_ReturnsFormattedString()
    {
        var evidence = new EvaluationEvidence(
            ruleId: "rule-001",
            ruleName: "Max Latency",
            metricsUsed: new List<MetricReference>(),
            actualValues: new Dictionary<string, double>(),
            expectedConstraint: "p95 < 200ms",
            constraintSatisfied: true,
            decision: "PASS",
            evaluatedAt: DateTime.UtcNow);

        var str = evidence.ToString();
        Assert.Contains("rule-001", str);
        Assert.Contains("Max Latency", str);
        Assert.Contains("PASS", str);
    }

    [Fact]
    public void Equality_SameValues_AreEqual()
    {
        var metrics = new List<MetricReference>
        {
            new("latency", 150.0, "ms", CompletessStatus.COMPLETE)
        };
        var values = new Dictionary<string, double> { { "p95", 150.0 } };
        var now = DateTime.UtcNow;

        var ev1 = new EvaluationEvidence(
            "rule-001", "Test", metrics, values, "constraint", true, "PASS", now);
        var ev2 = new EvaluationEvidence(
            "rule-001", "Test", metrics, values, "constraint", true, "PASS", now);

        Assert.Equal(ev1, ev2);
    }

    [Fact]
    public void Equality_DifferentRuleIds_NotEqual()
    {
        var now = DateTime.UtcNow;
        var metrics = new List<MetricReference>();
        var values = new Dictionary<string, double>();

        var ev1 = new EvaluationEvidence("rule-001", "Test", metrics, values, "c", true, "PASS", now);
        var ev2 = new EvaluationEvidence("rule-002", "Test", metrics, values, "c", true, "PASS", now);

        Assert.NotEqual(ev1, ev2);
    }

    [Fact]
    public void Constructor_TrimsWhitespace()
    {
        var evidence = new EvaluationEvidence(
            ruleId: "  rule-001  ",
            ruleName: "  Test  ",
            metricsUsed: new List<MetricReference>(),
            actualValues: new Dictionary<string, double>(),
            expectedConstraint: "  constraint  ",
            constraintSatisfied: false,
            decision: "  FAIL  ",
            evaluatedAt: DateTime.UtcNow);

        Assert.Equal("rule-001", evidence.RuleId);
        Assert.Equal("Test", evidence.RuleName);
        Assert.Equal("constraint", evidence.ExpectedConstraint);
        Assert.Equal("FAIL", evidence.Decision);
    }
}
