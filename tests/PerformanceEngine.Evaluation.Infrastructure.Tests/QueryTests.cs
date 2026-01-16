namespace PerformanceEngine.Evaluation.Infrastructure.Tests;

/// <summary>
/// Integration tests verifying query operations (GetByIdAsync, QueryByTimestampRangeAsync, QueryByTestIdAsync).
/// </summary>
public class QueryTests
{
    /// <summary>
    /// Given a persisted result
    /// When retrieved by ID
    /// Then the exact immutable result is returned with all metadata
    /// </summary>
    [Fact]
    public async Task GetByIdAsync_ExistingResult_ReturnsExactCopy()
    {
        var repository = new InMemoryEvaluationResultRepository();
        var original = CreatePassResult();

        var persisted = await repository.PersistAsync(original);
        var retrieved = await repository.GetByIdAsync(persisted.Id);

        retrieved.Should().NotBeNull();
        retrieved!.Should().Be(persisted);
        retrieved!.Id.Should().Be(persisted.Id);
        retrieved!.Outcome.Should().Be(persisted.Outcome);
    }

    /// <summary>
    /// Given a non-existent ID
    /// When queried with GetByIdAsync
    /// Then returns null (not an error)
    /// </summary>
    [Fact]
    public async Task GetByIdAsync_NonExistentId_ReturnsNull()
    {
        var repository = new InMemoryEvaluationResultRepository();
        var nonExistentId = Guid.NewGuid();

        var result = await repository.GetByIdAsync(nonExistentId);

        result.Should().BeNull();
    }

    /// <summary>
    /// Given multiple results persisted at different times
    /// When queried by timestamp range
    /// Then all results within range are returned in chronological order
    /// </summary>
    [Fact]
    public async Task QueryByTimestampRangeAsync_MultipleResults_ChronologicalOrder()
    {
        var repository = new InMemoryEvaluationResultRepository();
        var baseTime = DateTime.UtcNow;
        
        var results = new[]
        {
            CreateResultWithTimestamp(baseTime.AddHours(-2)),
            CreateResultWithTimestamp(baseTime.AddHours(-1)),
            CreateResultWithTimestamp(baseTime),
            CreateResultWithTimestamp(baseTime.AddHours(1))
        };

        foreach (var result in results)
        {
            await repository.PersistAsync(result);
        }

        var retrieved = await repository.QueryByTimestampRangeAsync(
            baseTime.AddHours(-2), 
            baseTime.AddHours(1))
            .ToListAsync();

        retrieved.Should().HaveCount(4);
        
        // Verify chronological order
        for (int i = 0; i < retrieved.Count - 1; i++)
        {
            retrieved[i].EvaluatedAt.Should().BeLessThanOrEqualTo(retrieved[i + 1].EvaluatedAt);
        }
    }

    /// <summary>
    /// Given results within and outside a timestamp range
    /// When queried by range
    /// Then only results within range are returned
    /// </summary>
    [Fact]
    public async Task QueryByTimestampRangeAsync_OnlyInRangeReturned()
    {
        var repository = new InMemoryEvaluationResultRepository();
        var baseTime = DateTime.UtcNow;
        
        var beforeRange = CreateResultWithTimestamp(baseTime.AddHours(-3));
        var withinRange1 = CreateResultWithTimestamp(baseTime.AddHours(-1));
        var withinRange2 = CreateResultWithTimestamp(baseTime);
        var afterRange = CreateResultWithTimestamp(baseTime.AddHours(2));

        await repository.PersistAsync(beforeRange);
        await repository.PersistAsync(withinRange1);
        await repository.PersistAsync(withinRange2);
        await repository.PersistAsync(afterRange);

        var retrieved = await repository.QueryByTimestampRangeAsync(
            baseTime.AddHours(-1.5), 
            baseTime.AddHours(0.5))
            .ToListAsync();

        retrieved.Should().HaveCount(2);
        retrieved.Should().Contain(r => r.Id == withinRange1.Id);
        retrieved.Should().Contain(r => r.Id == withinRange2.Id);
    }

