namespace PerformanceEngine.Baseline.Domain.Tests.Domain;

using FluentAssertions;
using PerformanceEngine.Baseline.Domain.Domain.Comparisons;
using PerformanceEngine.Baseline.Domain.Domain.Tolerances;
using Xunit;

public class DeterminismTests
{
    private readonly ComparisonCalculator _calculator = new();

    [Fact]
    public void CalculateMetric_1000Runs_ProducesIdenticalResults()
    {
        // Arrange
        var baseline = 150m;
        var current = 187m;
        var tolerance = new Tolerance("ResponseTime", ToleranceType.Absolute, 25m);

        var results = new List<ComparisonMetric>();

        // Act
        for (int i = 0; i < 1000; i++)
        {
            var result = _calculator.CalculateMetric(baseline, current, tolerance);
            results.Add(result);
        }

        // Assert - All results should be identical
        var firstResult = results[0];
        foreach (var result in results)
        {
            result.MetricName.Should().Be(firstResult.MetricName);
            result.BaselineValue.Should().Be(firstResult.BaselineValue);
            result.CurrentValue.Should().Be(firstResult.CurrentValue);
            result.AbsoluteChange.Should().Be(firstResult.AbsoluteChange);
            result.Outcome.Should().Be(firstResult.Outcome);
            result.Confidence.Value.Should().Be(firstResult.Confidence.Value);
        }
    }

    [Theory]
    [InlineData(100, 200)]
    [InlineData(50, 50)]
    [InlineData(1000, 1)]
    public void CalculateMetric_VariousInputs_AreReproducible(int baseline, int current)
    {
        // Arrange
        var tolerance = new Tolerance("Metric", ToleranceType.Absolute, 10m);

        // Act
        var result1 = _calculator.CalculateMetric((decimal)baseline, (decimal)current, tolerance);
        var result2 = _calculator.CalculateMetric((decimal)baseline, (decimal)current, tolerance);

        // Assert
        result1.Should().Be(result2);
        result1.Outcome.Should().Be(result2.Outcome);
        result1.Confidence.Value.Should().Be(result2.Confidence.Value);
    }
}
