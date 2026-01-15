namespace PerformanceEngine.Baseline.Domain.Tests.Domain;

using FluentAssertions;
using Moq;
using PerformanceEngine.Baseline.Domain.Domain.Comparisons;
using PerformanceEngine.Baseline.Domain.Domain.Confidence;
using PerformanceEngine.Baseline.Domain.Domain.Tolerances;
using PerformanceEngine.Metrics.Domain.Ports;
using Xunit;

public class DeterminismTests
{
    private readonly ComparisonCalculator _calculator = new();

    [Fact]
    public void CalculateMetric_1000Runs_ProducesIdenticalResults()
    {
        // Arrange
        var baselineValue = 150.75;
        var currentValue = 187.25;
        var tolerance = new Tolerance("ResponseTime", ToleranceType.Absolute, 25.0);
        var metric = CreateMockMetric("ResponseTime", baselineValue);

        var results = new List<ComparisonMetric>();

        // Act
        for (int i = 0; i < 1000; i++)
        {
            var result = _calculator.CalculateMetric(metric.Object, currentValue, tolerance);
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
            result.RelativeChange.Should().Be(firstResult.RelativeChange);
            result.Outcome.Should().Be(firstResult.Outcome);
            result.Confidence.Value.Should().Be(firstResult.Confidence.Value);
        }
    }

    [Fact]
    public void ComparisonResult_1000Runs_ProducesIdenticalResults()
    {
        // Arrange
        var baselineId = new Baseline.Domain.Baselines.BaselineId(Guid.Parse("12345678-1234-5678-1234-567812345678"));
        var comparisonId1 = ComparisonResultId.Create();
        var tolerance = new Tolerance("Metric", ToleranceType.Absolute, 10.0);
        var metricResults = new[]
        {
            new ComparisonMetric(
                "ResponseTime",
                100.0,
                120.0,
                tolerance,
                ComparisonOutcome.Regression,
                new ConfidenceLevel(0.75)
            ),
            new ComparisonMetric(
                "Throughput",
                1000.0,
                950.0,
                tolerance,
                ComparisonOutcome.Regression,
                new ConfidenceLevel(0.6)
            ),
        };

        var results = new List<ComparisonResult>();

        // Act
        for (int i = 0; i < 1000; i++)
        {
            var id = ComparisonResultId.Create();
            var result = new ComparisonResult(
                id,
                baselineId,
                metricResults,
                ComparisonOutcome.Regression,
                new ConfidenceLevel(0.6)
            );
            results.Add(result);
        }

        // Assert - All non-ID properties should be identical
        var firstResult = results[0];
        foreach (var result in results)
        {
            result.BaselineId.Should().Be(firstResult.BaselineId);
            result.MetricResults.Should().HaveCount(firstResult.MetricResults.Count);
            result.OverallOutcome.Should().Be(firstResult.OverallOutcome);
            result.OverallConfidence.Value.Should().Be(firstResult.OverallConfidence.Value);

            // Verify each metric result
            for (int j = 0; j < result.MetricResults.Count; j++)
            {
                result.MetricResults[j].MetricName.Should().Be(firstResult.MetricResults[j].MetricName);
                result.MetricResults[j].BaselineValue.Should().Be(firstResult.MetricResults[j].BaselineValue);
                result.MetricResults[j].CurrentValue.Should().Be(firstResult.MetricResults[j].CurrentValue);
                result.MetricResults[j].Outcome.Should().Be(firstResult.MetricResults[j].Outcome);
                result.MetricResults[j].Confidence.Value.Should().Be(firstResult.MetricResults[j].Confidence.Value);
            }
        }
    }

    [Fact]
    public void ConfidenceCalculator_1000Runs_ProducesIdenticalResults()
    {
        // Arrange
        var confidenceCalculator = new ConfidenceCalculator();
        var baselineValue = 512.5;
        var currentValue = 768.75;
        var tolerance = new Tolerance("MemoryUsage", ToleranceType.Absolute, 100.0);

        var results = new List<ConfidenceLevel>();

        // Act
        for (int i = 0; i < 1000; i++)
        {
            var confidence = confidenceCalculator.CalculateConfidence(baselineValue, currentValue, tolerance);
            results.Add(confidence);
        }

        // Assert - All results should be identical
        var firstResult = results[0];
        foreach (var result in results)
        {
            result.Value.Should().Be(firstResult.Value);
        }
    }

    [Fact]
    public void FloatingPointCalculations_NoAmbiguity()
    {
        // Arrange
        var baselineValue = 100.0;
        var currentValue = 100.0 + 1e-15; // Micro-difference
        var tolerance = new Tolerance("Metric", ToleranceType.Absolute, 1.0);

        // Act
        var results = new List<ComparisonMetric>();
        for (int i = 0; i < 100; i++)
        {
            var metric = CreateMockMetric("Metric", baselineValue);
            var result = _calculator.CalculateMetric(metric.Object, currentValue, tolerance);
            results.Add(result);
        }

        // Assert - All results should be identical despite floating point arithmetic
        var firstResult = results[0];
        foreach (var result in results)
        {
            result.AbsoluteChange.Should().Be(firstResult.AbsoluteChange);
            result.Outcome.Should().Be(firstResult.Outcome);
        }
    }

    [Fact]
    public void DeterministicOrdering_MultipleMetrics()
    {
        // Arrange
        var aggregator = new OutcomeAggregator();
        var outcomes = new[] { ComparisonOutcome.Improvement, ComparisonOutcome.Regression, ComparisonOutcome.NoSignificantChange };

        var result1 = aggregator.Aggregate(outcomes);
        var result2 = aggregator.Aggregate(outcomes);
        var result3 = aggregator.Aggregate(outcomes.Reverse().ToArray());

        // Act & Assert
        result1.Should().Be(result2); // Same order = same result
        result1.Should().Be(result3); // Reversed order = same result (aggregation is order-independent for worst-case)
    }

    [Theory]
    [InlineData(100.0, 200.0)]
    [InlineData(50.0, 50.0)]
    [InlineData(1000.0, 1.0)]
    [InlineData(0.001, 0.002)]
    public void CalculateMetric_VariousInputs_AreReproducible(double baseline, double current)
    {
        // Arrange
        var tolerance = new Tolerance("Metric", ToleranceType.Absolute, 10.0);
        var metric = CreateMockMetric("Metric", baseline);

        // Act
        var result1 = _calculator.CalculateMetric(metric.Object, current, tolerance);
        var result2 = _calculator.CalculateMetric(metric.Object, current, tolerance);

        // Assert
        result1.Should().Be(result2);
        result1.Outcome.Should().Be(result2.Outcome);
        result1.Confidence.Value.Should().Be(result2.Confidence.Value);
    }

    [Fact]
    public void ConfidenceLevel_ArithmeticOperations_AreReproducible()
    {
        // Arrange
        var confidence1 = new ConfidenceLevel(0.75);
        var confidence2 = new ConfidenceLevel(0.6);

        // Act
        var combined1 = confidence1 * confidence2; // Multiple runs
        var combined2 = confidence1 * confidence2;

        // Assert
        combined1.Value.Should().Be(combined2.Value);
    }

    private static Mock<IMetric> CreateMockMetric(string name, double value)
    {
        var mock = new Mock<IMetric>();
        mock.Setup(m => m.MetricType).Returns(name);
        mock.Setup(m => m.Value).Returns(value);
        return mock;
    }
}
