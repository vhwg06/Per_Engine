namespace PerformanceEngine.Metrics.Domain.Tests.Aggregations;

using Xunit;
using FluentAssertions;
using PerformanceEngine.Metrics.Domain.Aggregations;
using PerformanceEngine.Metrics.Domain.Metrics;

public class AggregationTests
{
    private readonly ExecutionContext _context = new("test-engine", Guid.NewGuid());
    private readonly AggregationWindow _fullWindow = AggregationWindow.FullExecution();

    [Fact]
    public void AverageAggregation_EmptyCollection_Throws()
    {
        // Arrange
        var aggregation = new AverageAggregation();
        var emptySamples = new SampleCollection();

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => 
            aggregation.Aggregate(emptySamples, _fullWindow));
        ex.Message.Should().Contain("empty");
    }

    [Fact]
    public void AverageAggregation_SingleSample_ReturnsExactValue()
    {
        // Arrange
        var sample = new Sample(
            DateTime.UtcNow.AddSeconds(-1),
            new Latency(100, LatencyUnit.Milliseconds),
            SampleStatus.Success,
            null,
            _context);

        var samples = new SampleCollection().Add(sample);
        var aggregation = new AverageAggregation();

        // Act
        var result = aggregation.Aggregate(samples, _fullWindow);

        // Assert
        result.Value.Value.Should().Be(100.0);
    }

    [Fact]
    public void AverageAggregation_MultipleSamples_ComputesCorrectMean()
    {
        // Arrange
        var samples = new SampleCollection();
        for (int i = 1; i <= 4; i++)
        {
            var sample = new Sample(
                DateTime.UtcNow.AddSeconds(-i),
                new Latency(i * 25, LatencyUnit.Milliseconds), // 25, 50, 75, 100
                SampleStatus.Success,
                null,
                _context);
            samples = samples.Add(sample);
        }

        var aggregation = new AverageAggregation();

        // Act
        var result = aggregation.Aggregate(samples, _fullWindow);

        // Assert
        result.Value.Value.Should().Be(62.5); // (25+50+75+100)/4 = 62.5
    }

    [Fact]
    public void MaxAggregation_SingleSample_ReturnsValue()
    {
        // Arrange
        var sample = new Sample(
            DateTime.UtcNow.AddSeconds(-1),
            new Latency(42, LatencyUnit.Milliseconds),
            SampleStatus.Success,
            null,
            _context);

        var samples = new SampleCollection().Add(sample);
        var aggregation = new MaxAggregation();

        // Act
        var result = aggregation.Aggregate(samples, _fullWindow);

        // Assert
        result.Value.Value.Should().Be(42.0);
    }

    [Fact]
    public void MaxAggregation_MultipleSamples_ReturnsMaximum()
    {
        // Arrange
        var samples = new SampleCollection();
        var values = new[] { 10, 50, 20, 100, 30 };
        foreach (var val in values)
        {
            var sample = new Sample(
                DateTime.UtcNow.AddSeconds(-val),
                new Latency(val, LatencyUnit.Milliseconds),
                SampleStatus.Success,
                null,
                _context);
            samples = samples.Add(sample);
        }

        var aggregation = new MaxAggregation();

        // Act
        var result = aggregation.Aggregate(samples, _fullWindow);

        // Assert
        result.Value.Value.Should().Be(100.0);
    }

    [Fact]
    public void MinAggregation_SingleSample_ReturnsValue()
    {
        // Arrange
        var sample = new Sample(
            DateTime.UtcNow.AddSeconds(-1),
            new Latency(42, LatencyUnit.Milliseconds),
            SampleStatus.Success,
            null,
            _context);

        var samples = new SampleCollection().Add(sample);
        var aggregation = new MinAggregation();

        // Act
        var result = aggregation.Aggregate(samples, _fullWindow);

        // Assert
        result.Value.Value.Should().Be(42.0);
    }

    [Fact]
    public void MinAggregation_MultipleSamples_ReturnsMinimum()
    {
        // Arrange
        var samples = new SampleCollection();
        var values = new[] { 100, 5, 75, 20, 50 };
        foreach (var val in values)
        {
            var sample = new Sample(
                DateTime.UtcNow.AddSeconds(-val),
                new Latency(val, LatencyUnit.Milliseconds),
                SampleStatus.Success,
                null,
                _context);
            samples = samples.Add(sample);
        }

        var aggregation = new MinAggregation();

        // Act
        var result = aggregation.Aggregate(samples, _fullWindow);

        // Assert
        result.Value.Value.Should().Be(5.0);
    }

    [Theory]
    [InlineData(0)]   // p0 = minimum
    [InlineData(50)]  // p50 = median
    [InlineData(100)] // p100 = maximum
    public void PercentileAggregation_StandardPercentiles_ComputesCorrectly(double percentile)
    {
        // Arrange
        var samples = new SampleCollection();
        for (int i = 1; i <= 100; i++)
        {
            var sample = new Sample(
                DateTime.UtcNow.AddSeconds(-i),
                new Latency(i, LatencyUnit.Milliseconds),
                SampleStatus.Success,
                null,
                _context);
            samples = samples.Add(sample);
        }

        var p = new Percentile(percentile);
        var aggregation = new PercentileAggregation(p);

        // Act
        var result = aggregation.Aggregate(samples, _fullWindow);

        // Assert
        if (percentile == 0)
            result.Value.Value.Should().Be(1.0); // minimum
        else if (percentile == 100)
            result.Value.Value.Should().Be(100.0); // maximum
        else if (percentile == 50)
            result.Value.Value.Should().Be(50.0); // median of 1-100
    }

    [Fact]
    public void PercentileAggregation_AllEqualValues_ReturnsSameValue()
    {
        // Arrange
        var samples = new SampleCollection();
        for (int i = 0; i < 10; i++)
        {
            var sample = new Sample(
                DateTime.UtcNow.AddSeconds(-i),
                new Latency(42, LatencyUnit.Milliseconds),
                SampleStatus.Success,
                null,
                _context);
            samples = samples.Add(sample);
        }

        var p95 = new Percentile(95);
        var aggregation = new PercentileAggregation(p95);

        // Act
        var result = aggregation.Aggregate(samples, _fullWindow);

        // Assert
        result.Value.Value.Should().Be(42.0); // All values are 42
    }

    [Fact]
    public void Aggregation_WithNullSamples_Throws()
    {
        // Arrange
        var aggregation = new AverageAggregation();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            aggregation.Aggregate(null!, _fullWindow));
    }

    [Fact]
    public void Aggregation_WithNullWindow_Throws()
    {
        // Arrange
        var sample = new Sample(
            DateTime.UtcNow.AddSeconds(-1),
            new Latency(100, LatencyUnit.Milliseconds),
            SampleStatus.Success,
            null,
            _context);
        var samples = new SampleCollection().Add(sample);
        var aggregation = new AverageAggregation();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            aggregation.Aggregate(samples, null!));
    }
}
