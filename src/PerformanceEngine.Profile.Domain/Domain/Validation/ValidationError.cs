namespace PerformanceEngine.Profile.Domain.Domain.Validation;

/// <summary>
/// Value object representing a single validation error.
/// Immutable; contains error code, message, and affected field name.
/// </summary>
public sealed class ValidationError : ValueObject
{
    /// <summary>
    /// Machine-readable error code (e.g., "CIRCULAR_DEPENDENCY", "MISSING_REQUIRED_KEY", "TYPE_MISMATCH").
    /// Used for programmatic error handling and localization.
    /// </summary>
    public string ErrorCode { get; }

    /// <summary>
    /// Human-readable error message describing the validation failure.
    /// Provides context and guidance for remediation.
    /// </summary>
    public string Message { get; }

    /// <summary>
    /// Name of the profile field or override key that failed validation.
    /// Used to pinpoint the specific issue location.
    /// Null if error is not field-specific (e.g., global configuration issues).
    /// </summary>
    public string? FieldName { get; }

    /// <summary>
    /// Creates a new ValidationError with the specified properties.
    /// </summary>
    /// <param name="errorCode">Machine-readable error identifier (required, non-empty)</param>
    /// <param name="message">Human-readable error message (required, non-empty)</param>
    /// <param name="fieldName">Optional field name where error occurred</param>
    /// <exception cref="ArgumentException">If errorCode or message are null or empty</exception>
    public ValidationError(string errorCode, string message, string? fieldName = null)
    {
        if (string.IsNullOrWhiteSpace(errorCode))
            throw new ArgumentException("ErrorCode must be non-empty", nameof(errorCode));
        
        if (string.IsNullOrWhiteSpace(message))
            throw new ArgumentException("Message must be non-empty", nameof(message));

        ErrorCode = errorCode;
        Message = message;
        FieldName = fieldName;
    }

    /// <summary>
    /// Returns all properties for value object equality comparison.
    /// Ensures two ValidationErrors are equal if all properties match.
    /// </summary>
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return ErrorCode;
        yield return Message;
        yield return FieldName;
    }

    public override string ToString()
        => $"{ErrorCode}: {Message}" + (FieldName != null ? $" (Field: {FieldName})" : string.Empty);
}
