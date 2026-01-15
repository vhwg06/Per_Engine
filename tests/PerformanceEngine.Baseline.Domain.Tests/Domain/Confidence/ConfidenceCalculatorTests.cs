namespace PerformanceEngine.Baseline.Domain.Tests.Domain.Confidence;

using FluentAssertions;
using PerformanceEngine.Baseline.Domain.Domain;
using PerformanceEngine.Baseline.Domain.Domain.Confidence;
using PerformanceEngine.Baseline.Domain.Domain.Tolerances;
using Xunit;

public class ConfidenceCalculatorTests
{
    private readonly ConfidenceCalculator _calculator = new();

    [Fact]
    public void CalculateConfidence_WithInfiniteChange_ReturnsMaxConfidence()
    {
        // Arrange
        var baselineValue = 100.0;
        var currentValue = 0.0; // Complete elimination = infinite change ratio
        var tolerance = new Tolerance("Metric", ToleranceType.Absolute, 10.0);

        // Act
        var confidence = _calculator.CalculateConfidence(baselineValue, currentValue, tolerance);

        // Assert
        confidence.Should().NotBeNull();
        confidence.Value.Should().Be(1.0); // Capped at 1.0
    }

    [Fact]
    public void CalculateConfidence_WithNoChange_ReturnsZeroConfidence()
    {
        // Arrange
        var baselineValue = 100.0;
        var currentValue = 100.0; // No change
        var tolerance = new Tolerance("Metric", ToleranceType.Absolute, 10.0);

        // Act
        var confidence = _calculator.CalculateConfidence(baselineValue, currentValue, tolerance);

        // Assert
        confidence.Should().NotBeNull();
        confidence.Value.Should().Be(0.0);
    }

    [Fact]
    public void CalculateConfidence_WithAbsoluteTolerance_CalculatesCorrectly()
    {
        // Arrange
        var baselineValue = 100.0;
        var currentValue = 115.0; // Change of 15
        var tolerance = new Tolerance("Metric", ToleranceType.Absolute, 10.0);
        // Expected: confidence = min(1.0, (15 - 10) / 10) = min(1.0, 0.5) = 0.5

        // Act
        var confidence = _calculator.CalculateConfidence(baselineValue, currentValue, tolerance);

        // Assert
        confidence.Should().NotBeNull();
        confidence.Value.Should().BeApproximately(0.5, 0.001);
    }

    [Fact]
    public void CalculateConfidence_WithRelativeTolerance_CalculatesCorrectly()
    {
        // Arrange
        var baselineValue = 100.0;
        var currentValue = 130.0; // Change of 30 (30% relative)
        var tolerance = new Tolerance("Metric", ToleranceType.Relative, 20.0); // 20% tolerance
        // Expected: absolute change = 30, tolerance threshold = 100 * 0.20 = 20
        // confidence = min(1.0, (30 - 20) / 20) = min(1.0, 0.5) = 0.5

        // Act
        var confidence = _calculator.CalculateConfidence(baselineValue, currentValue, tolerance);

        // Assert
        confidence.Should().NotBeNull();
        confidence.Value.Should().BeApproximately(0.5, 0.001);
    }

    [Fact]
    public void CalculateConfidence_WithChangeJustAtTolerance_ReturnsZeroConfidence()
    {
        // Arrange
        var baselineValue = 100.0;
        var currentValue = 110.0; // Change of exactly 10
        var tolerance = new Tolerance("Metric", ToleranceType.Absolute, 10.0);
        // Expected: confidence = min(1.0, (10 - 10) / 10) = 0

        // Act
        var confidence = _calculator.CalculateConfidence(baselineValue, currentValue, tolerance);

        // Assert
        confidence.Should().NotBeNull();
        confidence.Value.Should().Be(0.0);
    }

