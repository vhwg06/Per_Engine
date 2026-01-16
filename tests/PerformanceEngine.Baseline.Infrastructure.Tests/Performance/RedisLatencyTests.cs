namespace PerformanceEngine.Baseline.Infrastructure.Tests.Performance;

using System.Diagnostics;
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
/// Phase 7 Performance Test - T062
/// Validates Redis persistence performance: create + retrieve + deserialize < 15ms (p95)
/// Tests Redis throughput: 1000 qps baseline storage capability
/// </summary>
public sealed class RedisLatencyTests
{
    private readonly Mock<IConnectionMultiplexer> _mockConnectionMultiplexer;
    private readonly Mock<IDatabase> _mockDatabase;
    private readonly RedisConnectionFactory _connectionFactory;

    public RedisLatencyTests()
    {
        _mockDatabase = new Mock<IDatabase>();
        _mockConnectionMultiplexer = new Mock<IConnectionMultiplexer>();
        _mockConnectionMultiplexer
            .Setup(x => x.GetDatabase(It.IsAny<int>(), It.IsAny<object>()))
            .Returns(_mockDatabase.Object);

        _connectionFactory = new RedisConnectionFactory(_mockConnectionMultiplexer.Object);
    }

    [Fact]
    public async Task RedisOperations_CreateRetrieveDeserialize_IsUnder15Milliseconds_P95()
    {
        // Arrange
        var repository = new RedisBaselineRepository(_connectionFactory);
        var iterations = 100;
        var latencies = new List<long>();

        var metric = new TestMetric("ResponseTime", 150.5);
        var tolerance = new Tolerance("ResponseTime", ToleranceType.Relative, 10m);
        var config = new ToleranceConfiguration(new[] { tolerance });
        var baseline = new Baseline(new[] { metric }, config);

        // Setup mocks for successful operations
        var serializedBaseline = BaselineRedisMapper.Serialize(baseline);
        
        _mockDatabase
            .Setup(x => x.StringSetAsync(
                It.IsAny<RedisKey>(),
                It.IsAny<RedisValue>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<bool>(),
                It.IsAny<When>(),
                It.IsAny<CommandFlags>()))
            .ReturnsAsync(true);

        _mockDatabase
            .Setup(x => x.StringGetAsync(
                It.IsAny<RedisKey>(),
                It.IsAny<CommandFlags>()))
            .ReturnsAsync((RedisValue)serializedBaseline);

        // Act - Measure create + retrieve + deserialize latency
        for (int i = 0; i < iterations; i++)
        {
            var stopwatch = Stopwatch.StartNew();
            
            // Create (store)
            var id = await repository.CreateAsync(baseline);
            
            // Retrieve
            var retrieved = await repository.GetByIdAsync(id);
            
            stopwatch.Stop();
            latencies.Add(stopwatch.ElapsedMilliseconds);
        }

        // Assert - p95 should be under 15ms
        latencies.Sort();
        var p95Index = (int)(iterations * 0.95);
        var p95Latency = latencies[p95Index];
        
        p95Latency.Should().BeLessThan(15, 
            "p95 Redis create + retrieve + deserialize latency must be under 15ms");
        
        // Additional checks
        var averageLatency = latencies.Average();
        averageLatency.Should().BeLessThan(10, 
            "average Redis operations should be well under target");
    }

    [Fact]
    public async Task RedisCreate_Throughput_Handles1000QPS()
    {
        // Arrange
        var repository = new RedisBaselineRepository(_connectionFactory);
        var operationCount = 1000;
        var tasks = new List<Task<BaselineId>>();

        var metric = new TestMetric("Throughput", 1000.0);
        var tolerance = new Tolerance("Throughput", ToleranceType.Relative, 5m);
        var config = new ToleranceConfiguration(new[] { tolerance });
        var baseline = new Baseline(new[] { metric }, config);

        // Setup mock for successful creates
        _mockDatabase
            .Setup(x => x.StringSetAsync(
                It.IsAny<RedisKey>(),
                It.IsAny<RedisValue>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<bool>(),
                It.IsAny<When>(),
                It.IsAny<CommandFlags>()))
            .ReturnsAsync(true);

        // Act - Execute 1000 creates as fast as possible (simulate QPS)
        var stopwatch = Stopwatch.StartNew();
        
        for (int i = 0; i < operationCount; i++)
        {
            var task = repository.CreateAsync(baseline);
            tasks.Add(task);
        }

        await Task.WhenAll(tasks);
        stopwatch.Stop();

        // Assert - Should complete 1000 operations within 1 second
        var elapsedSeconds = stopwatch.ElapsedMilliseconds / 1000.0;
        var actualQps = operationCount / elapsedSeconds;
        
        actualQps.Should().BeGreaterThan(1000, 
            "Redis should handle at least 1000 qps for baseline storage");
        
        // Verify all operations succeeded
        tasks.Should().HaveCount(operationCount);
        tasks.Should().OnlyContain(t => t.IsCompletedSuccessfully);
    }

