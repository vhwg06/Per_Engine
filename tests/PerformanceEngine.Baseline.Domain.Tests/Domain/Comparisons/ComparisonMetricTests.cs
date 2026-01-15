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
        var baselineValue = 100m;
        var currentValue = 110m;
        var tolerance = new Tolerance(metricName, ToleranceType.Absolute, 10m);
        var outcome = ComparisonOutcome.NoSignificantChange;
        var confidence = new ConfidenceLevel(0.5m);

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
        var baselineValue = 100m;
        var currentValue = 125m;
        var tolerance = new Tolerance(metricName, ToleranceType.Absolute, 20m);
        var outcome = ComparisonOutcome.Regression;
        var confidence = new ConfidenceLevel(0.8m);

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
        metric.AbsoluteChange.Should().Be(25m);
    }

    [Fact]
    public void RelativeChange_IsCalculatedCorrectly()
    {
        // Arrange
        var metricName = "Throughput";
        var baselineValue = 1000m;
        var currentValue = 1200m;
        var tolerance = new Tolerance(metricName, ToleranceType.Relative, 10m);
        var outcome = ComparisonOutcome.Improvement;
        var confidence = new ConfidenceLevel(0.7m);

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
        metric.RelativeChange.Should().Be(20m);
    }

    [Fact]
    public void NegativeAbsoluteChange_IsCalculatedCorrectly()
    {
        // Arrange
        var metricName = "ErrorRate";
        var baselineValue = 50m;
        var currentValue = 30m;
        var tolerance = new Tolerance(metricName, ToleranceType.Absolute, 5m);
        var outcome = ComparisonOutcome.Improvement;
        var confidence = new ConfidenceLevel(0.9m);

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
        metric.AbsoluteChange.Should().Be(-20m);
    }

    [Fact]
    public void Equality_SameProperties_AreEqual()
    {
        // Arrange
        var tolerance = new Tolerance("Metric", ToleranceType.Absolute, 10m);
        var outcome = ComparisonOutcome.Regression;
        var confidence = new ConfidenceLevel(0.7m);

        var metric1 = new ComparisonMetric("Metric", 100m, 120m, tolerance, outcome, confidence);
        var metric2 = new ComparisonMetric("Metric", 100m, 120m, tolerance, outcome, confidence);

        // Act & Assert
        metric1.Should().Be(metric2);
    }

    [Fact]
    public void ToString_ReturnsFormattedString()
    {
        // Arrange
        var tolerance = new Tolerance("ResponseTime", ToleranceType.Absolute, 10m);
        var metric = new ComparisonMetric(
            "ResponseTime",
            100m,
            120m,
            tolerance,
            ComparisonOutcome.Regression,
            new ConfidenceLevel(0.8m)
        );

        // Act
        var result = metric.ToString();

        // Assert
        result.Should().Contain("ResponseTime");
        result.Should().Contain("100");
        result.Should().Contain("120");
    }
}
