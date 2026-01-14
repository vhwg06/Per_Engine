namespace PerformanceEngine.Metrics.Domain.Application.UseCases;

using System.Collections.Immutable;

/// <summary>
/// Represents the result of validating aggregation parameters.
/// Contains validation status and any error messages if validation failed.
/// </summary>
public sealed class ValidationResult
{
    /// <summary>
    /// Gets whether the validation passed
    /// </summary>
    public bool IsValid { get; }

    /// <summary>
    /// Gets the list of validation error messages (empty if valid)
    /// </summary>
    public ImmutableList<string> Errors { get; }

    private ValidationResult(bool isValid, ImmutableList<string> errors)
    {
        IsValid = isValid;
        Errors = errors ?? ImmutableList<string>.Empty;
    }

    /// <summary>
    /// Creates a successful validation result
    /// </summary>
    public static ValidationResult Success() => new(true, ImmutableList<string>.Empty);

    /// <summary>
    /// Creates a failed validation result with error messages
    /// </summary>
    public static ValidationResult Failure(params string[] errors)
    {
        return new(false, ImmutableList.CreateRange(errors ?? Array.Empty<string>()));
    }

    /// <summary>
    /// Creates a failed validation result with error messages
    /// </summary>
    public static ValidationResult Failure(IEnumerable<string> errors)
    {
        return new(false, ImmutableList.CreateRange(errors ?? Array.Empty<string>()));
    }
}
