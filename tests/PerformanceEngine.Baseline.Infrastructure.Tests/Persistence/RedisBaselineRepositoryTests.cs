namespace PerformanceEngine.Baseline.Infrastructure.Tests.Persistence;

using PerformanceEngine.Baseline.Domain.Domain.Baselines;
using PerformanceEngine.Metrics.Domain.Ports;

/// <summary>
/// Tests for RedisBaselineRepository adapter.
/// Verifies persistence behavior using Redis as backing store.
/// </summary>
public class RedisBaselineRepositoryTests
{
    [Fact]
    public void Constructor_WithNullConnectionFactory_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => new RedisBaselineRepository(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public async Task CreateAsync_WithNullBaseline_ThrowsArgumentNullException()
    {
        // Arrange
        var connectionFactory = CreateMockConnectionFactory();
        var repository = new RedisBaselineRepository(connectionFactory);

        // Act & Assert
        await repository.Invoking(r => r.CreateAsync(null!))
            .Should()
            .ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task GetByIdAsync_WithNullId_ThrowsArgumentNullException()
    {
        // Arrange
        var connectionFactory = CreateMockConnectionFactory();
        var repository = new RedisBaselineRepository(connectionFactory);

        // Act & Assert
        await repository.Invoking(r => r.GetByIdAsync(null!))
            .Should()
            .ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task ListRecentAsync_WithZeroCount_ThrowsArgumentException()
    {
        // Arrange
        var connectionFactory = CreateMockConnectionFactory();
        var repository = new RedisBaselineRepository(connectionFactory);

        // Act & Assert
        await repository.Invoking(r => r.ListRecentAsync(0))
            .Should()
            .ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task ListRecentAsync_WithNegativeCount_ThrowsArgumentException()
    {
        // Arrange
        var connectionFactory = CreateMockConnectionFactory();
        var repository = new RedisBaselineRepository(connectionFactory);

        // Act & Assert
        await repository.Invoking(r => r.ListRecentAsync(-1))
            .Should()
            .ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task ListRecentAsync_WithValidCount_ReturnsEmptyListWhenNoData()
    {
        // Arrange
        var connectionFactory = CreateMockConnectionFactory();
        var repository = new RedisBaselineRepository(connectionFactory);

        // Act
        var result = await repository.ListRecentAsync(10);

        // Assert
        result.Should().BeEmpty();
    }

    private static RedisConnectionFactory CreateMockConnectionFactory()
    {
        var mockMultiplexer = new Mock<IConnectionMultiplexer>();
        var mockDb = new Mock<IDatabase>();
        
        mockMultiplexer
            .Setup(m => m.GetDatabase(-1, null))
            .Returns(mockDb.Object);

        var connectionFactory = new RedisConnectionFactory(mockMultiplexer.Object);
        return connectionFactory;
    }}