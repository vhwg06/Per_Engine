using PerformanceEngine.Metrics.Domain.Metrics;

namespace PerformanceEngine.Evaluation.Domain.Tests.Domain.Rules;

/// <summary>
/// Test helpers for creating test metrics with aggregations.
/// </summary>
public static class TestMetricFactory
{
    private static readonly Metrics.Domain.Metrics.ExecutionContext _context = new(
        engineName: "test-engine",
        executionId: Guid.NewGuid(),
        scenarioName: "test-scenario"
    );

    /// <summary>
    /// Creates a simple metric with specified aggregation results.
    /// </summary>
    public static Metric CreateMetricWithAggregations(
        string metricType,
        Dictionary<string, double> aggregations)
    {
        // Create a sample to satisfy metric requirements
        var sample = new Sample(
            timestamp: DateTime.UtcNow.AddSeconds(-1),
            duration: new Latency(100, LatencyUnit.Milliseconds),
            status: SampleStatus.Success,
            errorClassification: null,
            executionContext: _context
        );

        var sampleCollection = SampleCollection.Create(new[] { sample });
        var window = AggregationWindow.FullExecution();

        var aggregationResults = aggregations.Select(kvp =>
        {
            // Latency value objects disallow negatives; clamp for test scenarios that use generic metrics
            var safeValue = Math.Max(kvp.Value, 0);
            return new AggregationResult(
                value: new Latency(safeValue, LatencyUnit.Milliseconds),
                operationName: kvp.Key,
                computedAt: DateTime.UtcNow
            );
        }).ToList();

        return new Metric(
            samples: sampleCollection,
            window: window,
            metricType: metricType,
            aggregatedValues: aggregationResults,
            computedAt: DateTime.UtcNow
        );
    }

    /// <summary>
    /// Creates a metric with a single aggregation (convenience method).
    /// </summary>
    public static Metric CreateMetric(
        string aggregationName,
        double aggregationValue,
        string metricType = "TestMetric")
    {
        return CreateMetricWithAggregations(
            metricType,
            new Dictionary<string, double> { { aggregationName, aggregationValue } }
        );
    }

    /// <summary>
    /// Creates a metric with multiple aggregations (for batch testing).
    /// </summary>
    public static Metric CreateMetricWithMultipleAggregations(
        Dictionary<string, double> aggregations,
        string metricType = "TestMetric")
    {
        return CreateMetricWithAggregations(metricType, aggregations);
    }
}
