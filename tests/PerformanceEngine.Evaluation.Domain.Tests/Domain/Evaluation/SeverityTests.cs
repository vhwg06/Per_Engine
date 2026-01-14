using FluentAssertions;
using PerformanceEngine.Evaluation.Domain.Domain.Evaluation;
using Xunit;

namespace PerformanceEngine.Evaluation.Domain.Tests.Domain.Evaluation;

public class SeverityTests
{
    [Fact]
    public void Severity_Should_Have_Correct_Ordering()
    {
        // Arrange & Act
        var pass = Severity.PASS;
        var warn = Severity.WARN;
        var fail = Severity.FAIL;

        // Assert
        ((int)pass).Should().Be(0);
        ((int)warn).Should().Be(1);
        ((int)fail).Should().Be(2);
        ((int)fail).Should().BeGreaterThan((int)warn);
        ((int)warn).Should().BeGreaterThan((int)pass);
    }

    [Theory]
    [InlineData(Severity.PASS, Severity.PASS, Severity.PASS)]
    [InlineData(Severity.PASS, Severity.WARN, Severity.WARN)]
    [InlineData(Severity.PASS, Severity.FAIL, Severity.FAIL)]
    [InlineData(Severity.WARN, Severity.PASS, Severity.WARN)]
    [InlineData(Severity.WARN, Severity.WARN, Severity.WARN)]
    [InlineData(Severity.WARN, Severity.FAIL, Severity.FAIL)]
    [InlineData(Severity.FAIL, Severity.PASS, Severity.FAIL)]
    [InlineData(Severity.FAIL, Severity.WARN, Severity.FAIL)]
    [InlineData(Severity.FAIL, Severity.FAIL, Severity.FAIL)]
    public void Escalate_Should_Return_Most_Severe(Severity current, Severity other, Severity expected)
    {
        // Act
        var result = current.Escalate(other);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void MostSevere_Should_Return_Highest_Severity()
    {
        // Arrange
        var severities = new[] { Severity.PASS, Severity.WARN, Severity.FAIL, Severity.PASS };

        // Act
        var result = severities.MostSevere();

        // Assert
        result.Should().Be(Severity.FAIL);
    }

    [Fact]
    public void MostSevere_Should_Return_PASS_For_Empty_Collection()
    {
        // Arrange
        var severities = Array.Empty<Severity>();

        // Act
        var result = severities.MostSevere();

        // Assert
        result.Should().Be(Severity.PASS);
    }

    [Fact]
    public void MostSevere_Should_Return_Single_Value()
    {
        // Arrange
        var severities = new[] { Severity.WARN };

        // Act
        var result = severities.MostSevere();

        // Assert
        result.Should().Be(Severity.WARN);
    }
}
