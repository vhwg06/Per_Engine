namespace PerformanceEngine.Application.Ports;

using PerformanceEngine.Metrics.Domain.Metrics;

/// <summary>
/// Port abstraction for accessing collected performance metrics.
/// Provides read-only access to available metric samples.
/// </summary>
public interface IMetricsProvider
{
    /// <summary>
    /// Retrieves all available metric samples for the current evaluation context.
    /// </summary>
    /// <returns>Immutable collection of metric samples. May be empty if no metrics available.</returns>
    IReadOnlyCollection<Sample> GetAvailableSamples();

    /// <summary>
    /// Checks if a specific metric is available in the current collection.
    /// </summary>
    /// <param name="metricName">Name of the metric to check.</param>
    /// <returns>True if the metric is available; otherwise false.</returns>
    bool IsMetricAvailable(string metricName);

    /// <summary>
    /// Gets all metric names that are currently available.
    /// </summary>
    /// <returns>Collection of available metric names.</returns>
    IReadOnlyCollection<string> GetAvailableMetricNames();
}
