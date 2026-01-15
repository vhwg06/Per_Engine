namespace PerformanceEngine.Baseline.Domain.Ports;

using PerformanceEngine.Baseline.Domain.Domain.Baselines;

/// <summary>
/// Port (interface) for baseline persistence abstraction.
/// Implementations handle storage/retrieval via infrastructure (Redis, database, etc.).
/// </summary>
public interface IBaselineRepository
{
    /// <summary>
    /// Creates and persists a new baseline.
    /// </summary>
    /// <param name="baseline">Baseline to persist</param>
    /// <returns>ID of the persisted baseline</returns>
    /// <exception cref="RepositoryException">If persistence fails</exception>
    Task<BaselineId> CreateAsync(Baseline baseline);

    /// <summary>
    /// Retrieves a baseline by ID.
    /// </summary>
    /// <param name="id">Baseline ID to retrieve</param>
    /// <returns>Baseline if found and not expired, null if not found or expired</returns>
    /// <exception cref="RepositoryException">If retrieval fails due to infrastructure error</exception>
    Task<Baseline?> GetByIdAsync(BaselineId id);

    /// <summary>
    /// Retrieves recent baselines, ordered by creation time (newest first).
    /// </summary>
    /// <param name="count">Maximum number of baselines to retrieve</param>
    /// <returns>List of recent baselines (may be fewer than requested)</returns>
    /// <exception cref="RepositoryException">If retrieval fails</exception>
    Task<IReadOnlyList<Baseline>> ListRecentAsync(int count);
}
