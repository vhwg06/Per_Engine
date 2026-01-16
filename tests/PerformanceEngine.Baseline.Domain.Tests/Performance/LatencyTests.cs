namespace PerformanceEngine.Baseline.Domain.Tests.Performance;

using System.Diagnostics;
using FluentAssertions;
using PerformanceEngine.Baseline.Domain.Domain.Baselines;
using PerformanceEngine.Baseline.Domain.Domain.Comparisons;
using PerformanceEngine.Baseline.Domain.Domain.Confidence;
using PerformanceEngine.Baseline.Domain.Domain.Tolerances;
using PerformanceEngine.Metrics.Domain.Metrics;
using PerformanceEngine.Metrics.Domain.Ports;
using Xunit;

/// <summary>
/// Phase 7 Performance Test - T061
/// Validates comparison latency and concurrent execution performance.
/// Target: All comparisons < 100ms (SC-002), p95 < 20ms
/// </summary>
public sealed class LatencyTests
{
    private readonly ComparisonCalculator _calculator = new();

    [Fact]
    public void ComparisonLatency_SingleComparison_IsUnder20Milliseconds_P95()
    {
        // Arrange
        var tolerance = new Tolerance("ResponseTime", ToleranceType.Relative, 10m);
        var iterations = 1000;
        var latencies = new List<long>();

        // Act - Run 1000 comparisons to establish p95
        for (int i = 0; i < iterations; i++)
        {
            var stopwatch = Stopwatch.StartNew();
            
            var result = _calculator.CalculateMetric(
                baseline: 100m,
                current: 115m,
                tolerance: tolerance
            );
            
            stopwatch.Stop();
            latencies.Add(stopwatch.ElapsedMilliseconds);
        }

        // Assert - p95 should be under 20ms
        latencies.Sort();
        var p95Index = (int)(iterations * 0.95);
        var p95Latency = latencies[p95Index];
        
        p95Latency.Should().BeLessThan(20, "p95 comparison latency must be under 20ms");
        
        // Additional checks
        var averageLatency = latencies.Average();
        averageLatency.Should().BeLessThan(10, "average latency should be well under target");
    }

    [Fact]
    public void ComparisonLatency_MultipleMetrics_IsUnder100Milliseconds()
    {
        // Arrange - Test with multiple metrics (realistic scenario)
        var tolerances = new[]
        {
            new Tolerance("ResponseTime", ToleranceType.Relative, 10m),
            new Tolerance("Throughput", ToleranceType.Relative, 5m),
            new Tolerance("ErrorRate", ToleranceType.Absolute, 1m),
            new Tolerance("CPUUsage", ToleranceType.Relative, 15m),
            new Tolerance("MemoryUsage", ToleranceType.Relative, 20m)
        };

        var baselineValues = new[] { 100m, 1000m, 2m, 45m, 512m };
        var currentValues = new[] { 115m, 950m, 2.5m, 52m, 550m };

        // Act
        var stopwatch = Stopwatch.StartNew();
        
        for (int i = 0; i < tolerances.Length; i++)
        {
            var result = _calculator.CalculateMetric(
                baseline: baselineValues[i],
                current: currentValues[i],
                tolerance: tolerances[i]
            );
        }
        
        stopwatch.Stop();

        // Assert - Total time for 5 metrics should be well under 100ms
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(100, 
            "multi-metric comparison must complete in under 100ms (SC-002)");
    }

