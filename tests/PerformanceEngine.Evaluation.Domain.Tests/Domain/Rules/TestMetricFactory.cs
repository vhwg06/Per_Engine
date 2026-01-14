using PerformanceEngine.Metrics.Domain.Metrics;

namespace PerformanceEngine.Evaluation.Domain.Tests.Domain.Rules;

/// <summary>
/// Test helpers for creating test metrics with aggregations.
/// </summary>
public static class TestMetricFactory
{
    /// <summary>
    /// Creates a simple metric with specified aggregation results.
    /// </summary>
    public static Metric CreateMetricWithAggregations(
        string metricType,
        params (string operationName, double valueMs)[] aggregations)
    {
        // Create a sample to satisfy metric requirements
        var sample = new Sample(
            latency: new Latency(100, LatencyUnit.Milliseconds),
            status: SampleStatus.Success,
            timestamp: DateTime.UtcNow,
            executionContext: new ExecutionContext("test", "test", "test", "test")
        );

        var sampleCollection = new SampleCollection(new[] { sample });
        var window = new AggregationWindow(DateTime.UtcNow.AddMinutes(-5), DateTime.UtcNow);

        var aggregationResults = aggregations.Select(a =>
            new AggregationResult(
                value: new Latency(a.valueMs, LatencyUnit.Milliseconds),
                operationName: a.operationName,
                computedAt: DateTime.UtcNow
            )
        ).ToList();

        return new Metric(
            samples: sampleCollection,
            window: window,
            metricType: metricType,
            aggregatedValues: aggregationResults,
            computedAt: DateTime.UtcNow
        );
    }
}
