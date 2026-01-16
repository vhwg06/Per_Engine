# Repository Port Contracts

**Version**: 1.0  
**Created**: 2026-01-16  
**Specification**: specs/001-repository-port/spec.md  
**Status**: Design

## Overview

This document defines the repository port contracts for the Performance & Reliability Testing Platform following Clean Architecture principles. Repository ports are domain-defined interfaces that abstract persistence concerns from domain logic.

## Design Principles

1. **Ports defined in domain layer** - Infrastructure implements, domain defines
2. **Technology-agnostic** - No storage-specific concepts (SQL, NoSQL, ORM)
3. **Single Responsibility** - Separate concerns: CRUD, Audit, Versioning, Transactions
4. **Generic where beneficial** - Reduce duplication across aggregate roots
5. **Explicit contracts** - Clear method signatures, error semantics, consistency guarantees

## Core Contracts

### IRepository<TEntity, TId>

**Purpose**: Generic CRUD operations for aggregate roots.

**Location**: `src/PerformanceEngine.Domain.Shared/Ports/IRepository.cs` (new shared domain)

**Interface Definition**:

```csharp
namespace PerformanceEngine.Domain.Shared.Ports;

/// <summary>
/// Generic repository port for aggregate root persistence.
/// Provides CRUD operations with strong consistency guarantees.
/// </summary>
/// <typeparam name="TEntity">Aggregate root entity type</typeparam>
/// <typeparam name="TId">Entity identifier value object type</typeparam>
public interface IRepository<TEntity, TId> 
    where TEntity : class
    where TId : notnull
{
    /// <summary>
    /// Creates and persists a new entity.
    /// </summary>
    /// <param name="entity">Entity to create</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The entity identifier assigned by the repository</returns>
    /// <exception cref="ArgumentNullException">When entity is null</exception>
    /// <exception cref="RepositoryException">When persistence fails</exception>
    /// <remarks>
    /// FR-005: Repository ports MUST support Create operation
    /// FR-010: Create operation MUST handle duplicate identifier scenarios
    /// FR-036: Write operations MUST ensure durability before returning
    /// </remarks>
    Task<TId> CreateAsync(TEntity entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves an entity by its unique identifier.
    /// </summary>
    /// <param name="id">Entity identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Entity if found, null if not found</returns>
    /// <exception cref="ArgumentNullException">When id is null</exception>
    /// <exception cref="RepositoryException">When retrieval fails due to infrastructure error</exception>
    /// <remarks>
    /// FR-006: Repository ports MUST support Read operation
    /// FR-009: Read operation MUST clearly indicate when entity does not exist
    /// FR-035: Read operations use strong consistency by default
    /// FR-037: Read-after-write MUST reflect most recent write
    /// </remarks>
    Task<TEntity?> GetByIdAsync(TId id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing entity.
    /// </summary>
    /// <param name="entity">Entity with modified state</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if updated, false if entity not found</returns>
    /// <exception cref="ArgumentNullException">When entity is null</exception>
    /// <exception cref="ConcurrencyException">When concurrent modification detected</exception>
    /// <exception cref="RepositoryException">When persistence fails</exception>
    /// <remarks>
    /// FR-007: Repository ports MUST support Update operation
    /// FR-036: Write operations MUST ensure durability before returning
    /// Implementations SHOULD support optimistic concurrency control
    /// </remarks>
    Task<bool> UpdateAsync(TEntity entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes an entity by its identifier.
    /// </summary>
    /// <param name="id">Entity identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if deleted, false if entity not found</returns>
    /// <exception cref="ArgumentNullException">When id is null</exception>
    /// <exception cref="RepositoryException">When deletion fails</exception>
    /// <remarks>
    /// FR-008: Repository ports MUST support Delete operation
    /// Audit trail MUST persist after deletion (see IAuditLog)
    /// </remarks>
    Task<bool> DeleteAsync(TId id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if an entity with the given identifier exists.
    /// </summary>
    /// <param name="id">Entity identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if exists, false otherwise</returns>
    /// <exception cref="ArgumentNullException">When id is null</exception>
    /// <remarks>
    /// Utility method for existence checks without loading full entity.
    /// Uses strong consistency (FR-035).
    /// </remarks>
    Task<bool> ExistsAsync(TId id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Queries entities matching the given specification.
    /// </summary>
    /// <param name="specification">Query criteria</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of matching entities</returns>
    /// <exception cref="ArgumentNullException">When specification is null</exception>
    /// <exception cref="RepositoryException">When query fails</exception>
    /// <remarks>
    /// FR-011: Repository ports MUST support query operations with filter criteria
    /// FR-012: Query operations MUST support pagination
    /// FR-013: Query operations MUST support result ordering
    /// FR-014: Query criteria MUST be expressed in domain concepts
    /// FR-015: Query operations MUST return empty results when no matches, not errors
    /// </remarks>
    Task<IReadOnlyList<TEntity>> QueryAsync(
        IQuerySpecification<TEntity> specification, 
        CancellationToken cancellationToken = default);
}
```

