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
        var id = ComparisonResultId.Create();
        var baselineId = BaselineId.Create();
        var metricResults = new[]
        {
            new ComparisonMetric(
                "ResponseTime",
                100.0,
                110.0,
                new Tolerance("ResponseTime", ToleranceType.Absolute, 10.0),
                ComparisonOutcome.NoSignificantChange,
                new ConfidenceLevel(0.5)
            ),
        };

        // Act
        var result = new ComparisonResult(
            id,
            baselineId,
            metricResults,
            ComparisonOutcome.NoSignificantChange,
            new ConfidenceLevel(0.5)
        );

        // Assert
        result.Id.Should().Be(id);
        result.BaselineId.Should().Be(baselineId);
        result.MetricResults.Should().HaveCount(1);
        result.OverallOutcome.Should().Be(ComparisonOutcome.NoSignificantChange);
        result.OverallConfidence.Value.Should().Be(0.5);
    }

    [Fact]
    public void Constructor_WithMultipleMetrics_AggregatesOutcomeCorrectly()
    {
        // Arrange
        var id = ComparisonResultId.Create();
        var baselineId = BaselineId.Create();
        var tolerance = new Tolerance("Metric", ToleranceType.Absolute, 10.0);
        var metricResults = new[]
        {
            new ComparisonMetric(
                "ResponseTime",
                100.0,
                110.0,
                tolerance,
                ComparisonOutcome.NoSignificantChange,
                new ConfidenceLevel(0.5)
            ),
            new ComparisonMetric(
                "Throughput",
                1000.0,
                1200.0,
                tolerance,
                ComparisonOutcome.Improvement,
                new ConfidenceLevel(0.8)
            ),
        };

        // Act
        var result = new ComparisonResult(
            id,
            baselineId,
            metricResults,
            ComparisonOutcome.Improvement,
            new ConfidenceLevel(0.5)
        );

        // Assert
        result.MetricResults.Should().HaveCount(2);
        result.OverallOutcome.Should().Be(ComparisonOutcome.Improvement);
    }

    [Fact]
    public void Constructor_WithRegressionInMetrics_OverallOutcomeIsRegression()
    {
        // Arrange
        var id = ComparisonResultId.Create();
        var baselineId = BaselineId.Create();
        var tolerance = new Tolerance("Metric", ToleranceType.Absolute, 10.0);
        var metricResults = new[]
        {
            new ComparisonMetric(
                "ResponseTime",
                100.0,
                150.0,
                tolerance,
                ComparisonOutcome.Regression,
                new ConfidenceLevel(0.8)
            ),
            new ComparisonMetric(
                "Throughput",
                1000.0,
                1200.0,
                tolerance,
                ComparisonOutcome.Improvement,
                new ConfidenceLevel(0.8)
            ),
        };

        // Act
        var result = new ComparisonResult(
            id,
            baselineId,
            metricResults,
            ComparisonOutcome.Regression,
            new ConfidenceLevel(0.5)
        );

        // Assert
        result.OverallOutcome.Should().Be(ComparisonOutcome.Regression);
    }

    [Fact]
    public void HasRegression_WithRegressionOutcome_ReturnsTrue()
    {
        // Arrange
        var id = ComparisonResultId.Create();
        var baselineId = BaselineId.Create();
        var tolerance = new Tolerance("Metric", ToleranceType.Absolute, 10.0);
        var metricResults = new[]
        {
            new ComparisonMetric(
                "ResponseTime",
                100.0,
                150.0,
                tolerance,
                ComparisonOutcome.Regression,
                new ConfidenceLevel(0.8)
            ),
        };

        var result = new ComparisonResult(
            id,
            baselineId,
            metricResults,
            ComparisonOutcome.Regression,
            new ConfidenceLevel(0.8)
        );

        // Act & Assert
        result.HasRegression().Should().BeTrue();
    }

    [Fact]
    public void HasRegression_WithoutRegressionOutcome_ReturnsFalse()
    {
        // Arrange
        var id = ComparisonResultId.Create();
        var baselineId = BaselineId.Create();
        var tolerance = new Tolerance("Metric", ToleranceType.Absolute, 10.0);
        var metricResults = new[]
        {
            new ComparisonMetric(
                "Throughput",
                1000.0,
                1200.0,
                tolerance,
                ComparisonOutcome.Improvement,
                new ConfidenceLevel(0.8)
            ),
        };

        var result = new ComparisonResult(
            id,
            baselineId,
            metricResults,
            ComparisonOutcome.Improvement,
            new ConfidenceLevel(0.8)
        );

        // Act & Assert
        result.HasRegression().Should().BeFalse();
    }

    [Fact]
    public void ComparedAt_IsSetToCurrentTime()
    {
        // Arrange
        var id = ComparisonResultId.Create();
        var baselineId = BaselineId.Create();
        var beforeCreation = DateTime.UtcNow;
        var tolerance = new Tolerance("Metric", ToleranceType.Absolute, 10.0);
        var metricResults = new[]
        {
            new ComparisonMetric(
                "ResponseTime",
                100.0,
                110.0,
                tolerance,
                ComparisonOutcome.NoSignificantChange,
                new ConfidenceLevel(0.5)
            ),
        };

        // Act
        var result = new ComparisonResult(
            id,
            baselineId,
            metricResults,
            ComparisonOutcome.NoSignificantChange,
            new ConfidenceLevel(0.5)
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
        var id = ComparisonResultId.Create();
        var baselineId = BaselineId.Create();
        var tolerance = new Tolerance("Metric", ToleranceType.Absolute, 10.0);
        var metricResults = new[]
        {
            new ComparisonMetric(
                "ResponseTime",
                100.0,
                110.0,
                tolerance,
                ComparisonOutcome.NoSignificantChange,
                new ConfidenceLevel(0.5)
            ),
        };

        var result = new ComparisonResult(
            id,
            baselineId,
            metricResults,
            ComparisonOutcome.NoSignificantChange,
            new ConfidenceLevel(0.5)
        );

        // Act & Assert
        result.MetricResults.Should().BeOfType<IReadOnlyList<ComparisonMetric>>();
    }

    [Fact]
    public void Constructor_WithEmptyMetricResults_Throws()
    {
        // Arrange
        var id = ComparisonResultId.Create();
        var baselineId = BaselineId.Create();
        var emptyMetricResults = Array.Empty<ComparisonMetric>();

        // Act & Assert
        var action = () => new ComparisonResult(
            id,
            baselineId,
            emptyMetricResults,
            ComparisonOutcome.NoSignificantChange,
            new ConfidenceLevel(0.5)
        );

        action.Should().Throw<DomainInvariantViolatedException>();
    }

    [Fact]
    public void Constructor_WithDuplicateMetricNames_Throws()
    {
        // Arrange
        var id = ComparisonResultId.Create();
        var baselineId = BaselineId.Create();
        var tolerance = new Tolerance("Metric", ToleranceType.Absolute, 10.0);
        var metricResults = new[]
        {
            new ComparisonMetric(
                "ResponseTime",
                100.0,
                110.0,
                tolerance,
                ComparisonOutcome.NoSignificantChange,
                new ConfidenceLevel(0.5)
            ),
            new ComparisonMetric(
                "ResponseTime",
                100.0,
                120.0,
                tolerance,
                ComparisonOutcome.Regression,
                new ConfidenceLevel(0.8)
            ),
        };

        // Act & Assert
        var action = () => new ComparisonResult(
            id,
            baselineId,
            metricResults,
            ComparisonOutcome.NoSignificantChange,
            new ConfidenceLevel(0.5)
        );

        action.Should().Throw<DomainInvariantViolatedException>();
    }

    [Fact]
    public void OverallConfidence_AggregatesMetricConfidences()
    {
        // Arrange
        var id = ComparisonResultId.Create();
        var baselineId = BaselineId.Create();
        var tolerance = new Tolerance("Metric", ToleranceType.Absolute, 10.0);
        var metricResults = new[]
        {
            new ComparisonMetric(
                "ResponseTime",
                100.0,
                110.0,
                tolerance,
                ComparisonOutcome.NoSignificantChange,
                new ConfidenceLevel(0.8)
            ),
            new ComparisonMetric(
                "Throughput",
                1000.0,
                1200.0,
                tolerance,
                ComparisonOutcome.Improvement,
                new ConfidenceLevel(0.6)
            ),
        };

        // Act
        var result = new ComparisonResult(
            id,
            baselineId,
            metricResults,
            ComparisonOutcome.Improvement,
            new ConfidenceLevel(0.6) // Minimum of 0.8 and 0.6
        );

        // Assert
        result.OverallConfidence.Value.Should().Be(0.6); // Should be minimum confidence
    }

    [Fact]
    public void Equality_SameProperties_AreEqual()
    {
        // Arrange
        var id = ComparisonResultId.Create();
        var baselineId = BaselineId.Create();
        var tolerance = new Tolerance("Metric", ToleranceType.Absolute, 10.0);
        var metricResults = new[]
        {
            new ComparisonMetric(
                "ResponseTime",
                100.0,
                110.0,
                tolerance,
                ComparisonOutcome.NoSignificantChange,
                new ConfidenceLevel(0.5)
            ),
        };

        var result1 = new ComparisonResult(
            id,
            baselineId,
            metricResults,
            ComparisonOutcome.NoSignificantChange,
            new ConfidenceLevel(0.5)
        );

        var result2 = new ComparisonResult(
            id,
            baselineId,
            metricResults,
            ComparisonOutcome.NoSignificantChange,
            new ConfidenceLevel(0.5)
        );

        // Act & Assert
        result1.Should().Be(result2);
    }

    [Fact]
    public void ToString_ReturnsFormattedString()
    {
        // Arrange
        var id = ComparisonResultId.Create();
        var baselineId = BaselineId.Create();
        var tolerance = new Tolerance("Metric", ToleranceType.Absolute, 10.0);
        var metricResults = new[]
        {
            new ComparisonMetric(
                "ResponseTime",
                100.0,
                110.0,
                tolerance,
                ComparisonOutcome.NoSignificantChange,
                new ConfidenceLevel(0.5)
            ),
        };

        var result = new ComparisonResult(
            id,
            baselineId,
            metricResults,
            ComparisonOutcome.NoSignificantChange,
            new ConfidenceLevel(0.5)
        );

        // Act
        var toString = result.ToString();

        // Assert
        toString.Should().Contain(id.Value.ToString());
        toString.Should().Contain(baselineId.Value.ToString());
    }
}
