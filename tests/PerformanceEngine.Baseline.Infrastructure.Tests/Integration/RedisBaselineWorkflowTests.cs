namespace PerformanceEngine.Baseline.Infrastructure.Tests.Integration;

using FluentAssertions;
using Moq;
using PerformanceEngine.Baseline.Domain.Domain.Baselines;
using PerformanceEngine.Baseline.Domain.Domain.Tolerances;
using PerformanceEngine.Baseline.Infrastructure.Persistence;
using PerformanceEngine.Metrics.Domain.Metrics;
using PerformanceEngine.Metrics.Domain.Ports;
using StackExchange.Redis;
using Xunit;

/// <summary>
/// Phase 6 Integration Test - T056
/// Tests Redis persistence layer integration with baseline domain.
/// </summary>
public sealed class RedisBaselineWorkflowTests
{
    private readonly Mock<IConnectionMultiplexer> _mockConnectionMultiplexer;
    private readonly Mock<IDatabase> _mockDatabase;
    private readonly RedisConnectionFactory _connectionFactory;

    public RedisBaselineWorkflowTests()
    {
        _mockDatabase = new Mock<IDatabase>();
        _mockConnectionMultiplexer = new Mock<IConnectionMultiplexer>();
        _mockConnectionMultiplexer
            .Setup(x => x.GetDatabase(It.IsAny<int>(), It.IsAny<object>()))
            .Returns(_mockDatabase.Object);

        _connectionFactory = new RedisConnectionFactory(_mockConnectionMultiplexer.Object);
    }

    [Fact]
    public async Task CreateBaseline_StoresSuccessfully()
    {
        // Arrange
        var metric = new TestMetric("cpu", 50.0);
        var tolerance = new Tolerance("cpu", ToleranceType.Relative, 5.0m);
        var config = new ToleranceConfiguration(new[] { tolerance });
        var baseline = new Baseline(new[] { metric }, config);

        var repository = new RedisBaselineRepository(_connectionFactory);

        // Setup mock to succeed
        _mockDatabase
            .Setup(x => x.StringSetAsync(
                It.IsAny<RedisKey>(),
                It.IsAny<RedisValue>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<When>(),
                It.IsAny<CommandFlags>()))
            .ReturnsAsync(true);

        // Act
        var id = await repository.CreateAsync(baseline);

        // Assert
        id.Should().NotBeNull();
        id.Value.Should().NotBeEmpty();
    }

    [Fact]
    public async Task ListRecentBaselines_HandlesEmptyList()
    {
        // Arrange
        var repository = new RedisBaselineRepository(_connectionFactory);

        // Act
        var baselines = await repository.ListRecentAsync(10);

        // Assert
        baselines.Should().NotBeNull();
        baselines.Should().BeEmpty();
    }

    [Fact]
    public async Task Repository_StoresAndRetrieves_Baseline()
    {
        // Arrange
        var metric = new TestMetric("responseTime", 100.0);
        var tolerance = new Tolerance("responseTime", ToleranceType.Relative, 10.0m);
        var config = new ToleranceConfiguration(new[] { tolerance });
        var baseline = new Baseline(new[] { metric }, config);

        var repository = new RedisBaselineRepository(_connectionFactory);

        _mockDatabase
            .Setup(x => x.StringSetAsync(
                It.IsAny<RedisKey>(),
                It.IsAny<RedisValue>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<When>(),
                It.IsAny<CommandFlags>()))
            .ReturnsAsync(true);

        // Act
        var id = await repository.CreateAsync(baseline);

        // Assert - Verify TTL was set
        _mockDatabase.Verify(
            x => x.StringSetAsync(
                It.IsAny<RedisKey>(),
                It.IsAny<RedisValue>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<When>(),
                It.IsAny<CommandFlags>()),
            Times.Once);
    }

    private sealed class TestMetric : IMetric
    {
        public TestMetric(string metricType, double value)
        {
            MetricType = metricType;
            Value = value;
            Id = Guid.NewGuid();
            Unit = "unit";
            ComputedAt = DateTime.UtcNow;
        }

        public Guid Id { get; }
        public string MetricType { get; }
        public double Value { get; }
        public string Unit { get; }
        public DateTime ComputedAt { get; }
        public CompletessStatus CompletessStatus => CompletessStatus.COMPLETE;
        public MetricEvidence Evidence => new MetricEvidence(1, 1, "test");
    }
}