### IQuerySpecification<TEntity>

**Purpose**: Express query criteria in domain language without storage-specific syntax.

**Location**: `src/PerformanceEngine.Domain.Shared/Ports/IQuerySpecification.cs`

**Interface Definition**:

```csharp
namespace PerformanceEngine.Domain.Shared.Ports;

/// <summary>
/// Specification pattern for expressing query criteria in domain language.
/// Implementations encapsulate filter logic, ordering, and pagination.
/// </summary>
/// <typeparam name="TEntity">Entity type to query</typeparam>
public interface IQuerySpecification<TEntity> where TEntity : class
{
    /// <summary>
    /// Predicate to filter entities. Implementation can translate to storage-specific query.
    /// </summary>
    /// <remarks>
    /// FR-014: Query criteria MUST be expressed in domain concepts, not storage-specific languages
    /// Expression trees allow translation to SQL, LINQ, or other query languages
    /// </remarks>
    Expression<Func<TEntity, bool>>? FilterPredicate { get; }

    /// <summary>
    /// Ordering criteria for results.
    /// </summary>
    /// <remarks>FR-013: Query operations MUST support result ordering</remarks>
    IReadOnlyList<(Expression<Func<TEntity, object>> KeySelector, bool Ascending)> OrderBy { get; }

    /// <summary>
    /// Number of results to skip (for pagination).
    /// </summary>
    /// <remarks>FR-012: Query operations MUST support pagination through offset and limit</remarks>
    int? Skip { get; }

    /// <summary>
    /// Maximum number of results to return (for pagination).
    /// </summary>
    /// <remarks>FR-012: Query operations MUST support pagination through offset and limit</remarks>
    int? Take { get; }
}
```

**Example Specification**:

```csharp
/// <summary>
/// Specification for querying baselines within a date range.
/// </summary>
public class BaselinesByDateRangeSpec : IQuerySpecification<Baseline>
{
    private readonly DateTime _startDate;
    private readonly DateTime _endDate;

    public BaselinesByDateRangeSpec(DateTime startDate, DateTime endDate)
    {
        _startDate = startDate;
        _endDate = endDate;
    }

    public Expression<Func<Baseline, bool>> FilterPredicate => 
        b => b.CreatedAt >= _startDate && b.CreatedAt <= _endDate;

    public IReadOnlyList<(Expression<Func<Baseline, object>> KeySelector, bool Ascending)> OrderBy =>
        new[] { ((Expression<Func<Baseline, object>>)(b => b.CreatedAt), false) }; // Descending

    public int? Skip => null;
    public int? Take => null;
}
```

### IAuditLog

**Purpose**: Record all entity modifications for audit trail and compliance.

**Location**: `src/PerformanceEngine.Domain.Shared/Ports/IAuditLog.cs`

**Interface Definition**:

