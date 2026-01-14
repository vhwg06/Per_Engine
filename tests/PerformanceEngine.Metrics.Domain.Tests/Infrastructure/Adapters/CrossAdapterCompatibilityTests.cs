namespace PerformanceEngine.Metrics.Domain.Tests.Infrastructure.Adapters;

using FluentAssertions;
using PerformanceEngine.Metrics.Domain.Infrastructure.Adapters;
using PerformanceEngine.Metrics.Domain.Metrics;
using Xunit;

/// <summary>
/// Tests verifying that different adapters produce equivalent results from similar inputs.
/// Ensures engine-agnostic behavior and compatibility.
/// </summary>
public sealed class CrossAdapterCompatibilityTests
{
    [Fact]
    public void K6AndJMeterAdapters_ProduceSameSampleCount()
    {
        // Arrange
        var now = DateTime.UtcNow.AddSeconds(-5);
        var executionId = Guid.NewGuid();
        var k6Adapter = new K6EngineAdapter();
        var jmeterAdapter = new JMeterEngineAdapter();

        var k6Results = new[]
        {
            K6ResultData.Create(now, 100, 200, false),
            K6ResultData.Create(now.AddSeconds(1), 150, 200, false),
            K6ResultData.Create(now.AddSeconds(2), 120, 200, false)
        };

        var jmeterResults = new[]
        {
            JMeterResultData.Create(now, 100, "200", "OK", true),
            JMeterResultData.Create(now.AddSeconds(1), 150, "200", "OK", true),
            JMeterResultData.Create(now.AddSeconds(2), 120, "200", "OK", true)
        };

        // Act
        var k6Samples = k6Adapter.MapK6ResultsToDomain(k6Results, executionId);
        var jmeterSamples = jmeterAdapter.MapJMeterResultsToDomain(jmeterResults, executionId);

        // Assert
        k6Samples.Count.Should().Be(jmeterSamples.Count);
    }

    [Fact]
    public void K6AndJMeterAdapters_ProduceCompatibleLatencies()
    {
        // Arrange
        var now = DateTime.UtcNow.AddSeconds(-5);
        var executionId = Guid.NewGuid();
        var k6Adapter = new K6EngineAdapter();
        var jmeterAdapter = new JMeterEngineAdapter();

        var k6Results = new[]
        {
            K6ResultData.Create(now, 100.0, 200, false),
            K6ResultData.Create(now.AddSeconds(1), 200.0, 200, false)
        };

        var jmeterResults = new[]
        {
            JMeterResultData.Create(now, 100.0, "200", "OK", true),
            JMeterResultData.Create(now.AddSeconds(1), 200.0, "200", "OK", true)
        };

        // Act
        var k6Samples = k6Adapter.MapK6ResultsToDomain(k6Results, executionId);
        var jmeterSamples = jmeterAdapter.MapJMeterResultsToDomain(jmeterResults, executionId);

        // Assert - Both should have compatible latency values
        var k6Latencies = k6Samples.AllSamples.Select(s => s.Duration.Value).ToList();
        var jmeterLatencies = jmeterSamples.AllSamples.Select(s => s.Duration.Value).ToList();

        for (int i = 0; i < k6Latencies.Count; i++)
        {
            k6Latencies[i].Should().BeApproximately(jmeterLatencies[i], 0.01);
        }
    }

    [Fact]
    public void K6AndJMeterAdapters_BothNormalizeToMilliseconds()
    {
        // Arrange
        var now = DateTime.UtcNow.AddSeconds(-5);
        var executionId = Guid.NewGuid();
        var k6Adapter = new K6EngineAdapter();
        var jmeterAdapter = new JMeterEngineAdapter();

        var k6Results = new[]
        {
            K6ResultData.Create(now, 100, 200, false)
        };

        var jmeterResults = new[]
        {
            JMeterResultData.Create(now, 100, "200", "OK", true)
        };

        // Act
        var k6Samples = k6Adapter.MapK6ResultsToDomain(k6Results, executionId);
        var jmeterSamples = jmeterAdapter.MapJMeterResultsToDomain(jmeterResults, executionId);

        // Assert - Both should report latencies in milliseconds
        k6Samples.AllSamples.First().Duration.Unit.Should().Be(LatencyUnit.Milliseconds);
        jmeterSamples.AllSamples.First().Duration.Unit.Should().Be(LatencyUnit.Milliseconds);
    }

