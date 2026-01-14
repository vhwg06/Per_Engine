namespace PerformanceEngine.Metrics.Domain.Tests.Metrics;

using Xunit;
using FluentAssertions;
using PerformanceEngine.Metrics.Domain;
using PerformanceEngine.Metrics.Domain.Metrics;

public class SampleCollectionTests
{
    private readonly ExecutionContext _context = new("test-engine", Guid.NewGuid());

    private Sample CreateSample(double durationMs, SampleStatus status = SampleStatus.Success, ErrorClassification? error = null)
    {
        return new Sample(
            DateTime.UtcNow,
            new Latency(durationMs, LatencyUnit.Milliseconds),
            status,
            error,
            _context);
    }

    [Fact]
    public void SampleCollection_Empty_IsValid()
    {
        // Act
        var collection = SampleCollection.Empty;

        // Assert
        collection.Count.Should().Be(0);
        collection.IsEmpty.Should().BeTrue();
    }

    [Fact]
    public void SampleCollection_Add_IncreasesCount()
    {
        // Arrange
        var collection = SampleCollection.Empty;
        var sample = CreateSample(100);

        // Act
        var newCollection = collection.Add(sample);

        // Assert
        collection.Count.Should().Be(0); // Original unchanged
        newCollection.Count.Should().Be(1);
    }

    [Fact]
    public void SampleCollection_AddRange_AddsMultipleSamples()
    {
        // Arrange
        var samples = new[]
        {
            CreateSample(100),
            CreateSample(200),
            CreateSample(150)
        };

        // Act
        var collection = SampleCollection.Create(samples);

        // Assert
        collection.Count.Should().Be(3);
    }

    [Fact]
    public void SampleCollection_GetSnapshot_ReturnsImmutableList()
    {
        // Arrange
        var samples = new[] { CreateSample(100), CreateSample(200) };
        var collection = SampleCollection.Create(samples);

        // Act
        var snapshot = collection.GetSnapshot();

        // Assert
        snapshot.Count.Should().Be(2);
        snapshot.Should().BeOfType<System.Collections.Immutable.ImmutableList<Sample>>();
    }

    [Fact]
    public void SampleCollection_PreservesInsertionOrder()
    {
        // Arrange
        var sample1 = CreateSample(100);
        var sample2 = CreateSample(200);
        var sample3 = CreateSample(150);

        // Act
        var collection = SampleCollection.Empty
            .Add(sample1)
            .Add(sample2)
            .Add(sample3);

        // Assert
        var samples = collection.GetSnapshot();
        samples[0].Should().Be(sample1);
        samples[1].Should().Be(sample2);
        samples[2].Should().Be(sample3);
    }

    [Fact]
    public void SampleCollection_SuccessfulSamples_FiltersCorrectly()
    {
        // Arrange
        var collection = SampleCollection.Empty
            .Add(CreateSample(100, SampleStatus.Success))
            .Add(CreateSample(200, SampleStatus.Failure, ErrorClassification.Timeout))
            .Add(CreateSample(150, SampleStatus.Success));

        // Act
        var successful = collection.SuccessfulSamples;

        // Assert
        successful.Count().Should().Be(2);
    }

    [Fact]
    public void SampleCollection_FailedSamples_FiltersCorrectly()
    {
        // Arrange
        var collection = SampleCollection.Empty
            .Add(CreateSample(100, SampleStatus.Success))
            .Add(CreateSample(200, SampleStatus.Failure, ErrorClassification.Timeout))
            .Add(CreateSample(150, SampleStatus.Success));

        // Act
        var failed = collection.FailedSamples;

        // Assert
        failed.Count().Should().Be(1);
    }

    [Fact]
    public void SampleCollection_MinimumLatency_ReturnsSmallestValue()
    {
        // Arrange
        var collection = SampleCollection.Empty
            .Add(CreateSample(100))
            .Add(CreateSample(50))
            .Add(CreateSample(200));

        // Act
        var minimum = collection.MinimumLatency;

        // Assert
        minimum.Should().NotBeNull();
        minimum!.Value.Should().Be(50);
    }

    [Fact]
    public void SampleCollection_MaximumLatency_ReturnsLargestValue()
    {
        // Arrange
        var collection = SampleCollection.Empty
            .Add(CreateSample(100))
            .Add(CreateSample(50))
            .Add(CreateSample(200));

        // Act
        var maximum = collection.MaximumLatency;

        // Assert
        maximum.Should().NotBeNull();
        maximum!.Value.Should().Be(200);
    }

    [Fact]
    public void SampleCollection_EmptyCollection_NoMinMax()
    {
        // Arrange
        var collection = SampleCollection.Empty;

        // Act & Assert
        collection.MinimumLatency.Should().BeNull();
        collection.MaximumLatency.Should().BeNull();
    }

    [Fact]
    public void SampleCollection_IsFunctional_OriginalUnchanged()
    {
        // Arrange
        var original = SampleCollection.Empty;

        // Act
        var modified = original.Add(CreateSample(100));

        // Assert
        original.IsEmpty.Should().BeTrue();
        modified.Count.Should().Be(1);
    }
}
