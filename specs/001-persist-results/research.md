# Phase 0 Research: Persist Results for Audit & Replay

**Date**: 2026-01-16 | **Feature**: 001-persist-results | **Status**: Complete

## Overview

Phase 0 research resolved all technical clarifications identified in the planning phase. All findings confirm that the specification is implementable within the existing .NET 8.0 architecture following Clean Architecture and SOLID principles already established in the codebase.

---

## Technology Stack Validation

### Language & Framework ✅

**Decision**: C# 12 targeting .NET 8.0

**Rationale**:
- Existing project uses C# 12 with .NET 8.0 across all domain projects
- Strong type system enables compile-time enforcement of immutability and append-only semantics
- Async/await patterns align with concurrent persistence requirements
- Record types (C# 9+) provide value-based equality and immutability by default

**Alternatives Considered**:
- C# 11: Lacks some minor language features; C# 12 is project standard
- Python/Node.js: Project established in C#/.NET; language switching unnecessary

### Repository Pattern in Existing Codebase ✅

**Finding**: Project already demonstrates port abstraction pattern:
- `PerformanceEngine.Evaluation.Domain/Ports/IPartialMetricPolicy.cs` shows port definition pattern
- Existing infrastructure projects implement these ports
- Dependency injection via service collection registration

**Application to persist-results**:
```csharp
// Port defined in domain
public interface IEvaluationResultRepository { ... }

// Implemented in infrastructure
public class InMemoryEvaluationResultRepository : IEvaluationResultRepository { ... }

// Registered in DI
services.AddScoped<IEvaluationResultRepository, InMemoryEvaluationResultRepository>();
```

**Benefits**:
- Tested pattern reduces implementation risk
- Substitutability enables testing without infrastructure
- Technology-agnostic interface protects domain layer

---

## Deterministic Serialization Strategy

### Challenge

Specification requires (SC-002): "Persisted results remain byte-identical after storage - retrieved results match original results exactly (deterministic serialization)"

This prevents silent data corruption and enables reliable audit trails.

### Solution: System.Text.Json with Deterministic Ordering ✅

**Decision**: Use `System.Text.Json` with custom serialization context to enforce deterministic ordering

**Configuration**:
```csharp
var options = new JsonSerializerOptions
{
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    PropertyNameCaseInsensitive = false,
    WriteIndented = false,  // Compact format for binary comparison
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    TypeInfoResolver = new DefaultJsonTypeInfoResolver() // Custom for ordering
};
```

**Byte-Identity Verification**:
- Serialize → Deserialize → Serialize → Compare hashes
- Unit tests verify hash(original) == hash(retrieved)

### Timestamp Handling ✅

**Decision**: UTC-based ISO 8601 string format

**Implementation**:
```csharp
public DateTime EvaluatedAt { get; }  // Always UTC
// Serialization: "2026-01-16T14:30:45.1234567Z" (ISO 8601)
```

**Why ISO 8601**:
- Deterministic string representation
- No timezone ambiguity
- Standard across JSON tooling

### Metric Precision ✅

**Decision**: Preserve metric values as strings in evidence trail

**Rationale**:
- Decimal values prone to floating-point precision loss
- String preservation ensures exact replay value matching
- Evidence layer captures original values; application layer converts as needed

**Example**:
```csharp
public class MetricReferenceDto
{
    public string Name { get; set; }        // "ResponseTime"
    public string Value { get; set; }       // "95.5" (string, not double)
}
```

### Collection Ordering ✅

**Decision**: Collections (Violations, Evidence) persisted in definition order

**Implementation**:
- Use `ImmutableList<T>` to enforce ordering preservation
- Serialize arrays in list order (not hash-based dictionaries)
- No sorting or reordering during persistence/retrieval

**Test Coverage**:
```csharp
// Given violations in order: [Rule1, Rule2, Rule3]
// When persisted and retrieved
// Then violations remain in original order [Rule1, Rule2, Rule3]
```

---

## Immutability Enforcement Strategy

### Compile-Time Enforcement ✅

**C# Record Types**:
```csharp
public record EvaluationResult(
    Guid Id,
    Severity Outcome,
    ImmutableList<Violation> Violations,
    ImmutableList<EvaluationEvidence> Evidence,
    string OutcomeReason,
    DateTime EvaluatedAt
);
```

**Benefits**:
- Read-only by default
- Value-based equality
- Immutability guaranteed by type system
- No accidental mutations

**Alternative (rejected)**: Classes with private setters
- Verbose, error-prone (developers forget to make setters private)
- Record syntax more concise and idiomatic for immutable data

### Test Enforcement ✅

```csharp
[Fact]
public void EvaluationResult_IsImmutable()
{
    var result = new EvaluationResult(...);
    // This must NOT compile:
    // result.Outcome = Severity.Fail;  // Compile error: record is read-only
}
```

---

## Atomicity & Consistency Strategy

### In-Memory Repository ✅

**For Phase 1 (foundation)**: Simple atomic writes via lock

```csharp
private readonly ConcurrentDictionary<Guid, EvaluationResult> _store;

public async Task<EvaluationResult> PersistAsync(EvaluationResult result, CancellationToken ct)
{
    if (!_store.TryAdd(result.Id, result))
        throw new InvalidOperationException($"Result {result.Id} already exists");
    return result;
}
```

**Atomicity Guarantee**: `TryAdd` is atomic; no partial writes possible

**Testing**:
```csharp
[Fact]
public async Task ConcurrentPersist_AllSucceed_NoCollisions()
{
    var repo = new InMemoryEvaluationResultRepository();
    var tasks = Enumerable.Range(1, 100)
        .Select(i => repo.PersistAsync(CreateResult(Guid.NewGuid())))
        .ToList();
    
    var results = await Task.WhenAll(tasks);
    Assert.Equal(100, results.Count);  // All succeeded
}
```

### SQL Repository (Future Phase) ✅

**Pattern**: Database transaction with unique constraint

```sql
INSERT INTO EvaluationResults (Id, Outcome, EvaluatedAt, Payload)
VALUES (@Id, @Outcome, @EvaluatedAt, @Payload);
-- Unique constraint on Id prevents duplicates
-- Transaction ensures atomicity
```

**Append-only semantics**: No UPDATE clause in SQL; violations managed at application layer

---

## Concurrency & Replay Determinism

### Concurrent Persistence Without Race Conditions ✅

**Specification (SC-007)**: "100 concurrent persist operations complete without data corruption"

**Implementation Strategy**:
- Each result has unique GUID identifier
- TryAdd semantics prevent duplicate writes
- No shared mutable state during persistence
- Application layer assigns ID before persistence

**Verification**:
```csharp
[Fact]
public async Task ConcurrentPersist_100Ops_NoDataCorruption()
{
    var repo = new InMemoryEvaluationResultRepository();
    var results = new List<EvaluationResult>();
    
    for (int i = 0; i < 100; i++)
    {
        results.Add(CreateResult(Guid.NewGuid()));
    }
    
    var tasks = results.Select(r => repo.PersistAsync(r));
    var persisted = await Task.WhenAll(tasks);
    
    // Verify all results retrieved exactly as persisted
    foreach (var original in results)
    {
        var retrieved = await repo.GetByIdAsync(original.Id);
        Assert.Equal(original, retrieved);  // Value equality
    }
}
```

### Deterministic Replay ✅

**Specification (SC-003)**: "Replay of persisted evaluation produces identical outcomes"

**Enabled by**:
1. **Immutable evidence**: All metric values and rule states captured
2. **Deterministic serialization**: Same input → same byte output
3. **Timestamp preservation**: Original evaluation timestamp preserved
4. **Metric string preservation**: Exact decimal values in evidence

**Replay Flow**:
```csharp
// Step 1: Retrieve persisted result
var persisted = await repo.GetByIdAsync(resultId);

// Step 2: Extract metrics from evidence
var metrics = persisted.Evidence.SelectMany(e => e.Metrics).ToList();

// Step 3: Re-run evaluation with same metrics + rules
var replayResult = await evaluationService.EvaluateAsync(
    metrics,
    persisted.Rules,  // Same rules as original
    evaluatedAt: persisted.EvaluatedAt  // Preserve timestamp
);

// Step 4: Compare outcomes
Assert.Equal(persisted.Outcome, replayResult.Outcome);
Assert.Equal(persisted.Violations, replayResult.Violations);
```

---

## Consistency Boundary Definition

### Aggregate Root Pattern ✅

**Decision**: `EvaluationResult` is the aggregate root

**Entities within aggregate**:
- `Violation` (value object)
- `EvaluationEvidence` (value object)
- `MetricReference` (value object)

**Justification**:
- EvaluationResult is the coherent unit of persistence
- Violations cannot exist without a result
- Evidence cannot exist without a result
- Atomic persistence operates on the entire aggregate

**Query Operations**:
- GetByIdAsync returns entire aggregate (all violations + evidence)
- Filtering/projection happens at application layer, not persistence layer

---

## Repository Query Operations

### Query by ID ✅

**Implementation**:
```csharp
public Task<EvaluationResult?> GetByIdAsync(Guid id) 
    => Task.FromResult(_store.TryGetValue(id, out var result) ? result : null);
```

**Contract**: Returns null if not found (not error) per FR-012

### Query by Timestamp Range ✅

**Implementation**:
```csharp
public IAsyncEnumerable<EvaluationResult> QueryByTimestampRangeAsync(
    DateTime startUtc, DateTime endUtc)
{
    return _store.Values
        .Where(r => r.EvaluatedAt >= startUtc && r.EvaluatedAt <= endUtc)
        .OrderBy(r => r.EvaluatedAt)
        .ToAsyncEnumerable();
}
```

**Contract**: Returns results in chronological order per FR-008

### Query by Test ID ✅

**Design Decision**: TestId not on EvaluationResult directly

**Rationale**: 
- Specification doesn't mandate TestId as result property
- Test context belongs to application layer, not domain
- Repository focuses on core persistence contract

**Future Extension**: Application layer can create secondary index if needed

---

## Testing Strategy

### Unit Tests: Immutability ✅

```csharp
namespace PerformanceEngine.Evaluation.Domain.Tests
{
    public class ImmutabilityTests
    {
        [Fact]
        public void EvaluationResult_Properties_AreReadOnly() { ... }
        
        [Fact]
        public void Violations_CannotBeModifiedAfterConstruction() { ... }
        
        [Fact]
        public void Evidence_CannotBeModifiedAfterConstruction() { ... }
    }
}
```

### Unit Tests: Deterministic Serialization ✅

```csharp
public class DeterministicSerializationTests
{
    [Fact]
    public void Serialize_Same_Input_Produces_Same_Bytes()
    {
        var result = CreateEvaluationResult();
        var bytes1 = Serialize(result);
        var deserialized = Deserialize(bytes1);
        var bytes2 = Serialize(deserialized);
        
        Assert.Equal(bytes1, bytes2);  // Byte-identical
    }
}
```

### Integration Tests: Atomic Persistence ✅

```csharp
namespace PerformanceEngine.Evaluation.Infrastructure.Tests
{
    public class AtomicPersistenceTests
    {
        [Fact]
        public async Task PersistAsync_WithDuplicateId_Throws() { ... }
        
        [Fact]
        public async Task ConcurrentPersist_100Ops_AllSucceed() { ... }
        
        [Fact]
        public async Task PersistAsync_FailedOperation_LeavesStoreUnchanged() { ... }
    }
}
```

### Integration Tests: Query Operations ✅

```csharp
public class QueryTests
{
    [Fact]
    public async Task GetByIdAsync_ExistingResult_ReturnsExactCopy() { ... }
    
    [Fact]
    public async Task QueryByTimestampRange_ReturnsChronological() { ... }
    
    [Fact]
    public async Task Query_NoResults_ReturnsEmpty() { ... }
}
```

---

## Error Handling & Failure Modes

### Persistence Failure ✅

**Specification (FR-011)**: "Fail fast with clear error messages when persistence operations fail"

**Scenarios**:
1. Duplicate ID attempt → `InvalidOperationException("Result with ID already exists")`
2. Storage unavailable → `IOException("Storage is not accessible")`
3. Invalid data → `ArgumentException("Result violates constraints")`

**Application layer catches and logs**; never swallowed

### Query Failure ✅

**Specification (FR-012)**: "Return empty results (not errors) when queries match no persisted data"

**Implementation**:
```csharp
// No results found → return empty enumerable (not throw)
public IAsyncEnumerable<EvaluationResult> QueryByTimestampRange(DateTime s, DateTime e)
{
    var results = _store.Values.Where(/* ... */);
    return results.Any() ? results.ToAsyncEnumerable() : AsyncEnumerable.Empty<EvaluationResult>();
}
```

---

## Design Patterns Applied

### Aggregate Pattern ✅
EvaluationResult + contained Violations/Evidence = single consistency boundary

### Repository Pattern ✅
Port abstraction enables multiple storage implementations without domain coupling

### Value Object Pattern ✅
Violations, Evidence, MetricReference are immutable value objects (no identity)

### DTO Pattern ✅
DTOs separate serialization contract from domain model evolution

### Dependency Inversion ✅
Domain depends on IEvaluationResultRepository abstraction; infrastructure implements

---

## Risk Mitigation

| Risk | Probability | Impact | Mitigation |
|------|-------------|--------|-----------|
| Serialization non-determinism | Medium | High | Deterministic JSON config + hash verification tests |
| Concurrent write corruption | Low | Critical | In-memory TryAdd + SQL unique constraint |
| Immutability violations | Low | Medium | Record types + compile-time enforcement |
| Query performance degradation | Medium | Low | Out of scope (Phase 2 optimization); in-memory sufficient for Phase 1 |
| TestId missing from results | Low | Low | Document as application-layer responsibility; refactor if needed |

---

## Technology Dependencies

### Required (Already Present)
- .NET 8.0
- C# 12 features (records, init properties)
- System.Text.Json
- xUnit

### Optional (Future Phases)
- Entity Framework Core (SQL persistence adapter)
- Redis (caching layer for queries)
- Elasticsearch (advanced query indexing)

---

## Summary of Findings

✅ **All clarifications resolved**
- Language: C# 12 / .NET 8.0 (confirmed)
- Repository pattern: Existing codebase demonstrates (confirmed)
- Deterministic serialization: Achievable via System.Text.Json (confirmed)
- Atomic persistence: In-memory TryAdd, SQL transactions (confirmed)
- Concurrency: Lock-free semantics + GUID uniqueness (confirmed)
- Replay determinism: Immutability + evidence preservation (confirmed)

✅ **No blockers identified**
- All requirements map to implementable patterns
- Existing project provides proven architectural approach
- Test infrastructure ready (xUnit)
- Technology stack stable

✅ **Phase 0 research complete**
- Ready for Phase 1 (contracts/data-model specification)
- Ready for Phase 2 (implementation via /speckit.tasks)

---

**Research Status**: ✅ Complete | **Quality Gate**: ✅ Passed | **Next Phase**: Phase 1 (Design & Contracts)
