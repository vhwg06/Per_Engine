# Quickstart Guide: Persist Results for Audit & Replay

**Date**: 2026-01-16 | **Feature**: 001-persist-results | **Target Audience**: Developers implementing or using the persistence layer

---

## Overview

This guide explains how to use the evaluation result persistence layer to store evaluation decisions, retrieve historical results, and enable deterministic replay scenarios. The persistence layer provides a repository abstraction that remains independent of storage implementation (in-memory, SQL, cloud storage, etc.).

---

## Core Concepts

### Immutable Results

Evaluation results are immutable records containing all context needed for audit trails and replay:

```csharp
// Create an immutable evaluation result
var result = EvaluationResult.Create(
    outcome: Severity.Fail,
    violations: new[] { violation1, violation2 },
    evidence: new[] { evidence1, evidence2 },
    outcomeReason: "Test failed: Response time exceeded threshold",
    evaluatedAtUtc: DateTime.UtcNow);

// Result is immutable; the following are NOT possible:
// result.Outcome = Severity.Pass;  ❌ Compile error
// result.Violations.Add(new Violation(...));  ❌ Compile error
```

### Append-Only Persistence

Once persisted, results cannot be modified or deleted. Only new results can be created:

```csharp
// Persist the immutable result
var persisted = await repository.PersistAsync(result);

// Later, retrieve the exact same result
var retrieved = await repository.GetByIdAsync(persisted.Id);

// retrieved equals persisted (byte-identical after serialization)
Assert.Equal(persisted, retrieved);

// ❌ These operations are NOT possible:
// await repository.UpdateAsync(result);  ❌ Method doesn't exist
// await repository.DeleteAsync(result.Id);  ❌ Method doesn't exist
```

### Atomic Writes

Persistence operations are atomic—either the entire result is stored or nothing is stored:

```csharp
// All-or-nothing semantics
var result = EvaluationResult.Create(...);
var persisted = await repository.PersistAsync(result);

// If PersistAsync completes successfully:
// - All violations are persisted
// - All evidence is persisted
// - All metadata is persisted
// - Result is immediately queryable

// If PersistAsync throws an exception:
// - Nothing was persisted
// - No partial writes
// - Storage remains unchanged
```

---

## Creating Evaluation Results

### Basic Creation

```csharp
using PerformanceEngine.Evaluation.Domain;

// Create violations
var violations = new[]
{
    Violation.Create(
        ruleName: "MaxResponseTimeRule",
        metricName: "ResponseTime",
        severity: Severity.Fail,
        actualValue: "1250.5",        // String to preserve precision
        thresholdValue: "1000.0",     // String to preserve precision
        message: "Response time exceeded threshold")
};

// Create evidence for audit trail
var evidence = new[]
{
    EvaluationEvidence.Create(
        ruleId: "rule-max-rt",
        ruleName: "MaxResponseTimeRule",
        metrics: new[] 
        { 
            MetricReference.Create("ResponseTime", "1250.5") 
        },
        actualValues: new Dictionary<string, string>
        {
            ["ResponseTime"] = "1250.5"
        },
        expectedConstraint: "ResponseTime <= 1000",
        constraintSatisfied: false,
        decisionOutcome: "Violation: Response time exceeds threshold",
        recordedAtUtc: DateTime.UtcNow)
};

// Create the immutable evaluation result
var result = EvaluationResult.Create(
    outcome: Severity.Fail,
    violations: violations,
    evidence: evidence,
    outcomeReason: "Test failed: Response time exceeded acceptable threshold by 250ms",
    evaluatedAtUtc: DateTime.UtcNow);  // Must be UTC
```

### Validation During Creation

Validation occurs at construction time, enforcing domain invariants:

```csharp
// ❌ This throws InvalidOperationException:
// "Outcome cannot be Pass when violations are present"
var invalid = EvaluationResult.Create(
    outcome: Severity.Pass,           // Contradicts violations
    violations: new[] { violation },   // Has violations
    evidence: new[] { evidence },
    outcomeReason: "...",
    evaluatedAtUtc: DateTime.UtcNow);

// ❌ This throws ArgumentException:
// "Evaluated timestamp must be UTC"
var invalidTime = EvaluationResult.Create(
    outcome: Severity.Fail,
    violations: new[] { violation },
    evidence: new[] { evidence },
    outcomeReason: "...",
    evaluatedAtUtc: DateTime.Now);    // Wrong: local time, not UTC
```

