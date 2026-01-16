# Baseline Domain: Developer Quick Start

**Status**: Design Phase 1  
**Date**: 2026-01-15  
**Audience**: Development team implementing baseline domain  

---

## Project Structure & Setup

### 1. Create Domain Project

```bash
# From repo root
mkdir -p src/PerformanceEngine.Baseline.Domain
mkdir -p src/PerformanceEngine.Baseline.Infrastructure
mkdir -p tests/PerformanceEngine.Baseline.Domain.Tests
mkdir -p tests/PerformanceEngine.Baseline.Infrastructure.Tests
```

### 2. Project Files (csproj)

**src/PerformanceEngine.Baseline.Domain/PerformanceEngine.Baseline.Domain.csproj**:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <LangVersion>13.0</LangVersion>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="../PerformanceEngine.Metrics.Domain/PerformanceEngine.Metrics.Domain.csproj" />
  </ItemGroup>

  <ItemGroup>
    <!-- No external dependencies in domain layer -->
  </ItemGroup>

</Project>
```

**src/PerformanceEngine.Baseline.Infrastructure/PerformanceEngine.Baseline.Infrastructure.csproj**:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <LangVersion>13.0</LangVersion>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="../PerformanceEngine.Baseline.Domain/PerformanceEngine.Baseline.Domain.csproj" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="StackExchange.Redis" Version="2.8.0" />
    <PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="10.0.0" />
  </ItemGroup>

</Project>
```

**tests/PerformanceEngine.Baseline.Domain.Tests/PerformanceEngine.Baseline.Domain.Tests.csproj**:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <LangVersion>13.0</LangVersion>
    <Nullable>enable</Nullable>
    <IsTestProject>true</IsTestProject>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="xunit" Version="2.8.1" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.5.8" />
    <PackageReference Include="FluentAssertions" Version="6.12.1" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="../../src/PerformanceEngine.Baseline.Domain/PerformanceEngine.Baseline.Domain.csproj" />
    <ProjectReference Include="../../src/PerformanceEngine.Metrics.Domain/PerformanceEngine.Metrics.Domain.csproj" />
  </ItemGroup>

</Project>
```

---

## Core Domain Classes

### 1. Baseline Aggregate

**File**: `src/PerformanceEngine.Baseline.Domain/Domain/Baselines/Baseline.cs`

```csharp
using PerformanceEngine.Metrics.Domain;

namespace PerformanceEngine.Baseline.Domain.Domain.Baselines;

/// <summary>
/// Immutable snapshot of metrics and evaluation results designated as a reference point for comparisons.
/// </summary>
public class Baseline
{
    private readonly List<IMetric> _metrics;
    private readonly List<string> _evaluationResults;
    
    public BaselineId Id { get; }
    public DateTime CreatedAt { get; }
    public IReadOnlyList<IMetric> Metrics { get; }
    public IReadOnlyList<string> EvaluationResults { get; }
    public ToleranceConfiguration ToleranceConfig { get; }
    
    public Baseline(
        BaselineId id,
        IEnumerable<IMetric> metrics,
        ToleranceConfiguration toleranceConfig,
        IEnumerable<string>? evaluationResults = null)
    {
        if (!metrics.Any())
            throw new ArgumentException("Baseline must contain at least one metric");
        
        Id = id ?? throw new ArgumentNullException(nameof(id));
        CreatedAt = DateTime.UtcNow;
        _metrics = metrics.ToList();
        _evaluationResults = evaluationResults?.ToList() ?? new();
        ToleranceConfig = toleranceConfig ?? throw new ArgumentNullException(nameof(toleranceConfig));
        
        Metrics = _metrics.AsReadOnly();
        EvaluationResults = _evaluationResults.AsReadOnly();
        
        BaselineInvariants.AssertValid(this);
    }
    
    public IMetric? GetMetric(string metricName) =>
        _metrics.FirstOrDefault(m => m.MetricName == metricName);
}
```

**File**: `src/PerformanceEngine.Baseline.Domain/Domain/Baselines/BaselineId.cs`

```csharp
namespace PerformanceEngine.Baseline.Domain.Domain.Baselines;

/// <summary>
/// Value object representing unique identifier for a baseline snapshot.
/// </summary>
public class BaselineId : IEquatable<BaselineId>
{
    private const string UuidPattern = @"^[0-9a-f]{8}-[0-9a-f]{4}-4[0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$";
    
