namespace PerformanceEngine.Metrics.Domain.Metrics;

/// <summary>
/// Represents the context of an execution - which engine ran the test and other metadata.
/// This is a value object that links samples to their source execution.
/// </summary>
public sealed class ExecutionContext : ValueObject
{
    /// <summary>
    /// Gets the name of the execution engine that produced this sample
    /// (e.g., "k6", "JMeter", "Gatling")
    /// </summary>
    public string EngineName { get; }

    /// <summary>
    /// Gets the unique identifier for this execution run
    /// </summary>
    public Guid ExecutionId { get; }

    /// <summary>
    /// Gets the name of the scenario being executed (optional)
    /// </summary>
    public string? ScenarioName { get; }

    /// <summary>
    /// Initializes a new instance of the ExecutionContext class.
    /// </summary>
    /// <param name="engineName">The name of the execution engine</param>
    /// <param name="executionId">The unique identifier for this execution</param>
    /// <param name="scenarioName">Optional name of the scenario being executed</param>
    /// <exception cref="ArgumentException">Thrown when engineName is null or empty</exception>
    public ExecutionContext(string engineName, Guid executionId, string? scenarioName = null)
    {
        if (string.IsNullOrWhiteSpace(engineName))
        {
            throw new ArgumentException("Engine name cannot be null or empty", nameof(engineName));
        }

        if (executionId == Guid.Empty)
        {
            throw new ArgumentException("Execution ID cannot be empty", nameof(executionId));
        }

        EngineName = engineName.Trim();
        ExecutionId = executionId;
        ScenarioName = string.IsNullOrWhiteSpace(scenarioName) ? null : scenarioName.Trim();
    }

    protected override IEnumerable<object?> GetAtomicValues()
    {
        yield return EngineName;
        yield return ExecutionId;
        yield return ScenarioName;
    }

    public override string ToString()
    {
        if (string.IsNullOrEmpty(ScenarioName))
        {
            return $"{EngineName}/{ExecutionId:N}";
        }

        return $"{EngineName}/{ExecutionId:N}/{ScenarioName}";
    }
}
