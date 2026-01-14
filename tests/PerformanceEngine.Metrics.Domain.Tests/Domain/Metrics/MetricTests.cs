namespace PerformanceEngine.Metrics.Domain.Tests.Metrics;

using Xunit;
using FluentAssertions;
using PerformanceEngine.Metrics.Domain;
using PerformanceEngine.Metrics.Domain.Metrics;

public class MetricTests
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

    private SampleCollection CreateSampleCollection(int count = 3)
    {
        var samples = Enumerable.Range(0, count)
            .Select(i => CreateSample(100 + (i * 10)))
            .ToList();

        return SampleCollection.Create(samples);
    }

    [Fact]
    public void Metric_WithValidSamples_CreatesSuccessfully()
    {
        // Arrange
        var samples = CreateSampleCollection(5);
        var window = AggregationWindow.FullExecution();

        // Act
        var metric = new Metric(samples, window, "latency");

        // Assert
        metric.Should().NotBeNull();
        metric.MetricType.Should().Be("latency");
        metric.SampleCount.Should().Be(5);
    }

    [Fact]
    public void Metric_WithEmptySamples_Throws()
    {
        // Arrange
        var emptySamples = SampleCollection.Empty;
        var window = AggregationWindow.FullExecution();

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() =>
            new Metric(emptySamples, window, "latency"));

        ex.Message.Should().Contain("empty");
    }

    [Fact]
    public void Metric_CannotExistWithoutSamples()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentException>(() =>
            new Metric(SampleCollection.Empty, AggregationWindow.FullExecution(), "latency"));
    }

    [Fact]
    public void Metric_SuccessRate_CalculatedCorrectly()
    {
        // Arrange
        var collection = SampleCollection.Empty
            .Add(CreateSample(100, SampleStatus.Success))
            .Add(CreateSample(200, SampleStatus.Success))
            .Add(CreateSample(300, SampleStatus.Failure, ErrorClassification.Timeout))
            .Add(CreateSample(150, SampleStatus.Success));

        var metric = new Metric(collection, AggregationWindow.FullExecution(), "latency");

        // Act & Assert
        metric.SampleCount.Should().Be(4);
        metric.SuccessCount.Should().Be(3);
        metric.FailureCount.Should().Be(1);
        metric.SuccessRate.Should().Be(75.0);
    }

    [Fact]
    public void Metric_WithDifferentWindows_ValidatesWindow()
    {
        // Arrange
        var samples = CreateSampleCollection();

        // Act
        var metric1 = new Metric(samples, AggregationWindow.FullExecution(), "latency");
        var metric2 = new Metric(samples, AggregationWindow.Fixed(TimeSpan.FromSeconds(5)), "latency");

        // Assert
        metric1.Window.Should().BeOfType<FullExecutionWindow>();
        metric2.Window.Should().BeOfType<FixedWindow>();
    }

    [Fact]
    public void Metric_IsImmutable_PropertiesNotChangeable()
    {
        // Arrange
        var samples = CreateSampleCollection();
        var metric = new Metric(samples, AggregationWindow.FullExecution(), "latency");

        // Act & Assert
        // All properties are read-only and cannot be changed
        var originalCount = metric.SampleCount;
        metric.SampleCount.Should().Be(originalCount);
    }

    [Fact]
    public void Metric_Equality_BasedOnId()
    {
        // Arrange
        var samples = CreateSampleCollection();
        var window = AggregationWindow.FullExecution();
        var metric1 = new Metric(samples, window, "latency");
        var metric2 = new Metric(samples, window, "latency");

        // Act & Assert
        metric1.Should().NotBe(metric2);
        metric1.Id.Should().NotBe(metric2.Id);
    }

    [Fact]
    public void Metric_WithAggregatedValues_PreservesResults()
    {
        // Arrange
        var samples = CreateSampleCollection();
        var window = AggregationWindow.FullExecution();
        var results = new[]
        {
            new AggregationResult(new Latency(150, LatencyUnit.Milliseconds), "p50"),
            new AggregationResult(new Latency(250, LatencyUnit.Milliseconds), "p95")
        };

        // Act
        var metric = new Metric(samples, window, "latency", results);

        // Assert
        metric.AggregatedValues.Should().HaveCount(2);
        metric.AggregatedValues[0].OperationName.Should().Be("p50");
        metric.AggregatedValues[1].OperationName.Should().Be("p95");
    }
}
