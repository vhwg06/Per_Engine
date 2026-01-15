namespace PerformanceEngine.Metrics.Domain.Tests.Domain.Metrics;

using PerformanceEngine.Metrics.Domain.Metrics;
using Xunit;

/// <summary>
/// Unit tests for MetricEvidence value object.
/// Verifies invariants, computed properties, and immutability.
/// </summary>
public class MetricEvidenceTests
{
    [Fact]
    public void Constructor_ValidParameters_CreatesInstance()
    {
        var evidence = new MetricEvidence(sampleCount: 50, requiredSampleCount: 100, aggregationWindow: "5m");

        Assert.Equal(50, evidence.SampleCount);
        Assert.Equal(100, evidence.RequiredSampleCount);
        Assert.Equal("5m", evidence.AggregationWindow);
    }

    [Fact]
    public void IsComplete_SampleCountGreaterThanRequired_ReturnsTrue()
    {
        var evidence = new MetricEvidence(sampleCount: 150, requiredSampleCount: 100, aggregationWindow: "5m");
        Assert.True(evidence.IsComplete);
    }

    [Fact]
    public void IsComplete_SampleCountEqualToRequired_ReturnsTrue()
    {
        var evidence = new MetricEvidence(sampleCount: 100, requiredSampleCount: 100, aggregationWindow: "5m");
        Assert.True(evidence.IsComplete);
    }

    [Fact]
    public void IsComplete_SampleCountLessThanRequired_ReturnsFalse()
    {
        var evidence = new MetricEvidence(sampleCount: 50, requiredSampleCount: 100, aggregationWindow: "5m");
        Assert.False(evidence.IsComplete);
    }

    [Fact]
    public void Constructor_NegativeSampleCount_ThrowsArgumentException()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            new MetricEvidence(sampleCount: -1, requiredSampleCount: 100, aggregationWindow: "5m"));
        Assert.Contains("non-negative", ex.Message);
    }

    [Fact]
    public void Constructor_ZeroRequiredSampleCount_ThrowsArgumentException()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            new MetricEvidence(sampleCount: 50, requiredSampleCount: 0, aggregationWindow: "5m"));
        Assert.Contains("positive", ex.Message);
    }

    [Fact]
    public void Constructor_NegativeRequiredSampleCount_ThrowsArgumentException()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            new MetricEvidence(sampleCount: 50, requiredSampleCount: -1, aggregationWindow: "5m"));
        Assert.Contains("positive", ex.Message);
    }

    [Fact]
    public void Constructor_NullAggregationWindow_ThrowsArgumentException()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            new MetricEvidence(sampleCount: 50, requiredSampleCount: 100, aggregationWindow: null!));
        Assert.Contains("not be empty", ex.Message);
    }

    [Fact]
    public void Constructor_EmptyAggregationWindow_ThrowsArgumentException()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            new MetricEvidence(sampleCount: 50, requiredSampleCount: 100, aggregationWindow: ""));
        Assert.Contains("not be empty", ex.Message);
    }

    [Fact]
    public void Constructor_WhitespaceAggregationWindow_TrimsAndSucceeds()
    {
        var evidence = new MetricEvidence(sampleCount: 50, requiredSampleCount: 100, aggregationWindow: "  5m  ");
        Assert.Equal("5m", evidence.AggregationWindow);
    }

    [Fact]
    public void Equality_IdenticalValues_ReturnsEqual()
    {
        var evidence1 = new MetricEvidence(sampleCount: 50, requiredSampleCount: 100, aggregationWindow: "5m");
        var evidence2 = new MetricEvidence(sampleCount: 50, requiredSampleCount: 100, aggregationWindow: "5m");

        Assert.Equal(evidence1, evidence2);
    }

    [Fact]
    public void Equality_DifferentSampleCount_NotEqual()
    {
        var evidence1 = new MetricEvidence(sampleCount: 50, requiredSampleCount: 100, aggregationWindow: "5m");
        var evidence2 = new MetricEvidence(sampleCount: 60, requiredSampleCount: 100, aggregationWindow: "5m");

        Assert.NotEqual(evidence1, evidence2);
    }

    [Fact]
    public void Equality_DifferentRequiredSampleCount_NotEqual()
    {
        var evidence1 = new MetricEvidence(sampleCount: 50, requiredSampleCount: 100, aggregationWindow: "5m");
        var evidence2 = new MetricEvidence(sampleCount: 50, requiredSampleCount: 110, aggregationWindow: "5m");

        Assert.NotEqual(evidence1, evidence2);
    }

    [Fact]
    public void Equality_DifferentAggregationWindow_NotEqual()
    {
        var evidence1 = new MetricEvidence(sampleCount: 50, requiredSampleCount: 100, aggregationWindow: "5m");
        var evidence2 = new MetricEvidence(sampleCount: 50, requiredSampleCount: 100, aggregationWindow: "1h");

        Assert.NotEqual(evidence1, evidence2);
    }

    [Fact]
    public void ToString_ProducesReadableString()
    {
        var evidence = new MetricEvidence(sampleCount: 50, requiredSampleCount: 100, aggregationWindow: "5m");
        var str = evidence.ToString();

        Assert.Contains("50", str);
        Assert.Contains("100", str);
        Assert.Contains("5m", str);
        Assert.Contains("MetricEvidence", str);
    }

    [Fact]
    public void ZeroSampleCount_StillValid()
    {
        var evidence = new MetricEvidence(sampleCount: 0, requiredSampleCount: 100, aggregationWindow: "5m");
        Assert.Equal(0, evidence.SampleCount);
        Assert.False(evidence.IsComplete);
    }

    [Fact]
    public void LargeSampleCounts_HandledCorrectly()
    {
        var evidence = new MetricEvidence(sampleCount: 1_000_000, requiredSampleCount: 1_000_000, aggregationWindow: "1d");
        Assert.True(evidence.IsComplete);
    }
}
