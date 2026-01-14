namespace PerformanceEngine.Metrics.Domain.Tests.Application.Services;

using FluentAssertions;
using PerformanceEngine.Metrics.Domain.Application.Dto;
using PerformanceEngine.Metrics.Domain.Application.Services;
using PerformanceEngine.Metrics.Domain.Infrastructure.Adapters;
using PerformanceEngine.Metrics.Domain.Metrics;
using Xunit;

/// <summary>
/// End-to-end integration tests for MetricService with adapters.
/// Verifies complete workflow from adapter output to computed metrics.
/// </summary>
public sealed class MetricServiceIntegrationTests
{
    [Fact]
    public void MetricService_ComputesMetricFromK6AdaptedSamples()
    {
        // Arrange
        var k6Adapter = new K6EngineAdapter();
        var metricService = new MetricService();
        var executionId = Guid.NewGuid();
        var now = DateTime.UtcNow.AddSeconds(-5); // Use past timestamp

        // Create K6 results
        var k6Results = new[]
        {
            K6ResultData.Create(now, 100, 200, false),
            K6ResultData.Create(now.AddMilliseconds(100), 120, 200, false),
            K6ResultData.Create(now.AddMilliseconds(200), 80, 200, false)
        };

        // Map to domain
        var samples = k6Adapter.MapK6ResultsToDomain(k6Results, executionId);

        // Create aggregation request
        var request = new AggregationRequestDto(
            samples,
            AggregationWindow.FullExecution(),
            "average");

        // Act
        var result = metricService.ComputeMetric(request);

        // Assert
        result.Should().NotBeNull();
        result!.MetricType.Should().Be("average");
        result.Samples.Count.Should().Be(3);
        result.AggregatedValues.Should().HaveCount(1);
        // Average of 100, 120, 80 = 100
        result.AggregatedValues[0].Value.Should().BeApproximately(100, 0.1);
    }

    [Fact]
    public void MetricService_ComputesMetricFromJMeterAdaptedSamples()
    {
        // Arrange
        var jmeterAdapter = new JMeterEngineAdapter();
        var metricService = new MetricService();
        var executionId = Guid.NewGuid();
        var now = DateTime.UtcNow.AddSeconds(-5);

        // Create JMeter results
        var jmeterResults = new[]
        {
            JMeterResultData.Create(now, 100, "200", "OK", true),
            JMeterResultData.Create(now.AddMilliseconds(100), 120, "200", "OK", true),
            JMeterResultData.Create(now.AddMilliseconds(200), 80, "200", "OK", true)
        };

        // Map to domain
        var samples = jmeterAdapter.MapJMeterResultsToDomain(jmeterResults, executionId);

        // Create aggregation request
        var request = new AggregationRequestDto(
            samples,
            AggregationWindow.FullExecution(),
            "average");

        // Act
        var result = metricService.ComputeMetric(request);

        // Assert
        result.Should().NotBeNull();
        result!.MetricType.Should().Be("average");
        result.Samples.Count.Should().Be(3);
        result.AggregatedValues.Should().HaveCount(1);
        result.AggregatedValues[0].Value.Should().BeApproximately(100, 0.1);
    }

    [Fact]
    public void MetricService_ComputesP95PercentileFromAdaptedSamples()
    {
        // Arrange
        var k6Adapter = new K6EngineAdapter();
        var metricService = new MetricService();
        var executionId = Guid.NewGuid();
        var now = DateTime.UtcNow.AddSeconds(-5);

        // Create 100 K6 results with latencies 1..100ms
        var k6Results = Enumerable.Range(1, 100)
            .Select(i => K6ResultData.Create(now.AddMilliseconds(i), i, 200, false))
            .ToArray();

        var samples = k6Adapter.MapK6ResultsToDomain(k6Results, executionId);

        var request = new AggregationRequestDto(
            samples,
            AggregationWindow.FullExecution(),
            "p95");

        // Act
        var result = metricService.ComputeMetric(request);

        // Assert
        result.Should().NotBeNull();
        result!.MetricType.Should().Be("p95");
        result.AggregatedValues.Should().HaveCount(1);
        // p95 of [1..100] should be >= 90 and <= 100
        result.AggregatedValues[0].Value.Should().BeGreaterThanOrEqualTo(90).And.BeLessThanOrEqualTo(100);
    }

    [Fact]
    public void MetricService_HandlesFailedSamplesInComputation()
    {
        // Arrange
        var jmeterAdapter = new JMeterEngineAdapter();
        var metricService = new MetricService();
        var executionId = Guid.NewGuid();
        var now = DateTime.UtcNow.AddSeconds(-5);

        // Mix of successful and failed samples
        var jmeterResults = new[]
        {
            JMeterResultData.Create(now, 100, "200", "OK", true),
            JMeterResultData.Create(now.AddMilliseconds(100), 150, "500", "Server Error", false),
            JMeterResultData.Create(now.AddMilliseconds(200), 120, "200", "OK", true)
        };

        var samples = jmeterAdapter.MapJMeterResultsToDomain(jmeterResults, executionId);

        var request = new AggregationRequestDto(
            samples,
            AggregationWindow.FullExecution(),
            "average");

        // Act - Aggregation includes both successful and failed samples
        var result = metricService.ComputeMetric(request);

        // Assert
        result.Should().NotBeNull();
        result!.Samples.Count.Should().Be(3);
        // Count successful by checking original samples
        var successCount = samples.SuccessfulSamples.Count();
        var failureCount = samples.FailedSamples.Count();
        successCount.Should().Be(2);
        failureCount.Should().Be(1);
        // Average of all 3: (100 + 150 + 120) / 3 = 123.33
        result.AggregatedValues[0].Value.Should().BeApproximately(123.33, 0.1);
    }

