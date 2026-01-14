namespace PerformanceEngine.Metrics.Domain.Tests.Infrastructure.Adapters;

using FluentAssertions;
using PerformanceEngine.Metrics.Domain.Infrastructure.Adapters;
using PerformanceEngine.Metrics.Domain.Metrics;
using Xunit;

/// <summary>
/// Tests for JMeter engine adapter mapping and error classification.
/// </summary>
public sealed class JMeterAdapterTests
{
    [Fact]
    public void MapJMeterResultsToDomain_CreatesValidSampleCollection()
    {
        // Arrange
        var adapter = new JMeterEngineAdapter();
        var executionId = Guid.NewGuid();
        var now = DateTime.UtcNow.AddSeconds(-5);
        var jmeterResults = new[]
        {
            JMeterResultData.Create(now, 100, "200", "OK", true, "HTTP Request"),
            JMeterResultData.Create(now.AddSeconds(1), 150, "200", "OK", true, "HTTP Request")
        };

        // Act
        var result = adapter.MapJMeterResultsToDomain(jmeterResults, executionId, "test-plan");

        // Assert
        result.Should().NotBeNull();
        result.Count.Should().Be(2);
        result.AllSamples.Should().AllSatisfy(s =>
        {
            s.Status.Should().Be(SampleStatus.Success);
            s.ErrorClassification.Should().BeNull();
            s.ExecutionContext.EngineName.Should().Be("jmeter");
            s.ExecutionContext.ExecutionId.Should().Be(executionId);
            s.ExecutionContext.ScenarioName.Should().Be("test-plan");
        });
    }

    [Fact]
    public void MapJMeterResultsToDomain_HandleFailedRequests()
    {
        // Arrange
        var adapter = new JMeterEngineAdapter();
        var executionId = Guid.NewGuid();
        var now = DateTime.UtcNow.AddSeconds(-5);
        var jmeterResults = new[]
        {
            JMeterResultData.Create(now, 100, "500", "Internal Server Error", false, "HTTP Request"),
            JMeterResultData.Create(now.AddSeconds(1), 150, "200", "OK", true, "HTTP Request")
        };

        // Act
        var result = adapter.MapJMeterResultsToDomain(jmeterResults, executionId);

        // Assert
        result.Should().NotBeNull();
        result.Count.Should().Be(2);
        result.FailedSamples.Should().HaveCount(1);
        result.SuccessfulSamples.Should().HaveCount(1);
        
        var failedSample = result.FailedSamples.First();
        failedSample.Status.Should().Be(SampleStatus.Failure);
        failedSample.ErrorClassification.Should().Be(ErrorClassification.ApplicationError);
    }

    [Fact]
    public void MapJMeterResultsToDomain_ClassifiesNetworkErrors()
    {
        // Arrange
        var adapter = new JMeterEngineAdapter();
        var executionId = Guid.NewGuid();
        var now = DateTime.UtcNow.AddSeconds(-5);
        var jmeterResults = new[]
        {
            JMeterResultData.Create(now, 100, "Non HTTP response code: java.net.ConnectException", "Connection refused", false, "HTTP Request")
        };

        // Act
        var result = adapter.MapJMeterResultsToDomain(jmeterResults, executionId);

        // Assert
        result.Count.Should().Be(1);
        var sample = result.AllSamples.First();
        sample.ErrorClassification.Should().Be(ErrorClassification.NetworkError);
    }

    [Fact]
    public void MapJMeterResultsToDomain_ClassifiesTimeoutErrors()
    {
        // Arrange
        var adapter = new JMeterEngineAdapter();
        var executionId = Guid.NewGuid();
        var now = DateTime.UtcNow.AddSeconds(-5);
        var jmeterResults = new[]
        {
            JMeterResultData.Create(now, 100, "Non HTTP response code: java.net.SocketTimeoutException", "Socket timeout", false, "HTTP Request")
        };

        // Act
        var result = adapter.MapJMeterResultsToDomain(jmeterResults, executionId);

        // Assert
        result.Count.Should().Be(1);
        var sample = result.AllSamples.First();
        sample.ErrorClassification.Should().Be(ErrorClassification.Timeout);
    }

    [Fact]
    public void MapJMeterResultsToDomain_ClassifiesClientErrors()
    {
        // Arrange
        var adapter = new JMeterEngineAdapter();
        var executionId = Guid.NewGuid();
        var now = DateTime.UtcNow.AddSeconds(-5);
        var jmeterResults = new[]
        {
            JMeterResultData.Create(now, 100, "404", "Not Found", false, "HTTP Request")
        };

        // Act
        var result = adapter.MapJMeterResultsToDomain(jmeterResults, executionId);

        // Assert
        result.Count.Should().Be(1);
        var sample = result.AllSamples.First();
        sample.ErrorClassification.Should().Be(ErrorClassification.ApplicationError);
    }

