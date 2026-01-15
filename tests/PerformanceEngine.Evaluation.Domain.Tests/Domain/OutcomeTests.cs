namespace PerformanceEngine.Evaluation.Domain.Tests.Domain;

using PerformanceEngine.Evaluation.Domain.Domain;
using Xunit;

/// <summary>
/// Unit tests for Outcome enum extension.
/// Verifies INCONCLUSIVE support, backward compatibility with existing outcomes.
/// </summary>
public class OutcomeTests
{
    [Fact]
    public void Outcome_HasPassValue()
    {
        Assert.Equal(1, (int)Outcome.PASS);
    }

    [Fact]
    public void Outcome_HasFailValue()
    {
        Assert.Equal(2, (int)Outcome.FAIL);
    }

    [Fact]
    public void Outcome_HasInconclusiveValue()
    {
        Assert.Equal(3, (int)Outcome.INCONCLUSIVE);
    }

    [Fact]
    public void Outcome_AllValuesAreDefined()
    {
        Assert.NotNull(Outcome.PASS);
        Assert.NotNull(Outcome.FAIL);
        Assert.NotNull(Outcome.INCONCLUSIVE);
    }

    [Fact]
    public void Outcome_HasThreeValues()
    {
        var values = Enum.GetValues(typeof(Outcome));
        Assert.Equal(3, values.Length);
    }

    [Fact]
    public void Outcome_CanConvertToString()
    {
        Assert.Equal("PASS", Outcome.PASS.ToString());
        Assert.Equal("FAIL", Outcome.FAIL.ToString());
        Assert.Equal("INCONCLUSIVE", Outcome.INCONCLUSIVE.ToString());
    }

    [Fact]
    public void Outcome_CanParseFromString()
    {
        Assert.Equal(Outcome.PASS, Enum.Parse<Outcome>("PASS"));
        Assert.Equal(Outcome.FAIL, Enum.Parse<Outcome>("FAIL"));
        Assert.Equal(Outcome.INCONCLUSIVE, Enum.Parse<Outcome>("INCONCLUSIVE"));
    }

    [Fact]
    public void Outcome_BackwardCompatible_ExistingCode()
    {
        // Existing code checking for PASS/FAIL should still work
        Outcome outcome = Outcome.PASS;
        Assert.True(outcome == Outcome.PASS);

        outcome = Outcome.FAIL;
        Assert.True(outcome == Outcome.FAIL);
    }

    [Fact]
    public void Outcome_InconlusiveDistinct()
    {
        // INCONCLUSIVE should be distinct from PASS and FAIL
        Assert.NotEqual(Outcome.INCONCLUSIVE, Outcome.PASS);
        Assert.NotEqual(Outcome.INCONCLUSIVE, Outcome.FAIL);
        Assert.NotEqual(Outcome.PASS, Outcome.FAIL);
    }

    [Fact]
    public void Outcome_CanUseInSwitch()
    {
        Outcome outcome = Outcome.INCONCLUSIVE;
        string result = outcome switch
        {
            Outcome.PASS => "Passed",
            Outcome.FAIL => "Failed",
            Outcome.INCONCLUSIVE => "Inconclusive",
            _ => "Unknown"
        };

        Assert.Equal("Inconclusive", result);
    }
}
