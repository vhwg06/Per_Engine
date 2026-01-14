namespace PerformanceEngine.Metrics.Domain.Tests.Aggregations;

using Xunit;
using FluentAssertions;
using PerformanceEngine.Metrics.Domain.Aggregations;
using PerformanceEngine.Metrics.Domain.Metrics;

public class DeterminismContract
{
    private readonly ExecutionContext _context = new("test-engine", Guid.NewGuid());
    private readonly AggregationWindow _fullWindow = AggregationWindow.FullExecution();

    [Fact]
    public void AverageAggregation_IdenticalInputs_ProducesByteIdenticalResults()
    {
        // Arrange
        var samples = CreateFixedSamples(100);
        var aggregation = new AverageAggregation();

        // Act
        var result1 = aggregation.Aggregate(samples, _fullWindow);
        var result2 = aggregation.Aggregate(samples, _fullWindow);

        // Assert - exact equality, not approximate
        result1.Value.Value.Should().Be(result2.Value.Value);
    }

    [Fact]
    public void PercentileAggregation_IdenticalInputs_ProducesByteIdenticalResults()
    {
        // Arrange
        var samples = CreateFixedSamples(100);
        var p95 = new Percentile(95);
        var aggregation = new PercentileAggregation(p95);

        // Act
        var result1 = aggregation.Aggregate(samples, _fullWindow);
        var result2 = aggregation.Aggregate(samples, _fullWindow);

        // Assert - exact equality
        result1.Value.Value.Should().Be(result2.Value.Value);
    }

    [Fact]
    public void MaxAggregation_IdenticalInputs_ProducesByteIdenticalResults()
    {
        // Arrange
        var samples = CreateFixedSamples(100);
        var aggregation = new MaxAggregation();

        // Act
        var result1 = aggregation.Aggregate(samples, _fullWindow);
        var result2 = aggregation.Aggregate(samples, _fullWindow);

        // Assert
        result1.Value.Value.Should().Be(result2.Value.Value);
    }

    [Fact]
    public void MinAggregation_IdenticalInputs_ProducesByteIdenticalResults()
    {
        // Arrange
        var samples = CreateFixedSamples(100);
        var aggregation = new MinAggregation();

        // Act
        var result1 = aggregation.Aggregate(samples, _fullWindow);
        var result2 = aggregation.Aggregate(samples, _fullWindow);

        // Assert
        result1.Value.Value.Should().Be(result2.Value.Value);
    }

    [Fact]
    public void LargeDataset_AllAggregations_AreDeterministic()
    {
        // Arrange
        var largeDataset = CreateFixedSamples(10_000);
        var aggregations = new IAggregationOperation[]
        {
            new AverageAggregation(),
            new MaxAggregation(),
            new MinAggregation(),
            new PercentileAggregation(new Percentile(50)),
            new PercentileAggregation(new Percentile(95)),
            new PercentileAggregation(new Percentile(99))
        };

        // Act & Assert
        foreach (var aggregation in aggregations)
        {
            var result1 = aggregation.Aggregate(largeDataset, _fullWindow);
            var result2 = aggregation.Aggregate(largeDataset, _fullWindow);

            result1.Value.Value.Should().Be(result2.Value.Value, 
                because: $"{aggregation.OperationName} should produce identical results");
        }
    }

    [Fact]
    public void DifferentAggregationInstances_SameInput_ProduceIdenticalResults()
    {
        // Arrange
        var samples = CreateFixedSamples(100);

        // Act
        var avg1 = new AverageAggregation().Aggregate(samples, _fullWindow);
        var avg2 = new AverageAggregation().Aggregate(samples, _fullWindow);

        // Assert - different instances, same results
        avg1.Value.Value.Should().Be(avg2.Value.Value);
    }

    [Fact]
    public void MixedUnits_NormalizedBySameAggregation_ProduceDeterministicResults()
    {
        // Arrange
        var samples1 = CreateFixedSamplesWithMixedUnits(100);
        var samples2 = CreateFixedSamplesWithMixedUnits(100);

        var aggregation = new AverageAggregation();

        // Act
        var result1 = aggregation.Aggregate(samples1, _fullWindow);
        var result2 = aggregation.Aggregate(samples2, _fullWindow);

        // Assert
        result1.Value.Value.Should().Be(result2.Value.Value);
    }

    private SampleCollection CreateFixedSamples(int count)
    {
        var samples = new SampleCollection();
        for (int i = 1; i <= count; i++)
        {
            var sample = new Sample(
                DateTime.UtcNow.AddSeconds(-i),
                new Latency(i % 1000 + 1, LatencyUnit.Milliseconds), // Predictable pattern
                SampleStatus.Success,
                null,
                _context);
            samples = samples.Add(sample);
        }

        return samples;
    }

    private SampleCollection CreateFixedSamplesWithMixedUnits(int count)
    {
        var samples = new SampleCollection();
        for (int i = 1; i <= count; i++)
        {
            var unitChoice = i % 3;
            var unit = unitChoice switch
            {
                0 => LatencyUnit.Nanoseconds,
                1 => LatencyUnit.Milliseconds,
                _ => LatencyUnit.Microseconds
            };

            var latency = unit switch
            {
                LatencyUnit.Nanoseconds => new Latency((i % 100 + 1) * 1_000_000, unit),
                LatencyUnit.Milliseconds => new Latency(i % 100 + 1, unit),
                _ => new Latency((i % 100 + 1) * 1_000, unit)
            };

            var sample = new Sample(
                DateTime.UtcNow.AddSeconds(-i),
                latency,
                SampleStatus.Success,
                null,
                _context);
            samples = samples.Add(sample);
        }

        return samples;
    }
}
