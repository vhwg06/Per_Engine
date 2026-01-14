namespace PerformanceEngine.Metrics.Domain.Metrics;

/// <summary>
/// Represents the different units of time that can be used to measure latency.
/// All latencies within a single metric must use the same unit consistently.
/// </summary>
public enum LatencyUnit
{
    /// <summary>
    /// Nanoseconds (1 billionth of a second)
    /// </summary>
    Nanoseconds = 0,

    /// <summary>
    /// Microseconds (1 millionth of a second)
    /// </summary>
    Microseconds = 1,

    /// <summary>
    /// Milliseconds (1 thousandth of a second)
    /// </summary>
    Milliseconds = 2,

    /// <summary>
    /// Seconds
    /// </summary>
    Seconds = 3
}