    [Fact]
    public void ConcurrentComparisons_100Parallel_CompleteWithoutError()
    {
        // Arrange
        var tolerance = new Tolerance("Metric", ToleranceType.Relative, 10m);
        var concurrentTasks = 100;
        var tasks = new List<Task<ComparisonMetric>>();

        // Act - Launch 100 concurrent comparisons
        var stopwatch = Stopwatch.StartNew();
        
        for (int i = 0; i < concurrentTasks; i++)
        {
            var taskId = i;
            var task = Task.Run(() => _calculator.CalculateMetric(
                baseline: 100m + taskId,
                current: 120m + taskId,
                tolerance: tolerance
            ));
            tasks.Add(task);
        }

        Task.WaitAll(tasks.ToArray());
        stopwatch.Stop();

        // Assert - All tasks completed successfully
        tasks.Should().HaveCount(concurrentTasks);
        tasks.Should().OnlyContain(t => t.IsCompletedSuccessfully);
        
        // Verify all results are valid
        foreach (var task in tasks)
        {
            var result = task.Result;
            result.Should().NotBeNull();
            result.Outcome.Should().NotBe(default(ComparisonOutcome));
            result.Confidence.Should().NotBeNull();
        }

        // Performance check - 100 concurrent comparisons should complete quickly
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(1000,
            "100 concurrent comparisons should complete within 1 second");
    }

    [Fact]
    public void MemoryUsage_RepeatedComparisons_NoMemoryLeak()
    {
        // Arrange
        var tolerance = new Tolerance("Metric", ToleranceType.Relative, 10m);
        var iterations = 10000;

        // Act - Force GC before measurement
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        
        var memoryBefore = GC.GetTotalMemory(false);

        for (int i = 0; i < iterations; i++)
        {
            var result = _calculator.CalculateMetric(
                baseline: 100m,
                current: 115m,
                tolerance: tolerance
            );
        }

        // Force GC after operations
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        
        var memoryAfter = GC.GetTotalMemory(false);

        // Assert - Memory growth should be minimal (< 10MB for 10k operations)
        var memoryGrowth = memoryAfter - memoryBefore;
        memoryGrowth.Should().BeLessThan(10 * 1024 * 1024,
            "repeated comparisons should not cause significant memory growth");
    }

    [Fact]
    public void BaselineCreation_WithMultipleMetrics_IsEfficient()
    {
        // Arrange
        var metrics = new List<IMetric>();
        for (int i = 0; i < 50; i++)
        {
            metrics.Add(new TestMetric($"Metric{i}", 100.0 + i));
        }

        var tolerances = metrics.Select(m => 
            new Tolerance(m.MetricType, ToleranceType.Relative, 10m)
        ).ToArray();
        var config = new ToleranceConfiguration(tolerances);

        // Act
        var stopwatch = Stopwatch.StartNew();
        var baseline = new Baseline(metrics, config);
        stopwatch.Stop();

        // Assert - Baseline creation should be fast even with many metrics
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(50,
            "baseline creation with 50 metrics should be fast");
        
        baseline.Metrics.Should().HaveCount(50);
    }

    [Fact]
    public void ComparisonResult_Aggregation_IsEfficient()
    {
        // Arrange - Create many comparison metrics
        var tolerance = new Tolerance("Metric", ToleranceType.Relative, 10m);
        var metrics = new List<ComparisonMetric>();
        
        for (int i = 0; i < 100; i++)
        {
            var outcome = i % 3 == 0 ? ComparisonOutcome.Regression :
                         i % 3 == 1 ? ComparisonOutcome.Improvement :
                         ComparisonOutcome.NoSignificantChange;
            
            var metric = new ComparisonMetric(
                $"Metric{i}",
                100m + i,
                110m + i,
                tolerance,
                outcome,
                new ConfidenceLevel(0.8m)
            );
            metrics.Add(metric);
        }

        var baselineId = new BaselineId();
        var aggregator = new OutcomeAggregator();

        // Act
        var stopwatch = Stopwatch.StartNew();
        
        var overallOutcome = aggregator.Aggregate(metrics);
        var overallConfidence = aggregator.AggregateConfidence(metrics);
        
        var result = new ComparisonResult(
            baselineId,
            metrics,
            overallOutcome,
            overallConfidence
        );
        
        stopwatch.Stop();

        // Assert - Aggregation should be fast even with 100 metrics
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(50,
            "aggregation of 100 metrics should complete quickly");
        
        result.MetricResults.Should().HaveCount(100);
        result.OverallOutcome.Should().Be(ComparisonOutcome.Regression);
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
