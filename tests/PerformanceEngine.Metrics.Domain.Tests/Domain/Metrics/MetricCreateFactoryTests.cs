namespace PerformanceEngine.Metrics.Domain.Tests.Domain.Metrics;

using PerformanceEngine.Metrics.Domain.Metrics;
using Xunit;

/// <summary>
/// Unit tests for Metric.Create() factory method.
/// Verifies completeness determination, evidence creation, and factory logic.
/// </summary>
public class MetricCreateFactoryTests
{
    private SampleCollection CreateSampleCollection(int count)
    {
        if (count <= 0)
            count = 1;

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

    [Fact]
    public void Create_AllRequiredSamples_ReturnsCompleteMetric()
    {
        var samples = CreateSampleCollection(100);
        var window = AggregationWindow.FullExecution();

        var metric = Metric.Create(
            samples: samples,
            window: window,
            metricType: "latency",
            sampleCount: 100,
            requiredSampleCount: 100,
            aggregationWindow: "5m");

        Assert.Equal(CompletessStatus.COMPLETE, metric.CompletessStatus);
        Assert.True(metric.Evidence.IsComplete);
        Assert.Equal(100, metric.Evidence.SampleCount);
    }

    [Fact]
    public void Create_MoreThanRequiredSamples_ReturnsCompleteMetric()
    {
        var samples = CreateSampleCollection(150);
        var window = AggregationWindow.FullExecution();

        var metric = Metric.Create(
            samples: samples,
            window: window,
            metricType: "latency",
            sampleCount: 150,
            requiredSampleCount: 100,
            aggregationWindow: "5m");

        Assert.Equal(CompletessStatus.COMPLETE, metric.CompletessStatus);
        Assert.True(metric.Evidence.IsComplete);
    }

    [Fact]
    public void Create_FewerThanRequiredSamples_ReturnsPartialMetric()
    {
        var samples = CreateSampleCollection(50);
        var window = AggregationWindow.FullExecution();

        var metric = Metric.Create(
            samples: samples,
            window: window,
            metricType: "latency",
            sampleCount: 50,
            requiredSampleCount: 100,
            aggregationWindow: "5m");

        Assert.Equal(CompletessStatus.PARTIAL, metric.CompletessStatus);
        Assert.False(metric.Evidence.IsComplete);
        Assert.Equal(50, metric.Evidence.SampleCount);
    }

    [Fact]
    public void Create_WithOverrideStatus_UsesProvidedStatus()
    {
        var samples = CreateSampleCollection(150);
        var window = AggregationWindow.FullExecution();

        var metric = Metric.Create(
            samples: samples,
            window: window,
            metricType: "latency",
            sampleCount: 150,
            requiredSampleCount: 100,
            aggregationWindow: "5m",
            overrideStatus: CompletessStatus.PARTIAL);

        Assert.Equal(CompletessStatus.PARTIAL, metric.CompletessStatus);
    }

    [Fact]
    public void Create_EvidencePopulatedCorrectly()
    {
        var samples = CreateSampleCollection(75);
        var window = AggregationWindow.FullExecution();

        var metric = Metric.Create(
            samples: samples,
            window: window,
            metricType: "latency",
            sampleCount: 75,
            requiredSampleCount: 100,
            aggregationWindow: "10m");

        Assert.Equal(75, metric.Evidence.SampleCount);
        Assert.Equal(100, metric.Evidence.RequiredSampleCount);
        Assert.Equal("10m", metric.Evidence.AggregationWindow);
    }

    [Fact]
    public void Create_MetricPropertiesPreserved()
    {
        var samples = CreateSampleCollection(100);
        var window = AggregationWindow.FullExecution();
        var computedAt = DateTime.UtcNow.AddMinutes(-5);

        var metric = Metric.Create(
            samples: samples,
            window: window,
            metricType: "throughput",
            sampleCount: 100,
            requiredSampleCount: 100,
            aggregationWindow: "5m",
            computedAt: computedAt);

        Assert.Equal("throughput", metric.MetricType);
        Assert.Equal(computedAt, metric.ComputedAt);
        Assert.NotEqual(Guid.Empty, metric.Id);
    }

    [Fact]
    public void Create_ZeroSampleCount_ReturnsPartial()
    {
        var samples = CreateSampleCollection(1);
        var window = AggregationWindow.FullExecution();

        var metric = Metric.Create(
            samples: samples,
            window: window,
            metricType: "latency",
            sampleCount: 0,
            requiredSampleCount: 100,
            aggregationWindow: "5m");

        Assert.Equal(CompletessStatus.PARTIAL, metric.CompletessStatus);
        Assert.Equal(0, metric.Evidence.SampleCount);
    }

    [Fact]
    public void Create_CompletessStatusConsistency_AgreedWithEvidence()
    {
        var samples = CreateSampleCollection(80);
        var window = AggregationWindow.FullExecution();

        var metric = Metric.Create(
            samples: samples,
            window: window,
            metricType: "latency",
            sampleCount: 80,
            requiredSampleCount: 100,
            aggregationWindow: "5m");

        // Status should match evidence.IsComplete
        if (metric.Evidence.IsComplete)
        {
            Assert.Equal(CompletessStatus.COMPLETE, metric.CompletessStatus);
        }
        else
        {
            // For this test, since 80 < 100, evidence.IsComplete is false
            Assert.False(metric.Evidence.IsComplete);
            Assert.Equal(CompletessStatus.PARTIAL, metric.CompletessStatus);
        }
    }

    [Fact]
    public void Create_InvalidParameters_ThrowsException()
    {
        var window = AggregationWindow.FullExecution();

        // Should handle null samples
        Assert.Throws<ArgumentNullException>(() =>
            Metric.Create(
                samples: null!,
                window: window,
                metricType: "latency",
                sampleCount: 10,
                requiredSampleCount: 100,
                aggregationWindow: "5m"));
    }
}

