namespace PerformanceEngine.Baseline.Domain.Domain.Tolerances;

/// <summary>
/// Enumeration of tolerance evaluation strategies.
/// </summary>
public enum ToleranceType
{
    /// <summary>
    /// Relative tolerance: deviation as percentage of baseline value.
    /// </summary>
    Relative = 0,

    /// <summary>
    /// Absolute tolerance: fixed deviation amount in metric units.
    /// </summary>
    Absolute = 1
}
