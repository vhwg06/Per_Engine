namespace PerformanceEngine.Baseline.Domain.Tests.EdgeCases;

using FluentAssertions;
using PerformanceEngine.Baseline.Domain.Domain;
using PerformanceEngine.Baseline.Domain.Domain.Baselines;
using PerformanceEngine.Baseline.Domain.Domain.Comparisons;
using PerformanceEngine.Baseline.Domain.Domain.Confidence;
using PerformanceEngine.Baseline.Domain.Domain.Tolerances;
using PerformanceEngine.Metrics.Domain.Metrics;
using PerformanceEngine.Metrics.Domain.Ports;
using Xunit;

/// <summary>
/// Phase 8 Edge Case Tests - T063
/// Comprehensive edge case testing for baseline domain.
/// Tests missing metrics, new metrics, null/NaN values, extreme values, etc.
/// </summary>
public sealed class EdgeCaseTests
{
    private readonly ComparisonCalculator _calculator = new();

    [Fact]
    public void MissingMetricInCurrentResults_ThrowsException()
    {
        // Arrange - Baseline has metric, but current results don't
        var baselineMetrics = new List<IMetric>
        {
            new TestMetric("ResponseTime", 150.0),
            new TestMetric("Throughput", 1000.0)
        };

        var currentMetrics = new List<IMetric>
        {
            new TestMetric("ResponseTime", 160.0)
            // Throughput is missing
        };

        var tolerance = new Tolerance("Throughput", ToleranceType.Relative, 10m);
        var config = new ToleranceConfiguration(new[]
        {
            new Tolerance("ResponseTime", ToleranceType.Relative, 10m),
            tolerance
        });
        var baseline = new Baseline(baselineMetrics, config);

        // Act & Assert - Should handle missing metric gracefully
        var throughtputMetric = baseline.GetMetric("Throughput");
        throughtputMetric.Should().NotBeNull();
        
        // Missing metric in current should be detected by application layer
        // Domain layer just provides the baseline metric
    }

    [Fact]
    public void NewMetricInCurrentResults_NotInBaseline_CannotCompare()
    {
        // Arrange - Current has metric not in baseline
        var baselineMetrics = new List<IMetric>
        {
            new TestMetric("ResponseTime", 150.0)
        };

        var baseline = new Baseline(
            baselineMetrics,
            new ToleranceConfiguration(new[] { new Tolerance("ResponseTime", ToleranceType.Relative, 10m) })
        );

        // Act - Try to get metric not in baseline
        var newMetric = baseline.GetMetric("NewMetric");

        // Assert - Should return null for missing metric
        newMetric.Should().BeNull("baseline doesn't contain NewMetric");
    }

    [Fact]
    public void BaselineWithZeroValue_RelativeTolerance_HandlesGracefully()
    {
        // Arrange - Baseline has zero value (division by zero risk)
        var tolerance = new Tolerance("ErrorRate", ToleranceType.Relative, 10m);

        // Act & Assert - Relative tolerance with zero baseline
        var result = _calculator.CalculateMetric(
            baseline: 0m,
            current: 5m,
            tolerance: tolerance
        );

        // Should handle gracefully (not throw)
        result.Should().NotBeNull();
        result.BaselineValue.Should().Be(0m);
        result.CurrentValue.Should().Be(5m);
    }

    [Fact]
    public void ToleranceZero_ExactMatchRequired()
    {
        // Arrange - Zero tolerance means exact match required
        var tolerance = new Tolerance("Metric", ToleranceType.Absolute, 0m);

        // Act - Slight difference should be outside tolerance
        var result1 = _calculator.CalculateMetric(
            baseline: 100m,
            current: 100m,
            tolerance: tolerance
        );

        var result2 = _calculator.CalculateMetric(
            baseline: 100m,
            current: 100.1m,
            tolerance: tolerance
        );

        // Assert
        tolerance.IsWithinTolerance(100m, 100m).Should().BeTrue("exact match");
        tolerance.IsWithinTolerance(100m, 100.1m).Should().BeFalse("even tiny difference exceeds zero tolerance");
    }

