namespace PerformanceEngine.Metrics.Domain.Infrastructure.Adapters;

using PerformanceEngine.Metrics.Domain.Metrics;

/// <summary>
/// Adapter for mapping Apache JMeter engine results to domain model.
/// Converts JMeter-specific response data into domain Sample entities.
/// </summary>
public sealed class JMeterEngineAdapter
{
    /// <summary>
    /// Maps JMeter HTTP sampler results to a domain SampleCollection.
    /// </summary>
    /// <param name="jmeterResults">JMeter result objects with elapsed time and response codes</param>
    /// <param name="executionId">The execution context ID</param>
    /// <param name="testPlanName">Optional test plan name from JMeter execution</param>
    /// <returns>Domain SampleCollection with mapped samples</returns>
    public SampleCollection MapJMeterResultsToDomain(
        IEnumerable<JMeterResultData> jmeterResults,
        Guid executionId,
        string? testPlanName = null)
    {
        if (jmeterResults is null)
            throw new ArgumentNullException(nameof(jmeterResults));

        var collection = new SampleCollection();
        var executionContext = new ExecutionContext("jmeter", executionId, testPlanName);

        foreach (var result in jmeterResults)
        {
            var sample = MapJMeterResultToSample(result, executionContext);
            collection = collection.Add(sample);
        }

        return collection;
    }

    /// <summary>
    /// Maps a single JMeter result to a domain Sample.
    /// </summary>
    private static Sample MapJMeterResultToSample(JMeterResultData result, ExecutionContext context)
    {
        if (result is null)
            throw new ArgumentNullException(nameof(result));

        var latency = new Latency(result.ElapsedMs, LatencyUnit.Milliseconds);
        var status = result.Success ? SampleStatus.Success : SampleStatus.Failure;

        var errorClassification = DetermineErrorClassification(result);

        var metadata = new Dictionary<string, object>
        {
            { "response_code", result.ResponseCode ?? "0" },
            { "response_message", result.ResponseMessage ?? string.Empty },
            { "elapsed_ms", result.ElapsedMs },
            { "success", result.Success }
        };

        if (!string.IsNullOrEmpty(result.SamplerLabel))
            metadata["sampler_label"] = result.SamplerLabel;

        return new Sample(
            result.Timestamp,
            latency,
            status,
            errorClassification,
            context,
            metadata);
    }

    /// <summary>
    /// Determines error classification from JMeter-specific response codes and messages.
    /// </summary>
    private static ErrorClassification? DetermineErrorClassification(JMeterResultData result)
    {
        if (result.Success)
            return null;

        var responseCode = result.ResponseCode ?? "0";
        var responseMessage = result.ResponseMessage ?? string.Empty;

        // Map JMeter response codes to domain classifications
        return responseCode switch
        {
            // Network errors - connection refused, unreachable host, etc.
            "Non HTTP response code: java.net.ConnectException" => ErrorClassification.NetworkError,
            "Non HTTP response code: java.net.UnknownHostException" => ErrorClassification.NetworkError,
            "Non HTTP response code: java.net.SocketException" => ErrorClassification.NetworkError,

            // Timeout errors
            "Non HTTP response code: java.net.SocketTimeoutException" => ErrorClassification.Timeout,
            "Non HTTP response code: java.util.concurrent.TimeoutException" => ErrorClassification.Timeout,

            // Server errors (5xx)
            _ when responseCode.StartsWith("5") => ErrorClassification.ApplicationError,

            // Client errors (4xx)
            _ when responseCode.StartsWith("4") => ErrorClassification.ApplicationError,

            // SSL/TLS errors
            _ when responseCode.Contains("SSL") || responseMessage.Contains("SSL") => ErrorClassification.NetworkError,

            // Default to unknown
            _ => ErrorClassification.UnknownError
        };
    }
}

/// <summary>
/// Represents JMeter HTTP sampler result data.
/// </summary>
public sealed class JMeterResultData
{
    /// <summary>
    /// Gets the timestamp when the sample occurred
    /// </summary>
    public DateTime Timestamp { get; }

    /// <summary>
    /// Gets the elapsed time in milliseconds
    /// </summary>
    public double ElapsedMs { get; }

    /// <summary>
    /// Gets the HTTP response code (200, 404, 500, or JMeter error code)
    /// </summary>
    public string? ResponseCode { get; }

    /// <summary>
    /// Gets the response message
    /// </summary>
    public string? ResponseMessage { get; }

    /// <summary>
    /// Gets whether the request succeeded
    /// </summary>
    public bool Success { get; }

    /// <summary>
    /// Gets the optional sampler label (e.g., "HTTP Request")
    /// </summary>
    public string? SamplerLabel { get; }

    private JMeterResultData(
        DateTime timestamp,
        double elapsedMs,
        string? responseCode,
        string? responseMessage,
        bool success,
        string? samplerLabel)
    {
        Timestamp = timestamp;
        ElapsedMs = elapsedMs;
        ResponseCode = responseCode;
        ResponseMessage = responseMessage;
        Success = success;
        SamplerLabel = samplerLabel;
    }

    /// <summary>
    /// Creates JMeterResultData from raw JMeter result fields.
    /// </summary>
    public static JMeterResultData Create(
        DateTime timestamp,
        double elapsedMs,
        string? responseCode,
        string? responseMessage,
        bool success,
        string? samplerLabel = null)
    {
        if (elapsedMs < 0)
            throw new ArgumentException("Elapsed time must be non-negative", nameof(elapsedMs));

        return new JMeterResultData(timestamp, elapsedMs, responseCode, responseMessage, success, samplerLabel);
    }
}
