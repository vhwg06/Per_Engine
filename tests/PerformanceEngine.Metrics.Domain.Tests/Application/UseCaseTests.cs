namespace PerformanceEngine.Metrics.Domain.Tests.Application;

using FluentAssertions;
using PerformanceEngine.Metrics.Domain.Application.Dto;
using PerformanceEngine.Metrics.Domain.Application.UseCases;
using PerformanceEngine.Metrics.Domain.Metrics;
using Xunit;

/// <summary>
/// Tests for application use cases: validation, normalization, and metric computation.
/// </summary>
public sealed class UseCaseTests
{
    [Fact]
    public void NormalizeSamplesUseCase_ConvertsAllSamplesToMilliseconds()
    {
        // Arrange
        var context = new ExecutionContext("test-engine", Guid.NewGuid());
        var now = DateTime.UtcNow.AddSeconds(-5);
        var sample1 = new Sample(now, new Latency(5000, LatencyUnit.Microseconds), SampleStatus.Success, null, context);
        var sample2 = new Sample(now, new Latency(2, LatencyUnit.Seconds), SampleStatus.Success, null, context);
        var samples = new SampleCollection()
            .Add(sample1)
            .Add(sample2);

        var useCase = new NormalizeSamplesUseCase();

        // Act
        var result = useCase.Execute(samples);

        // Assert
        result.Should().NotBeNull();
        result.Count.Should().Be(2);
        // Normalized values should be in milliseconds
        result.AllSamples.Should().AllSatisfy(s => s.Duration.Unit.Should().Be(LatencyUnit.Milliseconds));
    }

    [Fact]
    public void ValidateAggregationUseCase_PassesForValidRequest()
    {
        // Arrange
        var context = new ExecutionContext("test-engine", Guid.NewGuid());
        var now = DateTime.UtcNow.AddSeconds(-5);
        var sample = new Sample(now, new Latency(100, LatencyUnit.Milliseconds), SampleStatus.Success, null, context);
        var samples = new SampleCollection().Add(sample);
        var window = AggregationWindow.FullExecution();
        var request = new AggregationRequestDto(samples, window, "average");

        var useCase = new ValidateAggregationUseCase();

        // Act
        var result = useCase.Execute(request);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void ValidateAggregationUseCase_FailsForNullRequest()
    {
        // Arrange
        var useCase = new ValidateAggregationUseCase();

        // Act & Assert
        var ex = Assert.Throws<ArgumentNullException>(() => useCase.Execute(null!));
        ex.ParamName.Should().Be("request");
    }

    [Fact]
    public void ValidateAggregationUseCase_FailsForEmptyOperationName()
    {
        // Arrange
        var context = new ExecutionContext("test-engine", Guid.NewGuid());
        var now = DateTime.UtcNow.AddSeconds(-5);
        var sample = new Sample(now, new Latency(100, LatencyUnit.Milliseconds), SampleStatus.Success, null, context);
        var samples = new SampleCollection().Add(sample);
        var window = AggregationWindow.FullExecution();

        // Act & Assert - DTO constructor validates and throws for empty operation name
        var ex = Assert.Throws<ArgumentException>(() => 
            new AggregationRequestDto(samples, window, "  "));
        ex.Message.Should().Contain("Aggregation operation cannot be null or empty");
    }

    [Fact]
    public void ComputeMetricUseCase_ComputesAverageSuccessfully()
    {
        // Arrange
        var context = new ExecutionContext("test-engine", Guid.NewGuid());
        var now = DateTime.UtcNow.AddSeconds(-5);
        var sample1 = new Sample(now, new Latency(100, LatencyUnit.Milliseconds), SampleStatus.Success, null, context);
        var sample2 = new Sample(now, new Latency(200, LatencyUnit.Milliseconds), SampleStatus.Success, null, context);
        var samples = new SampleCollection()
            .Add(sample1)
            .Add(sample2);
        var window = AggregationWindow.FullExecution();
        var useCase = new ComputeMetricUseCase();

        // Act
        var metric = useCase.Execute(samples, window, "average");

        // Assert
        metric.Should().NotBeNull();
        metric.MetricType.Should().Be("average");
        metric.Samples.Count.Should().Be(2);
        metric.AggregatedValues.Should().HaveCount(1);
        var result = metric.AggregatedValues[0];
        result.Value.Value.Should().BeApproximately(150, 0.1); // Average of 100 and 200
    }

    [Fact]
    public void ComputeMetricUseCase_ComputesPercentileSuccessfully()
    {
        // Arrange
        var context = new ExecutionContext("test-engine", Guid.NewGuid());
        var samples = new SampleCollection();
        var baseTime = DateTime.UtcNow.AddSeconds(-10);
        for (int i = 1; i <= 100; i++)
        {
            var sample = new Sample(
                baseTime.AddMilliseconds(i),
                new Latency(i, LatencyUnit.Milliseconds),
                SampleStatus.Success,
                null,
                context);
            samples = samples.Add(sample);
        }
        var window = AggregationWindow.FullExecution();
        var useCase = new ComputeMetricUseCase();

        // Act
        var metric = useCase.Execute(samples, window, "p95");

        // Assert
        metric.Should().NotBeNull();
        metric.MetricType.Should().Be("p95");
        metric.AggregatedValues.Should().HaveCount(1);
        var result = metric.AggregatedValues[0];
        // p95 of [1..100] should be approximately 95
        result.Value.Value.Should().BeGreaterThan(90).And.BeLessThanOrEqualTo(100);
    }

    [Fact]
    public void ComputeMetricUseCase_FailsForInvalidOperationName()
    {
        // Arrange
        var context = new ExecutionContext("test-engine", Guid.NewGuid());
        var now = DateTime.UtcNow.AddSeconds(-5);
        var sample = new Sample(now, new Latency(100, LatencyUnit.Milliseconds), SampleStatus.Success, null, context);
        var samples = new SampleCollection().Add(sample);
        var window = AggregationWindow.FullExecution();
        var useCase = new ComputeMetricUseCase();

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => useCase.Execute(samples, window, "invalid-op"));
        ex.Message.Should().Contain("Unknown aggregation operation");
    }

