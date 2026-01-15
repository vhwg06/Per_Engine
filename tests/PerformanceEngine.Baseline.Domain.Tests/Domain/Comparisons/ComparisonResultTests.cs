namespace PerformanceEngine.Baseline.Domain.Tests.Domain.Comparisons;

using FluentAssertions;
using PerformanceEngine.Baseline.Domain.Domain;
using PerformanceEngine.Baseline.Domain.Domain.Baselines;
using PerformanceEngine.Baseline.Domain.Domain.Comparisons;
using PerformanceEngine.Baseline.Domain.Domain.Confidence;
using PerformanceEngine.Baseline.Domain.Domain.Tolerances;
using Xunit;

public class ComparisonResultTests
{
    [Fact]
    public void Constructor_WithValidInputs_CreatesComparisonResult()
    {
        // Arrange
        var baselineId = new BaselineId();
        var metricResults = new[]
        {
            new ComparisonMetric(
                "ResponseTime",
                100m,
                110m,
                new Tolerance("ResponseTime", ToleranceType.Absolute, 10m),
                ComparisonOutcome.NoSignificantChange,
                new ConfidenceLevel(0.5m)
            ),
        };

        // Act
        var result = new ComparisonResult(
            baselineId,
            metricResults,
            ComparisonOutcome.NoSignificantChange,
            new ConfidenceLevel(0.5m)
        );

        // Assert
        result.BaselineId.Should().Be(baselineId);
        result.MetricResults.Should().HaveCount(1);
        result.OverallOutcome.Should().Be(ComparisonOutcome.NoSignificantChange);
    }

    [Fact]
    public void HasRegression_WithRegressionOutcome_ReturnsTrue()
    {
        // Arrange
        var baselineId = new BaselineId();
        var tolerance = new Tolerance("Metric", ToleranceType.Absolute, 10m);
        var metricResults = new[]
        {
            new ComparisonMetric(
                "ResponseTime",
                100m,
                150m,
                tolerance,
                ComparisonOutcome.Regression,
                new ConfidenceLevel(0.8m)
            ),
        };

        var result = new ComparisonResult(
            baselineId,
            metricResults,
            ComparisonOutcome.Regression,
            new ConfidenceLevel(0.8m)
        );

        // Act & Assert
        result.HasRegression().Should().BeTrue();
    }

    [Fact]
    public void HasRegression_WithoutRegressionOutcome_ReturnsFalse()
    {
        // Arrange
        var baselineId = new BaselineId();
        var tolerance = new Tolerance("Metric", ToleranceType.Absolute, 10m);
        var metricResults = new[]
        {
            new ComparisonMetric(
                "Throughput",
                1000m,
                1200m,
                tolerance,
                ComparisonOutcome.Improvement,
                new ConfidenceLevel(0.8m)
            ),
        };

        var result = new ComparisonResult(
            baselineId,
            metricResults,
            ComparisonOutcome.Improvement,
            new ConfidenceLevel(0.8m)
        );

        // Act & Assert
        result.HasRegression().Should().BeFalse();
    }

    [Fact]
    public void ComparedAt_IsSetToCurrentTime()
    {
        // Arrange
        var baselineId = new BaselineId();
        var beforeCreation = DateTime.UtcNow;
        var tolerance = new Tolerance("Metric", ToleranceType.Absolute, 10m);
        var metricResults = new[]
        {
            new ComparisonMetric(
                "ResponseTime",
                100m,
                110m,
                tolerance,
                ComparisonOutcome.NoSignificantChange,
                new ConfidenceLevel(0.5m)
            ),
        };

        // Act
        var result = new ComparisonResult(
            baselineId,
            metricResults,
            ComparisonOutcome.NoSignificantChange,
            new ConfidenceLevel(0.5m)
        );
        var afterCreation = DateTime.UtcNow;

        // Assert
        result.ComparedAt.Should().BeOnOrAfter(beforeCreation);
        result.ComparedAt.Should().BeOnOrBefore(afterCreation);
    }

    [Fact]
    public void MetricResults_IsImmutable()
    {
        // Arrange
        var baselineId = new BaselineId();
        var tolerance = new Tolerance("Metric", ToleranceType.Absolute, 10m);
        var metricResults = new[]
        {
            new ComparisonMetric(
                "ResponseTime",
                100m,
                110m,
                tolerance,
                ComparisonOutcome.NoSignificantChange,
                new ConfidenceLevel(0.5m)
            ),
        };

        var result = new ComparisonResult(
            baselineId,
            metricResults,
            ComparisonOutcome.NoSignificantChange,
            new ConfidenceLevel(0.5m)
        );

        // Act & Assert
        result.MetricResults.Should().HaveCount(1);
        result.MetricResults[0].MetricName.Should().Be("ResponseTime");
    }

    [Fact]
    public void Constructor_WithEmptyMetricResults_Throws()
    {
        // Arrange
        var baselineId = new BaselineId();
        var emptyMetricResults = Array.Empty<ComparisonMetric>();

        // Act & Assert
        var action = () => new ComparisonResult(
            baselineId,
            emptyMetricResults,
            ComparisonOutcome.NoSignificantChange,
            new ConfidenceLevel(0.5m)
        );

        action.Should().Throw<DomainInvariantViolatedException>();
    }
}
