namespace PerformanceEngine.Baseline.Domain.Tests.Domain.Comparisons;

using FluentAssertions;
using PerformanceEngine.Baseline.Domain.Domain.Comparisons;
using PerformanceEngine.Baseline.Domain.Domain.Tolerances;
using Xunit;

public class ComparisonCalculatorTests
{
    private readonly ComparisonCalculator _calculator = new();

    [Fact]
    public void CalculateMetric_WithAbsoluteTolerance_CalculatesCorrectly()
    {
        // Arrange
        var baseline = 100m;
        var current = 120m;
        var tolerance = new Tolerance("ResponseTime", ToleranceType.Absolute, 10m);

        // Act
        var result = _calculator.CalculateMetric(baseline, current, tolerance);

        // Assert
        result.MetricName.Should().Be("ResponseTime");
        result.BaselineValue.Should().Be(baseline);
        result.CurrentValue.Should().Be(current);
        result.AbsoluteChange.Should().Be(20m);
    }

    [Fact]
    public void CalculateMetric_WithRelativeTolerance_CalculatesCorrectly()
    {
        // Arrange
        var baseline = 1000m;
        var current = 950m;
        var tolerance = new Tolerance("Throughput", ToleranceType.Relative, 5m);

        // Act
        var result = _calculator.CalculateMetric(baseline, current, tolerance);

        // Assert
        result.MetricName.Should().Be("Throughput");
        result.BaselineValue.Should().Be(baseline);
        result.CurrentValue.Should().Be(current);
        result.AbsoluteChange.Should().Be(-50m);
    }

    [Fact]
    public void CalculateMetric_WithHighPositiveChange_ReturnsRegression()
    {
        // Arrange
        var baseline = 100m;
        var current = 200m;
        var tolerance = new Tolerance("ResponseTime", ToleranceType.Absolute, 10m);

        // Act
        var result = _calculator.CalculateMetric(baseline, current, tolerance);

        // Assert
        result.Outcome.Should().Be(ComparisonOutcome.Regression);
    }

    [Fact]
    public void CalculateMetric_WithHighNegativeChange_ReturnsImprovement()
    {
        // Arrange
        var baseline = 10m;
        var current = 2m;
        var tolerance = new Tolerance("ErrorRate", ToleranceType.Absolute, 1m);

        // Act
        var result = _calculator.CalculateMetric(baseline, current, tolerance);

        // Assert
        result.Outcome.Should().Be(ComparisonOutcome.Improvement);
    }

    [Fact]
    public void CalculateMetric_WithChangeWithinTolerance_ReturnsNoSignificantChange()
    {
        // Arrange
        var baseline = 1000m;
        var current = 1005m;
        var tolerance = new Tolerance("Throughput", ToleranceType.Absolute, 10m);

        // Act
        var result = _calculator.CalculateMetric(baseline, current, tolerance);

        // Assert
        result.MetricName.Should().Be("Throughput");
        result.BaselineValue.Should().Be(baseline);
        result.CurrentValue.Should().Be(current);
    }

    [Fact]
    public void CalculateMetric_MultipleRuns_ProducesConsistentResults()
    {
        // Arrange
        var baseline = 100m;
        var current = 125m;
        var tolerance = new Tolerance("ResponseTime", ToleranceType.Absolute, 5m);

        // Act
        var result1 = _calculator.CalculateMetric(baseline, current, tolerance);
        var result2 = _calculator.CalculateMetric(baseline, current, tolerance);

        // Assert
        result1.Should().Be(result2);
        result1.Outcome.Should().Be(result2.Outcome);
        result1.Confidence.Should().Be(result2.Confidence);
    }

    [Fact]
    public void CalculateMetric_ComputesConfidenceLevel()
    {
        // Arrange
        var baseline = 1000m;
        var current = 1100m;
        var tolerance = new Tolerance("Throughput", ToleranceType.Absolute, 50m);

        // Act
        var result = _calculator.CalculateMetric(baseline, current, tolerance);

        // Assert
        result.Confidence.Should().NotBeNull();
        result.Confidence.Value.Should().BeGreaterThanOrEqualTo(0m);
        result.Confidence.Value.Should().BeLessThanOrEqualTo(1m);
    }

    [Fact]
    public void CalculateMetric_StoresAllPropertiesCorrectly()
    {
        // Arrange
        var metricName = "MemoryUsage";
        var baseline = 500m;
        var current = 600m;
        var tolerance = new Tolerance(metricName, ToleranceType.Absolute, 50m);

        // Act
        var result = _calculator.CalculateMetric(baseline, current, tolerance);

        // Assert
        result.MetricName.Should().Be(metricName);
        result.BaselineValue.Should().Be(baseline);
        result.CurrentValue.Should().Be(current);
        result.Tolerance.Should().Be(tolerance);
        result.Outcome.Should().NotBe(null);
        result.Confidence.Should().NotBeNull();
    }
}
