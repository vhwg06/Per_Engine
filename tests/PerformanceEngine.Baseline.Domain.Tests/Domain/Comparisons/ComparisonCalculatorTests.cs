namespace PerformanceEngine.Baseline.Domain.Tests.Domain.Comparisons;

using FluentAssertions;
using Moq;
using PerformanceEngine.Baseline.Domain.Domain.Comparisons;
using PerformanceEngine.Baseline.Domain.Domain.Confidence;
using PerformanceEngine.Baseline.Domain.Domain.Tolerances;
using PerformanceEngine.Metrics.Domain.Ports;
using Xunit;

public class ComparisonCalculatorTests
{
    private readonly ComparisonCalculator _calculator = new();

    [Fact]
    public void CalculateMetric_WithRelativeTolerance_CalculatesCorrectly()
    {
        // Arrange
        var metric = CreateMockMetric("ResponseTime", 100.0);
        var currentValue = 120.0;
        var tolerance = new Tolerance("ResponseTime", ToleranceType.Relative, 10.0);

        // Act
        var result = _calculator.CalculateMetric(metric.Object, currentValue, tolerance);

        // Assert
        result.MetricName.Should().Be("ResponseTime");
        result.BaselineValue.Should().Be(100.0);
        result.CurrentValue.Should().Be(currentValue);
        result.AbsoluteChange.Should().Be(20.0);
        result.RelativeChange.Should().BeApproximately(20.0, 0.01);
    }

    [Fact]
    public void CalculateMetric_WithAbsoluteTolerance_CalculatesCorrectly()
    {
        // Arrange
        var metric = CreateMockMetric("Throughput", 1000.0);
        var currentValue = 950.0;
        var tolerance = new Tolerance("Throughput", ToleranceType.Absolute, 100.0);

        // Act
        var result = _calculator.CalculateMetric(metric.Object, currentValue, tolerance);

        // Assert
        result.MetricName.Should().Be("Throughput");
        result.BaselineValue.Should().Be(1000.0);
        result.CurrentValue.Should().Be(currentValue);
        result.AbsoluteChange.Should().Be(-50.0);
        result.Tolerance.Should().Be(tolerance);
    }

    [Fact]
    public void CalculateMetric_ConfidenceBelowThreshold_ResultsInInconclusiveOutcome()
    {
        // Arrange
        var metric = CreateMockMetric("ErrorRate", 5.0);
        var currentValue = 5.1; // Very small change
        var tolerance = new Tolerance("ErrorRate", ToleranceType.Absolute, 1.0);
        // Confidence should be < 0.5 threshold, resulting in INCONCLUSIVE

        // Act
        var result = _calculator.CalculateMetric(metric.Object, currentValue, tolerance);

        // Assert
        result.Outcome.Should().Be(ComparisonOutcome.Inconclusive);
    }

    [Fact]
    public void CalculateMetric_WithEdgeCase_BaselineZero_RelativeTolerance()
    {
        // Arrange
        var metric = CreateMockMetric("NewMetric", 0.0);
        var currentValue = 10.0;
        var tolerance = new Tolerance("NewMetric", ToleranceType.Relative, 10.0);

        // Act
        var result = _calculator.CalculateMetric(metric.Object, currentValue, tolerance);

        // Assert
        result.MetricName.Should().Be("NewMetric");
        result.BaselineValue.Should().Be(0.0);
        result.CurrentValue.Should().Be(currentValue);
        // With baseline=0 and relative tolerance, change is typically treated as infinite/conclusive
        result.Outcome.Should().NotBe(ComparisonOutcome.Inconclusive);
    }

    [Fact]
    public void DetermineOutcome_WithHighPositiveChange_ReturnsRegression()
    {
        // Arrange
        var metric = CreateMockMetric("ResponseTime", 100.0);
        var currentValue = 200.0; // 100% increase
        var tolerance = new Tolerance("ResponseTime", ToleranceType.Absolute, 10.0);

        // Act
        var result = _calculator.CalculateMetric(metric.Object, currentValue, tolerance);

        // Assert
        result.Outcome.Should().Be(ComparisonOutcome.Regression);
    }

    [Fact]
    public void DetermineOutcome_WithHighNegativeChange_ReturnsImprovement()
    {
        // Arrange
        var metric = CreateMockMetric("ErrorRate", 10.0);
        var currentValue = 2.0; // 80% decrease
        var tolerance = new Tolerance("ErrorRate", ToleranceType.Absolute, 1.0);

        // Act
        var result = _calculator.CalculateMetric(metric.Object, currentValue, tolerance);

        // Assert
        result.Outcome.Should().Be(ComparisonOutcome.Improvement);
    }

