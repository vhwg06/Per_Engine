namespace PerformanceEngine.Baseline.Domain.Domain;

/// <summary>
/// Base exception for all Baseline Domain errors.
/// </summary>
public class BaselineDomainException : Exception
{
    public BaselineDomainException(string message) : base(message) { }
    public BaselineDomainException(string message, Exception innerException) 
        : base(message, innerException) { }
}

/// <summary>
/// Thrown when a domain invariant is violated (e.g., immutability constraint).
/// </summary>
public class DomainInvariantViolatedException : BaselineDomainException
{
    public DomainInvariantViolatedException(string invariantName, string reason)
        : base($"Domain invariant violated: {invariantName}. {reason}") { }
}

/// <summary>
/// Thrown when a requested baseline cannot be found or has expired.
/// </summary>
public class BaselineNotFoundException : BaselineDomainException
{
    public BaselineNotFoundException(string baselineId)
        : base($"Baseline with ID '{baselineId}' not found or has expired.") { }
}

/// <summary>
/// Thrown when tolerance configuration is invalid.
/// </summary>
public class ToleranceValidationException : BaselineDomainException
{
    public ToleranceValidationException(string metricName, string reason)
        : base($"Tolerance validation failed for metric '{metricName}': {reason}") { }
}

/// <summary>
/// Thrown when confidence level is invalid (not in [0.0, 1.0] range).
/// </summary>
public class ConfidenceValidationException : BaselineDomainException
{
    public ConfidenceValidationException(decimal value)
        : base($"Confidence level {value} must be in range [0.0, 1.0].") { }
}

/// <summary>
/// Thrown when a metric is not found in baseline or comparison.
/// </summary>
public class MetricNotFoundException : BaselineDomainException
{
    public MetricNotFoundException(string metricName)
        : base($"Metric '{metricName}' not found.") { }
}

/// <summary>
/// Thrown when a repository/persistence operation fails.
/// </summary>
public class RepositoryException : BaselineDomainException
{
    public RepositoryException(string operation, string reason)
        : base($"Repository operation '{operation}' failed: {reason}") { }
}