    [Fact]
    public void MapJMeterResultsToDomain_ClassifiesServerErrors()
    {
        // Arrange
        var adapter = new JMeterEngineAdapter();
        var executionId = Guid.NewGuid();
        var now = DateTime.UtcNow.AddSeconds(-5);
        var jmeterResults = new[]
        {
            JMeterResultData.Create(now, 100, "502", "Bad Gateway", false, "HTTP Request")
        };

        // Act
        var result = adapter.MapJMeterResultsToDomain(jmeterResults, executionId);

        // Assert
        result.Count.Should().Be(1);
        var sample = result.AllSamples.First();
        sample.ErrorClassification.Should().Be(ErrorClassification.ApplicationError);
    }

    [Fact]
    public void MapJMeterResultsToDomain_ClassifiesSSLErrors()
    {
        // Arrange
        var adapter = new JMeterEngineAdapter();
        var executionId = Guid.NewGuid();
        var now = DateTime.UtcNow.AddSeconds(-5);
        var jmeterResults = new[]
        {
            JMeterResultData.Create(now, 100, "Non HTTP response code: javax.net.ssl.SSLException", "SSL handshake failed", false, "HTTP Request")
        };

        // Act
        var result = adapter.MapJMeterResultsToDomain(jmeterResults, executionId);

        // Assert
        result.Count.Should().Be(1);
        var sample = result.AllSamples.First();
        sample.ErrorClassification.Should().Be(ErrorClassification.NetworkError);
    }

    [Fact]
    public void MapJMeterResultsToDomain_PreservesMetadata()
    {
        // Arrange
        var adapter = new JMeterEngineAdapter();
        var executionId = Guid.NewGuid();
        var now = DateTime.UtcNow.AddSeconds(-5);
        var jmeterResults = new[]
        {
            JMeterResultData.Create(now, 123.45, "200", "OK", true, "HTTP Request")
        };

        // Act
        var result = adapter.MapJMeterResultsToDomain(jmeterResults, executionId);

        // Assert
        var sample = result.AllSamples.First();
        sample.Metadata.Should().NotBeNull();
        sample.Metadata!.Should().ContainKey("response_code");
        sample.Metadata!["response_code"].Should().Be("200");
        sample.Metadata!.Should().ContainKey("response_message");
        sample.Metadata!["response_message"].Should().Be("OK");
        sample.Metadata!.Should().ContainKey("elapsed_ms");
        sample.Metadata!["elapsed_ms"].Should().Be(123.45);
        sample.Metadata!.Should().ContainKey("success");
        sample.Metadata!["success"].Should().Be(true);
        sample.Metadata!.Should().ContainKey("sampler_label");
        sample.Metadata!["sampler_label"].Should().Be("HTTP Request");
    }

    [Fact]
    public void MapJMeterResultsToDomain_HandlesNullResponseCode()
    {
        // Arrange
        var adapter = new JMeterEngineAdapter();
        var executionId = Guid.NewGuid();
        var now = DateTime.UtcNow.AddSeconds(-5);
        var jmeterResults = new[]
        {
            JMeterResultData.Create(now, 100, null, null, true, "HTTP Request")
        };

        // Act
        var result = adapter.MapJMeterResultsToDomain(jmeterResults, executionId);

        // Assert
        result.Count.Should().Be(1);
        var sample = result.AllSamples.First();
        sample.Status.Should().Be(SampleStatus.Success);
    }

    [Fact]
    public void JMeterResultData_ValidatesNegativeDuration()
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => 
            JMeterResultData.Create(DateTime.UtcNow.AddSeconds(-5), -1, "200", "OK", true));
        ex.Message.Should().Contain("non-negative");
    }

    [Fact]
    public void JMeterResultData_AcceptsZeroDuration()
    {
        // Act
        var result = JMeterResultData.Create(DateTime.UtcNow.AddSeconds(-5), 0, "200", "OK", true);

        // Assert
        result.ElapsedMs.Should().Be(0);
    }

    [Fact]
    public void MapJMeterResultsToDomain_ThrowsForNullResults()
    {
        // Arrange
        var adapter = new JMeterEngineAdapter();

        // Act & Assert
        var ex = Assert.Throws<ArgumentNullException>(() => 
            adapter.MapJMeterResultsToDomain(null!, Guid.NewGuid()));
        ex.ParamName.Should().Be("jmeterResults");
    }
}
