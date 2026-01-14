namespace PerformanceEngine.Metrics.Domain.Tests.Domain;

using Xunit;
using FluentAssertions;
using PerformanceEngine.Metrics.Domain;
using PerformanceEngine.Metrics.Domain.Metrics;

/// <summary>
/// Contract tests that verify all domain invariants are consistently enforced.
/// These tests ensure that the domain maintains its integrity constraints.
/// </summary>
public class SampleInvariants
{
    private readonly ExecutionContext _context = new("test-engine", Guid.NewGuid());

    [Fact]
    public void Sample_Always_HasNonFutureTimestamp()
    {
        // Invariant: Timestamp cannot be in the future

        var now = DateTime.UtcNow;
        for (int i = 0; i < 100; i++)
        {
            var sample = new Sample(
                now,
                new Latency(50 + i, LatencyUnit.Milliseconds),
                SampleStatus.Success,
                null,
                _context);

            (sample.Timestamp <= DateTime.UtcNow.AddSeconds(1)).Should().BeTrue();
        }
    }

    [Fact]
    public void Sample_Always_HasNonNegativeDuration()
    {
        // Invariant: Duration ≥ 0

        var now = DateTime.UtcNow;
        var validLatencies = new[] { 0, 0.001, 1, 100, 1000, 10000.5 };

        foreach (var value in validLatencies)
        {
            var sample = new Sample(
                now,
                new Latency(value, LatencyUnit.Milliseconds),
                SampleStatus.Success,
                null,
                _context);

            sample.Duration.Value.Should().BeGreaterThanOrEqualTo(0);
        }
    }

    [Fact]
    public void Sample_Failure_Always_HasErrorClassification()
    {
        // Invariant: If Status is Failure, ErrorClassification cannot be null

        var now = DateTime.UtcNow;
        var errorTypes = new[]
        {
            ErrorClassification.Timeout,
            ErrorClassification.NetworkError,
            ErrorClassification.ApplicationError,
            ErrorClassification.UnknownError
        };

        foreach (var errorType in errorTypes)
        {
            var sample = new Sample(
                now,
                new Latency(5000, LatencyUnit.Milliseconds),
                SampleStatus.Failure,
                errorType,
                _context);

            sample.ErrorClassification.Should().NotBeNull();
            sample.ErrorClassification.Should().Be(errorType);
        }
    }

    [Fact]
    public void Sample_Success_Always_HasNullError()
    {
        // Invariant: If Status is Success, ErrorClassification must be null

        var now = DateTime.UtcNow;
        for (int i = 0; i < 50; i++)
        {
            var sample = new Sample(
                now,
                new Latency(50 + i, LatencyUnit.Milliseconds),
                SampleStatus.Success,
                null,
                _context);

            sample.ErrorClassification.Should().BeNull();
        }
    }

    [Fact]
    public void SampleCollection_Always_MaintainsInsertionOrder()
    {
        // Invariant: SampleCollection preserves insertion order

        var samples = new[]
        {
            new Sample(DateTime.UtcNow, new Latency(100, LatencyUnit.Milliseconds), SampleStatus.Success, null, _context),
            new Sample(DateTime.UtcNow, new Latency(200, LatencyUnit.Milliseconds), SampleStatus.Success, null, _context),
            new Sample(DateTime.UtcNow, new Latency(50, LatencyUnit.Milliseconds), SampleStatus.Success, null, _context),
            new Sample(DateTime.UtcNow, new Latency(300, LatencyUnit.Milliseconds), SampleStatus.Success, null, _context),
        };

        var collection = SampleCollection.Create(samples);
        var snapshot = collection.GetSnapshot();

        for (int i = 0; i < samples.Length; i++)
        {
            snapshot[i].Should().Be(samples[i]);
        }
    }

    [Fact]
    public void SampleCollection_Always_ReturnsImmutableSnapshot()
    {
        // Invariant: GetSnapshot returns immutable, not mutable, list

        var collection = SampleCollection.Empty
            .Add(new Sample(DateTime.UtcNow, new Latency(100, LatencyUnit.Milliseconds), SampleStatus.Success, null, _context));

        var snapshot = collection.GetSnapshot();

        snapshot.Should().BeOfType<System.Collections.Immutable.ImmutableList<Sample>>();
    }

    [Fact]
    public void Metric_Always_HasAtLeastOneSample()
    {
        // Invariant: Metric cannot exist without samples

        var collection = SampleCollection.Empty
            .Add(new Sample(DateTime.UtcNow, new Latency(100, LatencyUnit.Milliseconds), SampleStatus.Success, null, _context));

        var metric = new Metric(collection, AggregationWindow.FullExecution(), "latency");

        metric.SampleCount.Should().BeGreaterThan(0);
        metric.Samples.Count.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Metric_Always_RejectsEmptyCollection()
    {
        // Invariant enforcement: Empty collections are rejected

        var emptyCollection = SampleCollection.Empty;

        var ex = Assert.Throws<ArgumentException>(() =>
            new Metric(emptyCollection, AggregationWindow.FullExecution(), "latency"));

        ex.Message.Should().Contain("empty");
    }

    [Fact]
    public void Percentile_Always_InRange()
    {
        // Invariant: Percentile values are always in [0, 100]

        var validPercentiles = new[] { 0, 1, 25, 50, 75, 95, 99, 100 };

        foreach (var value in validPercentiles)
        {
            var percentile = new Percentile(value);
            percentile.Value.Should().BeGreaterThanOrEqualTo(0);
            percentile.Value.Should().BeLessThanOrEqualTo(100);
        }
    }

    [Fact]
    public void Latency_Always_NonNegative()
    {
        // Invariant: Latency values cannot be negative

        var validValues = new[] { 0.0, 0.001, 1.0, 100.5, 10000.0 };

        foreach (var value in validValues)
        {
            var latency = new Latency(value, LatencyUnit.Milliseconds);
            latency.Value.Should().BeGreaterThanOrEqualTo(0);
        }
    }
}
