namespace PerformanceEngine.Profile.Domain.Domain.Validation;

/// <summary>
/// Value object representing the result of profile validation.
/// Immutable; contains validation status and all collected errors.
/// Supports non-early-exit validation: all errors collected at once for complete feedback.
/// Equality is based on value comparison of status and error list.
/// </summary>
public sealed class ValidationResult : IEquatable<ValidationResult>
{
    /// <summary>
    /// Indicates whether the profile passed validation (true = valid, false = invalid).
    /// </summary>
    public bool IsValid { get; }

    /// <summary>
    /// Collection of all validation errors found.
    /// Empty if validation passed (IsValid = true).
    /// All errors collected at once (non-early-exit) for complete feedback to caller.
    /// </summary>
    public IReadOnlyList<ValidationError> Errors { get; }

    /// <summary>
    /// Creates a new ValidationResult with the specified validation status and errors.
    /// </summary>
    /// <param name="isValid">true if validation passed, false otherwise</param>
    /// <param name="errors">List of validation errors (null treated as empty list)</param>
    /// <exception cref="ArgumentException">If isValid=true but errors are provided, or if errors list contains null</exception>
    public ValidationResult(bool isValid, IReadOnlyList<ValidationError>? errors = null)
    {
        if (isValid && errors?.Count > 0)
            throw new ArgumentException("Cannot be valid with errors", nameof(errors));

        if (errors?.Any(e => e == null) == true)
            throw new ArgumentException("Errors list cannot contain null entries", nameof(errors));

        IsValid = isValid;
        Errors = errors ?? new List<ValidationError>();
    }

    /// <summary>
    /// Creates a successful validation result (IsValid = true, no errors).
    /// </summary>
    public static ValidationResult Success()
        => new(isValid: true);

    /// <summary>
    /// Creates a failed validation result with the specified errors.
    /// </summary>
    /// <param name="errors">One or more validation errors (required, non-empty)</param>
    /// <exception cref="ArgumentException">If errors is null or empty</exception>
    public static ValidationResult Failure(params ValidationError[] errors)
    {
        if (errors == null || errors.Length == 0)
            throw new ArgumentException("At least one error is required for failure result", nameof(errors));

        return new(isValid: false, errors);
    }

    /// <summary>
    /// Creates a failed validation result with the specified error list.
    /// </summary>
    /// <param name="errors">List of validation errors (required, non-empty)</param>
    /// <exception cref="ArgumentException">If errors is null or empty</exception>
    public static ValidationResult Failure(IReadOnlyList<ValidationError> errors)
    {
        if (errors == null || errors.Count == 0)
            throw new ArgumentException("At least one error is required for failure result", nameof(errors));

        return new(isValid: false, errors);
    }

    /// <summary>
    /// Equality comparison based on IsValid and Errors values.
    /// </summary>
    public bool Equals(ValidationResult? other)
    {
        if (other is null)
            return false;

        if (IsValid != other.IsValid)
            return false;

        if (Errors.Count != other.Errors.Count)
            return false;

        return Errors.SequenceEqual(other.Errors);
    }

    /// <summary>
    /// Equality override for object comparison.
    /// </summary>
    public override bool Equals(object? obj)
        => Equals(obj as ValidationResult);

    /// <summary>
    /// Hash code computation based on IsValid and Errors.
    /// </summary>
    public override int GetHashCode()
    {
        unchecked
        {
            int hash = IsValid.GetHashCode();
            foreach (var error in Errors)
            {
                hash = hash * 31 + (error?.GetHashCode() ?? 0);
            }
            return hash;
        }
    }

    /// <summary>
    /// Equality operator.
    /// </summary>
    public static bool operator ==(ValidationResult? left, ValidationResult? right)
    {
        if (left is null)
            return right is null;
        return left.Equals(right);
    }

    /// <summary>
    /// Inequality operator.
    /// </summary>
    public static bool operator !=(ValidationResult? left, ValidationResult? right)
        => !(left == right);

    public override string ToString()
        => IsValid ? "Valid" : $"Invalid: {Errors.Count} error(s)";
}
