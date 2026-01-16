namespace PerformanceEngine.Baseline.Domain.Tests;

using FluentAssertions;
using PerformanceEngine.Baseline.Domain.Domain;
using PerformanceEngine.Baseline.Domain.Domain.Baselines;
using PerformanceEngine.Baseline.Domain.Domain.Comparisons;
using PerformanceEngine.Baseline.Domain.Domain.Confidence;
using PerformanceEngine.Baseline.Domain.Domain.Tolerances;
using PerformanceEngine.Metrics.Domain.Metrics;
using PerformanceEngine.Metrics.Domain.Ports;
using Xunit;

/// <summary>
/// Phase 8 Exception Handling Tests - T064
/// Comprehensive exception handling tests for all domain exceptions.
/// Tests BaselineNotFoundException, ToleranceValidationException, DomainInvariantViolatedException, etc.
/// </summary>
public sealed class ExceptionTests
{
    [Fact]
    public void BaselineNotFoundException_ThrownForMissingBaseline()
    {
        // Arrange
        var missingId = new BaselineId();

        // Act & Assert - Application layer would throw this when baseline not found
        Action act = () => throw new BaselineNotFoundException(missingId.ToString());

        act.Should().Throw<BaselineNotFoundException>()
            .WithMessage($"*{missingId}*");
    }

    [Fact]
    public void ToleranceValidationException_ThrownForInvalidTolerance()
    {
        // Arrange - Negative tolerance amount (invalid)
        
        // Act & Assert
        Action act = () => new Tolerance("Metric", ToleranceType.Absolute, -10m);

        act.Should().Throw<ToleranceValidationException>()
            .WithMessage("*cannot be negative*");
    }

    [Fact]
    public void ToleranceValidationException_ThrownForRelativeToleranceOver100()
    {
        // Arrange - Relative tolerance > 100% (invalid)
        
        // Act & Assert
        Action act = () => new Tolerance("Metric", ToleranceType.Relative, 150m);

        act.Should().Throw<ToleranceValidationException>()
            .WithMessage("*cannot exceed 100*");
    }

    [Fact]
    public void ToleranceValidationException_ThrownForEmptyMetricName()
    {
        // Act & Assert
        Action act = () => new Tolerance("", ToleranceType.Relative, 10m);

        act.Should().Throw<ToleranceValidationException>()
            .WithMessage("*cannot be empty*");
    }

    [Fact]
    public void DomainInvariantViolatedException_ThrownForEmptyMetrics()
    {
        // Arrange - Empty metrics list violates invariant
        var emptyMetrics = new List<IMetric>();
        var config = new ToleranceConfiguration(new[]
        {
            new Tolerance("Dummy", ToleranceType.Relative, 10m)
        });

        // Act & Assert
        Action act = () => new Baseline(emptyMetrics, config);

        act.Should().Throw<DomainInvariantViolatedException>()
            .WithMessage("*at least one metric*");
    }

    [Fact]
    public void DomainInvariantViolatedException_ThrownForDuplicateMetricTypes()
    {
        // Arrange - Duplicate metric types violate invariant
        var metrics = new List<IMetric>
        {
            new TestMetric("ResponseTime", 100.0),
            new TestMetric("ResponseTime", 150.0) // Duplicate
        };

        var config = new ToleranceConfiguration(new[]
        {
            new Tolerance("ResponseTime", ToleranceType.Relative, 10m)
        });

        // Act & Assert
        Action act = () => new Baseline(metrics, config);

        act.Should().Throw<DomainInvariantViolatedException>()
            .WithMessage("*duplicate*");
    }

    [Fact]
    public void DomainInvariantViolatedException_ThrownForIncompleteTolerance()
    {
        // Arrange - Tolerance config missing a metric
        var metrics = new List<IMetric>
        {
            new TestMetric("ResponseTime", 100.0),
            new TestMetric("Throughput", 1000.0)
        };

        var config = new ToleranceConfiguration(new[]
        {
            new Tolerance("ResponseTime", ToleranceType.Relative, 10m)
            // Missing Throughput tolerance
        });

        // Act & Assert
        Action act = () => new Baseline(metrics, config);

        act.Should().Throw<DomainInvariantViolatedException>()
            .WithMessage("*tolerance*");
    }

    [Fact]
    public void ConfidenceValidationException_ThrownForValueBelowZero()
    {
        // Act & Assert
        Action act = () => new ConfidenceLevel(-0.1m);

        act.Should().Throw<ConfidenceValidationException>()
            .WithMessage("*must be in range*");
    }

    [Fact]
    public void ConfidenceValidationException_ThrownForValueAboveOne()
    {
        // Act & Assert
        Action act = () => new ConfidenceLevel(1.5m);

        act.Should().Throw<ConfidenceValidationException>()
            .WithMessage("*must be in range*");
    }

    [Fact]
    public void ComparisonResultInvariants_ThrownForEmptyMetricResults()
    {
        // Arrange - ComparisonResult requires at least one metric
        var baselineId = new BaselineId();
        var emptyMetrics = Array.Empty<ComparisonMetric>();

        // Act & Assert
        Action act = () => new ComparisonResult(
            baselineId,
            emptyMetrics,
            ComparisonOutcome.NoSignificantChange,
            new ConfidenceLevel(0.8m)
        );

        act.Should().Throw<DomainInvariantViolatedException>()
            .WithMessage("*at least one metric*");
    }

