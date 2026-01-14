namespace PerformanceEngine.Metrics.Domain.Tests.Infrastructure.Adapters;

using FluentAssertions;
using PerformanceEngine.Metrics.Domain.Infrastructure.Adapters;
using PerformanceEngine.Metrics.Domain.Metrics;
using Xunit;

/// <summary>
/// Tests for K6 engine adapter mapping and error classification.
/// </summary>
public sealed class K6AdapterTests
{
    [Fact]
    public void MapK6ResultsToDomain_CreatesValidSampleCollection()
    {
        // Arrange
        var adapter = new K6EngineAdapter();
        var executionId = Guid.NewGuid();
        var now = DateTime.UtcNow.AddSeconds(-5);
        var k6Results = new[]
        {
            K6ResultData.Create(now, 100, 200, false),
            K6ResultData.Create(now.AddSeconds(1), 150, 200, false)
        };

        // Act
        var result = adapter.MapK6ResultsToDomain(k6Results, executionId, "test-scenario");

        // Assert
        result.Should().NotBeNull();
        result.Count.Should().Be(2);
        result.AllSamples.Should().AllSatisfy(s =>
        {
            s.Status.Should().Be(SampleStatus.Success);
            s.ErrorClassification.Should().BeNull();
            s.ExecutionContext.EngineName.Should().Be("k6");
            s.ExecutionContext.ExecutionId.Should().Be(executionId);
            s.ExecutionContext.ScenarioName.Should().Be("test-scenario");
        });
    }

    [Fact]
    public void MapK6ResultsToDomain_HandleFailedRequests()
    {
        // Arrange
        var adapter = new K6EngineAdapter();
        var executionId = Guid.NewGuid();
        var now = DateTime.UtcNow.AddSeconds(-5);
        var k6Results = new[]
        {
            K6ResultData.Create(now, 100, 500, true, "ERR_K6_DIAL_SOCKET"),
            K6ResultData.Create(now.AddSeconds(1), 150, 200, false)
        };

        // Act
        var result = adapter.MapK6ResultsToDomain(k6Results, executionId);

        // Assert
        result.Should().NotBeNull();
        result.Count.Should().Be(2);
        result.FailedSamples.Should().HaveCount(1);
        result.SuccessfulSamples.Should().HaveCount(1);
        
        var failedSample = result.FailedSamples.First();
        failedSample.Status.Should().Be(SampleStatus.Failure);
        failedSample.ErrorClassification.Should().Be(ErrorClassification.NetworkError);
    }

    [Fact]
    public void MapK6ResultsToDomain_ClassifiesTimeoutErrors()
    {
        // Arrange
        var adapter = new K6EngineAdapter();
        var executionId = Guid.NewGuid();
        var now = DateTime.UtcNow.AddSeconds(-5);
        var k6Results = new[]
        {
            K6ResultData.Create(now, 100, 0, true, "ERR_K6_TIMEOUT")
        };

        // Act
        var result = adapter.MapK6ResultsToDomain(k6Results, executionId);

        // Assert
        result.Count.Should().Be(1);
        var sample = result.AllSamples.First();
        sample.ErrorClassification.Should().Be(ErrorClassification.Timeout);
    }

    [Fact]
    public void MapK6ResultsToDomain_ClassifiesServerErrors()
    {
        // Arrange
        var adapter = new K6EngineAdapter();
        var executionId = Guid.NewGuid();
        var now = DateTime.UtcNow.AddSeconds(-5);
        var k6Results = new[]
        {
            K6ResultData.Create(now, 100, 503, true)
        };

        // Act
        var result = adapter.MapK6ResultsToDomain(k6Results, executionId);

        // Assert
        result.Count.Should().Be(1);
        var sample = result.AllSamples.First();
        sample.ErrorClassification.Should().Be(ErrorClassification.ApplicationError);
    }

    [Fact]
    public void MapK6ResultsToDomain_ClassifiesClientErrors()
    {
        // Arrange
        var adapter = new K6EngineAdapter();
        var executionId = Guid.NewGuid();
        var now = DateTime.UtcNow.AddSeconds(-5);
        var k6Results = new[]
        {
            K6ResultData.Create(now, 100, 404, true)
        };

        // Act
        var result = adapter.MapK6ResultsToDomain(k6Results, executionId);

        // Assert
        result.Count.Should().Be(1);
        var sample = result.AllSamples.First();
        sample.ErrorClassification.Should().Be(ErrorClassification.ApplicationError);
    }

    [Fact]
    public void MapK6ResultsToDomain_PreservesMetadata()
    {
        // Arrange
        var adapter = new K6EngineAdapter();
        var executionId = Guid.NewGuid();
        var now = DateTime.UtcNow.AddSeconds(-5);
        var k6Results = new[]
        {
            K6ResultData.Create(now, 100.5, 200, false)
        };

        // Act
        var result = adapter.MapK6ResultsToDomain(k6Results, executionId);

        // Assert
        var sample = result.AllSamples.First();
        sample.Metadata.Should().NotBeNull();
        sample.Metadata!.Should().ContainKey("http_status_code");
        sample.Metadata!["http_status_code"].Should().Be(200);
        sample.Metadata!.Should().ContainKey("http_req_duration_ms");
        sample.Metadata!["http_req_duration_ms"].Should().Be(100.5);
        sample.Metadata!.Should().ContainKey("http_req_failed");
        sample.Metadata!["http_req_failed"].Should().Be(false);
    }

    [Fact]
    public void MapK6ResultsToDomain_HandlesNullErrorCode()
    {
        // Arrange
        var adapter = new K6EngineAdapter();
        var executionId = Guid.NewGuid();
        var now = DateTime.UtcNow.AddSeconds(-5);
        var k6Results = new[]
        {
            K6ResultData.Create(now, 100, 200, false, null)
        };

        // Act
        var result = adapter.MapK6ResultsToDomain(k6Results, executionId);

        // Assert
        result.Count.Should().Be(1);
        var sample = result.AllSamples.First();
        sample.Status.Should().Be(SampleStatus.Success);
        sample.ErrorClassification.Should().BeNull();
    }

    [Fact]
    public void K6ResultData_ValidatesNegativeDuration()
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => 
            K6ResultData.Create(DateTime.UtcNow.AddSeconds(-5), -1, 200, false));
        ex.Message.Should().Contain("non-negative");
    }

    [Fact]
    public void K6ResultData_AcceptsZeroDuration()
    {
        // Act
        var result = K6ResultData.Create(DateTime.UtcNow.AddSeconds(-5), 0, 200, false);

        // Assert
        result.HttpReqDurationMs.Should().Be(0);
    }

    [Fact]
    public void MapK6ResultsToDomain_ThrowsForNullResults()
    {
        // Arrange
        var adapter = new K6EngineAdapter();

        // Act & Assert
        var ex = Assert.Throws<ArgumentNullException>(() => 
            adapter.MapK6ResultsToDomain(null!, Guid.NewGuid()));
        ex.ParamName.Should().Be("k6Results");
    }
}
