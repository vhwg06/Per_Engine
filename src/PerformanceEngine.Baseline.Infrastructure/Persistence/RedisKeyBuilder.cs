namespace PerformanceEngine.Baseline.Infrastructure.Persistence;

using PerformanceEngine.Baseline.Domain.Domain.Baselines;

/// <summary>
/// Builds Redis keys for baseline storage using a consistent naming convention.
/// Prevents key collisions and enables efficient key scanning.
/// </summary>
public static class RedisKeyBuilder
{
    private const string BaselineKeyPrefix = "baseline";
    private const string RecentKeyPrefix = "baseline:recent";

    /// <summary>
    /// Builds the Redis key for a specific baseline.
    /// Format: baseline:{id}
    /// </summary>
    /// <param name="baselineId">The baseline ID</param>
    /// <returns>The Redis key</returns>
    public static string BuildBaselineKey(BaselineId baselineId)
    {
        ArgumentNullException.ThrowIfNull(baselineId);
        return $"{BaselineKeyPrefix}:{baselineId.Value}";
    }

    /// <summary>
    /// Builds the Redis key for storing recent baselines.
    /// Format: baseline:recent:{timestamp}:{id}
    /// </summary>
    /// <param name="createdAt">The baseline creation timestamp</param>
    /// <param name="baselineId">The baseline ID</param>
    /// <returns>The Redis sorted set key</returns>
    public static string BuildRecentKey(DateTime createdAt, BaselineId baselineId)
    {
        ArgumentNullException.ThrowIfNull(baselineId);
        
        // Use inverted score (negative) for newest first ordering
        var inversedTimestamp = -createdAt.Ticks;
        return $"{RecentKeyPrefix}:{inversedTimestamp}:{baselineId.Value}";
    }

    /// <summary>
    /// Builds the Redis key pattern for querying all baselines.
    /// </summary>
    /// <returns>The Redis key pattern for scanning</returns>
    public static string BuildAllBaselinesPattern() => $"{BaselineKeyPrefix}:*";

    /// <summary>
    /// Extracts baseline ID from a baseline key.
    /// </summary>
    /// <param name="key">The Redis key</param>
    /// <returns>The baseline ID, or null if key format is invalid</returns>
    public static string? ExtractBaselineId(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return null;

        const string prefix = $"{BaselineKeyPrefix}:";
        if (!key.StartsWith(prefix))
            return null;

        return key.Substring(prefix.Length);
    }
}
