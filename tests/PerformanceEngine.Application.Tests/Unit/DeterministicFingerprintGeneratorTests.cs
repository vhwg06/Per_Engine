namespace PerformanceEngine.Application.Tests.Unit;

using FluentAssertions;
using PerformanceEngine.Application.Services;
using PerformanceEngine.Metrics.Domain.Metrics;
using Xunit;

/// <summary>
/// Tests for deterministic fingerprint generation.
/// Same metrics must always produce same fingerprint.
/// </summary>
public class DeterministicFingerprintGeneratorTests
{
    [Fact]
    public void GenerateFingerprint_SameMetrics_ProducesSameHash()
    {
        // Arrange
        var generator = new DeterministicFingerprintGenerator();
        var samples = CreateTestSamples();
        
        // Act
        var fingerprint1 = generator.GenerateFingerprint(samples);
        var fingerprint2 = generator.GenerateFingerprint(samples);
        
        // Assert
        fingerprint1.Should().Be(fingerprint2);
        fingerprint1.Should().NotBeNullOrEmpty();
        fingerprint1.Length.Should().Be(64); // SHA256 produces 64 hex characters
    }

    [Fact]
    public void GenerateFingerprint_DifferentMetrics_ProducesDifferentHash()
    {
        // Arrange
        var generator = new DeterministicFingerprintGenerator();
        var samples1 = CreateTestSamples();
        var samples2 = CreateDifferentTestSamples();
        
        // Act
        var fingerprint1 = generator.GenerateFingerprint(samples1);
        var fingerprint2 = generator.GenerateFingerprint(samples2);
        
        // Assert
        fingerprint1.Should().NotBe(fingerprint2);
    }

    [Fact]
    public void GenerateFingerprint_EmptyMetrics_ReturnsConsistentHash()
    {
        // Arrange
        var generator = new DeterministicFingerprintGenerator();
        var emptySamples = Array.Empty<Sample>();
        
        // Act
        var fingerprint1 = generator.GenerateFingerprint(emptySamples);
        var fingerprint2 = generator.GenerateFingerprint(emptySamples);
        
        // Assert
        fingerprint1.Should().Be(fingerprint2);
        fingerprint1.Should().NotBeNullOrEmpty();
    }

    private IReadOnlyCollection<Sample> CreateTestSamples()
    {
        var executionContext = new PerformanceEngine.Metrics.Domain.Metrics.ExecutionContext("test-engine", Guid.NewGuid());
        var latency = new Latency(100.0, LatencyUnit.Milliseconds);
        
        var sample1 = new Sample(
            new DateTime(2026, 1, 15, 12, 0, 0, DateTimeKind.Utc),
            latency,
            SampleStatus.Success,
            null,
            executionContext);
        
        return new[] { sample1 };
    }

    private IReadOnlyCollection<Sample> CreateDifferentTestSamples()
    {
        var executionContext = new PerformanceEngine.Metrics.Domain.Metrics.ExecutionContext("test-engine", Guid.NewGuid());
        var latency = new Latency(200.0, LatencyUnit.Milliseconds); // Different latency
        
        var sample1 = new Sample(
            new DateTime(2026, 1, 15, 12, 0, 0, DateTimeKind.Utc),
            latency,
            SampleStatus.Success,
            null,
            executionContext);
        
        return new[] { sample1 };
    }
}