```csharp
namespace PerformanceEngine.Domain.Shared.Ports;

/// <summary>
/// Audit trail port for recording entity change operations.
/// Audit records are immutable and survive entity deletion.
/// </summary>
public interface IAuditLog
{
    /// <summary>
    /// Records an entity operation in the audit trail.
    /// </summary>
    /// <param name="record">Audit record to persist</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <exception cref="ArgumentNullException">When record is null</exception>
    /// <exception cref="RepositoryException">When audit recording fails</exception>
    /// <remarks>
    /// FR-016: Repository ports MUST support capturing audit metadata
    /// FR-019: Audit records MUST be immutable once created
    /// FR-020: Audit trail MUST persist independently of entity lifecycle
    /// SC-004: Audit metadata captured within 100ms of operation completion
    /// </remarks>
    Task RecordAsync(AuditRecord record, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves audit records for a specific entity.
    /// </summary>
    /// <param name="entityId">Entity identifier</param>
    /// <param name="entityType">Entity type name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Audit records in chronological order</returns>
    /// <exception cref="ArgumentException">When entityId or entityType is null/empty</exception>
    /// <remarks>
    /// FR-018: Audit trail MUST be queryable by entity identifier
    /// Results ordered chronologically (oldest first)
    /// </remarks>
    Task<IReadOnlyList<AuditRecord>> GetByEntityAsync(
        string entityId, 
        string entityType, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves audit records within a time range.
    /// </summary>
    /// <param name="startTime">Start of time range (inclusive)</param>
    /// <param name="endTime">End of time range (inclusive)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Audit records in chronological order</returns>
    /// <remarks>
    /// FR-018: Audit trail MUST be queryable by time range
    /// Results ordered chronologically (oldest first)
    /// </remarks>
    Task<IReadOnlyList<AuditRecord>> GetByTimeRangeAsync(
        DateTime startTime, 
        DateTime endTime, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves audit records for an entity within a time range.
    /// </summary>
    /// <param name="entityId">Entity identifier</param>
    /// <param name="entityType">Entity type name</param>
    /// <param name="startTime">Start of time range (inclusive)</param>
    /// <param name="endTime">End of time range (inclusive)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Audit records in chronological order</returns>
    /// <remarks>
    /// Combines entity and time range queries for targeted audit trail review
    /// </remarks>
    Task<IReadOnlyList<AuditRecord>> GetByEntityAndTimeRangeAsync(
        string entityId,
        string entityType,
        DateTime startTime,
        DateTime endTime,
        CancellationToken cancellationToken = default);
}
```

### AuditRecord (Value Object)

**Purpose**: Immutable record of an entity operation.

**Location**: `src/PerformanceEngine.Domain.Shared/Models/AuditRecord.cs`

```csharp
namespace PerformanceEngine.Domain.Shared.Models;

/// <summary>
/// Immutable audit record capturing entity operation metadata.
/// </summary>
/// <remarks>
/// FR-017: Audit metadata MUST include operation type, timestamp, and entity identifier
/// FR-019: Audit records MUST be immutable once created
/// </remarks>
public sealed record AuditRecord
{
    public required string EntityId { get; init; }
    public required string EntityType { get; init; }
    public required OperationType Operation { get; init; }
    public required DateTime Timestamp { get; init; }
    
    /// <summary>
    /// Optional: Properties that changed during Update operations.
    /// </summary>
    /// <remarks>FR-017: Optionally capture which properties changed</remarks>
    public IReadOnlyDictionary<string, string>? ChangedProperties { get; init; }

    public AuditRecord()
    {
        // Required for record initialization
    }

    public static AuditRecord ForCreate(string entityId, string entityType, DateTime timestamp)
    {
        return new AuditRecord
        {
            EntityId = entityId,
            EntityType = entityType,
            Operation = OperationType.Create,
            Timestamp = timestamp
        };
    }

    public static AuditRecord ForUpdate(
        string entityId, 
        string entityType, 
        DateTime timestamp,
        IReadOnlyDictionary<string, string>? changedProperties = null)
    {
        return new AuditRecord
        {
            EntityId = entityId,
            EntityType = entityType,
            Operation = OperationType.Update,
            Timestamp = timestamp,
            ChangedProperties = changedProperties
        };
    }

    public static AuditRecord ForDelete(string entityId, string entityType, DateTime timestamp)
    {
        return new AuditRecord
        {
            EntityId = entityId,
            EntityType = entityType,
            Operation = OperationType.Delete,
            Timestamp = timestamp
        };
    }
}

public enum OperationType
{
    Create,
    Update,
    Delete
}
```

