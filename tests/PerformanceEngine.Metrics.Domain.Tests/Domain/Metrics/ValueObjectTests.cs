namespace PerformanceEngine.Metrics.Domain.Tests.Metrics;

using Xunit;
using FluentAssertions;
using PerformanceEngine.Metrics.Domain;
using PerformanceEngine.Metrics.Domain.Metrics;

public class ValueObjectTests
{
    #region Latency Tests

    [Theory]
    [InlineData(0)]
    [InlineData(45.5)]
    [InlineData(1000)]
    public void Latency_ValidValues_CreatesSuccessfully(double value)
    {
        // Act
        var latency = new Latency(value, LatencyUnit.Milliseconds);

        // Assert
        latency.Value.Should().Be(value);
        latency.Unit.Should().Be(LatencyUnit.Milliseconds);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-0.1)]
    [InlineData(-1000)]
    public void Latency_NegativeValue_Throws(double value)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            new Latency(value, LatencyUnit.Milliseconds));
    }

    [Fact]
    public void Latency_ConvertTo_MillisecondsToNanoseconds()
    {
        // Arrange
        var latency = new Latency(1.0, LatencyUnit.Milliseconds);

        // Act
        var converted = latency.ConvertTo(LatencyUnit.Nanoseconds);

        // Assert
        converted.Value.Should().Be(1_000_000);
        converted.Unit.Should().Be(LatencyUnit.Nanoseconds);
    }

    [Fact]
    public void Latency_ConvertTo_NanosecondsToMilliseconds()
    {
        // Arrange
        var latency = new Latency(1_000_000, LatencyUnit.Nanoseconds);

        // Act
        var converted = latency.ConvertTo(LatencyUnit.Milliseconds);

        // Assert
        converted.Value.Should().Be(1.0);
        converted.Unit.Should().Be(LatencyUnit.Milliseconds);
    }

    [Fact]
    public void Latency_GetValueIn_ReturnsConvertedValue()
    {
        // Arrange
        var latency = new Latency(5.5, LatencyUnit.Milliseconds);

        // Act
        var valueInMicroseconds = latency.GetValueIn(LatencyUnit.Microseconds);

        // Assert
        valueInMicroseconds.Should().Be(5500);
    }

    [Fact]
    public void Latency_Equality_SameValuesAreEqual()
    {
        // Arrange
        var latency1 = new Latency(100, LatencyUnit.Milliseconds);
        var latency2 = new Latency(100, LatencyUnit.Milliseconds);

        // Act & Assert
        latency1.Should().Be(latency2);
    }

    [Fact]
    public void Latency_Equality_DifferentValuesAreNotEqual()
    {
        // Arrange
        var latency1 = new Latency(100, LatencyUnit.Milliseconds);
        var latency2 = new Latency(200, LatencyUnit.Milliseconds);

        // Act & Assert
        latency1.Should().NotBe(latency2);
    }

    #endregion

    #region Percentile Tests

    [Theory]
    [InlineData(0)]
    [InlineData(50)]
    [InlineData(95)]
    [InlineData(99)]
    [InlineData(100)]
    public void Percentile_ValidValues_CreatesSuccessfully(double value)
    {
        // Act
        var percentile = new Percentile(value);

        // Assert
        percentile.Value.Should().Be(value);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-0.00001)]
    [InlineData(100.1)]
    [InlineData(150)]
    public void Percentile_OutOfRangeValues_Throws(double value)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new Percentile(value));
    }

    [Fact]
    public void Percentile_IsMinimum_ForP0()
    {
        // Arrange
        var percentile = new Percentile(0);

        // Act & Assert
        percentile.IsMinimum.Should().BeTrue();
        percentile.IsMaximum.Should().BeFalse();
        percentile.IsMedian.Should().BeFalse();
    }

    [Fact]
    public void Percentile_IsMedian_ForP50()
    {
        // Arrange
        var percentile = new Percentile(50);

        // Act & Assert
        percentile.IsMedian.Should().BeTrue();
        percentile.IsMinimum.Should().BeFalse();
        percentile.IsMaximum.Should().BeFalse();
    }

    [Fact]
    public void Percentile_IsMaximum_ForP100()
    {
        // Arrange
        var percentile = new Percentile(100);

        // Act & Assert
        percentile.IsMaximum.Should().BeTrue();
        percentile.IsMinimum.Should().BeFalse();
        percentile.IsMedian.Should().BeFalse();
    }

    [Fact]
    public void Percentile_Parse_ValidName()
    {
        // Act
        var percentile = Percentile.Parse("p95");

        // Assert
        percentile.Value.Should().Be(95);
        percentile.Name.Should().Be("p95");
    }

    [Theory]
    [InlineData("P95")]
    [InlineData("P50")]
    [InlineData("p99")]
    public void Percentile_Parse_CaseInsensitive(string name)
    {
        // Act
        var percentile = Percentile.Parse(name);

        // Assert
        percentile.Should().NotBeNull();
    }

    [Theory]
    [InlineData("95")]
    [InlineData("invalid")]
    [InlineData("")]
    public void Percentile_Parse_InvalidFormat_Throws(string name)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => Percentile.Parse(name));
    }

    [Fact]
    public void Percentile_Equality_SameValuesAreEqual()
    {
        // Arrange
        var p1 = new Percentile(95);
        var p2 = new Percentile(95);

        // Act & Assert
        p1.Should().Be(p2);
    }

    #endregion

    #region AggregationWindow Tests

    [Fact]
    public void AggregationWindow_FullExecution_CreatesSuccessfully()
    {
        // Act
        var window = AggregationWindow.FullExecution();

        // Assert
        window.Should().BeOfType<FullExecutionWindow>();
        window.Name.Should().Be("FullExecution");
    }

    [Fact]
    public void AggregationWindow_Fixed_CreatesSuccessfully()
    {
        // Act
        var window = AggregationWindow.Fixed(TimeSpan.FromSeconds(5));

        // Assert
        window.Should().BeOfType<FixedWindow>();
        ((FixedWindow)window).WindowSize.Should().Be(TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void AggregationWindow_Fixed_ZeroSize_Throws()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            AggregationWindow.Fixed(TimeSpan.Zero));
    }

    [Fact]
    public void AggregationWindow_Sliding_CreatesSuccessfully()
    {
        // Act
        var window = AggregationWindow.Sliding(
            TimeSpan.FromSeconds(10),
            TimeSpan.FromSeconds(5));

        // Assert
        window.Should().BeOfType<SlidingWindow>();
        var sliding = (SlidingWindow)window;
        sliding.WindowSize.Should().Be(TimeSpan.FromSeconds(10));
        sliding.SlideInterval.Should().Be(TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void AggregationWindow_Sliding_SlideIntervalGreaterThanSize_Throws()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            AggregationWindow.Sliding(
                TimeSpan.FromSeconds(5),
                TimeSpan.FromSeconds(10)));
    }

    #endregion
}