    [Fact]
    public void MetricService_ComputesMaxFromAdaptedSamples()
    {
        // Arrange
        var k6Adapter = new K6EngineAdapter();
        var metricService = new MetricService();
        var executionId = Guid.NewGuid();
        var now = DateTime.UtcNow.AddSeconds(-5);

        var k6Results = new[]
        {
            K6ResultData.Create(now, 50, 200, false),
            K6ResultData.Create(now.AddMilliseconds(100), 300, 200, false),
            K6ResultData.Create(now.AddMilliseconds(200), 150, 200, false)
        };

        var samples = k6Adapter.MapK6ResultsToDomain(k6Results, executionId);

        var request = new AggregationRequestDto(
            samples,
            AggregationWindow.FullExecution(),
            "max");

        // Act
        var result = metricService.ComputeMetric(request);

        // Assert
        result.Should().NotBeNull();
        result!.AggregatedValues[0].Value.Should().Be(300);
    }

    [Fact]
    public void MetricService_ComputesMinFromAdaptedSamples()
    {
        // Arrange
        var jmeterAdapter = new JMeterEngineAdapter();
        var metricService = new MetricService();
        var executionId = Guid.NewGuid();
        var now = DateTime.UtcNow.AddSeconds(-5);

        var jmeterResults = new[]
        {
            JMeterResultData.Create(now, 50, "200", "OK", true),
            JMeterResultData.Create(now.AddMilliseconds(100), 300, "200", "OK", true),
            JMeterResultData.Create(now.AddMilliseconds(200), 150, "200", "OK", true)
        };

        var samples = jmeterAdapter.MapJMeterResultsToDomain(jmeterResults, executionId);

        var request = new AggregationRequestDto(
            samples,
            AggregationWindow.FullExecution(),
            "min");

        // Act
        var result = metricService.ComputeMetric(request);

        // Assert
        result.Should().NotBeNull();
        result!.AggregatedValues[0].Value.Should().Be(50);
    }

    [Fact]
    public void MetricService_ReturnsNullForInvalidRequest()
    {
        // Arrange
        var metricService = new MetricService();

        // Act & Assert
        var ex = Assert.Throws<ArgumentNullException>(() => metricService.ComputeMetric(null!));
        ex.ParamName.Should().Be("request");
    }

    [Fact]
    public void MetricService_ConvertsMetricToDtoSuccessfully()
    {
        // Arrange
        var k6Adapter = new K6EngineAdapter();
        var metricService = new MetricService();
        var executionId = Guid.NewGuid();
        var now = DateTime.UtcNow.AddSeconds(-5);

        var k6Results = new[]
        {
            K6ResultData.Create(now, 100, 200, false),
            K6ResultData.Create(now.AddMilliseconds(100), 200, 200, false)
        };

        var samples = k6Adapter.MapK6ResultsToDomain(k6Results, executionId);
        var request = new AggregationRequestDto(
            samples,
            AggregationWindow.FullExecution(),
            "average");

        // Act
        var metricDto = metricService.ComputeMetric(request);

        // Assert
        metricDto.Should().NotBeNull();
        metricDto!.Id.Should().NotBe(Guid.Empty);
        metricDto.Samples.Should().HaveCount(2);
        metricDto.WindowName.Should().Be("FullExecution");
        metricDto.MetricType.Should().Be("average");
        metricDto.AggregatedValues.Should().HaveCount(1);
        // ComputedAt should be recent (samples were created 5 seconds ago)
        metricDto.ComputedAt.Should().BeOnOrBefore(DateTime.UtcNow);
    }

    [Fact]
    public void MetricService_PreservesAdapterMetadataInDto()
    {
        // Arrange
        var k6Adapter = new K6EngineAdapter();
        var metricService = new MetricService();
        var executionId = Guid.NewGuid();
        var now = DateTime.UtcNow.AddSeconds(-5);

        var k6Results = new[]
        {
            K6ResultData.Create(now, 123.45, 404, false)
        };

        var samples = k6Adapter.MapK6ResultsToDomain(k6Results, executionId);
        var request = new AggregationRequestDto(
            samples,
            AggregationWindow.FullExecution(),
            "average");

        // Act
        var metricDto = metricService.ComputeMetric(request);

        // Assert
        metricDto.Should().NotBeNull();
        var sampleDto = metricDto!.Samples[0];
        // Metadata from K6 adapter should be preserved
        sampleDto.Metadata.Should().NotBeNull();
    }
}
