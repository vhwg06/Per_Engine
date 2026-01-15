namespace PerformanceEngine.Metrics.Domain.Tests.Determinism;

using PerformanceEngine.Metrics.Domain.Metrics;
using System.Text.Json;
using Xunit;

/// <summary>
/// Determinism verification tests for Metrics Domain enrichment.
/// Verifies that identical inputs produce consistent outputs across iterations.
/// </summary>
public class MetricDeterminismTests
{
    private static readonly JsonSerializerOptions CanonicalOptions = new()
    {
        WriteIndented = false,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    [Fact]
    public void MetricEvidence_Determinism_100Iterations()
    {
        var evidence = new MetricEvidence(
            sampleCount: 100,
            requiredSampleCount: 100,
            aggregationWindow: "5m");

        var json1 = JsonSerializer.Serialize(evidence, CanonicalOptions);
        
        for (int i = 0; i < 100; i++)
        {
            var jsonN = JsonSerializer.Serialize(evidence, CanonicalOptions);
            Assert.True(json1 == jsonN, "JSON should be identical across iterations");
        }
    }

    [Fact]
    public void CompletessStatus_Determinism_AllValues()
    {
        var statusComplete = CompletessStatus.COMPLETE;
        var statusPartial = CompletessStatus.PARTIAL;

        var jsonComplete1 = JsonSerializer.Serialize(statusComplete, CanonicalOptions);
        var jsonPartial1 = JsonSerializer.Serialize(statusPartial, CanonicalOptions);

        for (int i = 0; i < 50; i++)
        {
            var jsonCompleteN = JsonSerializer.Serialize(statusComplete, CanonicalOptions);
            var jsonPartialN = JsonSerializer.Serialize(statusPartial, CanonicalOptions);
            
            Assert.True(jsonComplete1 == jsonCompleteN);
            Assert.True(jsonPartial1 == jsonPartialN);
        }

        // Different statuses should have different JSON
        Assert.True(jsonComplete1 != jsonPartial1);
    }

    [Fact]
    public void MetricEvidence_DifferentValues_ProduceDifferentJson()
    {
        var evidence1 = new MetricEvidence(50, 100, "5m");
        var evidence2 = new MetricEvidence(100, 100, "5m");
        var evidence3 = new MetricEvidence(100, 150, "5m");
        var evidence4 = new MetricEvidence(100, 100, "10m");

        var json1 = JsonSerializer.Serialize(evidence1, CanonicalOptions);
        var json2 = JsonSerializer.Serialize(evidence2, CanonicalOptions);
        var json3 = JsonSerializer.Serialize(evidence3, CanonicalOptions);
        var json4 = JsonSerializer.Serialize(evidence4, CanonicalOptions);

        Assert.True(json1 != json2, "Different sample counts should produce different JSON");
        Assert.True(json2 != json3, "Different required counts should produce different JSON");
        Assert.True(json2 != json4, "Different windows should produce different JSON");
    }

    [Fact]
    public void MetricEvidence_BoundaryValues_Deterministic()
    {
        var boundaries = new[]
        {
            new MetricEvidence(0, 1, "1s"),
            new MetricEvidence(int.MaxValue, int.MaxValue, "max"),
            new MetricEvidence(1, 1, "1m"),
        };

        foreach (var evidence in boundaries)
        {
            var json1 = JsonSerializer.Serialize(evidence, CanonicalOptions);
            var json2 = JsonSerializer.Serialize(evidence, CanonicalOptions);
            
            Assert.True(json1 == json2, "Boundary values should serialize deterministically");
        }
    }

    [Fact]
    public void MetricEvidence_IsComplete_ConsistentAcrossIterations()
    {
        var evidence = new MetricEvidence(75, 100, "5m");
        var isComplete = evidence.IsComplete;

        for (int i = 0; i < 100; i++)
        {
            Assert.True(isComplete == evidence.IsComplete, "IsComplete should be consistent");
        }
    }

    [Fact]
    public void Metric_Create_DeterministicEvidence()
    {
        var json1 = GetMetricJson("latency", 100, 100, "5m");
        
        for (int i = 0; i < 20; i++)
        {
            var jsonN = GetMetricJson("latency", 100, 100, "5m");
            // Note: We can't compare full JSON because Guid.Id is different each time
            // Instead, we verify the evidence portion
            var obj1 = JsonSerializer.Deserialize<MetricSerializableView>(json1);
            var objN = JsonSerializer.Deserialize<MetricSerializableView>(jsonN);
            
            Assert.Equal(obj1!.Evidence.SampleCount, objN!.Evidence.SampleCount);
            Assert.Equal(obj1!.Evidence.RequiredSampleCount, objN!.Evidence.RequiredSampleCount);
            Assert.Equal(obj1!.Evidence.AggregationWindow, objN!.Evidence.AggregationWindow);
            Assert.Equal(obj1!.CompletessStatus, objN!.CompletessStatus);
        }
    }

    [Fact]
    public void MetricEvidence_ToString_Consistent()
    {
        var evidence = new MetricEvidence(95, 100, "5m");
        var str1 = evidence.ToString();

        for (int i = 0; i < 50; i++)
        {
            var strN = evidence.ToString();
            Assert.Equal(str1, strN);
        }
    }

    private string GetMetricJson(string metricType, int sampleCount, int requiredCount, string window)
    {
        // Create sample collection with enough samples
        var samples = new List<Sample>();
        for (int i = 0; i < Math.Max(sampleCount, 1); i++)
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

        var metric = Metric.Create(
            samples: sampleCollection,
            window: AggregationWindow.FullExecution(),
            metricType: metricType,
            sampleCount: sampleCount,
            requiredSampleCount: requiredCount,
            aggregationWindow: window);

        return JsonSerializer.Serialize(metric, CanonicalOptions);
    }

    // Helper class for deserializing metric JSON
    private class MetricSerializableView
    {
        public string MetricType { get; set; } = "";
        public string CompletessStatus { get; set; } = "";
        public EvidenceView Evidence { get; set; } = new();
    }

    private class EvidenceView
    {
        public int SampleCount { get; set; }
        public int RequiredSampleCount { get; set; }
        public string AggregationWindow { get; set; } = "";
    }
}