    public string Value { get; }
    
    public BaselineId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("BaselineId cannot be empty");
        
        Value = value;
    }
    
    public static BaselineId Generate() => new(Guid.NewGuid().ToString());
    
    public override bool Equals(object? obj) => Equals(obj as BaselineId);
    
    public bool Equals(BaselineId? other) => other?.Value == Value;
    
    public override int GetHashCode() => Value.GetHashCode();
    
    public override string ToString() => Value;
}
```

### 2. Tolerance Value Object

**File**: `src/PerformanceEngine.Baseline.Domain/Domain/Tolerances/Tolerance.cs`

```csharp
namespace PerformanceEngine.Baseline.Domain.Domain.Tolerances;

/// <summary>
/// Configuration specifying acceptable variance for a metric.
/// </summary>
public class Tolerance
{
    public string MetricName { get; }
    public ToleranceType Type { get; }
    public double Amount { get; }
    
    public Tolerance(string metricName, ToleranceType type, double amount)
    {
        if (string.IsNullOrWhiteSpace(metricName))
            throw new ArgumentException("Metric name required");
        
        if (amount < 0)
            throw new ArgumentException("Tolerance amount cannot be negative");
        
        MetricName = metricName;
        Type = type;
        Amount = amount;
    }
    
    public bool IsWithinTolerance(double baselineValue, double currentValue)
    {
        var change = currentValue - baselineValue;
        
        return Type switch
        {
            ToleranceType.RELATIVE =>
                Math.Abs(change / baselineValue) <= Amount,
            ToleranceType.ABSOLUTE =>
                Math.Abs(change) <= Amount,
            _ => throw new InvalidOperationException($"Unknown tolerance type: {Type}")
        };
    }
}

public enum ToleranceType
{
    RELATIVE,  // Percentage-based: ±X%
    ABSOLUTE   // Value-based: ±X units
}
```

### 3. ConfidenceLevel Value Object

**File**: `src/PerformanceEngine.Baseline.Domain/Domain/Confidence/ConfidenceLevel.cs`

```csharp
namespace PerformanceEngine.Baseline.Domain.Domain.Confidence;

/// <summary>
/// Measure of certainty [0.0, 1.0] in a comparison outcome.
/// </summary>
public class ConfidenceLevel : IEquatable<ConfidenceLevel>
{
    public double Value { get; }
    
    public ConfidenceLevel(double value)
    {
        if (value < 0.0 || value > 1.0)
            throw new ArgumentOutOfRangeException(nameof(value), "Confidence must be in [0.0, 1.0]");
        
        Value = value;
    }
    
    public bool IsConclusive(double threshold = 0.7) => Value >= threshold;
    
    public override bool Equals(object? obj) => Equals(obj as ConfidenceLevel);
    
    public bool Equals(ConfidenceLevel? other) => Math.Abs(other?.Value - Value ?? 0) < 0.0001;
    
    public override int GetHashCode() => Value.GetHashCode();
    
    public override string ToString() => $"{Value:P0}";
}
```

### 4. Comparison Result

**File**: `src/PerformanceEngine.Baseline.Domain/Domain/Comparisons/ComparisonOutcome.cs`

```csharp
namespace PerformanceEngine.Baseline.Domain.Domain.Comparisons;

/// <summary>
/// Outcome of comparing current metrics against baseline.
/// </summary>
public enum ComparisonOutcome
{
    IMPROVEMENT,              // Better than baseline
    REGRESSION,              // Worse than baseline
    NO_SIGNIFICANT_CHANGE,   // Within tolerance
    INCONCLUSIVE             // Confidence insufficient
}
```

**File**: `src/PerformanceEngine.Baseline.Domain/Domain/Comparisons/ComparisonResult.cs`

```csharp
using PerformanceEngine.Baseline.Domain.Domain.Baselines;
using PerformanceEngine.Baseline.Domain.Domain.Confidence;

namespace PerformanceEngine.Baseline.Domain.Domain.Comparisons;

/// <summary>
/// Immutable outcome of comparing current metrics against a baseline snapshot.
/// </summary>
public class ComparisonResult
{
    public string Id { get; }
    public BaselineId BaselineId { get; }
    public DateTime ComparedAt { get; }
    
    public ComparisonOutcome OverallOutcome { get; }
    public ConfidenceLevel OverallConfidence { get; }
    
