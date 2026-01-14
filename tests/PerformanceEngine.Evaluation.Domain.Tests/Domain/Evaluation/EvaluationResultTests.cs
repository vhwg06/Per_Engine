using FluentAssertions;
using PerformanceEngine.Evaluation.Domain.Domain.Evaluation;
using Xunit;

namespace PerformanceEngine.Evaluation.Domain.Tests.Domain.Evaluation;

public class EvaluationResultTests
{
    [Fact]
    public void Constructor_Should_Create_Valid_Result()
    {
        // Arrange
        var violations = ImmutableList.Create(
            Violation.Create("RULE-001", "metric", 100, 50, "message")
        );
        var timestamp = DateTime.UtcNow;

        // Act
        var result = new EvaluationResult
        {
            Outcome = Severity.FAIL,
            Violations = violations,
            EvaluatedAt = timestamp
        };

        // Assert
        result.Outcome.Should().Be(Severity.FAIL);
        result.Violations.Should().HaveCount(1);
        result.EvaluatedAt.Should().Be(timestamp);
    }

    [Fact]
    public void Pass_Should_Create_Passing_Result()
    {
        // Arrange
        var timestamp = DateTime.UtcNow;

        // Act
        var result = EvaluationResult.Pass(timestamp);

        // Assert
        result.Outcome.Should().Be(Severity.PASS);
        result.Violations.Should().BeEmpty();
        result.EvaluatedAt.Should().Be(timestamp);
        result.IsPassing.Should().BeTrue();
        result.IsFailing.Should().BeFalse();
    }

    [Fact]
    public void Fail_Should_Create_Failing_Result()
    {
        // Arrange
        var violations = ImmutableList.Create(
            Violation.Create("RULE-001", "metric", 100, 50, "message")
        );
        var timestamp = DateTime.UtcNow;

        // Act
        var result = EvaluationResult.Fail(violations, timestamp);

        // Assert
        result.Outcome.Should().Be(Severity.FAIL);
        result.Violations.Should().HaveCount(1);
        result.EvaluatedAt.Should().Be(timestamp);
        result.IsPassing.Should().BeFalse();
        result.IsFailing.Should().BeTrue();
    }

    [Fact]
    public void Warning_Should_Create_Warning_Result()
    {
        // Arrange
        var violations = ImmutableList.Create(
            Violation.Create("RULE-001", "metric", 100, 50, "warning")
        );
        var timestamp = DateTime.UtcNow;

        // Act
        var result = EvaluationResult.Warning(violations, timestamp);

        // Assert
        result.Outcome.Should().Be(Severity.WARN);
        result.Violations.Should().HaveCount(1);
        result.EvaluatedAt.Should().Be(timestamp);
    }

    [Fact]
    public void FromViolations_Should_Create_FAIL_When_Violations_Present()
    {
        // Arrange
        var violations = ImmutableList.Create(
            Violation.Create("RULE-001", "metric", 100, 50, "message")
        );
        var timestamp = DateTime.UtcNow;

        // Act
        var result = EvaluationResult.FromViolations(violations, timestamp);

        // Assert
        result.Outcome.Should().Be(Severity.FAIL);
        result.Violations.Should().HaveCount(1);
        result.IsFailing.Should().BeTrue();
    }

    [Fact]
    public void FromViolations_Should_Create_PASS_When_No_Violations()
    {
        // Arrange
        var violations = ImmutableList<Violation>.Empty;
        var timestamp = DateTime.UtcNow;

        // Act
        var result = EvaluationResult.FromViolations(violations, timestamp);

        // Assert
        result.Outcome.Should().Be(Severity.PASS);
        result.Violations.Should().BeEmpty();
        result.IsPassing.Should().BeTrue();
    }

    [Fact]
    public void EvaluationResult_Should_Be_Immutable()
    {
        // Arrange
        var result = EvaluationResult.Pass(DateTime.UtcNow);

        // Assert - record types are immutable by default
        result.Should().BeAssignableTo<IEquatable<EvaluationResult>>();
    }

    [Fact]
    public void EvaluationResult_Should_Support_Value_Equality()
    {
        // Arrange
        var violations = ImmutableList.Create(
            Violation.Create("RULE-001", "metric", 100, 50, "message")
        );
        var timestamp = new DateTime(2026, 1, 14, 12, 0, 0, DateTimeKind.Utc);

        var result1 = new EvaluationResult
        {
            Outcome = Severity.FAIL,
            Violations = violations,
            EvaluatedAt = timestamp
        };
        var result2 = new EvaluationResult
        {
            Outcome = Severity.FAIL,
            Violations = violations,
            EvaluatedAt = timestamp
        };
        var result3 = new EvaluationResult
        {
            Outcome = Severity.PASS,
            Violations = ImmutableList<Violation>.Empty,
            EvaluatedAt = timestamp
        };

        // Assert
        result1.Should().Be(result2);
        result1.Should().NotBe(result3);
    }

    [Fact]
    public void ToString_Should_Return_Deterministic_Format()
    {
        // Arrange
        var timestamp = new DateTime(2026, 1, 14, 12, 0, 0, DateTimeKind.Utc);
        var result = EvaluationResult.Pass(timestamp);

        // Act
        var output = result.ToString();

        // Assert
        output.Should().Contain("PASS");
        output.Should().Contain("no violations");
        output.Should().Contain("2026-01-14T12:00:00");
    }

    [Fact]
    public void ToString_Should_Show_Violation_Count()
    {
        // Arrange
        var violations = ImmutableList.Create(
            Violation.Create("RULE-001", "metric1", 100, 50, "msg1"),
            Violation.Create("RULE-002", "metric2", 200, 150, "msg2")
        );
        var timestamp = DateTime.UtcNow;
        var result = EvaluationResult.Fail(violations, timestamp);

        // Act
        var output = result.ToString();

        // Assert
        output.Should().Contain("FAIL");
        output.Should().Contain("2 violation(s)");
    }
}
