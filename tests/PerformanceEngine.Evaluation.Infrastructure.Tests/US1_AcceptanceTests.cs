namespace PerformanceEngine.Evaluation.Infrastructure.Tests;

/// <summary>
/// Acceptance tests for User Story 1: Persist Evaluation Results After Test Run
/// 
/// These tests validate the core MVP functionality:
/// - Atomic persistence of immutable evaluation results
/// - Complete context preservation (metrics, violations, evidence)
/// - Concurrent persistence without race conditions
/// 
/// Priority: P1 - Foundational capability
/// </summary>
public class US1_AcceptanceTests
{
    /// <summary>
    /// Scenario: Pass outcome with no violations persists and retrieves identically
    /// 
    /// Given an evaluation has been completed with outcome PASS and no violations
    /// When the system persists the evaluation result
    /// Then the result is stored atomically with all metadata (timestamp, outcome)
    /// and can be retrieved identically
    /// 
    /// Acceptance Criteria:
    /// - Persist operation completes successfully
    /// - Result ID is assigned
    /// - Retrieved result equals original (byte-identical)
    /// - All metadata preserved (timestamp, outcome)
    /// </summary>
    [Fact]
    public async Task US1_Scenario1_PassResultPersistsIdentically()
    {
        var repository = new InMemoryEvaluationResultRepository();
        var evaluatedAt = new DateTime(2026, 01, 16, 14, 30, 45, DateTimeKind.Utc);

        var original = EvaluationResult.Create(
            outcome: Severity.Pass,
            violations: Array.Empty<Violation>(),
            evidence: Array.Empty<EvaluationEvidence>(),
            outcomeReason: "All performance rules satisfied",
            evaluatedAtUtc: evaluatedAt);

        // When: Persist the result
        var persisted = await repository.PersistAsync(original);

        // Then: Check persistence
        persisted.Id.Should().NotBe(Guid.Empty, "Result should have assigned ID");
        persisted.Outcome.Should().Be(Severity.Pass);
        persisted.OutcomeReason.Should().Be("All performance rules satisfied");
        persisted.EvaluatedAt.Should().Be(evaluatedAt);
        persisted.Violations.Should().BeEmpty();

        // And: Retrieve and verify identical
        var retrieved = await repository.GetByIdAsync(persisted.Id);
        retrieved.Should().NotBeNull();
        retrieved!.Should().Be(persisted);  // Value equality - all properties match
    }

    /// <summary>
    /// Scenario: Fail outcome with multiple violations persists atomically with all context
    /// 
    /// Given an evaluation has been completed with outcome FAIL and multiple violations
    /// When the system persists the evaluation result
    /// Then all violations and evidence are persisted immutably with the result
    /// and can be retrieved completely intact
    /// 
    /// Acceptance Criteria:
    /// - Result persisted with all violations
    /// - All evidence preserved (rule context, metrics, decisions)
    /// - No partial writes (all-or-nothing atomicity)
    /// - Retrieved result is byte-identical
    /// </summary>
    [Fact]
    public async Task US1_Scenario2_FailResultWithViolationsPersistsAtomically()
    {
        var repository = new InMemoryEvaluationResultRepository();
        var evaluatedAt = new DateTime(2026, 01, 16, 14, 30, 45, DateTimeKind.Utc);

        // Create violations
        var violation1 = Violation.Create(
            ruleName: "MaxResponseTimeRule",
            metricName: "ResponseTime",
            severity: Severity.Fail,
            actualValue: "1250.5",
            thresholdValue: "1000.0",
            message: "Response time exceeded acceptable threshold");

        var violation2 = Violation.Create(
            ruleName: "MaxErrorRateRule",
            metricName: "ErrorRate",
            severity: Severity.Fail,
            actualValue: "5.2",
            thresholdValue: "1.0",
            message: "Error rate exceeded acceptable threshold");

        // Create evidence for audit trail
        var evidence1 = EvaluationEvidence.Create(
            ruleId: "rule-max-rt",
            ruleName: "MaxResponseTimeRule",
            metrics: new[] { MetricReference.Create("ResponseTime", "1250.5") },
            actualValues: new Dictionary<string, string> { ["ResponseTime"] = "1250.5" },
            expectedConstraint: "ResponseTime <= 1000",
            constraintSatisfied: false,
            decisionOutcome: "Violation: Response time exceeds threshold by 250ms",
            recordedAtUtc: evaluatedAt);

        var evidence2 = EvaluationEvidence.Create(
            ruleId: "rule-max-err",
            ruleName: "MaxErrorRateRule",
            metrics: new[] { MetricReference.Create("ErrorRate", "5.2") },
            actualValues: new Dictionary<string, string> { ["ErrorRate"] = "5.2" },
            expectedConstraint: "ErrorRate <= 1.0",
            constraintSatisfied: false,
            decisionOutcome: "Violation: Error rate exceeds threshold by 4.2%",
            recordedAtUtc: evaluatedAt);

        var result = EvaluationResult.Create(
            outcome: Severity.Fail,
            violations: new[] { violation1, violation2 },
            evidence: new[] { evidence1, evidence2 },
            outcomeReason: "Test failed: Multiple performance rules violated",
            evaluatedAtUtc: evaluatedAt);

        // When: Persist the result
        var persisted = await repository.PersistAsync(result);

        // Then: Verify atomic persistence (no partial writes)
        persisted.Outcome.Should().Be(Severity.Fail);
        persisted.Violations.Count.Should().Be(2, "All violations must be persisted");
        persisted.Evidence.Count.Should().Be(2, "All evidence must be persisted");

        // Verify violation details preserved
        var violations = persisted.Violations.ToList();
        violations[0].RuleName.Should().Be("MaxResponseTimeRule");
        violations[0].ActualValue.Should().Be("1250.5");
        violations[1].RuleName.Should().Be("MaxErrorRateRule");
        violations[1].ActualValue.Should().Be("5.2");

        // And: Retrieve and verify complete integrity
        var retrieved = await repository.GetByIdAsync(persisted.Id);
        retrieved.Should().NotBeNull();
        retrieved!.Should().Be(persisted);  // Byte-identical (value equality)
        retrieved!.Violations.Count.Should().Be(2);
        retrieved!.Evidence.Count.Should().Be(2);
    }

