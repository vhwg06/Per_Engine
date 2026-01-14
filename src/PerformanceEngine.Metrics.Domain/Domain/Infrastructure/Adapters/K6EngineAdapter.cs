namespace PerformanceEngine.Metrics.Domain.Infrastructure.Adapters;

using PerformanceEngine.Metrics.Domain.Metrics;

/// <summary>
/// Adapter for mapping k6 load testing engine results to domain model.
/// Converts k6-specific response data into domain Sample entities.
/// </summary>
public sealed class K6EngineAdapter
{
    /// <summary>
    /// Maps k6 HTTP response results to a domain SampleCollection.
    /// </summary>
    /// <param name="k6Results">K6 result objects with http_req_duration and http_req_failed fields</param>
    /// <param name="executionId">The execution context ID</param>
    /// <param name="scenarioName">Optional scenario name from k6 execution</param>
    /// <returns>Domain SampleCollection with mapped samples</returns>
    public SampleCollection MapK6ResultsToDomain(
        IEnumerable<K6ResultData> k6Results,
        Guid executionId,
        string? scenarioName = null)
    {
        if (k6Results is null)
            throw new ArgumentNullException(nameof(k6Results));

        var collection = new SampleCollection();
        var executionContext = new ExecutionContext("k6", executionId, scenarioName);

        foreach (var result in k6Results)
        {
            var sample = MapK6ResultToSample(result, executionContext);
            collection = collection.Add(sample);
        }

        return collection;
    }

    /// <summary>
    /// Maps a single k6 result to a domain Sample.
    /// </summary>
    private static Sample MapK6ResultToSample(K6ResultData result, ExecutionContext context)
    {
        if (result is null)
            throw new ArgumentNullException(nameof(result));

        var latency = new Latency(result.HttpReqDurationMs, LatencyUnit.Milliseconds);
        var status = result.HttpReqFailed ? SampleStatus.Failure : SampleStatus.Success;

        var errorClassification = DetermineErrorClassification(result);

        var metadata = new Dictionary<string, object>
        {
            { "http_status_code", result.HttpStatusCode },
            { "http_req_duration_ms", result.HttpReqDurationMs },
            { "http_req_failed", result.HttpReqFailed },
            { "http_error_code", result.HttpErrorCode ?? string.Empty }
        };

        return new Sample(
            result.Timestamp,
            latency,
            status,
            errorClassification,
            context,
            metadata);
    }

    /// <summary>
    /// Determines error classification from k6-specific error codes.
    /// </summary>
    private static ErrorClassification? DetermineErrorClassification(K6ResultData result)
    {
        if (!result.HttpReqFailed)
            return null;

        // Map k6 error codes to domain classifications
        var errorCode = result.HttpErrorCode ?? string.Empty;

        return errorCode switch
        {
            // Network errors
            "1000" or "ERR_K6_DIAL_SOCKET" => ErrorClassification.NetworkError,
            "1001" or "ERR_K6_SSL" => ErrorClassification.NetworkError,

            // Timeout errors
            "1100" or "ERR_K6_TIMEOUT" => ErrorClassification.Timeout,

            // Server errors (5xx)
            _ when result.HttpStatusCode >= 500 => ErrorClassification.ApplicationError,

            // Client errors (4xx)
            _ when result.HttpStatusCode >= 400 && result.HttpStatusCode < 500 => ErrorClassification.ApplicationError,

            // Default to unknown
            _ => ErrorClassification.UnknownError
        };
    }
}

/// <summary>
/// Represents k6 result data format from HTTP request module.
/// </summary>
public sealed class K6ResultData
{
    /// <summary>
    /// Gets the timestamp when the result occurred
    /// </summary>
    public DateTime Timestamp { get; }

    /// <summary>
    /// Gets the HTTP response duration in milliseconds
    /// </summary>
    public double HttpReqDurationMs { get; }

    /// <summary>
    /// Gets the HTTP status code (200, 404, 500, etc.)
    /// </summary>
    public int HttpStatusCode { get; }

    /// <summary>
    /// Gets whether the request failed
    /// </summary>
    public bool HttpReqFailed { get; }

    /// <summary>
    /// Gets the k6 error code string (optional)
    /// </summary>
    public string? HttpErrorCode { get; }

    private K6ResultData(
        DateTime timestamp,
        double httpReqDurationMs,
        int httpStatusCode,
        bool httpReqFailed,
        string? httpErrorCode)
    {
        Timestamp = timestamp;
        HttpReqDurationMs = httpReqDurationMs;
        HttpStatusCode = httpStatusCode;
        HttpReqFailed = httpReqFailed;
        HttpErrorCode = httpErrorCode;
    }

    /// <summary>
    /// Creates K6ResultData from raw k6 result fields.
    /// </summary>
    public static K6ResultData Create(
        DateTime timestamp,
        double httpReqDurationMs,
        int httpStatusCode,
        bool httpReqFailed,
        string? httpErrorCode = null)
    {
        if (httpReqDurationMs < 0)
            throw new ArgumentException("HTTP request duration must be non-negative", nameof(httpReqDurationMs));

        return new K6ResultData(timestamp, httpReqDurationMs, httpStatusCode, httpReqFailed, httpErrorCode);
    }
}