---

## Persisting Results

### Register Repository in Dependency Injection

```csharp
// In Startup.cs or Program.cs
var services = new ServiceCollection();

// Register the in-memory repository (for development/testing)
services.AddScoped<IEvaluationResultRepository, InMemoryEvaluationResultRepository>();

// Or register a SQL repository (when available)
// services.AddScoped<IEvaluationResultRepository, SqlEvaluationResultRepository>();

var provider = services.BuildServiceProvider();
```

### Persist an Evaluation Result

```csharp
using PerformanceEngine.Evaluation.Ports;

// Inject the repository
var repository = serviceProvider.GetRequiredService<IEvaluationResultRepository>();

// Create and persist a result
var result = EvaluationResult.Create(...);
var persisted = await repository.PersistAsync(result);

// persisted.Id is assigned by the repository for unique identification
Console.WriteLine($"Persisted result with ID: {persisted.Id}");
```

### Handle Persistence Errors

```csharp
var repository = serviceProvider.GetRequiredService<IEvaluationResultRepository>();
var result = EvaluationResult.Create(...);

try
{
    await repository.PersistAsync(result);
    Console.WriteLine("Result persisted successfully");
}
catch (InvalidOperationException ex) when (ex.Message.Contains("already exists"))
{
    // Duplicate ID (should not occur with new results, but possible in edge cases)
    Console.WriteLine($"Result with ID {result.Id} already persisted");
}
catch (IOException ex)
{
    // Storage unavailable
    Console.WriteLine($"Storage error: {ex.Message}");
}
catch (ArgumentNullException)
{
    // Invalid input
    Console.WriteLine("Result cannot be null");
}
```

---

## Retrieving Results

### Query by Unique Identifier

```csharp
var repository = serviceProvider.GetRequiredService<IEvaluationResultRepository>();

// Retrieve a specific result
var resultId = new Guid("550e8400-e29b-41d4-a716-446655440000");
var result = await repository.GetByIdAsync(resultId);

if (result != null)
{
    Console.WriteLine($"Found result: {result.Outcome}");
    Console.WriteLine($"Violations: {result.Violations.Count}");
}
else
{
    // Not found (not an error—graceful handling)
    Console.WriteLine("Result not found");
}
```

### Query by Timestamp Range

```csharp
var repository = serviceProvider.GetRequiredService<IEvaluationResultRepository>();

// Query results from the last 24 hours
var startUtc = DateTime.UtcNow.AddHours(-24);
var endUtc = DateTime.UtcNow;

var results = await repository.QueryByTimestampRangeAsync(startUtc, endUtc)
    .ToListAsync();

Console.WriteLine($"Found {results.Count} results in the last 24 hours");

// Results are guaranteed to be in chronological order (earliest first)
foreach (var result in results)
{
    Console.WriteLine($"{result.EvaluatedAt}: {result.Outcome}");
}
```

### Query by Test Identifier

```csharp
var repository = serviceProvider.GetRequiredService<IEvaluationResultRepository>();

// Query all results for a specific test
var testId = "api-performance-test-001";
var results = await repository.QueryByTestIdAsync(testId)
    .ToListAsync();

Console.WriteLine($"Found {results.Count} results for test '{testId}'");
```

### Handle Query Errors

```csharp
try
{
    var results = await repository.QueryByTimestampRangeAsync(startUtc, endUtc)
        .ToListAsync();
}
catch (ArgumentException ex)
{
    // Invalid arguments (e.g., endUtc before startUtc)
    Console.WriteLine($"Invalid query parameters: {ex.Message}");
}
catch (IOException ex)
{
    // Storage unavailable
    Console.WriteLine($"Storage error: {ex.Message}");
}

// Note: Empty result sets do NOT throw errors
var emptyResults = await repository.QueryByTimestampRangeAsync(
        DateTime.UtcNow.AddDays(-1), 
        DateTime.UtcNow.AddDays(-2))  // Empty range
    .ToListAsync();
// emptyResults.Count == 0 (not an error)
```

---

## Enabling Deterministic Replay

