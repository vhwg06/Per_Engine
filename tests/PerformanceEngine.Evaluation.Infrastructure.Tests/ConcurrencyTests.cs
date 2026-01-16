namespace PerformanceEngine.Evaluation.Infrastructure.Tests;

/// <summary>
/// Integration tests verifying concurrent persistence without race conditions.
/// Ensures 100+ concurrent operations complete successfully without data corruption.
/// </summary>
public class ConcurrencyTests
{
    /// <summary>
    /// Given 100 unique evaluation results
    /// When all are persisted concurrently
    /// Then all succeed with unique IDs and no data corruption
    /// </summary>
    [Fact]
    public async Task ConcurrentPersist_100Ops_AllSucceed()
    {
        var repository = new InMemoryEvaluationResultRepository();
        var tasks = new List<Task<EvaluationResult>>();

        for (int i = 0; i < 100; i++)
        {
            var result = EvaluationResult.Create(
                outcome: Severity.Pass,
                violations: Array.Empty<Violation>(),
                evidence: Array.Empty<EvaluationEvidence>(),
                outcomeReason: $"Test {i} passed",
                evaluatedAtUtc: DateTime.UtcNow);

            tasks.Add(repository.PersistAsync(result));
        }

        var persisted = await Task.WhenAll(tasks);

        persisted.Should().HaveCount(100);
        persisted.Should().AllSatisfy(r => r.Should().NotBeNull());
    }

    /// <summary>
    /// Given 100 concurrent persist operations
    /// When all complete successfully
    /// Then all IDs are unique (no collisions)
    /// </summary>
    [Fact]
    public async Task ConcurrentPersist_100Ops_AllIdsUnique()
    {
        var repository = new InMemoryEvaluationResultRepository();
        var tasks = new List<Task<EvaluationResult>>();

        for (int i = 0; i < 100; i++)
        {
            var result = EvaluationResult.Create(
                outcome: Severity.Pass,
                violations: Array.Empty<Violation>(),
                evidence: Array.Empty<EvaluationEvidence>(),
                outcomeReason: $"Test {i}",
                evaluatedAtUtc: DateTime.UtcNow);

            tasks.Add(repository.PersistAsync(result));
        }

        var persisted = await Task.WhenAll(tasks);
        var uniqueIds = persisted.Select(r => r.Id).Distinct();

        uniqueIds.Count().Should().Be(100, "All IDs should be unique");
    }

    /// <summary>
    /// Given 100 concurrent persist operations
    /// When all complete successfully
    /// Then each result is immediately retrievable (no async delays)
    /// </summary>
    [Fact]
    public async Task ConcurrentPersist_100Ops_AllImmediatelyRetrievable()
    {
        var repository = new InMemoryEvaluationResultRepository();
        var persistTasks = new List<Task<EvaluationResult>>();

        for (int i = 0; i < 100; i++)
        {
            var result = EvaluationResult.Create(
                outcome: Severity.Pass,
                violations: Array.Empty<Violation>(),
                evidence: Array.Empty<EvaluationEvidence>(),
                outcomeReason: $"Test {i}",
                evaluatedAtUtc: DateTime.UtcNow);

            persistTasks.Add(repository.PersistAsync(result));
        }

        var persisted = await Task.WhenAll(persistTasks);

        // Now retrieve all concurrently
        var retrieveTasks = persisted.Select(r => repository.GetByIdAsync(r.Id)).ToList();
        var retrieved = await Task.WhenAll(retrieveTasks);

        retrieved.Should().AllSatisfy(r => r.Should().NotBeNull());
    }

    /// <summary>
    /// Given 100 results with various violations and evidence
    /// When persisted concurrently
    /// Then no data corruption occurs (all content preserved)
    /// </summary>
    [Fact]
    public async Task ConcurrentPersist_WithComplexData_NoCorruption()
    {
        var repository = new InMemoryEvaluationResultRepository();
        var tasks = new List<Task<EvaluationResult>>();

        for (int i = 0; i < 50; i++)
        {
            var passResult = EvaluationResult.Create(
                outcome: Severity.Pass,
                violations: Array.Empty<Violation>(),
                evidence: Array.Empty<EvaluationEvidence>(),
                outcomeReason: $"Pass test {i}",
                evaluatedAtUtc: DateTime.UtcNow);

            tasks.Add(repository.PersistAsync(passResult));

            var violation = Violation.Create(
                ruleName: "Rule" + i,
                metricName: "Metric" + i,
                severity: Severity.Fail,
                actualValue: (1000 + i).ToString(),
                thresholdValue: "1000.0",
                message: $"Violation {i}");

            var failResult = EvaluationResult.Create(
                outcome: Severity.Fail,
                violations: new[] { violation },
                evidence: new[] { CreateEvidence(i) },
                outcomeReason: $"Fail test {i}",
                evaluatedAtUtc: DateTime.UtcNow);

            tasks.Add(repository.PersistAsync(failResult));
        }

        var persisted = await Task.WhenAll(tasks);
        persisted.Should().HaveCount(100);

        // Verify integrity
        foreach (var result in persisted)
        {
            var retrieved = await repository.GetByIdAsync(result.Id);
            retrieved.Should().Be(result);
        }
    }

    /// <summary>
    /// Given concurrent persist operations
    /// When some results have identical timestamps
    /// Then no conflicts or data loss (timestamps alone don't create uniqueness)
    /// </summary>
    [Fact]
    public async Task ConcurrentPersist_IdenticalTimestamps_NoConflict()
    {
        var repository = new InMemoryEvaluationResultRepository();
        var sharedTimestamp = DateTime.UtcNow;
        var tasks = new List<Task<EvaluationResult>>();

        for (int i = 0; i < 20; i++)
        {
            var result = EvaluationResult.Create(
                outcome: Severity.Pass,
                violations: Array.Empty<Violation>(),
                evidence: Array.Empty<EvaluationEvidence>(),
                outcomeReason: $"Test {i}",
                evaluatedAtUtc: sharedTimestamp);

            tasks.Add(repository.PersistAsync(result));
        }

        var persisted = await Task.WhenAll(tasks);
        persisted.Should().HaveCount(20);

        // Verify all timestamps are identical
        persisted.Should().AllSatisfy(r => r.EvaluatedAt.Should().Be(sharedTimestamp));

        // Verify all are unique (by ID, not timestamp)
        var uniqueIds = persisted.Select(r => r.Id).Distinct();
        uniqueIds.Count().Should().Be(20);
    }

    // Helper methods

    private static EvaluationEvidence CreateEvidence(int index)
    {
        return EvaluationEvidence.Create(
            ruleId: $"rule-{index}",
            ruleName: $"Rule{index}",
            metrics: new[] { MetricReference.Create($"Metric{index}", (1000 + index).ToString()) },
            actualValues: new Dictionary<string, string> { [$"Metric{index}"] = (1000 + index).ToString() },
            expectedConstraint: $"Metric{index} <= 1000",
            constraintSatisfied: false,
            decisionOutcome: $"Violation {index}",
            recordedAtUtc: DateTime.UtcNow);
    }
}
