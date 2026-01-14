namespace PerformanceEngine.Metrics.Domain.Metrics;

/// <summary>
/// Represents the classification of an error that occurred in a request.
/// This is a domain-level classification independent of HTTP status codes or tool-specific error codes.
/// </summary>
public enum ErrorClassification
{
    /// <summary>
    /// The request exceeded its time limit without completing
    /// </summary>
    Timeout = 0,

    /// <summary>
    /// A network or transport-level failure occurred (connection refused, DNS failure, etc.)
    /// </summary>
    NetworkError = 1,

    /// <summary>
    /// The application returned an error or exception at the application level
    /// </summary>
    ApplicationError = 2,

    /// <summary>
    /// The error type could not be determined or classified
    /// </summary>
    UnknownError = 3
}