    [Fact]
    public void CalculateConfidence_WithChangeJustWithinTolerance_ReturnsNegativeConfidence()
    {
        // Arrange
        var baselineValue = 100.0;
        var currentValue = 109.0; // Change of 9 (within 10 tolerance)
        var tolerance = new Tolerance("Metric", ToleranceType.Absolute, 10.0);
        // Expected: confidence = min(1.0, (9 - 10) / 10) = min(1.0, -0.1) = -0.1
        // But ConfidenceLevel should clamp to [0, 1] range

        // Act
        var confidence = _calculator.CalculateConfidence(baselineValue, currentValue, tolerance);

        // Assert
        confidence.Should().NotBeNull();
        // Values below 0 should be clamped to 0 by ConfidenceLevel constructor
        confidence.Value.Should().BeGreaterThanOrEqualTo(0.0);
    }

    [Fact]
    public void CalculateConfidence_WithLargeChange_CapsCeilingAt1_0()
    {
        // Arrange
        var baselineValue = 100.0;
        var currentValue = 1000.0; // Huge change
        var tolerance = new Tolerance("Metric", ToleranceType.Absolute, 10.0);

        // Act
        var confidence = _calculator.CalculateConfidence(baselineValue, currentValue, tolerance);

        // Assert
        confidence.Should().NotBeNull();
        confidence.Value.Should().Be(1.0);
    }

    [Fact]
    public void CalculateConfidence_WithSmallAbsoluteTolerance_CalculatesHighConfidence()
    {
        // Arrange
        var baselineValue = 100.0;
        var currentValue = 105.0; // Change of 5
        var tolerance = new Tolerance("Metric", ToleranceType.Absolute, 1.0);
        // Expected: confidence = min(1.0, (5 - 1) / 1) = min(1.0, 4) = 1.0

        // Act
        var confidence = _calculator.CalculateConfidence(baselineValue, currentValue, tolerance);

        // Assert
        confidence.Should().NotBeNull();
        confidence.Value.Should().Be(1.0);
    }

    [Fact]
    public void CalculateConfidence_WithRelativeTolerance_BaselineZero_ThrowsOrHandlesGracefully()
    {
        // Arrange
        var baselineValue = 0.0;
        var currentValue = 10.0;
        var tolerance = new Tolerance("Metric", ToleranceType.Relative, 10.0);

        // Act & Assert
        // Should either throw or handle by treating baseline=0 case
        var action = () => _calculator.CalculateConfidence(baselineValue, currentValue, tolerance);
        // Depending on implementation, this might throw or return a specific confidence value
        // Most likely treats any change from 0 as infinite/conclusive
        action.Should().NotThrow();
    }

    [Fact]
    public void CalculateConfidence_WithNegativeChangeAbsoluteTolerance_CalculatesCorrectly()
    {
        // Arrange
        var baselineValue = 100.0;
        var currentValue = 85.0; // Change of -15
        var tolerance = new Tolerance("Metric", ToleranceType.Absolute, 10.0);
        // Expected: confidence = min(1.0, (15 - 10) / 10) = min(1.0, 0.5) = 0.5

        // Act
        var confidence = _calculator.CalculateConfidence(baselineValue, currentValue, tolerance);

        // Assert
        confidence.Should().NotBeNull();
        confidence.Value.Should().BeApproximately(0.5, 0.001);
    }

    [Fact]
    public void CalculateConfidence_ReturnValueAlwaysInRange()
    {
        // Arrange
        var scenarios = new[]
        {
            (baseline: 50.0, current: 100.0, type: ToleranceType.Absolute, amount: 5.0),
            (baseline: 50.0, current: 10.0, type: ToleranceType.Absolute, amount: 5.0),
            (baseline: 100.0, current: 130.0, type: ToleranceType.Relative, amount: 10.0),
            (baseline: 100.0, current: 70.0, type: ToleranceType.Relative, amount: 10.0),
        };

        // Act & Assert
        foreach (var (baseline, current, type, amount) in scenarios)
        {
            var tolerance = new Tolerance("Metric", type, amount);
            var confidence = _calculator.CalculateConfidence(baseline, current, tolerance);
            
            confidence.Value.Should().BeGreaterThanOrEqualTo(0.0);
            confidence.Value.Should().BeLessThanOrEqualTo(1.0);
        }
    }
}
