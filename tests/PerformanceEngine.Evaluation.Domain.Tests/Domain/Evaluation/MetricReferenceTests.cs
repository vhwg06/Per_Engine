namespace PerformanceEngine.Evaluation.Domain.Tests.Domain.Evaluation;

using PerformanceEngine.Evaluation.Domain.Domain.Evaluation;
using PerformanceEngine.Metrics.Domain.Metrics;
using Xunit;

/// <summary>
/// Unit tests for MetricReference value object.
/// Verifies invariants, immutability, and equality-based comparison.
/// </summary>
public class MetricReferenceTests
{
    [Fact]
    public void Constructor_ValidParameters_CreatesInstance()
    {
        var reference = new MetricReference(
            aggregationName: "p95-latency",
            value: 250.5,
            unit: "ms",
            completessStatus: CompletessStatus.COMPLETE);

        Assert.Equal("p95-latency", reference.AggregationName);
        Assert.Equal(250.5, reference.Value);
        Assert.Equal("ms", reference.Unit);
        Assert.Equal(CompletessStatus.COMPLETE, reference.CompletessStatus);
    }

    [Fact]
    public void Constructor_TrimsFrontAndTrailingWhitespace()
    {
        var reference = new MetricReference(
            aggregationName: "  latency  ",
            value: 100.0,
            unit: "  ms  ",
            completessStatus: CompletessStatus.COMPLETE);

        Assert.Equal("latency", reference.AggregationName);
        Assert.Equal("ms", reference.Unit);
    }

    [Fact]
    public void Constructor_EmptyAggregationName_ThrowsException()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            new MetricReference(string.Empty, 100.0, "ms", CompletessStatus.COMPLETE));
        Assert.Contains("null or empty", ex.Message);
    }

    [Fact]
    public void Constructor_EmptyUnit_ThrowsException()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            new MetricReference("latency", 100.0, string.Empty, CompletessStatus.COMPLETE));
        Assert.Contains("null or empty", ex.Message);
    }

    [Fact]
    public void Constructor_PartialStatus_CreatesInstance()
    {
        var reference = new MetricReference(
            aggregationName: "throughput",
            value: 950.0,
            unit: "req/s",
            completessStatus: CompletessStatus.PARTIAL);

        Assert.Equal(CompletessStatus.PARTIAL, reference.CompletessStatus);
    }

    [Fact]
    public void ToString_ReturnsFormattedString()
    {
        var reference = new MetricReference("p99-latency", 500.0, "ms", CompletessStatus.COMPLETE);
        var str = reference.ToString();

        Assert.Contains("p99-latency", str);
        Assert.Contains("500", str);
        Assert.Contains("ms", str);
        Assert.Contains("COMPLETE", str);
    }

    [Fact]
    public void Equality_SameValues_AreEqual()
    {
        var ref1 = new MetricReference("latency", 100.0, "ms", CompletessStatus.COMPLETE);
        var ref2 = new MetricReference("latency", 100.0, "ms", CompletessStatus.COMPLETE);

        Assert.Equal(ref1, ref2);
    }

    [Fact]
    public void Equality_DifferentAggregationNames_NotEqual()
    {
        var ref1 = new MetricReference("latency", 100.0, "ms", CompletessStatus.COMPLETE);
        var ref2 = new MetricReference("throughput", 100.0, "ms", CompletessStatus.COMPLETE);

        Assert.NotEqual(ref1, ref2);
    }

    [Fact]
    public void Equality_DifferentValues_NotEqual()
    {
        var ref1 = new MetricReference("latency", 100.0, "ms", CompletessStatus.COMPLETE);
        var ref2 = new MetricReference("latency", 200.0, "ms", CompletessStatus.COMPLETE);

        Assert.NotEqual(ref1, ref2);
    }

    [Fact]
    public void Equality_DifferentStatuses_NotEqual()
    {
        var ref1 = new MetricReference("latency", 100.0, "ms", CompletessStatus.COMPLETE);
        var ref2 = new MetricReference("latency", 100.0, "ms", CompletessStatus.PARTIAL);

        Assert.NotEqual(ref1, ref2);
    }

    [Fact]
    public void GetHashCode_SameValues_SameHashCode()
    {
        var ref1 = new MetricReference("latency", 100.0, "ms", CompletessStatus.COMPLETE);
        var ref2 = new MetricReference("latency", 100.0, "ms", CompletessStatus.COMPLETE);

        Assert.Equal(ref1.GetHashCode(), ref2.GetHashCode());
    }
}
