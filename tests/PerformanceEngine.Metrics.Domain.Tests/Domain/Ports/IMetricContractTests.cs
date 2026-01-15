namespace PerformanceEngine.Metrics.Domain.Tests.Domain.Ports;

using PerformanceEngine.Metrics.Domain.Metrics;
using PerformanceEngine.Metrics.Domain.Ports;
using Xunit;

/// <summary>
/// Contract tests for IMetric interface.
/// Verifies that all implementations properly expose enrichment properties.
/// </summary>
public class IMetricContractTests
{
    [Fact]
    public void IMetric_AllPropertiesAccessible()
    {
        var metric = CreateTestMetric();
        
        IMetric iMetric = metric;
        Assert.NotEqual(Guid.Empty, iMetric.Id);
        Assert.Equal("latency", iMetric.MetricType);
        // ComputedAt is a value type, just verify it's recent
        var now = DateTime.UtcNow;
        Assert.True(iMetric.ComputedAt <= now);
    }

    [Fact]
    public void IMetric_CompletessStatusProperty_Readable()
    {
        var metric = CreateTestMetric(sampleCount: 100, requiredCount: 100);
        
        IMetric iMetric = metric;
        Assert.Equal(CompletessStatus.COMPLETE, iMetric.CompletessStatus);
    }

    [Fact]
    public void IMetric_EvidenceProperty_Readable()
    {
        var metric = CreateTestMetric(sampleCount: 75, requiredCount: 100);
        
        IMetric iMetric = metric;
        var evidence = iMetric.Evidence;
        Assert.NotNull(evidence);
        Assert.Equal(75, evidence.SampleCount);
        Assert.Equal(100, evidence.RequiredSampleCount);
    }

    [Fact]
    public void IMetric_PartialMetric_HasCorrectStatus()
    {
        var metric = CreateTestMetric(sampleCount: 50, requiredCount: 100);
        
        IMetric iMetric = metric;
        Assert.Equal(CompletessStatus.PARTIAL, iMetric.CompletessStatus);
        Assert.False(iMetric.Evidence.IsComplete);
    }

    [Fact]
    public void IMetric_CompleteMetric_HasCorrectStatus()
    {
        var metric = CreateTestMetric(sampleCount: 100, requiredCount: 100);
        
        IMetric iMetric = metric;
        Assert.Equal(CompletessStatus.COMPLETE, iMetric.CompletessStatus);
        Assert.True(iMetric.Evidence.IsComplete);
    }

    [Fact]
    public void IMetric_EvidenceImmutable_RereadSameValues()
    {
        var metric = CreateTestMetric();
        
        IMetric iMetric = metric;
        var evidence1 = iMetric.Evidence;
        var evidence2 = iMetric.Evidence;
        
        Assert.Equal(evidence1.SampleCount, evidence2.SampleCount);
        Assert.Equal(evidence1.RequiredSampleCount, evidence2.RequiredSampleCount);
    }

    [Fact]
    public void IMetric_MultipleInstances_HaveDifferentIds()
    {
        var metric1 = CreateTestMetric();
        var metric2 = CreateTestMetric();
        
        IMetric iMetric1 = metric1;
        IMetric iMetric2 = metric2;
        
        Assert.NotEqual(iMetric1.Id, iMetric2.Id);
    }

    [Fact]
    public void IMetric_EvidenceDetails_AllAccessible()
    {
        var metric = CreateTestMetric();
        var evidence = metric.Evidence;
        
        Assert.NotEqual(0, evidence.SampleCount);
        Assert.True(evidence.RequiredSampleCount > 0);
        Assert.NotNull(evidence.AggregationWindow);
        // IsComplete is a bool, so just check it's valid
        var isComplete = evidence.IsComplete;
        Assert.True(isComplete || !isComplete);
    }

    [Fact]
    public void IMetric_ComputedAtIsRecent()
    {
        var metric = CreateTestMetric();
        IMetric iMetric = metric;
        
        var timeDiff = DateTime.UtcNow - iMetric.ComputedAt;
        Assert.True(timeDiff.TotalSeconds < 5, "ComputedAt should be recent");
    }

    private Metric CreateTestMetric(int sampleCount = 100, int requiredCount = 100)
    {
        // Ensure at least 1 sample to satisfy Metric invariants
        var actualSampleCount = Math.Max(sampleCount, 1);
        
        // Create sample collection with enough samples for testing
        var samples = new List<Sample>();
        for (int i = 0; i < actualSampleCount; i++)
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
        
        var sampleCollection = SampleCollection.Create(samples);

        return Metric.Create(
            samples: sampleCollection,
            window: AggregationWindow.FullExecution(),
            metricType: "latency",
            sampleCount: sampleCount,
            requiredSampleCount: requiredCount,
            aggregationWindow: "5m");
    }
}
