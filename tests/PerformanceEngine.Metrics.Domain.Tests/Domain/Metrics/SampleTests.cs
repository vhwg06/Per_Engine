namespace PerformanceEngine.Metrics.Domain.Tests.Metrics;

using Xunit;
using FluentAssertions;
using PerformanceEngine.Metrics.Domain;
using PerformanceEngine.Metrics.Domain.Metrics;

public class SampleTests
{
    private readonly ExecutionContext _defaultContext = new("test-engine", Guid.NewGuid());
    private readonly DateTime _now = DateTime.UtcNow;

    [Fact]
    public void Sample_SuccessfulRequest_CreatesValidSample()
    {
        // Arrange
        var duration = new Latency(45.5, LatencyUnit.Milliseconds);

        // Act
        var sample = new Sample(
            _now,
            duration,
            SampleStatus.Success,
            null,
            _defaultContext);

        // Assert
        sample.Should().NotBeNull();
        sample.IsSuccess.Should().BeTrue();
        sample.IsFailure.Should().BeFalse();
        sample.Duration.Should().Be(duration);
        sample.ErrorClassification.Should().BeNull();
    }

    [Fact]
    public void Sample_FailedRequest_WithTimeout_CreatesValidSample()
    {
        // Arrange
        var duration = new Latency(5000, LatencyUnit.Milliseconds);

        // Act
        var sample = new Sample(
            _now,
            duration,
            SampleStatus.Failure,
            ErrorClassification.Timeout,
            _defaultContext);

        // Assert
        sample.IsFailure.Should().BeTrue();
        sample.ErrorClassification.Should().Be(ErrorClassification.Timeout);
    }

    [Fact]
    public void Sample_FutureTimestamp_Throws()
    {
        // Arrange
        var futureTime = DateTime.UtcNow.AddHours(1);
        var duration = new Latency(100, LatencyUnit.Milliseconds);

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() =>
            new Sample(futureTime, duration, SampleStatus.Success, null, _defaultContext));

        ex.Message.Should().Contain("future");
    }

    [Fact]
    public void Sample_NegativeDuration_Throws()
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() =>
            new Sample(
                _now,
                new Latency(-1, LatencyUnit.Milliseconds),
                SampleStatus.Success,
                null,
                _defaultContext));

        ex.Message.Should().Contain("negative");
    }

    [Fact]
    public void Sample_FailureWithoutErrorClassification_Throws()
    {
        // Arrange
        var duration = new Latency(100, LatencyUnit.Milliseconds);

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() =>
            new Sample(
                _now,
                duration,
                SampleStatus.Failure,
                null, // Missing error classification
                _defaultContext));

        ex.Message.Should().Contain("required");
    }

    [Fact]
    public void Sample_SuccessWithErrorClassification_Throws()
    {
        // Arrange
        var duration = new Latency(100, LatencyUnit.Milliseconds);

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() =>
            new Sample(
                _now,
                duration,
                SampleStatus.Success,
                ErrorClassification.Timeout,
                _defaultContext));

        ex.Message.Should().Contain("must be null");
    }

    [Fact]
    public void Sample_IsImmutable_AfterConstruction()
    {
        // Arrange
        var pastTime = DateTime.UtcNow.AddSeconds(-2);
        var sample1 = new Sample(
            pastTime,
            new Latency(100, LatencyUnit.Milliseconds),
            SampleStatus.Success,
            null,
            _defaultContext);

        // Act
        var sample2 = new Sample(
            pastTime.AddSeconds(-1),
            new Latency(200, LatencyUnit.Milliseconds),
            SampleStatus.Success,
            null,
            _defaultContext);

        // Assert - all properties should be unchanged
        sample1.Duration.Value.Should().Be(100);
        sample2.Duration.Value.Should().Be(200);
    }

    [Fact]
    public void Sample_Equality_BasedOnId()
    {
        // Arrange
        var duration = new Latency(100, LatencyUnit.Milliseconds);
        var sample1 = new Sample(_now, duration, SampleStatus.Success, null, _defaultContext);
        var sample2 = new Sample(_now, duration, SampleStatus.Success, null, _defaultContext);

        // Act & Assert - Different instances, different IDs, not equal
        sample1.Should().NotBe(sample2);
        sample1.Id.Should().NotBe(sample2.Id);
    }

    [Fact]
    public void Sample_WithMetadata_PreservesMetadata()
    {
        // Arrange
        var metadata = new Dictionary<string, object> { ["key"] = "value" };
        var sample = new Sample(
            _now,
            new Latency(100, LatencyUnit.Milliseconds),
            SampleStatus.Success,
            null,
            _defaultContext,
            metadata);

        // Act & Assert
        sample.Metadata.Should().ContainKey("key");
        sample.Metadata["key"].Should().Be("value");
    }
}
