using FluentAssertions;
using PerformanceEngine.Evaluation.Domain.Domain.Evaluation;
using Xunit;

namespace PerformanceEngine.Evaluation.Domain.Tests.Domain.Evaluation;

public class ViolationTests
{
    [Fact]
    public void Constructor_Should_Create_Valid_Violation()
    {
        // Arrange & Act
        var violation = Violation.Create(
            ruleId: "RULE-001",
            metricName: "p95_latency",
            actualValue: 250.5,
            threshold: 200.0,
            message: "p95 latency exceeded threshold"
        );

        // Assert
        violation.RuleId.Should().Be("RULE-001");
        violation.MetricName.Should().Be("p95_latency");
        violation.ActualValue.Should().Be(250.5);
        violation.Threshold.Should().Be(200.0);
        violation.Message.Should().Be("p95 latency exceeded threshold");
    }

    [Theory]
    [InlineData(null, "metric", "message")]
    [InlineData("", "metric", "message")]
    [InlineData("   ", "metric", "message")]
    public void Constructor_Should_Throw_When_RuleId_Invalid(string? ruleId, string metricName, string message)
    {
        // Act
        var act = () => Violation.Create(ruleId!, metricName, 100, 50, message);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("ruleId");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_Should_Throw_When_MetricName_Invalid(string? metricName)
    {
        // Act
        var act = () => Violation.Create("RULE-001", metricName!, 100, 50, "message");

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("metricName");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_Should_Throw_When_Message_Invalid(string? message)
    {
        // Act
        var act = () => Violation.Create("RULE-001", "metric", 100, 50, message!);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("message");
    }

    [Fact]
    public void Violation_Should_Be_Immutable()
    {
        // Arrange
        var violation = Violation.Create("RULE-001", "metric", 100, 50, "message");

        // Assert - record types are immutable by default
        violation.Should().BeAssignableTo<IEquatable<Violation>>();
    }

    [Fact]
    public void Violation_Should_Support_Value_Equality()
    {
        // Arrange
        var violation1 = Violation.Create("RULE-001", "metric", 100, 50, "message");
        var violation2 = Violation.Create("RULE-001", "metric", 100, 50, "message");
        var violation3 = Violation.Create("RULE-002", "metric", 100, 50, "message");

        // Assert
        violation1.Should().Be(violation2);
        violation1.Should().NotBe(violation3);
        (violation1 == violation2).Should().BeTrue();
        (violation1 == violation3).Should().BeFalse();
    }

    [Fact]
    public void ToString_Should_Return_Deterministic_Format()
    {
        // Arrange
        var violation = Violation.Create("RULE-001", "p95_latency", 250.5, 200.0, "Threshold exceeded");

        // Act
        var result = violation.ToString();

        // Assert
        result.Should().Be("RULE-001: p95_latency = 250.50 (expected 200.00) - Threshold exceeded");
    }

    [Fact]
    public void ToString_Should_Handle_Edge_Cases()
    {
        // Arrange
        var violation = Violation.Create("R1", "m", double.MaxValue, double.MinValue, "msg");

        // Act
        var result = violation.ToString();

        // Assert
        result.Should().Contain("R1");
        result.Should().Contain("m");
        result.Should().Contain("msg");
    }
}
