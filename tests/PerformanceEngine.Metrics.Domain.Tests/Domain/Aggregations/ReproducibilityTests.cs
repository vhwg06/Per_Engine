namespace PerformanceEngine.Metrics.Domain.Tests.Aggregations;

using Xunit;
using FluentAssertions;
using PerformanceEngine.Metrics.Domain.Aggregations;
using PerformanceEngine.Metrics.Domain.Metrics;

public class ReproducibilityTests
{
    private const int RandomSeed = 42; // Fixed seed for reproducibility
    private readonly ExecutionContext _context = new("test-engine", Guid.NewGuid());
    private readonly AggregationWindow _fullWindow = AggregationWindow.FullExecution();

    [Fact]
    public void AverageAggregation_WithFixedSeed_ProducesReproducibleResults()
    {
        // Arrange & Act
        var run1Samples = GeneratePseudoRandomSamples(RandomSeed, 1000);
        var run2Samples = GeneratePseudoRandomSamples(RandomSeed, 1000);

        var aggregation = new AverageAggregation();
        var run1Result = aggregation.Aggregate(run1Samples, _fullWindow);
        var run2Result = aggregation.Aggregate(run2Samples, _fullWindow);

        // Assert - bit-for-bit exact match
        run1Result.Value.Should().Be(run2Result.Value);
    }

    [Fact]
    public void MaxAggregation_WithFixedSeed_ProducesReproducibleResults()
    {
        // Arrange & Act
        var run1Samples = GeneratePseudoRandomSamples(RandomSeed, 1000);
        var run2Samples = GeneratePseudoRandomSamples(RandomSeed, 1000);

        var aggregation = new MaxAggregation();
        var run1Result = aggregation.Aggregate(run1Samples, _fullWindow);
        var run2Result = aggregation.Aggregate(run2Samples, _fullWindow);

        // Assert
        run1Result.Value.Should().Be(run2Result.Value);
    }

    [Fact]
    public void MinAggregation_WithFixedSeed_ProducesReproducibleResults()
    {
        // Arrange & Act
        var run1Samples = GeneratePseudoRandomSamples(RandomSeed, 1000);
        var run2Samples = GeneratePseudoRandomSamples(RandomSeed, 1000);

        var aggregation = new MinAggregation();
        var run1Result = aggregation.Aggregate(run1Samples, _fullWindow);
        var run2Result = aggregation.Aggregate(run2Samples, _fullWindow);

        // Assert
        run1Result.Value.Should().Be(run2Result.Value);
    }

    [Fact]
    public void PercentileAggregation_WithFixedSeed_ProducesReproducibleResults()
    {
        // Arrange & Act
        var run1Samples = GeneratePseudoRandomSamples(RandomSeed, 1000);
        var run2Samples = GeneratePseudoRandomSamples(RandomSeed, 1000);

        var p95 = new Percentile(95);
        var aggregation = new PercentileAggregation(p95);
        var run1Result = aggregation.Aggregate(run1Samples, _fullWindow);
        var run2Result = aggregation.Aggregate(run2Samples, _fullWindow);

        // Assert
        run1Result.Value.Should().Be(run2Result.Value);
    }

    [Fact]
    public void AllAggregations_WithFixedSeed_ProduceReproducibleResults()
    {
        // Arrange
        var run1Samples = GeneratePseudoRandomSamples(RandomSeed, 5000);
        var run2Samples = GeneratePseudoRandomSamples(RandomSeed, 5000);

        var aggregations = new (IAggregationOperation Op, string Name)[]
        {
            (new AverageAggregation(), "average"),
            (new MaxAggregation(), "max"),
            (new MinAggregation(), "min"),
            (new PercentileAggregation(new Percentile(50)), "p50"),
            (new PercentileAggregation(new Percentile(95)), "p95"),
            (new PercentileAggregation(new Percentile(99)), "p99"),
            (new PercentileAggregation(new Percentile(99.9)), "p99.9")
        };

        // Act & Assert
        foreach (var (aggregation, name) in aggregations)
        {
            var run1Result = aggregation.Aggregate(run1Samples, _fullWindow);
            var run2Result = aggregation.Aggregate(run2Samples, _fullWindow);

            run1Result.Value.Should().Be(run2Result.Value, 
                because: $"{name} should produce reproducible results with fixed seed");
        }
    }

    [Fact]
    public void DifferentSeed_ProducesDifferentResults()
    {
        // This test verifies that our pseudo-random generation actually creates
        // different samples with different seeds (inverse test)

        // Arrange
        var seed1Samples = GeneratePseudoRandomSamples(42, 100);
        var seed2Samples = GeneratePseudoRandomSamples(99, 100);

        var aggregation = new AverageAggregation();
        var result1 = aggregation.Aggregate(seed1Samples, _fullWindow);
        var result2 = aggregation.Aggregate(seed2Samples, _fullWindow);

        // Assert - different seeds should produce different results (with high probability)
        result1.Value.Should().NotBe(result2.Value);
    }

    private SampleCollection GeneratePseudoRandomSamples(int seed, int count)
    {
        var random = new Random(seed);
        var samples = new SampleCollection();

        for (int i = 0; i < count; i++)
        {
            // Generate pseudo-random latency between 1 and 1000 ms
            var latency = random.Next(1, 1001);
            
            var sample = new Sample(
                DateTime.UtcNow.AddSeconds(-(count - i)), // Descending timestamps
                new Latency(latency, LatencyUnit.Milliseconds),
                SampleStatus.Success,
                null,
                _context);

            samples = samples.Add(sample);
        }

        return samples;
    }
}
