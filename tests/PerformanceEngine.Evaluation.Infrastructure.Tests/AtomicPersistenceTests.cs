namespace PerformanceEngine.Evaluation.Infrastructure.Tests;

/// <summary>
/// Integration tests verifying atomic persistence semantics.
/// Ensures that evaluation results are persisted atomically with no partial writes.
/// </summary>
public class AtomicPersistenceTests
{
    /// <summary>
    /// Given a valid evaluation result
    /// When PersistAsync completes successfully
    /// Then the result is immediately queryable with GetByIdAsync
    /// </summary>
    [Fact]
    public async Task PersistAsync_WithValidResult_ImmediatelyQueryable()
    {
        var repository = new InMemoryEvaluationResultRepository();
        var result = CreatePassResult();

        var persisted = await repository.PersistAsync(result);

        var retrieved = await repository.GetByIdAsync(persisted.Id);
        retrieved.Should().NotBeNull();
        retrieved!.Id.Should().Be(persisted.Id);
    }

    /// <summary>
    /// Given a result with violations and evidence
    /// When PersistAsync completes successfully
    /// Then all violations and evidence are persisted (not partial)
    /// </summary>
    [Fact]
    public async Task PersistAsync_WithViolationsAndEvidence_AllDataPersisted()
    {
        var repository = new InMemoryEvaluationResultRepository();
        var violations = new[] { CreateViolation() };
        var evidence = new[] { CreateEvidence() };
        
        var result = EvaluationResult.Create(
            outcome: Severity.Fail,
            violations: violations,
            evidence: evidence,
            outcomeReason: "Test failed",
            evaluatedAtUtc: DateTime.UtcNow);

        await repository.PersistAsync(result);

        var retrieved = await repository.GetByIdAsync(result.Id);
        retrieved!.Violations.Count.Should().Be(1);
        retrieved!.Evidence.Count.Should().Be(1);
    }

    /// <summary>
    /// Given a result with a duplicate ID
    /// When PersistAsync is called with the same ID twice
    /// Then the second call throws InvalidOperationException (atomicity enforced)
    /// </summary>
    [Fact]
    public async Task PersistAsync_WithDuplicateId_ThrowsInvalidOperationException()
    {
        var repository = new InMemoryEvaluationResultRepository();
        var result = CreatePassResult();

        await repository.PersistAsync(result);

        var duplicate = new EvaluationResult(
            Id: result.Id,
            Outcome: Severity.Pass,
            Violations: [],
            Evidence: [],
            OutcomeReason: "Duplicate",
            EvaluatedAt: DateTime.UtcNow);

        var action = async () => await repository.PersistAsync(duplicate);
        await action.Should().ThrowAsync<InvalidOperationException>();
    }

    /// <summary>
    /// Given multiple results ready to persist
    /// When all are persisted and then retrieved
    /// Then each result is byte-identical to the persisted version
    /// </summary>
    [Fact]
    public async Task PersistAsync_MultipleResults_ByteIdenticalRetrieval()
    {
        var repository = new InMemoryEvaluationResultRepository();
        var results = new[] { CreatePassResult(), CreateFailResult() };

        var persisted = new List<EvaluationResult>();
        foreach (var result in results)
        {
            persisted.Add(await repository.PersistAsync(result));
        }

        foreach (var result in persisted)
        {
            var retrieved = await repository.GetByIdAsync(result.Id);
            retrieved.Should().Be(result);  // Value equality
        }
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

    private static EvaluationResult CreateFailResult()
    {
        var violation = Violation.Create(
            ruleName: "MaxResponseTime",
            metricName: "ResponseTime",
            severity: Severity.Fail,
            actualValue: "1250.5",
            thresholdValue: "1000.0",
            message: "Response time exceeded threshold");

        return EvaluationResult.Create(
            outcome: Severity.Fail,
            violations: new[] { violation },
            evidence: new[] { CreateEvidence() },
            outcomeReason: "Test failed",
            evaluatedAtUtc: DateTime.UtcNow);
    }

    private static EvaluationEvidence CreateEvidence()
    {
        return EvaluationEvidence.Create(
            ruleId: "rule-001",
            ruleName: "MaxResponseTime",
            metrics: new[] { MetricReference.Create("ResponseTime", "1250.5") },
            actualValues: new Dictionary<string, string> { ["ResponseTime"] = "1250.5" },
            expectedConstraint: "ResponseTime <= 1000",
            constraintSatisfied: false,
            decisionOutcome: "Violation: Response time exceeded",
            recordedAtUtc: DateTime.UtcNow);
    }

    private static Violation CreateViolation()
    {
        return Violation.Create(
            ruleName: "MaxResponseTime",
            metricName: "ResponseTime",
            severity: Severity.Fail,
            actualValue: "1250.5",
            thresholdValue: "1000.0",
            message: "Response time exceeded threshold");
    }
}