### IVersionStore<TEntity, TId>

**Purpose**: Store and retrieve historical versions of entities.

**Location**: `src/PerformanceEngine.Domain.Shared/Ports/IVersionStore.cs`

**Interface Definition**:

```csharp
namespace PerformanceEngine.Domain.Shared.Ports;

/// <summary>
/// Version store port for entity state snapshots and time-travel queries.
/// Supports advanced scenarios like debugging, recovery, and event sourcing.
/// </summary>
/// <typeparam name="TEntity">Entity type</typeparam>
/// <typeparam name="TId">Entity identifier type</typeparam>
public interface IVersionStore<TEntity, TId>
    where TEntity : class
    where TId : notnull
{
    /// <summary>
    /// Stores a new version snapshot of an entity.
    /// </summary>
    /// <param name="entityId">Entity identifier</param>
    /// <param name="entity">Entity state to snapshot</param>
    /// <param name="timestamp">Version timestamp</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Version identifier</returns>
    /// <exception cref="ArgumentNullException">When parameters are null</exception>
    /// <exception cref="RepositoryException">When version storage fails</exception>
    /// <remarks>
    /// FR-021: Repository ports MUST support storing multiple versions of entity state
    /// FR-022: Each version MUST have unique version identifier and timestamp
    /// Called automatically by repository on Update operations
    /// </remarks>
    Task<long> StoreVersionAsync(
        TId entityId, 
        TEntity entity, 
        DateTime timestamp, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves an entity at a specific version.
    /// </summary>
    /// <param name="entityId">Entity identifier</param>
    /// <param name="versionId">Version identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Entity state at version, or null if not found</returns>
    /// <remarks>
    /// FR-023: Repository ports MUST support retrieving entity state at specific version
    /// SC-005: Version retrieval latency under 500ms
    /// </remarks>
    Task<TEntity?> GetVersionAsync(
        TId entityId, 
        long versionId, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves an entity state at a specific point in time.
    /// </summary>
    /// <param name="entityId">Entity identifier</param>
    /// <param name="pointInTime">Target timestamp</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Entity state active at that time, or null if not found</returns>
    /// <remarks>
    /// FR-024: Repository ports MUST support retrieving entity state at specific point in time
    /// Returns version with timestamp ≤ pointInTime (most recent)
    /// </remarks>
    Task<TEntity?> GetVersionAtTimeAsync(
        TId entityId, 
        DateTime pointInTime, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves version history for an entity.
    /// </summary>
    /// <param name="entityId">Entity identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Version metadata in chronological order</returns>
    /// <remarks>
    /// FR-025: Version history MUST be queryable for given entity identifier
    /// Returns metadata only (not full entity snapshots) for efficiency
    /// </remarks>
    Task<IReadOnlyList<VersionMetadata>> GetVersionHistoryAsync(
        TId entityId, 
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Metadata about a specific entity version.
/// </summary>
public sealed record VersionMetadata
{
    public required long VersionId { get; init; }
    public required DateTime Timestamp { get; init; }
    public required string EntityId { get; init; }
    public required string EntityType { get; init; }
}
```

### IUnitOfWork

**Purpose**: Transaction boundary for atomic multi-entity operations.

**Location**: `src/PerformanceEngine.Domain.Shared/Ports/IUnitOfWork.cs`

**Interface Definition**:

