namespace PerformanceEngine.Metrics.Domain.Application.Dto;

using PerformanceEngine.Metrics.Domain.Metrics;

/// <summary>
/// Data transfer object for Sample domain entity.
/// Used for transferring sample data across application boundaries.
/// </summary>
public sealed class SampleDto
{
    /// <summary>
    /// Gets the timestamp of the sample
    /// </summary>
    public DateTime Timestamp { get; }

    /// <summary>
    /// Gets the latency value
    /// </summary>
    public double DurationValue { get; }

    /// <summary>
    /// Gets the latency unit
    /// </summary>
    public LatencyUnit DurationUnit { get; }

    /// <summary>
    /// Gets the status of the request
    /// </summary>
    public SampleStatus Status { get; }

    /// <summary>
    /// Gets the error classification (null if successful)
    /// </summary>
    public ErrorClassification? ErrorClassification { get; }

    /// <summary>
    /// Gets the execution context
    /// </summary>
    public ExecutionContextDto ExecutionContext { get; }

    /// <summary>
    /// Gets the optional metadata
    /// </summary>
    public IReadOnlyDictionary<string, object>? Metadata { get; }

    private SampleDto(
        DateTime timestamp,
        double durationValue,
        LatencyUnit durationUnit,
        SampleStatus status,
        ErrorClassification? errorClassification,
        ExecutionContextDto executionContext,
        IReadOnlyDictionary<string, object>? metadata)
    {
        Timestamp = timestamp;
        DurationValue = durationValue;
        DurationUnit = durationUnit;
        Status = status;
        ErrorClassification = errorClassification;
        ExecutionContext = executionContext;
        Metadata = metadata;
    }

    /// <summary>
    /// Creates a SampleDto from a Sample domain entity.
    /// </summary>
    public static SampleDto FromDomain(Sample sample)
    {
        if (sample is null)
            throw new ArgumentNullException(nameof(sample));

        var contextDto = ExecutionContextDto.FromDomain(sample.ExecutionContext);

        return new SampleDto(
            sample.Timestamp,
            sample.Duration.Value,
            sample.Duration.Unit,
            sample.Status,
            sample.ErrorClassification,
            contextDto,
            sample.Metadata);
    }

    /// <summary>
    /// Creates a Sample domain entity from this DTO.
    /// </summary>
    public Sample ToDomain()
    {
        var latency = new Latency(DurationValue, DurationUnit);
        var context = ExecutionContext.ToDomain();
        var metadata = Metadata != null ? new Dictionary<string, object>(Metadata) : null;

        return new Sample(
            Timestamp,
            latency,
            Status,
            ErrorClassification,
            context,
            metadata);
    }
}

/// <summary>
/// Data transfer object for ExecutionContext value object.
/// </summary>
public sealed class ExecutionContextDto
{
    /// <summary>
    /// Gets the execution engine name
    /// </summary>
    public string EngineName { get; }

    /// <summary>
    /// Gets the execution ID
    /// </summary>
    public Guid ExecutionId { get; }

    /// <summary>
    /// Gets the scenario name
    /// </summary>
    public string? ScenarioName { get; }

    private ExecutionContextDto(string engineName, Guid executionId, string? scenarioName)
    {
        EngineName = engineName;
        ExecutionId = executionId;
        ScenarioName = scenarioName;
    }

    /// <summary>
    /// Creates a DTO from a domain ExecutionContext.
    /// </summary>
    public static ExecutionContextDto FromDomain(ExecutionContext context)
    {
        if (context is null)
            throw new ArgumentNullException(nameof(context));

        return new ExecutionContextDto(context.EngineName, context.ExecutionId, context.ScenarioName);
    }

    /// <summary>
    /// Creates a domain ExecutionContext from this DTO.
    /// </summary>
    public ExecutionContext ToDomain()
    {
        return new ExecutionContext(EngineName, ExecutionId, ScenarioName);
    }
}