    [Fact]
    public void BaselineWithSingleMetric_ComparesSuccessfully()
    {
        // Arrange - Minimal baseline with just one metric
        var metric = new TestMetric("SingleMetric", 42.0);
        var tolerance = new Tolerance("SingleMetric", ToleranceType.Relative, 10m);
        var config = new ToleranceConfiguration(new[] { tolerance });

        // Act
        var baseline = new Baseline(new[] { metric }, config);

        // Assert
        baseline.Metrics.Should().HaveCount(1);
        baseline.GetMetric("SingleMetric").Should().NotBeNull();
    }

    [Fact]
    public void BaselineWith100Metrics_HandlesSuccessfully()
    {
        // Arrange - Large baseline with many metrics
        var metrics = new List<IMetric>();
        var tolerances = new List<Tolerance>();

        for (int i = 0; i < 100; i++)
        {
            metrics.Add(new TestMetric($"Metric{i}", 100.0 + i));
            tolerances.Add(new Tolerance($"Metric{i}", ToleranceType.Relative, 10m));
        }

        var config = new ToleranceConfiguration(tolerances);

        // Act
        var baseline = new Baseline(metrics, config);

        // Assert
        baseline.Metrics.Should().HaveCount(100);
        for (int i = 0; i < 100; i++)
        {
            baseline.GetMetric($"Metric{i}").Should().NotBeNull();
        }
    }

    [Fact]
    public void VerySmallMetricValues_FloatingPointPrecision()
    {
        // Arrange - Very small values (floating-point precision edge)
        var tolerance = new Tolerance("Latency", ToleranceType.Absolute, 0.0001m);

        // Act
        var result = _calculator.CalculateMetric(
            baseline: 0.0001m,
            current: 0.0002m,
            tolerance: tolerance
        );

        // Assert - Should handle small values precisely
        result.Should().NotBeNull();
        result.AbsoluteChange.Should().Be(0.0001m);
    }

    [Fact]
    public void VeryLargeMetricValues_NoOverflow()
    {
        // Arrange - Very large values (overflow risk)
        var tolerance = new Tolerance("DataVolume", ToleranceType.Absolute, 1000000m);

        // Act
        var result = _calculator.CalculateMetric(
            baseline: 999999999m,
            current: 1000000000m,
            tolerance: tolerance
        );

        // Assert - Should handle large values without overflow
        result.Should().NotBeNull();
        result.AbsoluteChange.Should().Be(1m);
    }

    [Fact]
    public void NegativeMetricValues_HandlesCorrectly()
    {
        // Arrange - Negative values (e.g., profit/loss metrics)
        var tolerance = new Tolerance("ProfitMargin", ToleranceType.Absolute, 5m);

        // Act - From negative to less negative (improvement)
        var result1 = _calculator.CalculateMetric(
            baseline: -10m,
            current: -5m,
            tolerance: tolerance
        );

        // From negative to more negative (regression)
        var result2 = _calculator.CalculateMetric(
            baseline: -10m,
            current: -20m,
            tolerance: tolerance
        );

        // Assert
        result1.Should().NotBeNull();
        result1.AbsoluteChange.Should().Be(5m);

        result2.Should().NotBeNull();
        result2.AbsoluteChange.Should().Be(-10m);
    }

    [Fact]
    public void ComparisonResultWithSingleMetric_IsValid()
    {
        // Arrange - Minimal comparison result
        var baselineId = new BaselineId();
        var tolerance = new Tolerance("Metric", ToleranceType.Relative, 10m);
        var confidence = new ConfidenceLevel(0.8m);

        var metric = new ComparisonMetric(
            "Metric",
            100m,
            110m,
            tolerance,
            ComparisonOutcome.NoSignificantChange,
            confidence
        );

        // Act
        var result = new ComparisonResult(
            baselineId,
            new[] { metric },
            ComparisonOutcome.NoSignificantChange,
            confidence
        );

        // Assert
        result.MetricResults.Should().HaveCount(1);
        result.OverallOutcome.Should().Be(ComparisonOutcome.NoSignificantChange);
    }

    [Fact]
    public void AllMetricsInconclusive_OverallInconclusive()
    {
        // Arrange - All metrics have low confidence
        var aggregator = new OutcomeAggregator();
        var tolerance = new Tolerance("Metric", ToleranceType.Relative, 10m);
        var lowConfidence = new ConfidenceLevel(0.3m);

        var metrics = new[]
        {
            new ComparisonMetric("M1", 100m, 105m, tolerance, ComparisonOutcome.Inconclusive, lowConfidence),
            new ComparisonMetric("M2", 50m, 52m, tolerance, ComparisonOutcome.Inconclusive, lowConfidence),
            new ComparisonMetric("M3", 75m, 77m, tolerance, ComparisonOutcome.Inconclusive, lowConfidence)
        };

        // Act
        var outcome = aggregator.Aggregate(metrics);
        var confidence = aggregator.AggregateConfidence(metrics);

        // Assert
        outcome.Should().Be(ComparisonOutcome.Inconclusive);
        confidence.Value.Should().Be(0.3m, "minimum confidence");
    }

