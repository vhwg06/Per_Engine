namespace PerformanceEngine.Tests.Fixtures;

using PerformanceEngine.Metrics.Domain.Metrics;

/// <summary>
/// Shared test fixtures for Metrics Domain test doubles and sample data.
/// </summary>
public static class MetricsFixtures
{
    /// <summary>
    /// Creates a SampleCollection with the specified number of samples.
    /// Each sample has a sequential value starting from 100.0ms.
    /// </summary>
    public static SampleCollection CreateSampleCollection(int count = 100)
    {
        if (count <= 0)
        {
            count = 1; // Minimum 1 sample to satisfy Metric invariants
        }

        var samples = new List<Sample>();
        for (int i = 0; i < count; i++)
        {
            var context = new ExecutionContext(
                engineName: "test-engine",
                executionId: Guid.NewGuid());

            samples.Add(new Sample(
                timestamp: DateTime.UtcNow.AddSeconds(-i),
                duration: new Latency(100.0 + i, LatencyUnit.Milliseconds),
                status: SampleStatus.Success,
                errorClassification: null,
                executionContext: context));
        }

        return SampleCollection.Create(samples);
    }

    /// <summary>
    /// Creates a Metric with standard test values.
    /// </summary>
    public static Metric CreateTestMetric(
        int sampleCount = 100,
        int requiredCount = 100,
        string metricType = "latency",
        string aggregationWindow = "5m")
    {
        return Metric.Create(
            samples: CreateSampleCollection(sampleCount),
            window: AggregationWindow.FullExecution(),
            metricType: metricType,
            sampleCount: sampleCount,
            requiredSampleCount: requiredCount,
            aggregationWindow: aggregationWindow);
    }
}


