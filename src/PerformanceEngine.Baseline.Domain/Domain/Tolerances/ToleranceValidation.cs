namespace PerformanceEngine.Baseline.Domain.Domain.Tolerances;

using PerformanceEngine.Baseline.Domain.Domain;

/// <summary>
/// Validates tolerance configuration constraints and invariants.
/// </summary>
public static class ToleranceValidation
{
    /// <summary>
    /// Validates that a tolerance configuration meets all domain constraints.
    /// </summary>
    /// <param name="config">Tolerance configuration to validate</param>
    /// <exception cref="ToleranceValidationException">If configuration is invalid</exception>
    public static void AssertValid(ToleranceConfiguration config)
    {
        if (config == null)
            throw new ToleranceValidationException("", "Tolerance configuration cannot be null.");

        if (config.Tolerances.Count == 0)
            throw new ToleranceValidationException("", "At least one tolerance rule must be defined.");

        // Validate each tolerance rule
        foreach (var tolerance in config.Tolerances)
        {
            AssertValidTolerance(tolerance);
        }
    }

    /// <summary>
    /// Validates a single tolerance rule.
    /// </summary>
    /// <param name="tolerance">Tolerance to validate</param>
    /// <exception cref="ToleranceValidationException">If tolerance is invalid</exception>
    public static void AssertValidTolerance(Tolerance tolerance)
    {
        if (tolerance == null)
            throw new ToleranceValidationException("", "Tolerance cannot be null.");

        if (string.IsNullOrWhiteSpace(tolerance.MetricName))
            throw new ToleranceValidationException("", "Metric name cannot be empty.");

        if (tolerance.Amount < 0)
            throw new ToleranceValidationException(
                tolerance.MetricName,
                $"Tolerance amount cannot be negative. Got: {tolerance.Amount}");

        if (tolerance.Type == ToleranceType.Relative && tolerance.Amount > 100)
            throw new ToleranceValidationException(
                tolerance.MetricName,
                $"Relative tolerance cannot exceed 100%. Got: {tolerance.Amount}%");
    }
}