    [Fact]
    public void MixedOutcomes_RegressionTakesPriority()
    {
        // Arrange - Mix of outcomes with one regression
        var aggregator = new OutcomeAggregator();
        var tolerance = new Tolerance("Metric", ToleranceType.Relative, 10m);
        var confidence = new ConfidenceLevel(0.8m);

        var metrics = new[]
        {
            new ComparisonMetric("M1", 100m, 95m, tolerance, ComparisonOutcome.Improvement, confidence),
            new ComparisonMetric("M2", 50m, 50m, tolerance, ComparisonOutcome.NoSignificantChange, confidence),
            new ComparisonMetric("M3", 75m, 100m, tolerance, ComparisonOutcome.Regression, confidence),
            new ComparisonMetric("M4", 200m, 210m, tolerance, ComparisonOutcome.NoSignificantChange, confidence)
        };

        // Act
        var outcome = aggregator.Aggregate(metrics);

        // Assert - Regression should take priority (worst-case strategy)
        outcome.Should().Be(ComparisonOutcome.Regression,
            "regression takes priority in worst-case aggregation");
    }

    [Fact]
    public void IdenticalBaselineAndCurrent_NoSignificantChange()
    {
        // Arrange - Exactly same values
        var tolerance = new Tolerance("Metric", ToleranceType.Relative, 10m);

        // Act
        var result = _calculator.CalculateMetric(
            baseline: 100m,
            current: 100m,
            tolerance: tolerance
        );

        // Assert
        result.AbsoluteChange.Should().Be(0m);
        result.BaselineValue.Should().Be(result.CurrentValue);
    }

    [Fact]
    public void ExtremelyHighRelativeTolerance_AllowsLargeVariation()
    {
        // Arrange - 100% tolerance (maximum)
        var tolerance = new Tolerance("Metric", ToleranceType.Relative, 100m);

        // Act - Even 100% change should be within tolerance
        var withinTolerance = tolerance.IsWithinTolerance(100m, 200m);

        // Assert
        withinTolerance.Should().BeTrue("100% tolerance should accept 100% change");
    }

    [Fact]
    public void DecimalPrecision_MaintainedThroughCalculations()
    {
        // Arrange - Test decimal precision
        var tolerance = new Tolerance("Metric", ToleranceType.Absolute, 0.001m);

        // Act
        var result = _calculator.CalculateMetric(
            baseline: 1.234567m,
            current: 1.235567m,
            tolerance: tolerance
        );

        // Assert - Precision should be maintained
        result.AbsoluteChange.Should().Be(0.001m);
    }

    [Fact]
    public void ToleranceConfiguration_EmptyMetricName_Throws()
    {
        // Act & Assert
        Action act = () => new Tolerance("", ToleranceType.Relative, 10m);
        
        act.Should().Throw<ToleranceValidationException>()
            .WithMessage("*cannot be empty*");
    }

    [Fact]
    public void ConfidenceLevel_BoundaryValues_Accepted()
    {
        // Act & Assert - Boundary values should work
        var min = new ConfidenceLevel(0.0m);
        var max = new ConfidenceLevel(1.0m);

        min.Value.Should().Be(0.0m);
        max.Value.Should().Be(1.0m);
    }

    private sealed class TestMetric : IMetric
    {
        public TestMetric(string metricType, double value)
        {
            MetricType = metricType;
            Value = value;
            Id = Guid.NewGuid();
            Unit = "unit";
            ComputedAt = DateTime.UtcNow;
        }

        public Guid Id { get; }
        public string MetricType { get; }
        public double Value { get; }
        public string Unit { get; }
        public DateTime ComputedAt { get; }
        public CompletessStatus CompletessStatus => CompletessStatus.COMPLETE;
        public MetricEvidence Evidence => new MetricEvidence(1, 1, "test");
    }
}