    /// <summary>
    /// Scenario: Multiple evaluation results persisted concurrently without data corruption
    /// 
    /// Given multiple evaluation results need to be persisted concurrently
    /// When the system persists each result
    /// Then each persist operation is atomic and independent (no partial writes)
    /// and all results remain queryable without corruption
    /// 
    /// Acceptance Criteria:
    /// - All 100 concurrent operations complete successfully
    /// - Each result has unique ID (no collisions)
    /// - No race conditions or data corruption
    /// - All results immediately retrievable
    /// </summary>
    [Fact]
    public async Task US1_Scenario3_ConcurrentPersistenceWithoutRaceConditions()
    {
        var repository = new InMemoryEvaluationResultRepository();
        var tasks = new List<Task<EvaluationResult>>();

        // Create 100 concurrent persist tasks
        for (int i = 0; i < 100; i++)
        {
            var outcome = i % 2 == 0 ? Severity.Pass : Severity.Fail;
            var violations = outcome == Severity.Fail
                ? new[] { CreateViolation(i) }
                : Array.Empty<Violation>();

            var result = EvaluationResult.Create(
                outcome: outcome,
                violations: violations,
                evidence: violations.Length > 0 ? new[] { CreateEvidence(i) } : Array.Empty<EvaluationEvidence>(),
                outcomeReason: outcome == Severity.Pass ? "Test passed" : $"Test failed with violation {i}",
                evaluatedAtUtc: DateTime.UtcNow);

            tasks.Add(repository.PersistAsync(result));
        }

        // When: All persist concurrently
        var persisted = await Task.WhenAll(tasks);

        // Then: Verify all succeeded with unique IDs
        persisted.Should().HaveCount(100);
        var uniqueIds = persisted.Select(r => r.Id).Distinct();
        uniqueIds.Count().Should().Be(100, "All IDs must be unique (no collisions)");

        // And: Verify all are immediately queryable (no async delays causing data loss)
        var retrieveTasks = persisted.Select(r => repository.GetByIdAsync(r.Id)).ToList();
        var retrieved = await Task.WhenAll(retrieveTasks);
        retrieved.Should().AllSatisfy(r => r.Should().NotBeNull());

        // And: Verify data integrity (no corruption from concurrent writes)
        for (int i = 0; i < persisted.Count; i++)
        {
            var original = persisted[i];
            var fromRepo = retrieved[i];
            
            fromRepo!.Id.Should().Be(original.Id);
            fromRepo!.Outcome.Should().Be(original.Outcome);
            fromRepo!.Violations.Count.Should().Be(original.Violations.Count,
                "Violations count must match (no partial writes)");
        }
    }

    /// <summary>
    /// Scenario: Error handling - duplicate persist throws clear error
    /// 
    /// Given a result has been persisted
    /// When attempting to persist another result with the same ID
    /// Then the system throws InvalidOperationException with clear message
    /// 
    /// Acceptance Criteria:
    /// - Duplicate persist throws InvalidOperationException
    /// - Error message explains append-only semantics
    /// - Original result remains unchanged
    /// </summary>
    [Fact]
    public async Task US1_Scenario4_DuplicatePersistThrowsClearError()
    {
        var repository = new InMemoryEvaluationResultRepository();
        var result = EvaluationResult.Create(
            outcome: Severity.Pass,
            violations: Array.Empty<Violation>(),
            evidence: Array.Empty<EvaluationEvidence>(),
            outcomeReason: "Test passed",
            evaluatedAtUtc: DateTime.UtcNow);

        // When: Persist once successfully
        var persisted = await repository.PersistAsync(result);

        // And: Attempt to persist again with same ID
        var duplicate = new EvaluationResult(
            Id: persisted.Id,  // Reuse same ID
            Outcome: Severity.Fail,
            Violations: [],
            Evidence: [],
            OutcomeReason: "Modified",
            EvaluatedAt: DateTime.UtcNow);

        var action = async () => await repository.PersistAsync(duplicate);

        // Then: Should throw with clear message
        var ex = await action.Should().ThrowAsync<InvalidOperationException>();
        ex.And.Message.Should().Contain("already exists", "Should explain duplicate prevention");
        ex.And.Message.Should().Contain("Append-only", "Should reference append-only semantics");

        // And: Original result should remain unchanged
        var retrieved = await repository.GetByIdAsync(persisted.Id);
        retrieved!.Outcome.Should().Be(Severity.Pass, "Original outcome preserved");
    }

    // Helper methods

    private static Violation CreateViolation(int index)
    {
        return Violation.Create(
            ruleName: $"Rule{index}",
            metricName: $"Metric{index}",
            severity: Severity.Fail,
            actualValue: (1000 + index).ToString(),
            thresholdValue: "1000.0",
            message: $"Violation {index}");
    }

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
