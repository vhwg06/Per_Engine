namespace PerformanceEngine.Metrics.Domain.Metrics;

using System;

/// <summary>
/// Represents an immutable raw observation from an execution engine.
/// A sample is the atomic unit of performance measurement data.
/// </summary>
public sealed class Sample
{
    /// <summary>
    /// Gets the unique identifier for this sample
    /// </summary>
    public Guid Id { get; }

    /// <summary>
    /// Gets the timestamp when this observation was made (UTC)
    /// </summary>
    public DateTime Timestamp { get; }

    /// <summary>
    /// Gets the duration/latency of the request
    /// </summary>
    public Latency Duration { get; }

    /// <summary>
    /// Gets the status of the request (Success or Failure)
    /// </summary>
    public SampleStatus Status { get; }

    /// <summary>
    /// Gets the classification of the error (null if Status is Success)
    /// </summary>
    public ErrorClassification? ErrorClassification { get; }

    /// <summary>
    /// Gets the execution context of this sample (engine name, run ID, etc.)
    /// </summary>
    public ExecutionContext ExecutionContext { get; }

    /// <summary>
    /// Gets any engine-specific metadata associated with this sample
    /// </summary>
    public IReadOnlyDictionary<string, object> Metadata { get; }

    /// <summary>
    /// Initializes a new instance of the Sample class.
    /// </summary>
    /// <param name="timestamp">The timestamp when the observation was made (UTC)</param>
    /// <param name="duration">The measured latency/duration</param>
    /// <param name="status">The status of the request</param>
    /// <param name="errorClassification">The classification of the error (required if status is Failure)</param>
    /// <param name="executionContext">The execution context</param>
    /// <param name="metadata">Optional engine-specific metadata</param>
    /// <exception cref="ArgumentException">Thrown when invariants are violated</exception>
    public Sample(
        DateTime timestamp,
        Latency duration,
        SampleStatus status,
        ErrorClassification? errorClassification,
        ExecutionContext executionContext,
        Dictionary<string, object>? metadata = null)
    {
        // Invariant 1: Timestamp cannot be in the future
        if (timestamp > DateTime.UtcNow)
        {
            throw new ArgumentException(
                "Timestamp cannot be in the future",
                nameof(timestamp));
        }

        // Invariant 2: Duration must be non-negative
        if (duration == null)
        {
            throw new ArgumentNullException(nameof(duration));
        }

        if (duration.Value < 0)
        {
            throw new ArgumentException(
                "Duration cannot be negative",
                nameof(duration));
        }

        // Invariant 3: Error classification required if status is Failure
        if (status == SampleStatus.Failure && errorClassification == null)
        {
            throw new ArgumentException(
                "ErrorClassification is required when Status is Failure",
                nameof(errorClassification));
        }

        // Invariant 4: Error classification must be null if status is Success
        if (status == SampleStatus.Success && errorClassification != null)
        {
            throw new ArgumentException(
                "ErrorClassification must be null when Status is Success",
                nameof(errorClassification));
        }

        if (executionContext == null)
        {
            throw new ArgumentNullException(nameof(executionContext));
        }

        Id = Guid.NewGuid();
        Timestamp = timestamp;
        Duration = duration;
        Status = status;
        ErrorClassification = errorClassification;
        ExecutionContext = executionContext;

        // Create an immutable copy of metadata
        Metadata = metadata == null
            ? new Dictionary<string, object>(0)
            : new Dictionary<string, object>(metadata);
    }

    /// <summary>
    /// Determines whether the sample represents a successful request
    /// </summary>
    public bool IsSuccess => Status == SampleStatus.Success;

    /// <summary>
    /// Determines whether the sample represents a failed request
    /// </summary>
    public bool IsFailure => Status == SampleStatus.Failure;

    /// <summary>
    /// Creates a copy of this sample with updated metadata
    /// </summary>
    public Sample WithMetadata(Dictionary<string, object> newMetadata)
    {
        return new Sample(Timestamp, Duration, Status, ErrorClassification, ExecutionContext, newMetadata);
    }

    public override string ToString()
    {
        var status = IsSuccess
            ? $"Success ({Duration})"
            : $"Failure ({ErrorClassification}: {Duration})";

        return $"Sample[{ExecutionContext.EngineName}]: {status}";
    }

    public override bool Equals(object? obj)
    {
        if (obj is not Sample other)
            return false;

        return Id == other.Id;
    }

    public override int GetHashCode() => Id.GetHashCode();

    public static bool operator ==(Sample? a, Sample? b)
    {
        if (a is null && b is null)
            return true;
        if (a is null || b is null)
            return false;
        return a.Equals(b);
    }

    public static bool operator !=(Sample? a, Sample? b) => !(a == b);
}
