namespace PerformanceEngine.Metrics.Domain.Tests.Aggregations;

using Xunit;
using FluentAssertions;
using PerformanceEngine.Metrics.Domain.Aggregations;
using PerformanceEngine.Metrics.Domain.Metrics;

public class NormalizationTests
{
    private readonly ExecutionContext _context = new("test-engine", Guid.NewGuid());

    [Fact]
    public void NormalizeSamples_FromMillisecondsToNanoseconds_PreservesPrecision()
    {
        // Arrange
        var sample = new Sample(
            DateTime.UtcNow.AddSeconds(-1),
            new Latency(1, LatencyUnit.Milliseconds), // 1 ms = 1,000,000 ns
            SampleStatus.Success,
            null,
            _context);
        var samples = new SampleCollection().Add(sample);

        // Act
        var normalized = AggregationNormalizer.NormalizeSamples(samples, LatencyUnit.Nanoseconds);
        var normalizedSnapshot = normalized.GetSnapshot();

        // Assert
        normalizedSnapshot.Should().HaveCount(1);
        normalizedSnapshot[0].Duration.Value.Should().Be(1_000_000.0); // 1 ms in nanoseconds
    }

    [Fact]
    public void NormalizeSamples_FromNanosecondsToMilliseconds_ConvertsCorrectly()
    {
        // Arrange
        var sample = new Sample(
            DateTime.UtcNow.AddSeconds(-1),
            new Latency(1_000_000, LatencyUnit.Nanoseconds), // 1,000,000 ns = 1 ms
            SampleStatus.Success,
            null,
            _context);
        var samples = new SampleCollection().Add(sample);

        // Act
        var normalized = AggregationNormalizer.NormalizeSamples(samples, LatencyUnit.Milliseconds);
        var normalizedSnapshot = normalized.GetSnapshot();

        // Assert
        normalizedSnapshot.Should().HaveCount(1);
        normalizedSnapshot[0].Duration.Value.Should().Be(1.0); // 1,000,000 ns in milliseconds
    }

    [Fact]
    public void NormalizeSamples_MixedUnits_NormalizesAll()
    {
        // Arrange
        var samples = new SampleCollection();
        
        var sample1 = new Sample(DateTime.UtcNow.AddSeconds(-3), new Latency(1, LatencyUnit.Seconds), SampleStatus.Success, null, _context);
        var sample2 = new Sample(DateTime.UtcNow.AddSeconds(-2), new Latency(1000, LatencyUnit.Milliseconds), SampleStatus.Success, null, _context);
        var sample3 = new Sample(DateTime.UtcNow.AddSeconds(-1), new Latency(1_000_000, LatencyUnit.Microseconds), SampleStatus.Success, null, _context);
        
        samples = samples.Add(sample1).Add(sample2).Add(sample3);

        // Act
        var normalized = AggregationNormalizer.NormalizeSamples(samples, LatencyUnit.Milliseconds);
        var normalizedSnapshot = normalized.GetSnapshot();

        // Assert
        normalizedSnapshot.Should().HaveCount(3);
        normalizedSnapshot[0].Duration.Value.Should().Be(1000.0);  // 1 second = 1000 ms
        normalizedSnapshot[1].Duration.Value.Should().Be(1000.0);  // 1000 ms = 1000 ms
        normalizedSnapshot[2].Duration.Value.Should().Be(1000.0);  // 1,000,000 microseconds = 1000 ms
    }

    [Fact]
    public void NormalizeSamples_PreservesMetadata()
    {
        // Arrange
        var metadata = new Dictionary<string, object> 
        { 
            { "scenario", (object)"load-test" },
            { "region", (object)"us-east-1" }
        };
        
        var sample = new Sample(
            DateTime.UtcNow.AddSeconds(-1),
            new Latency(100, LatencyUnit.Milliseconds),
            SampleStatus.Success,
            null,
            _context,
            metadata);
        var samples = new SampleCollection().Add(sample);

        // Act
        var normalized = AggregationNormalizer.NormalizeSamples(samples, LatencyUnit.Seconds);
        var normalizedSnapshot = normalized.GetSnapshot();

        // Assert
        normalizedSnapshot[0].Metadata.Should().NotBeNull();
        normalizedSnapshot[0].Metadata.Should().ContainKey("scenario");
        normalizedSnapshot[0].Metadata.Should().ContainKey("region");
        normalizedSnapshot[0].Metadata!["scenario"].Should().Be("load-test");
        normalizedSnapshot[0].Metadata!["region"].Should().Be("us-east-1");
    }

    [Fact]
    public void NormalizeSamples_PreservesAllProperties()
    {
        // Arrange
        var originalTime = DateTime.UtcNow.AddSeconds(-1);
        var sample = new Sample(
            originalTime,
            new Latency(100, LatencyUnit.Milliseconds),
            SampleStatus.Failure,
            ErrorClassification.Timeout,
            _context);
        var samples = new SampleCollection().Add(sample);

        // Act
        var normalized = AggregationNormalizer.NormalizeSamples(samples, LatencyUnit.Microseconds);
        var normalizedSnapshot = normalized.GetSnapshot();

        // Assert
        normalizedSnapshot[0].Timestamp.Should().Be(originalTime);
        normalizedSnapshot[0].Status.Should().Be(SampleStatus.Failure);
        normalizedSnapshot[0].ErrorClassification.Should().Be(ErrorClassification.Timeout);
        normalizedSnapshot[0].ExecutionContext.Should().Be(_context);
    }

    [Fact]
    public void NormalizeSamples_EmptyCollection_ReturnsEmpty()
    {
        // Arrange
        var samples = new SampleCollection();

        // Act
        var normalized = AggregationNormalizer.NormalizeSamples(samples, LatencyUnit.Milliseconds);
        var normalizedSnapshot = normalized.GetSnapshot();

        // Assert
        normalizedSnapshot.Should().BeEmpty();
    }

    [Fact]
    public void NormalizeSamples_WithNullCollection_Throws()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            AggregationNormalizer.NormalizeSamples(null!, LatencyUnit.Milliseconds));
    }

    [Fact]
    public void NormalizeSamples_LargeNumbers_MaintainsAccuracy()
    {
        // Arrange
        var largeValue = 1_000_000_000.0; // 1 billion milliseconds
        var sample = new Sample(
            DateTime.UtcNow.AddSeconds(-1),
            new Latency(largeValue, LatencyUnit.Milliseconds),
            SampleStatus.Success,
            null,
            _context);
        var samples = new SampleCollection().Add(sample);

        // Act
        var normalized = AggregationNormalizer.NormalizeSamples(samples, LatencyUnit.Seconds);
        var normalizedSnapshot = normalized.GetSnapshot();

        // Assert
        normalizedSnapshot[0].Duration.Value.Should().Be(largeValue / 1000.0);
    }
}
