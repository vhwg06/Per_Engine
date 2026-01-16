namespace PerformanceEngine.Baseline.Infrastructure.Persistence;

using PerformanceEngine.Baseline.Domain.Domain;
using PerformanceEngine.Baseline.Domain.Domain.Baselines;

/// <summary>
/// Redis implementation of IBaselineRepository port.
/// Provides persistence abstraction for baseline storage using Redis.
/// </summary>
public class RedisBaselineRepository : IBaselineRepository
{
    private readonly RedisConnectionFactory _connectionFactory;

    public RedisBaselineRepository(RedisConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
    }

    /// <summary>
    /// Creates and persists a new baseline in Redis.
    /// </summary>
    /// <param name="baseline">The baseline to persist</param>
    /// <returns>The baseline ID</returns>
    /// <exception cref="RepositoryException">If persistence fails</exception>
    public async Task<BaselineId> CreateAsync(Baseline baseline)
    {
        ArgumentNullException.ThrowIfNull(baseline);

        try
        {
            var db = _connectionFactory.GetDatabase();
            var key = RedisKeyBuilder.BuildBaselineKey(baseline.Id);
            var json = BaselineRedisMapper.Serialize(baseline);

            // Store baseline with TTL
            var setSuccess = await db.StringSetAsync(key, json, _connectionFactory.BaselineTtl);

            if (!setSuccess)
            {
                throw new RepositoryException("CreateAsync", $"Failed to store baseline '{baseline.Id.Value}' in Redis.");
            }

            // Also store in recent baselines sorted set (for ListRecentAsync)
            var recentKey = RedisKeyBuilder.BuildRecentKey(baseline.CreatedAt, baseline.Id);
            var score = -baseline.CreatedAt.Ticks; // Negative for descending order

            await db.SortedSetAddAsync(
                "baseline:recent",
                baseline.Id.Value,
                score
            );

            return baseline.Id;
        }
        catch (RepositoryException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new RepositoryException(
                "CreateAsync",
                $"Unable to store baseline: {ex.Message}"
            );
        }
    }

    /// <summary>
    /// Retrieves a baseline by ID from Redis.
    /// </summary>
    /// <param name="id">The baseline ID</param>
    /// <returns>The baseline if found and not expired, null otherwise</returns>
    /// <exception cref="RepositoryException">If retrieval fails due to infrastructure error</exception>
    public async Task<Baseline?> GetByIdAsync(BaselineId id)
    {
        ArgumentNullException.ThrowIfNull(id);

        try
        {
            var db = _connectionFactory.GetDatabase();
            var key = RedisKeyBuilder.BuildBaselineKey(id);

            var json = await db.StringGetAsync(key);

            if (!json.HasValue)
            {
                return null; // Not found or expired
            }

            return BaselineRedisMapper.Deserialize(json.ToString());
        }
        catch (Exception ex)
        {
            throw new RepositoryException(
                "GetByIdAsync",
                $"Unable to retrieve baseline '{id.Value}': {ex.Message}"
            );
        }
    }

    /// <summary>
    /// Retrieves recent baselines ordered by creation time (newest first).
    /// </summary>
    /// <param name="count">Maximum number of baselines to retrieve</param>
    /// <returns>List of recent baselines</returns>
    /// <exception cref="RepositoryException">If retrieval fails</exception>
    public async Task<IReadOnlyList<Baseline>> ListRecentAsync(int count)
    {
        if (count <= 0)
            throw new ArgumentException("Count must be greater than 0.", nameof(count));

        try
        {
            var db = _connectionFactory.GetDatabase();

            // Get the most recent baseline IDs from sorted set
            var recentIds = await db.SortedSetRangeByRankAsync(
                "baseline:recent",
                0,
                count - 1,
                order: Order.Ascending // Ascending because we inverted the score
            );

            if (recentIds.Length == 0)
            {
                return Array.Empty<Baseline>();
            }

            // Retrieve the full baseline objects
            var baselines = new List<Baseline>();

            foreach (var id in recentIds)
            {
                var baselineId = new BaselineId(id.ToString());
                var baseline = await GetByIdAsync(baselineId);

                if (baseline != null)
                {
                    baselines.Add(baseline);
                }
            }

            return baselines.AsReadOnly();
        }
        catch (Exception ex)
        {
            throw new RepositoryException(
                "ListRecentAsync",
                $"Unable to list recent baselines: {ex.Message}"
            );
        }
    }
}