### Scenario: Replay an Evaluation

```csharp
var repository = serviceProvider.GetRequiredService<IEvaluationResultRepository>();
var evaluationService = serviceProvider.GetRequiredService<IEvaluationService>();

// Step 1: Retrieve persisted result
var originalResult = await repository.GetByIdAsync(resultId);

// Step 2: Extract metrics and rules from evidence
var metrics = originalResult.Evidence
    .SelectMany(e => e.Metrics)
    .Distinct()
    .ToList();

var rules = originalResult.Evidence
    .Select(e => e.ExpectedConstraint)
    .ToList();

// Step 3: Re-evaluate with same inputs
var replayResult = await evaluationService.EvaluateAsync(
    metrics: metrics,
    rules: rules,
    evaluatedAtUtc: originalResult.EvaluatedAt);  // Use original timestamp

// Step 4: Compare outcomes (must be byte-identical)
if (replayResult.Outcome == originalResult.Outcome &&
    replayResult.Violations.Count == originalResult.Violations.Count)
{
    Console.WriteLine("✓ Replay produced identical outcome");
}
else
{
    Console.WriteLine("✗ Replay produced different outcome (regression detected)");
    // Investigate why evaluation logic changed
}
```

### Ensure Deterministic Serialization

```csharp
// Verify byte-identical serialization
var serialized1 = JsonSerializer.Serialize(originalResult, serializationOptions);
var deserialized = JsonSerializer.Deserialize<EvaluationResult>(serialized1, serializationOptions);
var serialized2 = JsonSerializer.Serialize(deserialized, serializationOptions);

// Serialization is deterministic
Assert.Equal(serialized1, serialized2);

// Optional: Hash verification
var hash1 = ComputeHash(serialized1);
var hash2 = ComputeHash(serialized2);
Assert.Equal(hash1, hash2);
```

---

## Testing Patterns

### Unit Test: Immutability

```csharp
[Fact]
public void EvaluationResult_IsImmutable()
{
    var result = EvaluationResult.Create(...);
    
    // These must NOT compile:
    // result.Outcome = Severity.Pass;  ❌
    // result.Violations.Add(new Violation(...));  ❌
    
    // Verify all properties are read-only
    Assert.NotNull(result.Id);
    Assert.NotNull(result.Outcome);
    Assert.NotNull(result.Violations);
}
```

### Integration Test: Atomic Persistence

```csharp
[Fact]
public async Task PersistAsync_WithValidResult_StoresAtomically()
{
    var repository = new InMemoryEvaluationResultRepository();
    var result = EvaluationResult.Create(...);
    
    var persisted = await repository.PersistAsync(result);
    
    // Immediately queryable
    var retrieved = await repository.GetByIdAsync(persisted.Id);
    Assert.NotNull(retrieved);
    Assert.Equal(persisted.Id, retrieved.Id);
}
```

### Integration Test: Concurrent Persistence

```csharp
[Fact]
public async Task ConcurrentPersist_100Ops_AllSucceedWithoutCorruption()
{
    var repository = new InMemoryEvaluationResultRepository();
    var tasks = new List<Task<EvaluationResult>>();
    
    for (int i = 0; i < 100; i++)
    {
        var result = EvaluationResult.Create(...);
        tasks.Add(repository.PersistAsync(result));
    }
    
    var results = await Task.WhenAll(tasks);
    
    // All succeeded
    Assert.Equal(100, results.Count);
    
    // All unique IDs
    var uniqueIds = results.Select(r => r.Id).Distinct().Count();
    Assert.Equal(100, uniqueIds);
    
    // All retrievable
    foreach (var result in results)
    {
        var retrieved = await repository.GetByIdAsync(result.Id);
        Assert.Equal(result, retrieved);
    }
}
```

### Integration Test: Deterministic Replay

```csharp
[Fact]
public async Task ReplayWithSameInputs_ProducesByteIdenticalResults()
{
    var repository = new InMemoryEvaluationResultRepository();
    var original = EvaluationResult.Create(...);
    
    // Persist
    await repository.PersistAsync(original);
    
    // Retrieve
    var retrieved = await repository.GetByIdAsync(original.Id);
    
    // Serialize both
    var original_bytes = Serialize(original);
    var retrieved_bytes = Serialize(retrieved);
    
    // Must be byte-identical
    Assert.Equal(original_bytes, retrieved_bytes);
}
```

