namespace PerformanceEngine.Baseline.Infrastructure.Persistence;

/// <summary>
/// Factory for creating and managing Redis connections with pooling and TTL configuration.
/// </summary>
public class RedisConnectionFactory
{
    private readonly IConnectionMultiplexer _connectionMultiplexer;
    private readonly TimeSpan _baselineTtl;

    /// <summary>
    /// Creates a new Redis connection factory.
    /// </summary>
    /// <param name="connectionMultiplexer">The Redis connection multiplexer (from DI)</param>
    /// <param name="baselineTtl">The TTL for baseline entries (default 24 hours)</param>
    public RedisConnectionFactory(
        IConnectionMultiplexer connectionMultiplexer,
        TimeSpan? baselineTtl = null)
    {
        _connectionMultiplexer = connectionMultiplexer ?? throw new ArgumentNullException(nameof(connectionMultiplexer));
        _baselineTtl = baselineTtl ?? TimeSpan.FromHours(24);
    }

    /// <summary>
    /// Gets the Redis database for baseline storage.
    /// </summary>
    public IDatabase GetDatabase() => _connectionMultiplexer.GetDatabase();

    /// <summary>
    /// Gets the baseline TTL duration.
    /// </summary>
    public TimeSpan BaselineTtl => _baselineTtl;

    /// <summary>
    /// Gets whether the connection is active.
    /// </summary>
    public bool IsConnected => _connectionMultiplexer.IsConnected;

    /// <summary>
    /// Disposes the connection multiplexer.
    /// </summary>
    public void Dispose() => _connectionMultiplexer?.Dispose();
}