    public IReadOnlyList<ComparisonMetric> MetricResults { get; }
    
    public ComparisonResult(
        BaselineId baselineId,
        IEnumerable<ComparisonMetric> metricResults)
    {
        if (!metricResults.Any())
            throw new ArgumentException("Comparison must include at least one metric");
        
        Id = Guid.NewGuid().ToString();
        BaselineId = baselineId;
        ComparedAt = DateTime.UtcNow;
        
        MetricResults = metricResults.ToList().AsReadOnly();
        
        OverallOutcome = OutcomeAggregator.Aggregate(MetricResults);
        OverallConfidence = OutcomeAggregator.AggregateConfidence(MetricResults);
        
        ComparisonResultInvariants.AssertValid(this);
    }
    
    public bool HasRegression() => OverallOutcome == ComparisonOutcome.REGRESSION;
}
```

---

## Repository Port

**File**: `src/PerformanceEngine.Baseline.Domain/Ports/IBaselineRepository.cs`

```csharp
using PerformanceEngine.Baseline.Domain.Domain.Baselines;

namespace PerformanceEngine.Baseline.Domain.Ports;

/// <summary>
/// Port: Abstraction for baseline persistence.
/// Infrastructure layer (Redis adapter) implements this interface.
/// </summary>
public interface IBaselineRepository
{
    /// <summary>
    /// Create and store new baseline snapshot.
    /// </summary>
    Task<BaselineId> CreateAsync(Baseline baseline, CancellationToken cancellationToken);
    
    /// <summary>
    /// Retrieve baseline by ID. Returns null if expired or not found.
    /// </summary>
    Task<Baseline?> GetByIdAsync(BaselineId id, CancellationToken cancellationToken);
    
    /// <summary>
    /// List recent baselines (optional, for dashboards).
    /// </summary>
    Task<IReadOnlyList<Baseline>> ListRecentAsync(int count, CancellationToken cancellationToken);
}
```

---

## Test Harness Pattern

**File**: `tests/PerformanceEngine.Baseline.Domain.Tests/Domain/Comparisons/DeterminismTests.cs`

```csharp
using FluentAssertions;
using Xunit;
using PerformanceEngine.Baseline.Domain.Domain.Comparisons;
using PerformanceEngine.Baseline.Domain.Domain.Tolerances;

namespace PerformanceEngine.Baseline.Domain.Tests.Domain.Comparisons;

public class DeterminismTests
{
    [Fact]
    public void Comparison_ProducesDeterministicResults_Across1000Runs()
    {
        // Arrange
        var baseline = TestData.CreateBaseline();
        var request = TestData.CreateComparisonRequest(baseline);
        var calculator = new ComparisonCalculator();
        
        // Act: Run comparison 1000 times
        var results = new List<ComparisonResult>();
        for (int i = 0; i < 1000; i++)
        {
            var result = calculator.CalculateOverallResult(
                baseline.Id,
                request.CurrentMetrics,
                baseline.ToleranceConfig);
            results.Add(result);
        }
        
        // Assert: All results identical
        var first = results.First();
        results.Should()
            .AllSatisfy(r =>
            {
                r.OverallOutcome.Should().Be(first.OverallOutcome);
                r.OverallConfidence.Value.Should().Be(first.OverallConfidence.Value);
            });
    }
}
```

---

## Integration Example: Using Baseline Domain in Application

**File**: `src/PerformanceEngine.Baseline.Domain/Application/Services/ComparisonOrchestrator.cs`

```csharp
using PerformanceEngine.Baseline.Domain.Domain.Baselines;
using PerformanceEngine.Baseline.Domain.Domain.Comparisons;
using PerformanceEngine.Baseline.Domain.Domain.Tolerances;
using PerformanceEngine.Baseline.Domain.Ports;

namespace PerformanceEngine.Baseline.Domain.Application.Services;

/// <summary>
/// Application service orchestrating baseline creation and comparison.
/// </summary>
public class ComparisonOrchestrator
{
    private readonly IBaselineRepository _baselineRepository;
    private readonly IComparisonCalculator _calculator;
    
    public ComparisonOrchestrator(
        IBaselineRepository baselineRepository,
        IComparisonCalculator calculator)
    {
        _baselineRepository = baselineRepository;
        _calculator = calculator;
    }
    