```csharp
namespace PerformanceEngine.Domain.Shared.Ports;

/// <summary>
/// Unit of Work port for transactional boundaries.
/// Ensures multiple repository operations succeed or fail atomically.
/// </summary>
/// <remarks>
/// FR-026: Repository ports MUST define transactional boundary interfaces
/// FR-027: Transaction interface MUST support commit and rollback
/// FR-028: Operations within transaction MUST succeed or fail as unit
/// FR-029: Transaction failure MUST rollback all operations
/// </remarks>
public interface IUnitOfWork : IDisposable
{
    /// <summary>
    /// Begins a new transaction scope.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <exception cref="InvalidOperationException">When transaction already active</exception>
    /// <exception cref="RepositoryException">When transaction cannot be started</exception>
    Task BeginAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Commits all operations in the current transaction.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <exception cref="InvalidOperationException">When no active transaction</exception>
    /// <exception cref="RepositoryException">When commit fails</exception>
    /// <remarks>
    /// FR-028: Operations succeed together
    /// All pending changes are persisted durably
    /// </remarks>
    Task CommitAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Rolls back all operations in the current transaction.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <exception cref="InvalidOperationException">When no active transaction</exception>
    /// <remarks>
    /// FR-029: Transaction failure rolls back all operations
    /// Entity state reverts to pre-transaction state
    /// </remarks>
    Task RollbackAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Indicates whether a transaction is currently active.
    /// </summary>
    bool IsActive { get; }
}
```

## Exception Hierarchy

**Location**: `src/PerformanceEngine.Domain.Shared/Exceptions/`

```csharp
/// <summary>
/// Base exception for all repository-related errors.
/// </summary>
public class RepositoryException : Exception
{
    public RepositoryException(string message) : base(message) { }
    public RepositoryException(string message, Exception innerException) 
        : base(message, innerException) { }
}

/// <summary>
/// Exception thrown when concurrent modification is detected.
/// </summary>
public class ConcurrencyException : RepositoryException
{
    public string EntityId { get; }
    public string EntityType { get; }

    public ConcurrencyException(string entityId, string entityType, string message) 
        : base(message)
    {
        EntityId = entityId;
        EntityType = entityType;
    }
}
```

## Backward Compatibility Strategy

### Existing Interfaces

1. **IPersistenceRepository** (Metrics.Domain)
   - Currently has: SaveMetricAsync, RetrieveMetricAsync, DeleteMetricAsync
   - Strategy: Extend to implement `IRepository<Metric, Guid>`
   - Add: CreateAsync delegates to SaveMetricAsync, GetByIdAsync to RetrieveMetricAsync, etc.

2. **IBaselineRepository** (Baseline.Domain)
   - Currently has: CreateAsync, GetByIdAsync, ListRecentAsync
   - Strategy: Extend to implement `IRepository<Baseline, BaselineId>`
   - Add: UpdateAsync, DeleteAsync, QueryAsync, ExistsAsync

### Migration Path

```csharp
// Phase 1: Existing interface unchanged (backward compatible)
public interface IBaselineRepository
{
    Task<BaselineId> CreateAsync(Baseline baseline);
    Task<Baseline?> GetByIdAsync(BaselineId id);
    Task<IReadOnlyList<Baseline>> ListRecentAsync(int count);
}

// Phase 2: Extend with new interface (additive)
public interface IBaselineRepository : IRepository<Baseline, BaselineId>
{
    // Existing methods remain
    Task<BaselineId> CreateAsync(Baseline baseline);
    Task<Baseline?> GetByIdAsync(BaselineId id);
    Task<IReadOnlyList<Baseline>> ListRecentAsync(int count);
    
    // IRepository methods explicitly implemented
    Task<BaselineId> IRepository<Baseline, BaselineId>.CreateAsync(
        Baseline entity, CancellationToken ct) => CreateAsync(entity);
    
    // Additional methods
    Task<bool> UpdateAsync(Baseline entity, CancellationToken ct = default);
    Task<bool> DeleteAsync(BaselineId id, CancellationToken ct = default);
    // ... etc
}
```

## Usage Examples

### Basic CRUD with Audit

