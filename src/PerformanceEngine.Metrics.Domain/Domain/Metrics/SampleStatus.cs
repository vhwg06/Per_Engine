namespace PerformanceEngine.Metrics.Domain.Metrics;

/// <summary>
/// Represents the status of a sample - whether it succeeded or failed.
/// </summary>
public enum SampleStatus
{
    /// <summary>
    /// The request completed successfully
    /// </summary>
    Success = 0,

    /// <summary>
    /// The request failed for some reason
    /// </summary>
    Failure = 1
}
