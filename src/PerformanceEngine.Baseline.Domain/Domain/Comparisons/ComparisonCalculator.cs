namespace PerformanceEngine.Baseline.Domain.Domain.Comparisons;

using PerformanceEngine.Baseline.Domain.Domain.Baselines;
using PerformanceEngine.Baseline.Domain.Domain.Confidence;
using PerformanceEngine.Baseline.Domain.Domain.Tolerances;

/// <summary>
/// Pure domain service that performs metric comparisons against baseline.
/// All methods are deterministic and side-effect-free.
/// </summary>
public class ComparisonCalculator
{
    private readonly ConfidenceCalculator _confidenceCalculator;

    public ComparisonCalculator(ConfidenceCalculator? confidenceCalculator = null)
    {
        _confidenceCalculator = confidenceCalculator ?? new ConfidenceCalculator();
    }

    /// <summary>
    /// Calculates the comparison result for a single metric.
    /// </summary>
    /// <param name="baseline">Baseline metric value</param>
    /// <param name="current">Current metric value</param>
    /// <param name="tolerance">Tolerance rule for this metric</param>
    /// <param name="confidenceThreshold">Minimum confidence for conclusive results (default 0.5)</param>
    /// <returns>Comparison result for this metric</returns>
    public ComparisonMetric CalculateMetric(
        decimal baseline,
        decimal current,
        Tolerance tolerance,
        decimal confidenceThreshold = 0.5m)
    {
        if (tolerance == null)
            throw new ArgumentNullException(nameof(tolerance));

        if (confidenceThreshold < 0 || confidenceThreshold > 1)
            throw new ArgumentException("Confidence threshold must be in range [0.0, 1.0].", nameof(confidenceThreshold));

        // Calculate confidence
        var confidence = _confidenceCalculator.CalculateConfidence(baseline, current, tolerance);

        // Determine outcome
        var outcome = DetermineOutcome(baseline, current, tolerance, confidence, confidenceThreshold);

        return new ComparisonMetric(tolerance.MetricName, baseline, current, tolerance, outcome, confidence);
    }

    /// <summary>
    /// Determines the comparison outcome based on tolerance and confidence.
    /// </summary>
    /// <param name="baseline">Baseline metric value</param>
    /// <param name="current">Current metric value</param>
    /// <param name="tolerance">Tolerance rule</param>
    /// <param name="confidence">Calculated confidence</param>
    /// <param name="confidenceThreshold">Minimum confidence threshold</param>
    /// <returns>Comparison outcome</returns>
    public ComparisonOutcome DetermineOutcome(
        decimal baseline,
        decimal current,
        Tolerance tolerance,
        ConfidenceLevel confidence,
        decimal confidenceThreshold = 0.5m)
    {
        if (tolerance == null)
            throw new ArgumentNullException(nameof(tolerance));

        if (confidence == null)
            throw new ArgumentNullException(nameof(confidence));

        // If confidence is below threshold, result is inconclusive
        if (!confidence.IsConclusive(confidenceThreshold))
            return ComparisonOutcome.Inconclusive;

        // Check if within tolerance
        var isWithinTolerance = tolerance.IsWithinTolerance(baseline, current);

        if (isWithinTolerance)
            return ComparisonOutcome.NoSignificantChange;

        // Determine if improvement or regression
        var difference = current - baseline;

        // For metrics where lower values are better (latency, errors, etc.)
        // negative difference = improvement, positive = regression
        // For metrics where higher values are better (throughput, success rate, etc.)
        // positive difference = improvement, negative = regression
        //
        // Since we don't have direction metadata in Baseline Domain,
        // we use the change magnitude:
        // - If current is "better" in relative terms, it's improvement
        // - Otherwise it's regression

        // Simple heuristic: if absolute value increased, likely regression
        // if decreased, likely improvement. This is imperfect without direction.
        var absoluteChangeIncrease = Math.Abs(current) > Math.Abs(baseline);

        return absoluteChangeIncrease ? ComparisonOutcome.Regression : ComparisonOutcome.Improvement;
    }
}