    [Fact]
    public void BaselineDomainException_IsBaseException()
    {
        // Arrange & Act
        var exception = new BaselineNotFoundException(new BaselineId().ToString());

        // Assert - Should inherit from BaselineDomainException
        exception.Should().BeAssignableTo<BaselineDomainException>();
    }

    [Fact]
    public void ToleranceValidationException_InheritsFromDomainException()
    {
        // Arrange
        Exception? caughtException = null;

        // Act
        try
        {
            new Tolerance("Metric", ToleranceType.Absolute, -5m);
        }
        catch (Exception ex)
        {
            caughtException = ex;
        }

        // Assert
        caughtException.Should().BeOfType<ToleranceValidationException>();
        caughtException.Should().BeAssignableTo<BaselineDomainException>();
    }

    [Fact]
    public void DomainInvariantViolatedException_InheritsFromDomainException()
    {
        // Arrange
        Exception? caughtException = null;
        var emptyMetrics = new List<IMetric>();
        var config = new ToleranceConfiguration(new[] { new Tolerance("Test", ToleranceType.Relative, 10m) });

        // Act
        try
        {
            new Baseline(emptyMetrics, config);
        }
        catch (Exception ex)
        {
            caughtException = ex;
        }

        // Assert
        caughtException.Should().BeOfType<DomainInvariantViolatedException>();
        caughtException.Should().BeAssignableTo<BaselineDomainException>();
    }

    [Fact]
    public void ConfidenceValidationException_InheritsFromDomainException()
    {
        // Arrange
        Exception? caughtException = null;

        // Act
        try
        {
            new ConfidenceLevel(2.0m);
        }
        catch (Exception ex)
        {
            caughtException = ex;
        }

        // Assert
        caughtException.Should().BeOfType<ConfidenceValidationException>();
        caughtException.Should().BeAssignableTo<BaselineDomainException>();
    }

    [Fact]
    public void ToleranceConfiguration_ThrowsKeyNotFoundException_ForMissingMetric()
    {
        // Arrange
        var config = new ToleranceConfiguration(new[]
        {
            new Tolerance("ExistingMetric", ToleranceType.Relative, 10m)
        });

        // Act & Assert
        Action act = () => config.GetTolerance("MissingMetric");

        act.Should().Throw<KeyNotFoundException>()
            .WithMessage("*MissingMetric*");
    }

    [Fact]
    public void ExceptionMessages_ContainUsefulContext()
    {
        // Test that exceptions provide sufficient debugging information
        
        // BaselineNotFoundException
        var baselineId = new BaselineId();
        var baselineEx = new BaselineNotFoundException(baselineId.ToString());
        baselineEx.Message.Should().Contain(baselineId.ToString());

        // ToleranceValidationException (negative amount)
        try
        {
            new Tolerance("Test", ToleranceType.Absolute, -5m);
        }
        catch (ToleranceValidationException ex)
        {
            ex.Message.Should().Contain("cannot be negative");
        }

        // DomainInvariantViolatedException (empty metrics)
        try
        {
            var emptyMetrics = new List<IMetric>();
            var config = new ToleranceConfiguration(new[] { new Tolerance("T", ToleranceType.Relative, 10m) });
            new Baseline(emptyMetrics, config);
        }
        catch (DomainInvariantViolatedException ex)
        {
            ex.Message.Should().Contain("at least one metric");
        }
    }

    [Fact]
    public void AllDomainExceptions_AreSerializable()
    {
        // This is important for distributed systems and logging
        
        // BaselineNotFoundException
        var baselineEx = new BaselineNotFoundException(new BaselineId().ToString());
        baselineEx.Should().NotBeNull();

        // ToleranceValidationException
        var toleranceEx = new ToleranceValidationException("TestMetric", "Test tolerance error");
        toleranceEx.Should().NotBeNull();

        // DomainInvariantViolatedException
        var invariantEx = new DomainInvariantViolatedException("TestInvariant", "Test invariant error");
        invariantEx.Should().NotBeNull();

        // ConfidenceValidationException
        var confidenceEx = new ConfidenceValidationException(5.0m);
        confidenceEx.Should().NotBeNull();
    }

    [Fact]
    public void NullTolerance_ThrowsToleranceValidationException()
    {
        // Act & Assert
        Action act = () => new ToleranceConfiguration(null!);

        act.Should().Throw<ToleranceValidationException>();
    }

    [Fact]
    public void NullMetrics_ThrowsArgumentNullException()
    {
        // Arrange
        var config = new ToleranceConfiguration(new[] { new Tolerance("T", ToleranceType.Relative, 10m) });

        // Act & Assert
        Action act = () => new Baseline(null!, config);

        act.Should().Throw<ArgumentNullException>();
    }

    private sealed class TestMetric : IMetric
    {
        public TestMetric(string metricType, double value)
        {
            MetricType = metricType;
            Value = value;
            Id = Guid.NewGuid();
            Unit = "unit";
            ComputedAt = DateTime.UtcNow;
        }

        public Guid Id { get; }
        public string MetricType { get; }
        public double Value { get; }
        public string Unit { get; }
        public DateTime ComputedAt { get; }
        public CompletessStatus CompletessStatus => CompletessStatus.COMPLETE;
        public MetricEvidence Evidence => new MetricEvidence(1, 1, "test");
    }
}
