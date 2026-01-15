namespace PerformanceEngine.Baseline.Domain.Domain.Confidence;

using PerformanceEngine.Baseline.Domain.Domain.Tolerances;

/// <summary>
/// Domain service that calculates confidence levels based on comparison magnitude.
/// Confidence represents how far a change deviates from the tolerance threshold.
/// </summary>
public class ConfidenceCalculator
{
    /// <summary>
    /// Calculates confidence level based on the magnitude of change relative to tolerance.
    /// 
    /// Formula:
    /// confidence = min(1.0, abs(change_magnitude - tolerance_threshold) / tolerance_threshold)
    /// 
    /// Examples:
    /// - Change at tolerance boundary (e.g., +10ms with ±10ms tolerance) → confidence = 0.0 (inconclusive)
    /// - Change 2x tolerance (e.g., +20ms with ±10ms tolerance) → confidence = 1.0 (high confidence)
    /// - Change within tolerance (e.g., +5ms with ±10ms tolerance) → confidence = 0.5 (moderate)
    /// </summary>
    /// <param name="baseline">Baseline metric value</param>
    /// <param name="current">Current metric value</param>
    /// <param name="tolerance">Tolerance rule for this metric</param>
    /// <returns>Confidence level in range [0.0, 1.0]</returns>
    public ConfidenceLevel CalculateConfidence(
        decimal baseline,
        decimal current,
        Tolerance tolerance)
    {
        if (tolerance == null)
            throw new ArgumentNullException(nameof(tolerance));

        var absoluteDifference = Math.Abs(current - baseline);

        decimal confidenceValue = tolerance.Type switch
        {
            ToleranceType.Absolute => 
                CalculateAbsoluteConfidence(absoluteDifference, tolerance.Amount),
            ToleranceType.Relative => 
                CalculateRelativeConfidence(baseline, absoluteDifference, tolerance.Amount),
            _ => throw new InvalidOperationException($"Unknown tolerance type: {tolerance.Type}")
        };

        // Clamp to [0.0, 1.0]
        confidenceValue = Math.Max(0, Math.Min(1, confidenceValue));

        return new ConfidenceLevel(confidenceValue);
    }

    /// <summary>
    /// Calculates confidence for absolute tolerance.
    /// </summary>
    private static decimal CalculateAbsoluteConfidence(decimal absoluteDifference, decimal toleranceAmount)
    {
        if (toleranceAmount == 0)
            return absoluteDifference == 0 ? 1.0m : 0.0m;

        // Confidence increases with difference beyond tolerance
        // 0 confidence at tolerance boundary, increasing beyond
        return (absoluteDifference - toleranceAmount) / toleranceAmount;
    }

    /// <summary>
    /// Calculates confidence for relative tolerance.
    /// </summary>
    private static decimal CalculateRelativeConfidence(decimal baseline, decimal absoluteDifference, decimal percentageTolerance)
    {
        if (baseline == 0)
        {
            // Special case: baseline = 0, relative tolerance undefined
            // Return max confidence if current = 0, min otherwise
            return absoluteDifference == 0 ? 1.0m : 0.0m;
        }

        var toleranceAmount = Math.Abs(baseline) * percentageTolerance / 100;

        if (toleranceAmount == 0)
            return absoluteDifference == 0 ? 1.0m : 0.0m;

        // Same formula as absolute: confidence increases beyond tolerance threshold
        return (absoluteDifference - toleranceAmount) / toleranceAmount;
    }
}
