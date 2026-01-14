namespace PerformanceEngine.Metrics.Domain.Ports;

using Metrics;

/// <summary>
/// Port interface for persisting domain Metric objects.
/// Implementations of this interface handle storage and retrieval of metrics in external systems.
/// This is a port (external dependency abstraction) - implementations are infrastructure concerns.
/// 
/// NOTE: Phase 1 implementation defers the actual method signatures. This interface serves as a placeholder
/// for the persistence contract that will be defined in Phase 2.
/// </summary>
public interface IPersistenceRepository
{
    /// <summary>
    /// Persists a computed metric to storage.
    /// </summary>
    /// <param name="metric">The metric to save</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The unique identifier assigned to this metric in storage</returns>
    /// <exception cref="ArgumentNullException">Thrown when metric is null</exception>
    Task<Guid> SaveMetricAsync(Metric metric, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a previously persisted metric by its ID.
    /// </summary>
    /// <param name="metricId">The unique identifier of the metric</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The metric if found; null if not found</returns>
    Task<Metric?> RetrieveMetricAsync(Guid metricId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a metric from storage.
    /// </summary>
    /// <param name="metricId">The unique identifier of the metric</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if the metric was deleted; false if it was not found</returns>
    Task<bool> DeleteMetricAsync(Guid metricId, CancellationToken cancellationToken = default);
}
