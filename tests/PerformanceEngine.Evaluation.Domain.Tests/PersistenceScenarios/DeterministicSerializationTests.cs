namespace PerformanceEngine.Evaluation.Domain.Tests.PersistenceScenarios;

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using PerformanceEngine.Evaluation.Domain;
using Xunit;

/// <summary>
/// Tests verifying deterministic serialization of evaluation results.
/// Same input must produce byte-identical output (critical for audit/replay).
/// </summary>
public class DeterministicSerializationTests
{
    private readonly JsonSerializerOptions _serializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = false,
        WriteIndented = false,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    [Fact]
    public void EvaluationResult_SerializeDeserializeSerialize_ProducesByteIdenticalJson()
    {
        // Arrange
        var original = EvaluationResult.Create(
            outcome: Severity.Fail,
            violations: new[]
            {
                Violation.Create(
                    ruleName: "MaxResponseTime",
                    metricName: "ResponseTime",
                    severity: Severity.Fail,
                    actualValue: "1250.5",
                    thresholdValue: "1000.0",
                    message: "Response time exceeded threshold")
            },
            evidence: new[]
            {
                EvaluationEvidence.Create(
                    ruleId: "rule-001",
                    ruleName: "MaxResponseTime",
                    metrics: new[] { MetricReference.Create("ResponseTime", "1250.5") },
                    actualValues: new Dictionary<string, string> { ["ResponseTime"] = "1250.5" },
                    expectedConstraint: "ResponseTime <= 1000",
                    constraintSatisfied: false,
                    decisionOutcome: "Constraint violated",
                    recordedAtUtc: new DateTime(2026, 1, 16, 14, 30, 45, DateTimeKind.Utc))
            },
            outcomeReason: "Test failed due to response time violation",
            evaluatedAtUtc: new DateTime(2026, 1, 16, 14, 30, 45, DateTimeKind.Utc));

        // Act
        var serialized1 = Serialize(original);
        var deserialized = Deserialize(serialized1);
        var serialized2 = Serialize(deserialized);

        // Assert: Byte-identical serialization
        Assert.Equal(serialized1, serialized2);
    }

    [Fact]
    public void EvaluationResult_MultipleSerializations_ProduceIdenticalHashes()
    {
        // Arrange
        var original = EvaluationResult.Create(
            outcome: Severity.Pass,
            violations: Array.Empty<Violation>(),
            evidence: Array.Empty<EvaluationEvidence>(),
            outcomeReason: "All rules passed",
            evaluatedAtUtc: new DateTime(2026, 1, 16, 12, 0, 0, DateTimeKind.Utc));

        // Act
        var serialized1 = Serialize(original);
        var serialized2 = Serialize(original);
        var serialized3 = Serialize(original);

        var hash1 = ComputeHash(serialized1);
        var hash2 = ComputeHash(serialized2);
        var hash3 = ComputeHash(serialized3);

        // Assert
        Assert.Equal(hash1, hash2);
        Assert.Equal(hash2, hash3);
    }

    [Fact]
    public void MetricReference_PreservesDecimalPrecision_ViaStringStorage()
    {
        // Arrange
        var metric1 = MetricReference.Create("Response", 95.5m);
        var metric2 = MetricReference.Create("Response", "95.5");

        // Act
        var serialized1 = Serialize(metric1);
        var serialized2 = Serialize(metric2);

        // Assert: Both produce identical JSON
        Assert.Equal(serialized1, serialized2);
        Assert.Contains("95.5", serialized1);
    }

    [Fact]
    public void Violation_PreservesStringValues_InSerialization()
    {
        // Arrange: Create with string values (preserving precision)
        var violation = Violation.Create(
            ruleName: "MaxResponseTime",
            metricName: "ResponseTime",
            severity: Severity.Fail,
            actualValue: "1250.5555",  // Exact decimal precision
            thresholdValue: "1000.0000",  // Exact decimal precision
            message: "Threshold exceeded");

        // Act
        var serialized = Serialize(violation);
        var deserialized = Deserialize<Violation>(serialized);

        // Assert: Values preserved exactly
        Assert.Equal("1250.5555", deserialized.ActualValue);
        Assert.Equal("1000.0000", deserialized.ThresholdValue);
    }

