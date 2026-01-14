namespace PerformanceEngine.Metrics.Domain.Ports;

using Metrics;
using System.Collections.Immutable;

/// <summary>
/// Port interface for adapting execution engine results to domain Sample objects.
/// Implementations of this interface map engine-specific result formats into the domain ubiquitous language.
/// This is a port (external dependency abstraction) - implementations are infrastructure concerns.
/// </summary>
public interface IExecutionEngineAdapter
{
    /// <summary>
    /// Gets the name of the execution engine this adapter handles
    /// </summary>
    string EngineName { get; }

    /// <summary>
    /// Maps raw execution engine results into a collection of domain Sample objects.
    /// This operation is deterministic - identical inputs always produce identical outputs.
    /// </summary>
    /// <param name="rawResults">The raw results from the execution engine (format is engine-specific)</param>
    /// <param name="executionContext">The context of this execution (engine name, run ID, etc.)</param>
    /// <returns>An immutable collection of domain Sample objects</returns>
    /// <exception cref="ArgumentNullException">Thrown when parameters are null</exception>
    /// <exception cref="InvalidOperationException">Thrown when results cannot be mapped to domain models</exception>
    ImmutableList<Sample> MapResultsToDomain(object rawResults, ExecutionContext executionContext);

    /// <summary>
    /// Validates that the raw results can be mapped by this adapter.
    /// </summary>
    /// <param name="rawResults">The raw results to validate</param>
    /// <returns>True if the results can be mapped; false otherwise</returns>
    bool CanHandle(object? rawResults);
}
