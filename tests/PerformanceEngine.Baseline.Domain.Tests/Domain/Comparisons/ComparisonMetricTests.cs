namespace PerformanceEngine.Baseline.Domain.Tests.Domain.Comparisons;

using FluentAssertions;
using PerformanceEngine.Baseline.Domain.Domain.Comparisons;
using PerformanceEngine.Baseline.Domain.Domain.Confidence;
using PerformanceEngine.Baseline.Domain.Domain.Tolerances;
using Xunit;

public class ComparisonMetricTests
{
    [Fact]
    public void Constructor_WithValidInputs_CreatesComparisonMetric()
    {
        // Arrange
        var metricName = "ResponseTime";
        var baselineValue = 100.0;
        var currentValue = 110.0;
        var tolerance = new Tolerance(metricName, ToleranceType.Absolute, 10.0);
        var outcome = ComparisonOutcome.NoSignificantChange;
        var confidence = new ConfidenceLevel(0.5);

        // Act
        var metric = new ComparisonMetric(
            metricName,
            baselineValue,
            currentValue,
            tolerance,
            outcome,
            confidence
        );

        // Assert
        metric.MetricName.Should().Be(metricName);
        metric.BaselineValue.Should().Be(baselineValue);
        metric.CurrentValue.Should().Be(currentValue);
        metric.Tolerance.Should().Be(tolerance);
        metric.Outcome.Should().Be(outcome);
        metric.Confidence.Should().Be(confidence);
    }

    [Fact]
    public void AbsoluteChange_IsCalculatedCorrectly()
    {
        // Arrange
        var metricName = "ResponseTime";
        var baselineValue = 100.0;
        var currentValue = 125.0;
        var tolerance = new Tolerance(metricName, ToleranceType.Absolute, 20.0);
        var outcome = ComparisonOutcome.Regression;
        var confidence = new ConfidenceLevel(0.8);

        // Act
        var metric = new ComparisonMetric(
            metricName,
            baselineValue,
            currentValue,
            tolerance,
            outcome,
            confidence
        );

        // Assert
        metric.AbsoluteChange.Should().Be(25.0);
    }

    [Fact]
    public void RelativeChange_IsCalculatedCorrectly()
    {
        // Arrange
        var metricName = "Throughput";
        var baselineValue = 1000.0;
        var currentValue = 1200.0;
        var tolerance = new Tolerance(metricName, ToleranceType.Relative, 10.0);
        var outcome = ComparisonOutcome.Improvement;
        var confidence = new ConfidenceLevel(0.7);

        // Act
        var metric = new ComparisonMetric(
            metricName,
            baselineValue,
            currentValue,
            tolerance,
            outcome,
            confidence
        );

        // Assert
        metric.RelativeChange.Should().BeApproximately(20.0, 0.01); // (1200-1000)/1000 * 100 = 20%
    }

    [Fact]
    public void RelativeChange_WithBaselineZero_HandlesGracefully()
    {
        // Arrange
        var metricName = "NewMetric";
        var baselineValue = 0.0;
        var currentValue = 100.0;
        var tolerance = new Tolerance(metricName, ToleranceType.Absolute, 10.0);
        var outcome = ComparisonOutcome.Inconclusive;
        var confidence = new ConfidenceLevel(0.0);

        // Act
        var metric = new ComparisonMetric(
            metricName,
            baselineValue,
            currentValue,
            tolerance,
            outcome,
            confidence
        );

        // Assert
        metric.RelativeChange.Should().BePositiveInfinity(); // Division by zero yields infinity
    }

    [Fact]
    public void NegativeAbsoluteChange_IsCalculatedCorrectly()
    {
        // Arrange
        var metricName = "ErrorRate";
        var baselineValue = 50.0;
        var currentValue = 30.0;
        var tolerance = new Tolerance(metricName, ToleranceType.Absolute, 5.0);
        var outcome = ComparisonOutcome.Improvement;
        var confidence = new ConfidenceLevel(0.9);

        // Act
        var metric = new ComparisonMetric(
            metricName,
            baselineValue,
            currentValue,
            tolerance,
            outcome,
            confidence
        );

        // Assert
        metric.AbsoluteChange.Should().Be(-20.0);
    }