    [Fact]
    public void EvaluationEvidence_Timestamp_SerializedAsUtcIso8601()
    {
        // Arrange
        var evidence = EvaluationEvidence.Create(
            ruleId: "rule-001",
            ruleName: "Test",
            metrics: Array.Empty<MetricReference>(),
            actualValues: new Dictionary<string, string>(),
            expectedConstraint: "Test <= 100",
            constraintSatisfied: true,
            decisionOutcome: "Pass",
            recordedAtUtc: new DateTime(2026, 1, 16, 14, 30, 45, 123, DateTimeKind.Utc));

        // Act
        var serialized = Serialize(evidence);

        // Assert: Contains ISO 8601 UTC timestamp
        Assert.Contains("2026-01-16T14:30:45", serialized);
        Assert.Contains("Z", serialized);  // UTC marker
    }

    [Fact]
    public void EvaluationResult_WithMultipleViolations_MaintainsOrder()
    {
        // Arrange: Violations in specific order
        var violations = new[]
        {
            Violation.Create("Rule1", "Metric1", Severity.Fail, "100", "50", "First"),
            Violation.Create("Rule2", "Metric2", Severity.Fail, "200", "100", "Second"),
            Violation.Create("Rule3", "Metric3", Severity.Fail, "300", "200", "Third")
        };

        var result = EvaluationResult.Create(
            outcome: Severity.Fail,
            violations: violations,
            evidence: Array.Empty<EvaluationEvidence>(),
            outcomeReason: "Multiple violations",
            evaluatedAtUtc: DateTime.UtcNow);

        // Act
        var serialized = Serialize(result);
        var deserialized = Deserialize(serialized);

        // Assert: Order preserved (not sorted)
        Assert.Equal(3, deserialized.Violations.Count);
        Assert.Equal("Rule1", deserialized.Violations[0].RuleName);
        Assert.Equal("Rule2", deserialized.Violations[1].RuleName);
        Assert.Equal("Rule3", deserialized.Violations[2].RuleName);
    }

    [Fact]
    public void EvaluationResult_Different_Outcomes_ProduceDifferentJson()
    {
        // Arrange
        var resultPass = EvaluationResult.Create(
            outcome: Severity.Pass,
            violations: Array.Empty<Violation>(),
            evidence: Array.Empty<EvaluationEvidence>(),
            outcomeReason: "Passed",
            evaluatedAtUtc: new DateTime(2026, 1, 16, 12, 0, 0, DateTimeKind.Utc));

        var resultFail = EvaluationResult.Create(
            outcome: Severity.Fail,
            violations: new[]
            {
                Violation.Create("Rule", "Metric", Severity.Fail, "100", "50", "Violation")
            },
            evidence: Array.Empty<EvaluationEvidence>(),
            outcomeReason: "Failed",
            evaluatedAtUtc: new DateTime(2026, 1, 16, 12, 0, 0, DateTimeKind.Utc));

        // Act
        var serializedPass = Serialize(resultPass);
        var serializedFail = Serialize(resultFail);

        // Assert: Different outcomes produce different JSON
        Assert.NotEqual(serializedPass, serializedFail);
    }

    // Helper methods
    private string Serialize<T>(T obj) => JsonSerializer.Serialize(obj, _serializerOptions);

    private EvaluationResult Deserialize(string json)
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };
        return JsonSerializer.Deserialize<EvaluationResult>(json, options) 
            ?? throw new InvalidOperationException("Failed to deserialize");
    }

    private T Deserialize<T>(string json)
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };
        return JsonSerializer.Deserialize<T>(json, options) 
            ?? throw new InvalidOperationException("Failed to deserialize");
    }

    private string ComputeHash(string input)
    {
        using var sha256 = SHA256.Create();
        var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
        return Convert.ToBase64String(hashedBytes);
    }
}