---

## Performance Considerations

### In-Memory Repository (Development/Testing)

- **Latency**: < 1ms (in-process)
- **Concurrency**: Supports 100+ concurrent operations without significant degradation
- **Scalability**: Suitable for testing; not for production with large result sets
- **Persistence**: Data lost on process restart (not suitable for audit trails in production)

### SQL Repository (Production)

- **Latency**: 10-50ms (network + database)
- **Concurrency**: Depends on database configuration; typically supports 1000+ concurrent operations
- **Scalability**: Suitable for millions of results
- **Persistence**: Durable storage with backup/recovery capabilities

---

## Common Patterns

### Create and Persist in One Step

```csharp
// Common flow: create result, persist immediately
var result = EvaluationResult.Create(
    outcome: Severity.Pass,
    violations: Array.Empty<Violation>(),
    evidence: evidenceList,
    outcomeReason: "Test passed",
    evaluatedAtUtc: DateTime.UtcNow);

var persisted = await repository.PersistAsync(result);
Console.WriteLine($"Persisted with ID: {persisted.Id}");
```

### Batch Query and Analyze

```csharp
// Query all results from a test, analyze trends
var testId = "performance-test-001";
var results = await repository.QueryByTestIdAsync(testId)
    .OrderBy(r => r.EvaluatedAt)
    .ToListAsync();

var passCount = results.Count(r => r.Outcome == Severity.Pass);
var failCount = results.Count(r => r.Outcome == Severity.Fail);
var passRate = (double)passCount / results.Count * 100;

Console.WriteLine($"Pass rate: {passRate}%");
```

### Replay Failed Tests for Debugging

```csharp
// Find a failed test and replay to verify fix
var failedResult = await repository.GetByIdAsync(failedResultId);

// Verify evidence completeness
if (!failedResult.Evidence.Any())
{
    throw new InvalidOperationException("Incomplete evidence; cannot replay");
}

// Re-evaluate with potentially updated rules
var newResult = await evaluationService.EvaluateAsync(
    metrics: failedResult.Evidence.SelectMany(e => e.Metrics).ToList(),
    rules: newRuleSet,  // Updated rules
    evaluatedAtUtc: failedResult.EvaluatedAt);

// Compare: newResult.Outcome vs failedResult.Outcome
```

---

## Troubleshooting

### Issue: "Result with ID already exists"

**Cause**: Attempting to persist a result with a GUID that's already in the repository

**Solution**:
```csharp
// Always use EvaluationResult.Create() to generate new IDs
var result = EvaluationResult.Create(...);  // ✓ New ID auto-generated
// vs.
var invalid = new EvaluationResult(
    Id: existingId,  // ❌ Don't reuse IDs
    ...);
```

### Issue: "Evaluated timestamp must be UTC"

**Cause**: Passing local time instead of UTC

**Solution**:
```csharp
// ❌ Wrong
var result = EvaluationResult.Create(
    ...,
    evaluatedAtUtc: DateTime.Now);

// ✓ Correct
var result = EvaluationResult.Create(
    ...,
    evaluatedAtUtc: DateTime.UtcNow);
```

### Issue: "Query returned no results"

**This is NOT an error**. Empty results are handled gracefully:

```csharp
var results = await repository.QueryByTimestampRangeAsync(start, end)
    .ToListAsync();

if (!results.Any())
{
    Console.WriteLine("No results in range (expected if range is old)");
}
```

---

## Next Steps

1. **Implement Domain Entities**: Create `EvaluationResult`, `Violation`, `EvaluationEvidence`, `MetricReference` classes
2. **Create Repository Port**: Define `IEvaluationResultRepository` in domain layer
3. **Implement In-Memory Repository**: Basic adapter for testing
4. **Add Unit Tests**: Immutability, validation, serialization
5. **Add Integration Tests**: Atomic persistence, concurrent operations, deterministic replay
6. **Integrate with Application Layer**: Use cases that orchestrate evaluation and persistence
7. **Implement SQL Repository**: Production-grade adapter (future phase)

---

**Status**: Ready to proceed with Phase 2 implementation via `/speckit.tasks`