    [Fact]
    public void DetermineOutcome_WithChangeWithinTolerance_ReturnsNoSignificantChange()
    {
        // Arrange
        var metric = CreateMockMetric("Throughput", 1000.0);
        var currentValue = 1005.0; // 5 unit change, within 10 unit tolerance
        var tolerance = new Tolerance("Throughput", ToleranceType.Absolute, 10.0);

        // Act
        var result = _calculator.CalculateMetric(metric.Object, currentValue, tolerance);

        // Assert
        result.Outcome.Should().Be(ComparisonOutcome.NoSignificantChange);
    }

    [Fact]
    public void CalculateMetric_WithMultipleCalculations_ProducesConsistentResults()
    {
        // Arrange
        var metric = CreateMockMetric("ResponseTime", 100.0);
        var currentValue = 125.0;
        var tolerance = new Tolerance("ResponseTime", ToleranceType.Absolute, 5.0);

        // Act
        var result1 = _calculator.CalculateMetric(metric.Object, currentValue, tolerance);
        var result2 = _calculator.CalculateMetric(metric.Object, currentValue, tolerance);

        // Assert
        result1.Should().Be(result2);
        result1.Outcome.Should().Be(result2.Outcome);
        result1.Confidence.Should().Be(result2.Confidence);
    }

    [Fact]
    public void CalculateMetric_WithRelativeToleranceNegativeChange_CalculatesCorrectly()
    {
        // Arrange
        var metric = CreateMockMetric("ErrorRate", 100.0);
        var currentValue = 80.0; // 20% decrease
        var tolerance = new Tolerance("ErrorRate", ToleranceType.Relative, 10.0); // 10% tolerance

        // Act
        var result = _calculator.CalculateMetric(metric.Object, currentValue, tolerance);

        // Assert
        result.RelativeChange.Should().BeApproximately(-20.0, 0.01);
        result.Outcome.Should().Be(ComparisonOutcome.Improvement); // 20% improvement beyond 10% tolerance
    }

    [Fact]
    public void CalculateMetric_ComputesConfidenceLevel()
    {
        // Arrange
        var metric = CreateMockMetric("Throughput", 1000.0);
        var currentValue = 1100.0;
        var tolerance = new Tolerance("Throughput", ToleranceType.Absolute, 50.0);

        // Act
        var result = _calculator.CalculateMetric(metric.Object, currentValue, tolerance);

        // Assert
        result.Confidence.Should().NotBeNull();
        result.Confidence.Value.Should().BeGreaterThanOrEqualTo(0.0);
        result.Confidence.Value.Should().BeLessThanOrEqualTo(1.0);
    }

    [Fact]
    public void CalculateMetric_StoresAllPropertiesCorrectly()
    {
        // Arrange
        var metricName = "MemoryUsage";
        var baselineValue = 500.0;
        var currentValue = 600.0;
        var tolerance = new Tolerance(metricName, ToleranceType.Absolute, 50.0);
        var metric = CreateMockMetric(metricName, baselineValue);

        // Act
        var result = _calculator.CalculateMetric(metric.Object, currentValue, tolerance);

        // Assert
        result.MetricName.Should().Be(metricName);
        result.BaselineValue.Should().Be(baselineValue);
        result.CurrentValue.Should().Be(currentValue);
        result.Tolerance.Should().Be(tolerance);
        result.Outcome.Should().NotBe(null);
        result.Confidence.Should().NotBeNull();
    }

    [Fact]
    public void CalculateMetric_WithZeroTolerance_StrictComparison()
    {
        // Arrange
        var metric = CreateMockMetric("ExactMetric", 100.0);
        var currentValue = 100.0;
        var tolerance = new Tolerance("ExactMetric", ToleranceType.Absolute, 0.0);

        // Act
        var result = _calculator.CalculateMetric(metric.Object, currentValue, tolerance);

        // Assert
        result.AbsoluteChange.Should().Be(0.0);
        result.Outcome.Should().Be(ComparisonOutcome.NoSignificantChange);
    }

    [Fact]
    public void CalculateMetric_WithSmallNegativeChange_StillDetectsChange()
    {
        // Arrange
        var metric = CreateMockMetric("Temperature", 100.0);
        var currentValue = 99.5; // Small decrease
        var tolerance = new Tolerance("Temperature", ToleranceType.Absolute, 0.1);

        // Act
        var result = _calculator.CalculateMetric(metric.Object, currentValue, tolerance);

        // Assert
        result.AbsoluteChange.Should().Be(-0.5);
        result.Outcome.Should().Be(ComparisonOutcome.Improvement); // Decrease is improvement for temperature
    }

    private static Mock<IMetric> CreateMockMetric(string name, double value)
    {
        var mock = new Mock<IMetric>();
        mock.Setup(m => m.MetricType).Returns(name);
        mock.Setup(m => m.Value).Returns(value);
        return mock;
    }
}