```csharp
// Inject dependencies
private readonly IRepository<Baseline, BaselineId> _repository;
private readonly IAuditLog _auditLog;

public async Task CreateBaselineWithAuditAsync(Baseline baseline)
{
    // Create entity
    var id = await _repository.CreateAsync(baseline);
    
    // Record audit
    var auditRecord = AuditRecord.ForCreate(
        id.Value, 
        nameof(Baseline), 
        DateTime.UtcNow
    );
    await _auditLog.RecordAsync(auditRecord);
}
```

### Query with Specification

```csharp
// Define specification
var spec = new BaselinesByDateRangeSpec(startDate, endDate);

// Execute query
var baselines = await _repository.QueryAsync(spec);
```

### Versioned Update

```csharp
private readonly IRepository<Baseline, BaselineId> _repository;
private readonly IVersionStore<Baseline, BaselineId> _versionStore;
private readonly IAuditLog _auditLog;

public async Task UpdateBaselineWithVersioningAsync(Baseline baseline)
{
    // Store current version before update
    await _versionStore.StoreVersionAsync(
        baseline.Id, 
        baseline, 
        DateTime.UtcNow
    );
    
    // Update entity
    var updated = await _repository.UpdateAsync(baseline);
    
    // Record audit
    if (updated)
    {
        var auditRecord = AuditRecord.ForUpdate(
            baseline.Id.Value, 
            nameof(Baseline), 
            DateTime.UtcNow
        );
        await _auditLog.RecordAsync(auditRecord);
    }
}
```

### Transactional Operation

```csharp
private readonly IRepository<Baseline, BaselineId> _baselineRepo;
private readonly IRepository<Metric, Guid> _metricRepo;
private readonly IUnitOfWork _unitOfWork;

public async Task CreateBaselineWithMetricsAsync(
    Baseline baseline, 
    IEnumerable<Metric> metrics)
{
    await _unitOfWork.BeginAsync();
    
    try
    {
        // Multiple operations in transaction
        var baselineId = await _baselineRepo.CreateAsync(baseline);
        
        foreach (var metric in metrics)
        {
            await _metricRepo.CreateAsync(metric);
        }
        
        await _unitOfWork.CommitAsync();
    }
    catch
    {
        await _unitOfWork.RollbackAsync();
        throw;
    }
}
```

## Implementation Notes

### Storage Backend Considerations

**Redis Implementation**:
- CRUD: Use String commands (SET, GET, DEL)
- Audit: Use Streams (XADD) for append-only log
- Versioning: Use Sorted Sets with timestamp scores
- Transactions: Use MULTI/EXEC

**SQL Implementation**:
- CRUD: Standard INSERT, SELECT, UPDATE, DELETE
- Audit: Separate audit_log table with FK to entities
- Versioning: Separate version_history table with entity snapshots
- Transactions: Native database transactions

**In-Memory Implementation** (for testing):
- CRUD: Dictionary<TId, TEntity>
- Audit: List<AuditRecord>
- Versioning: Dictionary<TId, List<(long version, TEntity snapshot)>>
- Transactions: Memento pattern with rollback support

### Performance Considerations

1. **Lazy Loading**: Version history returns metadata only, load full snapshots on demand
2. **Batch Operations**: Consider adding batch CRUD methods for bulk operations
3. **Caching**: Repository implementations may cache frequently accessed entities
4. **Pagination**: Query specifications support offset/limit to avoid loading large result sets

## Testing Strategy

### Unit Tests (Domain Layer)

- Test specification filter logic with mock entities
- Test audit record immutability
- Test exception hierarchy

### Integration Tests (Infrastructure Layer)

- Test repository implementations against real storage (Redis, SQL)
- Test audit log persistence and querying
- Test version store snapshot and retrieval
- Test transaction commit/rollback
- Test concurrency conflict detection

### Acceptance Tests (Specification Compliance)

- Verify all FR requirements from repository-port spec
- Test all acceptance scenarios from spec
- Verify success criteria (SC-001 to SC-010)

## References

- **Specification**: `/specs/001-repository-port/spec.md`
- **Constitution**: `/.specify/memory/constitution.md`
- **Base Plan**: `/docs/architecture/base-architecture-plan.md`