    [Fact]
    public void NegativeRelativeChange_IsCalculatedCorrectly()
    {
        // Arrange
        var metricName = "ErrorRate";
        var baselineValue = 100.0;
        var currentValue = 80.0;
        var tolerance = new Tolerance(metricName, ToleranceType.Relative, 10.0);
        var outcome = ComparisonOutcome.Improvement;
        var confidence = new ConfidenceLevel(0.8);

        // Act
        var metric = new ComparisonMetric(
            metricName,
            baselineValue,
            currentValue,
            tolerance,
            outcome,
            confidence
        );

        // Assert
        metric.RelativeChange.Should().BeApproximately(-20.0, 0.01); // (80-100)/100 * 100 = -20%
    }

    [Fact]
    public void ComparisonMetric_WithDifferentOutcomes_StoresCorrectly()
    {
        // Arrange
        var tolerance = new Tolerance("Metric", ToleranceType.Absolute, 10.0);
        var outcomes = new[] { ComparisonOutcome.Improvement, ComparisonOutcome.Regression, ComparisonOutcome.NoSignificantChange, ComparisonOutcome.Inconclusive };

        // Act & Assert
        foreach (var outcome in outcomes)
        {
            var metric = new ComparisonMetric(
                "Metric",
                100.0,
                110.0,
                tolerance,
                outcome,
                new ConfidenceLevel(0.5)
            );
            metric.Outcome.Should().Be(outcome);
        }
    }

    [Fact]
    public void ComparisonMetric_WithDifferentConfidenceLevels_StoresCorrectly()
    {
        // Arrange
        var tolerance = new Tolerance("Metric", ToleranceType.Absolute, 10.0);
        var confidenceLevels = new[] { 0.0, 0.25, 0.5, 0.75, 1.0 };

        // Act & Assert
        foreach (var confidenceValue in confidenceLevels)
        {
            var metric = new ComparisonMetric(
                "Metric",
                100.0,
                110.0,
                tolerance,
                ComparisonOutcome.NoSignificantChange,
                new ConfidenceLevel(confidenceValue)
            );
            metric.Confidence.Value.Should().Be(confidenceValue);
        }
    }

    [Fact]
    public void Equality_SameProperties_AreEqual()
    {
        // Arrange
        var tolerance = new Tolerance("Metric", ToleranceType.Absolute, 10.0);
        var outcome = ComparisonOutcome.Regression;
        var confidence = new ConfidenceLevel(0.7);

        var metric1 = new ComparisonMetric("Metric", 100.0, 120.0, tolerance, outcome, confidence);
        var metric2 = new ComparisonMetric("Metric", 100.0, 120.0, tolerance, outcome, confidence);

        // Act & Assert
        metric1.Should().Be(metric2);
    }

    [Fact]
    public void Equality_DifferentOutcomes_AreNotEqual()
    {
        // Arrange
        var tolerance = new Tolerance("Metric", ToleranceType.Absolute, 10.0);
        var confidence = new ConfidenceLevel(0.7);

        var metric1 = new ComparisonMetric("Metric", 100.0, 120.0, tolerance, ComparisonOutcome.Regression, confidence);
        var metric2 = new ComparisonMetric("Metric", 100.0, 120.0, tolerance, ComparisonOutcome.Improvement, confidence);

        // Act & Assert
        metric1.Should().NotBe(metric2);
    }

    [Fact]
    public void ToString_ReturnsFormattedString()
    {
        // Arrange
        var tolerance = new Tolerance("ResponseTime", ToleranceType.Absolute, 10.0);
        var metric = new ComparisonMetric(
            "ResponseTime",
            100.0,
            120.0,
            tolerance,
            ComparisonOutcome.Regression,
            new ConfidenceLevel(0.8)
        );

        // Act
        var result = metric.ToString();

        // Assert
        result.Should().Contain("ResponseTime");
        result.Should().Contain("100");
        result.Should().Contain("120");
    }
}