    [Fact]
    public void K6AndJMeterAdapters_HandleSuccessStatusConsistently()
    {
        // Arrange
        var now = DateTime.UtcNow.AddSeconds(-5);
        var executionId = Guid.NewGuid();
        var k6Adapter = new K6EngineAdapter();
        var jmeterAdapter = new JMeterEngineAdapter();

        var k6Results = new[]
        {
            K6ResultData.Create(now, 100, 200, false) // Not failed = success
        };

        var jmeterResults = new[]
        {
            JMeterResultData.Create(now, 100, "200", "OK", true) // Successful = success
        };

        // Act
        var k6Samples = k6Adapter.MapK6ResultsToDomain(k6Results, executionId);
        var jmeterSamples = jmeterAdapter.MapJMeterResultsToDomain(jmeterResults, executionId);

        // Assert - Both should have success status
        k6Samples.AllSamples.First().Status.Should().Be(SampleStatus.Success);
        jmeterSamples.AllSamples.First().Status.Should().Be(SampleStatus.Success);
        k6Samples.AllSamples.First().IsSuccess.Should().BeTrue();
        jmeterSamples.AllSamples.First().IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void K6AndJMeterAdapters_HandleFailureStatusConsistently()
    {
        // Arrange
        var now = DateTime.UtcNow.AddSeconds(-5);
        var executionId = Guid.NewGuid();
        var k6Adapter = new K6EngineAdapter();
        var jmeterAdapter = new JMeterEngineAdapter();

        var k6Results = new[]
        {
            K6ResultData.Create(now, 100, 500, true) // Failed
        };

        var jmeterResults = new[]
        {
            JMeterResultData.Create(now, 100, "500", "Internal Server Error", false) // Not successful
        };

        // Act
        var k6Samples = k6Adapter.MapK6ResultsToDomain(k6Results, executionId);
        var jmeterSamples = jmeterAdapter.MapJMeterResultsToDomain(jmeterResults, executionId);

        // Assert - Both should have failure status
        k6Samples.AllSamples.First().Status.Should().Be(SampleStatus.Failure);
        jmeterSamples.AllSamples.First().Status.Should().Be(SampleStatus.Failure);
        k6Samples.AllSamples.First().IsFailure.Should().BeTrue();
        jmeterSamples.AllSamples.First().IsFailure.Should().BeTrue();
    }

    [Fact]
    public void K6AndJMeterAdapters_PreserveExecutionContextConsistently()
    {
        // Arrange
        var now = DateTime.UtcNow.AddSeconds(-5);
        var executionId = Guid.NewGuid();
        var k6Adapter = new K6EngineAdapter();
        var jmeterAdapter = new JMeterEngineAdapter();

        var k6Results = new[] { K6ResultData.Create(now, 100, 200, false) };
        var jmeterResults = new[] { JMeterResultData.Create(now, 100, "200", "OK", true) };

        // Act
        var k6Samples = k6Adapter.MapK6ResultsToDomain(k6Results, executionId, "test-scenario");
        var jmeterSamples = jmeterAdapter.MapJMeterResultsToDomain(jmeterResults, executionId, "test-plan");

        // Assert - Execution ID should be preserved, engine names should differ
        var k6Sample = k6Samples.AllSamples.First();
        var jmeterSample = jmeterSamples.AllSamples.First();

        k6Sample.ExecutionContext.ExecutionId.Should().Be(executionId);
        jmeterSample.ExecutionContext.ExecutionId.Should().Be(executionId);

        k6Sample.ExecutionContext.EngineName.Should().Be("k6");
        jmeterSample.ExecutionContext.EngineName.Should().Be("jmeter");

        k6Sample.ExecutionContext.ScenarioName.Should().Be("test-scenario");
        jmeterSample.ExecutionContext.ScenarioName.Should().Be("test-plan");
    }

    [Fact]
    public void BothAdapters_CanBeUsedInSequenceWithoutStateConflict()
    {
        // Arrange
        var executionId = Guid.NewGuid();
        var k6Adapter = new K6EngineAdapter();
        var jmeterAdapter = new JMeterEngineAdapter();
        var now = DateTime.UtcNow.AddSeconds(-5);

        var k6Results = new[] { K6ResultData.Create(now, 100, 200, false) };
        var jmeterResults = new[] { JMeterResultData.Create(now, 100, "200", "OK", true) };

        // Act - Use both adapters in sequence (verifying no shared state issues)
        var k6Collection1 = k6Adapter.MapK6ResultsToDomain(k6Results, executionId);
        var jmeterCollection = jmeterAdapter.MapJMeterResultsToDomain(jmeterResults, executionId);
        var k6Collection2 = k6Adapter.MapK6ResultsToDomain(k6Results, executionId);

        // Assert - Results should be consistent despite interleaved usage
        k6Collection1.Count.Should().Be(k6Collection2.Count);
        jmeterCollection.Count.Should().Be(1);
    }
}