    [Fact]
    public void ComputeMetricUseCase_ComputesMaxSuccessfully()
    {
        // Arrange
        var context = new ExecutionContext("test-engine", Guid.NewGuid());
        var now = DateTime.UtcNow.AddSeconds(-5);
        var sample1 = new Sample(now, new Latency(50, LatencyUnit.Milliseconds), SampleStatus.Success, null, context);
        var sample2 = new Sample(now, new Latency(300, LatencyUnit.Milliseconds), SampleStatus.Success, null, context);
        var sample3 = new Sample(now, new Latency(150, LatencyUnit.Milliseconds), SampleStatus.Success, null, context);
        var samples = new SampleCollection()
            .Add(sample1)
            .Add(sample2)
            .Add(sample3);
        var window = AggregationWindow.FullExecution();
        var useCase = new ComputeMetricUseCase();

        // Act
        var metric = useCase.Execute(samples, window, "max");

        // Assert
        metric.Should().NotBeNull();
        metric.AggregatedValues.Should().HaveCount(1);
        var result = metric.AggregatedValues[0];
        result.Value.Value.Should().Be(300);
    }

    [Fact]
    public void ComputeMetricUseCase_ComputesMinSuccessfully()
    {
        // Arrange
        var context = new ExecutionContext("test-engine", Guid.NewGuid());
        var now = DateTime.UtcNow.AddSeconds(-5);
        var sample1 = new Sample(now, new Latency(50, LatencyUnit.Milliseconds), SampleStatus.Success, null, context);
        var sample2 = new Sample(now, new Latency(300, LatencyUnit.Milliseconds), SampleStatus.Success, null, context);
        var sample3 = new Sample(now, new Latency(150, LatencyUnit.Milliseconds), SampleStatus.Success, null, context);
        var samples = new SampleCollection()
            .Add(sample1)
            .Add(sample2)
            .Add(sample3);
        var window = AggregationWindow.FullExecution();
        var useCase = new ComputeMetricUseCase();

        // Act
        var metric = useCase.Execute(samples, window, "min");

        // Assert
        metric.Should().NotBeNull();
        metric.AggregatedValues.Should().HaveCount(1);
        var result = metric.AggregatedValues[0];
        result.Value.Value.Should().Be(50);
    }

    [Fact]
    public void ComputeMetricUseCase_ThrowsForNullSamples()
    {
        // Arrange
        var window = AggregationWindow.FullExecution();
        var useCase = new ComputeMetricUseCase();

        // Act & Assert
        var ex = Assert.Throws<ArgumentNullException>(() => useCase.Execute(null!, window, "average"));
        ex.ParamName.Should().Be("samples");
    }

    [Fact]
    public void ComputeMetricUseCase_ThrowsForNullWindow()
    {
        // Arrange
        var context = new ExecutionContext("test-engine", Guid.NewGuid());
        var now = DateTime.UtcNow.AddSeconds(-5);
        var sample = new Sample(now, new Latency(100, LatencyUnit.Milliseconds), SampleStatus.Success, null, context);
        var samples = new SampleCollection().Add(sample);
        var useCase = new ComputeMetricUseCase();

        // Act & Assert
        var ex = Assert.Throws<ArgumentNullException>(() => useCase.Execute(samples, null!, "average"));
        ex.ParamName.Should().Be("window");
    }
}
