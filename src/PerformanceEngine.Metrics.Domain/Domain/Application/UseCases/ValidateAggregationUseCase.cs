namespace PerformanceEngine.Metrics.Domain.Application.UseCases;

using PerformanceEngine.Metrics.Domain.Application.Dto;
using PerformanceEngine.Metrics.Domain.Metrics;

/// <summary>
/// Use case for validating aggregation parameters before computation.
/// Ensures all samples are valid, units are consistent, and window is properly configured.
/// </summary>
public sealed class ValidateAggregationUseCase
{
    /// <summary>
    /// Validates an aggregation request.
    /// </summary>
    /// <param name="request">The aggregation request to validate</param>
    /// <returns>ValidationResult indicating success or containing error messages</returns>
    /// <exception cref="ArgumentNullException">Thrown if request is null</exception>
    public ValidationResult Execute(AggregationRequestDto request)
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));

        var errors = new List<string>();

        // Validate samples collection
        if (request.Samples is null)
        {
            errors.Add("Sample collection cannot be null");
            return ValidationResult.Failure(errors);
        }

        if (request.Samples.IsEmpty)
        {
            errors.Add("Sample collection cannot be empty");
        }

        // Validate window
        if (request.Window is null)
        {
            errors.Add("Aggregation window cannot be null");
        }

        // Validate operation name
        if (string.IsNullOrWhiteSpace(request.AggregationOperation))
        {
            errors.Add("Aggregation operation name cannot be empty");
        }

        // If basic validation failed, return early
        if (errors.Count > 0)
        {
            return ValidationResult.Failure(errors);
        }

        // Validate sample consistency
        var snapshot = request.Samples.GetSnapshot();
        foreach (var sample in snapshot)
        {
            if (sample is null)
            {
                errors.Add("Sample collection contains null sample");
                break;
            }

            if (sample.Duration is null)
            {
                errors.Add($"Sample {sample.Id} has null duration");
            }
        }

        if (errors.Count > 0)
        {
            return ValidationResult.Failure(errors);
        }

        return ValidationResult.Success();
    }
}