    /// <summary>
    /// Create and store new baseline.
    /// </summary>
    public async Task<BaselineId> CreateBaselineAsync(
        IEnumerable<IMetric> metrics,
        ToleranceConfiguration config,
        CancellationToken cancellationToken)
    {
        var baseline = new Baseline(
            BaselineId.Generate(),
            metrics,
            config);
        
        return await _baselineRepository.CreateAsync(baseline, cancellationToken);
    }
    
    /// <summary>
    /// Compare current metrics against baseline.
    /// </summary>
    public async Task<ComparisonResult> CompareAsync(
        BaselineId baselineId,
        IEnumerable<IMetric> currentMetrics,
        CancellationToken cancellationToken)
    {
        var baseline = await _baselineRepository.GetByIdAsync(baselineId, cancellationToken);
        if (baseline == null)
            throw new BaselineExpiredException($"Baseline {baselineId} not found or expired");
        
        var metricResults = new List<ComparisonMetric>();
        foreach (var current in currentMetrics)
        {
            var baselineMetric = baseline.GetMetric(current.MetricName);
            if (baselineMetric == null)
                throw new MetricNotFoundException($"Metric {current.MetricName} not in baseline");
            
            var tolerance = baseline.ToleranceConfig.GetTolerance(current.MetricName);
            var metric = _calculator.CalculateMetric(baselineMetric, current, tolerance);
            metricResults.Add(metric);
        }
        
        return new ComparisonResult(baselineId, metricResults);
    }
}

public class BaselineExpiredException : Exception
{
    public BaselineExpiredException(string message) : base(message) { }
}

public class MetricNotFoundException : Exception
{
    public MetricNotFoundException(string message) : base(message) { }
}
```

---

## Running Tests

```bash
# From repo root
dotnet test tests/PerformanceEngine.Baseline.Domain.Tests --verbosity normal

# Run specific test class
dotnet test tests/PerformanceEngine.Baseline.Domain.Tests \
  --filter "FullyQualifiedName~DeterminismTests"

# Run with coverage (optional)
dotnet test tests/PerformanceEngine.Baseline.Domain.Tests \
  /p:CollectCoverage=true \
  /p:CoverageFormat=opencover
```

---

## Build & Publish

```bash
# Build
dotnet build src/PerformanceEngine.Baseline.Domain

# Build with infrastructure
dotnet build src/PerformanceEngine.Baseline.Infrastructure

# Publish (if creating NuGet package)
dotnet pack src/PerformanceEngine.Baseline.Domain \
  --configuration Release \
  --output ./nuget
```

---

## Configuration Example (appsettings.json)

```json
{
  "Redis": {
    "ConnectionString": "localhost:6379",
    "Database": 0,
    "BaselineTtl": "1.00:00:00",
    "CommandTimeout": 5000
  },
  "Baseline": {
    "ConfidenceThreshold": 0.7,
    "DefaultToleranceType": "RELATIVE",
    "DefaultToleranceAmount": 0.10
  }
}
```

---

## Common Development Tasks

### Adding a New Tolerance Type

1. Add to `ToleranceType` enum
2. Update `Tolerance.IsWithinTolerance()` logic
3. Add unit tests in `ToleranceTests.cs`
4. Update `research.md` if changing semantics

### Extending Comparison Logic

1. Modify `ComparisonCalculator` with new calculation
2. Add invariant to `ComparisonResultInvariants`
3. Add determinism test to verify 1000-run consistency
4. Document in `data-model.md`

### Debugging Comparison Results

```csharp
// In test or debugger
var result = new ComparisonResult(baselineId, metricResults);

Console.WriteLine($"Overall: {result.OverallOutcome}");
Console.WriteLine($"Confidence: {result.OverallConfidence}");

foreach (var metric in result.MetricResults)
{
    Console.WriteLine($"  {metric.MetricName}:");
    Console.WriteLine($"    Baseline: {metric.BaselineValue}");
    Console.WriteLine($"    Current: {metric.CurrentValue}");
    Console.WriteLine($"    Change: {metric.RelativeChange:P}");
    Console.WriteLine($"    Outcome: {metric.Outcome}");
}
```

---

## Next Steps

1. Implement remaining classes from data-model.md
2. Create test stubs in DomainTests, ToleranceTests, ComparisonTests
3. Implement Redis adapter in Infrastructure
4. Integration tests between domain and Redis
5. Generate task breakdown from plan.md (Phase 2)
