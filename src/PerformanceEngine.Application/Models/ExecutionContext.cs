namespace PerformanceEngine.Application.Models;

/// <summary>
/// Immutable value object representing the execution context for an evaluation.
/// Provides traceability information about when and where the evaluation occurred.
/// </summary>
public sealed record ExecutionContext
{
    /// <summary>
    /// Unique identifier for this execution.
    /// </summary>
    public required Guid ExecutionId { get; init; }

    /// <summary>
    /// Timestamp when the evaluation was requested (UTC).
    /// </summary>
    public required DateTime ExecutionTimestamp { get; init; }

    /// <summary>
    /// Optional environment identifier (e.g., "production", "staging").
    /// </summary>
    public string? Environment { get; init; }

    /// <summary>
    /// Optional additional metadata about the execution context.
    /// </summary>
    public IReadOnlyDictionary<string, string>? Metadata { get; init; }

    /// <summary>
    /// Creates a new execution context with the current timestamp.
    /// </summary>
    public static ExecutionContext Create(string? environment = null)
    {
        return new ExecutionContext
        {
            ExecutionId = Guid.NewGuid(),
            ExecutionTimestamp = DateTime.UtcNow,
            Environment = environment
        };
    }

    /// <summary>
    /// Creates a new execution context with explicit values (for testing).
    /// </summary>
    public static ExecutionContext CreateWithId(Guid executionId, DateTime timestamp, string? environment = null)
    {
        return new ExecutionContext
        {
            ExecutionId = executionId,
            ExecutionTimestamp = timestamp,
            Environment = environment
        };
    }
}
