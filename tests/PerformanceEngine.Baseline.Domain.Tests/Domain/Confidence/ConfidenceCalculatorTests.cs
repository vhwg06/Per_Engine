namespace PerformanceEngine.Baseline.Domain.Tests.Domain.Confidence;

using FluentAssertions;
using PerformanceEngine.Baseline.Domain.Domain.Confidence;
using PerformanceEngine.Baseline.Domain.Domain.Tolerances;
using Xunit;

public class ConfidenceCalculatorTests
{
    private readonly ConfidenceCalculator _calculator = new();

    [Fact]
    public void CalculateConfidence_WithAbsoluteTolerance_CalculatesCorrectly()
    {
        // Arrange
        var baseline = 100m;
        var current = 115m; // Change of 15
        var tolerance = new Tolerance("Metric", ToleranceType.Absolute, 10m);

        // Act
        var confidence = _calculator.CalculateConfidence(baseline, current, tolerance);

        // Assert
        confidence.Should().NotBeNull();
        confidence.Value.Should().BeGreaterThan(0m);
        confidence.Value.Should().BeLessThanOrEqualTo(1m);
    }

    [Fact]
    public void CalculateConfidence_WithRelativeTolerance_CalculatesCorrectly()
    {
        // Arrange
        var baseline = 100m;
        var current = 130m; // Change of 30 (30% relative)
        var tolerance = new Tolerance("Metric", ToleranceType.Relative, 20m);

        // Act
        var confidence = _calculator.CalculateConfidence(baseline, current, tolerance);

        // Assert
        confidence.Should().NotBeNull();
        confidence.Value.Should().BeGreaterThan(0m);
        confidence.Value.Should().BeLessThanOrEqualTo(1m);
    }

    [Fact]
    public void CalculateConfidence_WithNoChange_ReturnsZeroConfidence()
    {
        // Arrange
        var baseline = 100m;
        var current = 100m;
        var tolerance = new Tolerance("Metric", ToleranceType.Absolute, 10m);

        // Act
        var confidence = _calculator.CalculateConfidence(baseline, current, tolerance);

        // Assert
        confidence.Should().NotBeNull();
        confidence.Value.Should().Be(0m);
    }

    [Fact]
    public void CalculateConfidence_WithLargeChange_CapsCeilingAt1()
    {
        // Arrange
        var baseline = 100m;
        var current = 1000m;
        var tolerance = new Tolerance("Metric", ToleranceType.Absolute, 10m);

        // Act
        var confidence = _calculator.CalculateConfidence(baseline, current, tolerance);

        // Assert
        confidence.Should().NotBeNull();
        confidence.Value.Should().Be(1m);
    }

    [Fact]
    public void CalculateConfidence_ReturnValueAlwaysInRange()
    {
        // Arrange
        var scenarios = new[]
        {
            (baseline: 50m, current: 100m, type: ToleranceType.Absolute, amount: 5m),
            (baseline: 50m, current: 10m, type: ToleranceType.Absolute, amount: 5m),
            (baseline: 100m, current: 130m, type: ToleranceType.Relative, amount: 10m),
            (baseline: 100m, current: 70m, type: ToleranceType.Relative, amount: 10m),
        };

        // Act & Assert
        foreach (var (baseline, current, type, amount) in scenarios)
        {
            var tolerance = new Tolerance("Metric", type, amount);
            var confidence = _calculator.CalculateConfidence(baseline, current, tolerance);
            
            confidence.Value.Should().BeGreaterThanOrEqualTo(0m);
            confidence.Value.Should().BeLessThanOrEqualTo(1m);
        }
    }

    [Fact]
    public void CalculateConfidence_WithNegativeChange_CalculatesCorrectly()
    {
        // Arrange
        var baseline = 100m;
        var current = 85m;
        var tolerance = new Tolerance("Metric", ToleranceType.Absolute, 10m);

        // Act
        var confidence = _calculator.CalculateConfidence(baseline, current, tolerance);

        // Assert
        confidence.Should().NotBeNull();
        confidence.Value.Should().BeGreaterThanOrEqualTo(0m);
        confidence.Value.Should().BeLessThanOrEqualTo(1m);
    }
}