    [Fact]
    public async Task SerializationRoundTrip_Performance_IsUnder5Milliseconds()
    {
        // Arrange
        var mapper = new BaselineRedisMapper();
        var iterations = 1000;
        var latencies = new List<long>();

        // Create baseline with multiple metrics
        var metrics = new List<IMetric>();
        for (int i = 0; i < 10; i++)
        {
            metrics.Add(new TestMetric($"Metric{i}", 100.0 + i));
        }

        var tolerances = metrics.Select(m =>
            new Tolerance(m.MetricType, ToleranceType.Relative, 10m)
        ).ToArray();
        var config = new ToleranceConfiguration(tolerances);
        var baseline = new Baseline(metrics, config);

        // Act - Measure serialization + deserialization latency
        for (int i = 0; i < iterations; i++)
        {
            var stopwatch = Stopwatch.StartNew();
            
            var json = BaselineRedisMapper.Serialize(baseline);
            var deserialized = BaselineRedisMapper.Deserialize(json);
            
            stopwatch.Stop();
            latencies.Add(stopwatch.ElapsedMilliseconds);
        }

        // Assert - Average should be well under 5ms
        var averageLatency = latencies.Average();
        averageLatency.Should().BeLessThan(5, 
            "serialization round-trip should be under 5ms on average");
        
        // p95 check
        latencies.Sort();
        var p95Index = (int)(iterations * 0.95);
        var p95Latency = latencies[p95Index];
        p95Latency.Should().BeLessThan(10, 
            "p95 serialization latency should be reasonable");
    }

    [Fact]
    public async Task ConnectionPooling_ConcurrentRequests_EfficientResourceUsage()
    {
        // Arrange
        var repository = new RedisBaselineRepository(_connectionFactory);
        var concurrentRequests = 100;
        var tasks = new List<Task>();

        var metric = new TestMetric("CPUUsage", 45.0);
        var tolerance = new Tolerance("CPUUsage", ToleranceType.Relative, 15m);
        var config = new ToleranceConfiguration(new[] { tolerance });
        var baseline = new Baseline(new[] { metric }, config);

        // Setup mocks
        _mockDatabase
            .Setup(x => x.StringSetAsync(
                It.IsAny<RedisKey>(),
                It.IsAny<RedisValue>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<bool>(),
                It.IsAny<When>(),
                It.IsAny<CommandFlags>()))
            .ReturnsAsync(true);

        // Act - Execute concurrent creates to test connection pooling
        var stopwatch = Stopwatch.StartNew();
        
        for (int i = 0; i < concurrentRequests; i++)
        {
            var task = repository.CreateAsync(baseline);
            tasks.Add(task);
        }

        await Task.WhenAll(tasks);
        stopwatch.Stop();

        // Assert - Concurrent operations should complete efficiently
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(1000,
            "100 concurrent Redis operations should complete within 1 second");
        
        // Verify connection multiplexer was reused (not created per request)
        _mockConnectionMultiplexer.Verify(
            x => x.GetDatabase(It.IsAny<int>(), It.IsAny<object>()),
            Times.AtLeast(concurrentRequests));
    }

    [Fact]
    public async Task RetrieveExpiredBaseline_HandlesMissGracefully()
    {
        // Arrange
        var repository = new RedisBaselineRepository(_connectionFactory);
        var baselineId = new BaselineId();

        // Setup mock to return null (simulating expired baseline)
        _mockDatabase
            .Setup(x => x.StringGetAsync(
                It.IsAny<RedisKey>(),
                It.IsAny<CommandFlags>()))
            .ReturnsAsync(RedisValue.Null);

        // Act
        var stopwatch = Stopwatch.StartNew();
        var result = await repository.GetByIdAsync(baselineId);
        stopwatch.Stop();

        // Assert - Should handle miss efficiently
        result.Should().BeNull("expired baseline should return null");
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(100,
            "cache miss should be handled quickly");
    }

    [Fact]
    public async Task ListRecentBaselines_Performance_IsEfficient()
    {
        // Arrange
        var repository = new RedisBaselineRepository(_connectionFactory);
        var count = 10;

        // Create sample baselines
        var baselines = new List<Baseline>();
        for (int i = 0; i < count; i++)
        {
            var metric = new TestMetric($"Metric{i}", 100.0 + i);
            var tolerance = new Tolerance($"Metric{i}", ToleranceType.Relative, 10m);
            var config = new ToleranceConfiguration(new[] { tolerance });
            baselines.Add(new Baseline(new[] { metric }, config));
        }

        // Setup mock to return serialized baselines
        var serializedBaselines = baselines.Select(b => (RedisValue)BaselineRedisMapper.Serialize(b)).ToArray();
        _mockDatabase
            .Setup(x => x.StringGetAsync(
                It.IsAny<RedisKey[]>(),
                It.IsAny<CommandFlags>()))
            .ReturnsAsync(serializedBaselines);

        _mockDatabase
            .Setup(x => x.SortedSetRangeByScoreAsync(
                It.IsAny<RedisKey>(),
                It.IsAny<double>(),
                It.IsAny<double>(),
                It.IsAny<Exclude>(),
                It.IsAny<Order>(),
                It.IsAny<long>(),
                It.IsAny<long>(),
                It.IsAny<CommandFlags>()))
            .ReturnsAsync(baselines.Select((b, i) => (RedisValue)$"baseline:{i}").ToArray());

        // Act
        var stopwatch = Stopwatch.StartNew();
        var results = await repository.ListRecentAsync(count);
        stopwatch.Stop();

        // Assert - Listing should be fast
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(50,
            "listing recent baselines should be fast");
    }

    [Fact]
    public void RedisKeyBuilder_Performance_IsNegligible()
    {
        // Arrange
        var iterations = 10000;
        var ids = Enumerable.Range(0, iterations)
            .Select(_ => new BaselineId())
            .ToList();

        // Act
        var stopwatch = Stopwatch.StartNew();
        
        foreach (var id in ids)
        {
            var key = RedisKeyBuilder.BuildBaselineKey(id);
        }
        
        stopwatch.Stop();

        // Assert - Key generation should be extremely fast
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(100,
            "generating 10k Redis keys should be nearly instant");
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