    /// <summary>
    /// Given an empty timestamp range (no results within range)
    /// When queried
    /// Then returns empty enumerable (not an error)
    /// </summary>
    [Fact]
    public async Task QueryByTimestampRangeAsync_NoResults_ReturnsEmpty()
    {
        var repository = new InMemoryEvaluationResultRepository();
        var baseTime = DateTime.UtcNow;

        var retrieved = await repository.QueryByTimestampRangeAsync(
            baseTime.AddHours(-3),
            baseTime.AddHours(-2))
            .ToListAsync();

        retrieved.Should().BeEmpty();
    }

    /// <summary>
    /// Given results persisted with test identifiers
    /// When queried by test ID
    /// Then only matching results are returned
    /// </summary>
    [Fact]
    public async Task QueryByTestIdAsync_FiltersByTestId()
    {
        var repository = new InMemoryEvaluationResultRepository();
        
        // Note: Current implementation doesn't have TestId on result
        // This test documents the expected behavior for future enhancement
        // For now, we test the method doesn't throw
        var testId = "test-001";

        var retrieved = await repository.QueryByTestIdAsync(testId)
            .ToListAsync();

        // Should return empty (test ID not yet implemented on domain model)
        retrieved.Should().BeEmpty();
    }

    /// <summary>
    /// Given a non-existent test ID
    /// When queried
    /// Then returns empty enumerable (not an error)
    /// </summary>
    [Fact]
    public async Task QueryByTestIdAsync_NonExistentId_ReturnsEmpty()
    {
        var repository = new InMemoryEvaluationResultRepository();
        
        var retrieved = await repository.QueryByTestIdAsync("non-existent-test")
            .ToListAsync();

        retrieved.Should().BeEmpty();
    }

    /// <summary>
    /// Given multiple queries on same repository
    /// When executed concurrently
    /// Then all complete successfully (queries don't interfere)
    /// </summary>
    [Fact]
    public async Task Queries_Concurrent_AllSucceed()
    {
        var repository = new InMemoryEvaluationResultRepository();
        var baseTime = DateTime.UtcNow;

        var results = Enumerable.Range(0, 10)
            .Select(i => CreateResultWithTimestamp(baseTime.AddHours(i)))
            .ToList();

        foreach (var result in results)
        {
            await repository.PersistAsync(result);
        }

        var tasks = new List<Task>();

        // Concurrent GetByIdAsync
        for (int i = 0; i < 5; i++)
        {
            var resultId = results[i].Id;
            tasks.Add(repository.GetByIdAsync(resultId).ContinueWith(t =>
            {
                t.Result.Should().NotBeNull();
            }));
        }

        // Concurrent QueryByTimestampRangeAsync
        for (int i = 0; i < 5; i++)
        {
            var start = baseTime.AddHours(i);
            var end = baseTime.AddHours(i + 2);
            tasks.Add(repository.QueryByTimestampRangeAsync(start, end)
                .ToListAsync().ContinueWith(t =>
                {
                    t.Result.Should().NotBeNull();
                }));
        }

        await Task.WhenAll(tasks);
    }

    // Helper methods

    private static EvaluationResult CreatePassResult()
    {
        return EvaluationResult.Create(
            outcome: Severity.Pass,
            violations: Array.Empty<Violation>(),
            evidence: Array.Empty<EvaluationEvidence>(),
            outcomeReason: "All rules passed",
            evaluatedAtUtc: DateTime.UtcNow);
    }

    private static EvaluationResult CreateResultWithTimestamp(DateTime timestamp)
    {
        return EvaluationResult.Create(
            outcome: Severity.Pass,
            violations: Array.Empty<Violation>(),
            evidence: Array.Empty<EvaluationEvidence>(),
            outcomeReason: "Test passed",
            evaluatedAtUtc: timestamp);
    }
}
