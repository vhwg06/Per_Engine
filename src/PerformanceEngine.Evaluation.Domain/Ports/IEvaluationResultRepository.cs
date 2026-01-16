namespace PerformanceEngine.Evaluation.Ports;

/// <summary>
/// Repository port (interface) defining the contract for persisting and retrieving
/// evaluation results. This abstraction is technology-agnostic and remains stable
/// across different storage implementations (in-memory, SQL, cloud storage, etc.).
/// 
/// Implements append-only semantics:
/// - Persist: Create new evaluation result record (atomic, all-or-nothing)
/// - GetById: Retrieve persisted result by unique identifier
/// - Query: Search results by timestamp range or other criteria
/// - No Update, Delete, or Modify operations (append-only enforcement)
/// 
/// All operations are async-first to support concurrent persistence without blocking.
/// </summary>
public interface IEvaluationResultRepository
{
    /// <summary>
    /// Persist an evaluation result atomically to storage.
    /// 
    /// Atomicity Guarantee: Either the entire result is persisted with all metadata,
    /// violations, and evidence, or nothing is persisted (no partial writes).
    /// 
    /// Append-Only Semantics: Once persisted, the result is immutable. Subsequent
    /// persist operations create new result records, never modify existing ones.
    /// 
    /// Concurrency Safety: Multiple concurrent persist operations are safe and will
    /// not result in race conditions or data corruption. Each result has a unique
    /// identifier that prevents duplicate writes.
    /// </summary>
    /// <param name="result">
    /// The evaluation result to persist. Must not be null and must contain all
    /// required data (ID, outcome, violations, evidence, timestamp).
    /// </param>
    /// <param name="cancellationToken">
    /// Cancellation token to allow graceful shutdown of long-running operations.
    /// </param>
    /// <returns>
    /// The persisted result (identical to the input after storage).
    /// Returned result is guaranteed to be byte-identical when serialized.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown if result is null.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown if a result with the same ID already exists (duplicate prevention).
    /// </exception>
    /// <exception cref="IOException">
    /// Thrown if storage is unavailable or inaccessible.
    /// </exception>
    Task<EvaluationResult> PersistAsync(
        EvaluationResult result,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieve a persisted evaluation result by its unique identifier.
    /// 
    /// Returns the exact immutable result with all original metadata, violations,
    /// and evidence completely intact. Results are byte-identical to the originally
    /// persisted data (deterministic serialization preserved).
    /// 
    /// Empty Result Handling: If no result exists with the given ID, returns null
    /// (not an error). This allows graceful handling of missing results at the
    /// application layer.
    /// </summary>
    /// <param name="id">
    /// The unique identifier of the result to retrieve. Must not be Guid.Empty.
    /// </param>
    /// <param name="cancellationToken">
    /// Cancellation token to allow graceful shutdown of long-running operations.
    /// </param>
    /// <returns>
    /// The evaluation result if found; null if no result exists with the given ID.
    /// When returned, the result is guaranteed to be identical (byte-for-byte) to
    /// the originally persisted data.
    /// </returns>
    /// <exception cref="IOException">
    /// Thrown if storage is unavailable or inaccessible.
    /// </exception>
    Task<EvaluationResult?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Query persisted evaluation results by timestamp range.
    /// 
    /// Returns all results evaluated between the start and end timestamps (inclusive).
    /// Results are returned in chronological order (earliest to latest).
    /// 
    /// Empty Result Handling: If no results exist within the range, returns an empty
    /// enumerable (not an error). This allows graceful handling of queries with no
    /// matching results.
    /// 
    /// Uses IAsyncEnumerable for memory efficiency with large result sets.
    /// </summary>
    /// <param name="startUtc">
    /// The start of the timestamp range (inclusive). Must be UTC-based.
    /// </param>
    /// <param name="endUtc">
    /// The end of the timestamp range (inclusive). Must be UTC-based and >= startUtc.
    /// </param>
    /// <param name="cancellationToken">
    /// Cancellation token to allow graceful shutdown of long-running operations.
    /// </param>
    /// <returns>
    /// An async enumerable of evaluation results within the timestamp range, ordered
    /// chronologically (earliest first). Empty enumerable if no results match.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Thrown if endUtc is before startUtc.
    /// </exception>
    /// <exception cref="IOException">
    /// Thrown if storage is unavailable or inaccessible.
    /// </exception>
    IAsyncEnumerable<EvaluationResult> QueryByTimestampRangeAsync(
        DateTime startUtc,
        DateTime endUtc,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Query persisted evaluation results by test identifier.
    /// 
    /// Returns all results associated with a specific test. The mapping between
    /// results and test identifiers is maintained at the application layer; the
    /// repository stores this relationship in result metadata or through secondary
    /// index lookups.
    /// 
    /// Empty Result Handling: If no results exist for the given test ID, returns
    /// an empty enumerable (not an error).
    /// 
    /// Uses IAsyncEnumerable for memory efficiency with large result sets.
    /// </summary>
    /// <param name="testId">
    /// The unique identifier of the test. Must not be null or empty.
    /// </param>
    /// <param name="cancellationToken">
    /// Cancellation token to allow graceful shutdown of long-running operations.
    /// </param>
    /// <returns>
    /// An async enumerable of evaluation results associated with the test ID.
    /// Empty enumerable if no results match.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Thrown if testId is null or empty.
    /// </exception>
    /// <exception cref="IOException">
    /// Thrown if storage is unavailable or inaccessible.
    /// </exception>
    IAsyncEnumerable<EvaluationResult> QueryByTestIdAsync(
        string testId,
        CancellationToken cancellationToken = default);
}
