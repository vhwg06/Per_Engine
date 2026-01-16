namespace PerformanceEngine.Evaluation.Infrastructure.Persistence;

using System.Collections.Concurrent;
using PerformanceEngine.Evaluation.Domain;
using PerformanceEngine.Evaluation.Ports;

/// <summary>
/// In-memory implementation of the evaluation result repository.
/// 
/// Uses ConcurrentDictionary for thread-safe atomic operations.
/// Suitable for testing and development; not for production with large result sets.
/// 
/// Guarantees:
/// - Atomic persistence via TryAdd (no partial writes)
/// - No duplicates (TryAdd prevents same ID being persisted twice)
/// - Thread-safe concurrent operations
/// - Deterministic serialization
/// </summary>
public class InMemoryEvaluationResultRepository : IEvaluationResultRepository
{
    private readonly ConcurrentDictionary<Guid, EvaluationResult> _store = new();

    /// <summary>
    /// Persist an evaluation result atomically to in-memory storage.
    /// 
    /// Uses TryAdd to ensure atomicity: if the same ID already exists,
    /// throws InvalidOperationException (no silent overwrites).
    /// </summary>
    public Task<EvaluationResult> PersistAsync(
        EvaluationResult result,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(result);

        if (result.Id == Guid.Empty)
            throw new ArgumentException("Result ID must not be empty GUID", nameof(result));

        if (!_store.TryAdd(result.Id, result))
            throw new InvalidOperationException(
                $"Result with ID {result.Id} already exists. " +
                "Append-only semantics prevent overwriting existing results.");

        return Task.FromResult(result);
    }

    /// <summary>
    /// Retrieve a result by unique identifier.
    /// Returns null if not found (graceful empty handling).
    /// </summary>
    public Task<EvaluationResult?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("ID must not be empty GUID", nameof(id));

        var found = _store.TryGetValue(id, out var result);
        return Task.FromResult(found ? result : null);
    }

    /// <summary>
    /// Query results by timestamp range in chronological order.
    /// Returns empty enumerable if no results match.
    /// </summary>
    public IAsyncEnumerable<EvaluationResult> QueryByTimestampRangeAsync(
        DateTime startUtc,
        DateTime endUtc,
        CancellationToken cancellationToken = default)
    {
        if (startUtc.Kind != DateTimeKind.Utc)
            throw new ArgumentException("Start timestamp must be UTC", nameof(startUtc));

        if (endUtc.Kind != DateTimeKind.Utc)
            throw new ArgumentException("End timestamp must be UTC", nameof(endUtc));

        if (endUtc < startUtc)
            throw new ArgumentException(
                "End timestamp must be greater than or equal to start timestamp",
                nameof(endUtc));

        var results = _store.Values
            .Where(r => r.EvaluatedAt >= startUtc && r.EvaluatedAt <= endUtc)
            .OrderBy(r => r.EvaluatedAt)
            .AsAsyncEnumerable();

        return results;
    }

    /// <summary>
    /// Query results by test identifier.
    /// Returns empty enumerable if no results match.
    /// 
    /// Note: Currently returns empty as TestId is not part of the evaluation result.
    /// This method signature is provided for future implementation when test context
    /// is added to the result model.
    /// </summary>
    public IAsyncEnumerable<EvaluationResult> QueryByTestIdAsync(
        string testId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(testId))
            throw new ArgumentException("Test ID must not be empty", nameof(testId));

        // Currently, EvaluationResult doesn't have TestId.
        // This method returns empty; future implementation will add TestId context.
        return AsyncEnumerable.Empty<EvaluationResult>();
    }

    /// <summary>
    /// Clear all stored results (for testing purposes).
    /// </summary>
    internal void Clear()
    {
        _store.Clear();
    }

    /// <summary>
    /// Get count of persisted results (for testing purposes).
    /// </summary>
    internal int Count => _store.Count;
}
