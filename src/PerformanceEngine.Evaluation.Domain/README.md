# PerformanceEngine.Evaluation.Domain

**A Clean Architecture domain library for deterministic performance evaluation**

[![.NET](https://img.shields.io/badge/.NET-8.0-blue)]()
[![Architecture](https://img.shields.io/badge/architecture-Clean%20Architecture-orange)]()
[![DDD](https://img.shields.io/badge/design-Domain%20Driven-purple)]()

---

## Overview

The **Evaluation Domain** provides a **deterministic**, **extensible**, and **engine-agnostic** rule engine for evaluating performance metrics against quality gates. It answers the fundamental question: **"Does this performance result meet our requirements, and why not?"**

### Key Features

- ✅ **Deterministic Evaluation**: Identical metrics + rules = byte-identical results (1000+ iteration tested)
- ✅ **Extensible Rules**: Strategy pattern allows custom rule types without modifying core logic
- ✅ **Engine-Agnostic**: Works with metrics from K6, JMeter, Gatling, or any IMetric implementation
- ✅ **Clean Architecture**: Zero infrastructure dependencies in domain layer
- ✅ **Immutable Results**: EvaluationResult cannot be modified after creation
- ✅ **Structured Violations**: Clear failure reports with actual vs expected values

---

## Quick Start

### Installation

```bash
# Clone repository
git clone <repository-url>
cd Per_Engine

# Build project
dotnet build src/PerformanceEngine.Evaluation.Domain/

# Run tests
dotnet test tests/PerformanceEngine.Evaluation.Domain.Tests/
```

### Basic Usage

```csharp
using PerformanceEngine.Evaluation.Domain.Domain.Rules;
using PerformanceEngine.Evaluation.Domain.Domain.Evaluation;
using PerformanceEngine.Metrics.Domain.Metrics;

// 1. Create a rule
var rule = new ThresholdRule
{
    Id = "RULE-001",
    Name = "P95 Latency SLA",
    Description = "P95 latency must be under 200ms",
    AggregationName = "P95",
    Threshold = 200.0,
    Operator = ComparisonOperator.LessThan
};

// 2. Create an evaluator
var evaluator = new Evaluator();

// 3. Evaluate a metric (assume 'metric' is an IMetric instance)
var result = evaluator.Evaluate(metric, rule);

// 4. Check result
if (result.Outcome == Severity.PASS)
{
    Console.WriteLine("✓ Performance test passed!");
}
else
{
    Console.WriteLine($"✗ Test failed: {result.Violations[0].Message}");
    Console.WriteLine($"  Expected: {result.Violations[0].Threshold}ms");
    Console.WriteLine($"  Actual: {result.Violations[0].ActualValue}ms");
}
```

---

## Architecture

### Layering

```
┌─────────────────────────────────────────┐
│       APPLICATION                        │
│  EvaluationService → UseCases → DTOs    │
└────────────────────┬────────────────────┘
                     ↓
┌─────────────────────────────────────────┐
│       DOMAIN                             │
│  Rules → Evaluator → EvaluationResult   │
└────────────────────┬────────────────────┘
                     ↓
┌─────────────────────────────────────────┐
│  Metrics Domain (input, no dependency)  │
└─────────────────────────────────────────┘
```

### Core Concepts

**Rule**: Strategy pattern interface for evaluation logic
- `ThresholdRule`: Single comparison (p95 < 200ms)
- `RangeRule`: Range check (10% < error_rate < 20%)
- `CompositeRule`: Logical combinations (AND/OR)
- Custom implementations via `IRule` interface

**EvaluationResult**: Immutable evaluation outcome
- `Outcome`: PASS, WARN, or FAIL severity
- `Violations`: List of rule failures with details
- `EvaluatedAt`: Timestamp for reproducibility

**Violation**: Structured failure report
- Rule ID, metric name, actual/expected values, human-readable message

**Evaluator**: Pure domain service (stateless, deterministic)
- `Evaluate(metric, rule)`: Single evaluation
- `EvaluateMultiple(metrics, rules)`: Batch evaluation

---

## Determinism Guarantees

The Evaluation Domain is **byte-identical deterministic**:

- Same metric + same rule = identical JSON serialization across 1000+ runs
- No `DateTime.Now`, `Random`, or non-deterministic operations in domain
- Order-independent batch evaluation (sorted internally)
- Floating-point comparisons use epsilon tolerance (0.001)
- InvariantCulture for all string operations

**Verification**:
```bash
# Run determinism tests (1000+ iterations each)
dotnet test --filter "FullyQualifiedName~Determinism"
```

---

## Rule Types

### ThresholdRule

Evaluates a single aggregation against a threshold with comparison operators.

**Supported Operators**:
- `LessThan` (<)
- `LessThanOrEqual` (≤)
- `GreaterThan` (>)
- `GreaterThanOrEqual` (≥)
- `Equal` (==, epsilon-based)
- `NotEqual` (!=)

**Example**:
```csharp
var rule = new ThresholdRule
{
    Id = "LATENCY-P99",
    Name = "P99 Latency SLA",
    Description = "P99 must be under 500ms",
    AggregationName = "P99",
    Threshold = 500.0,
    Operator = ComparisonOperator.LessThan
};
```

### RangeRule

Evaluates whether a value falls within an acceptable range (exclusive bounds).

**Example**:
```csharp
var rule = new RangeRule
{
    Id = "ERROR-RATE-RANGE",
    Name = "Error Rate Tolerance",
    Description = "Error rate between 0.1% and 1.0%",
    AggregationName = "ErrorRate",
    MinBound = 0.1,
    MaxBound = 1.0
};
```

### CompositeRule

Combines multiple rules with logical operators (AND/OR).

**Example**:
```csharp
var compositeRule = new CompositeRule
{
    Id = "COMBINED-SLA",
    Name = "Combined Performance SLA",
    Description = "P95 < 200ms AND error_rate < 1%",
    Operator = LogicalOperator.And,
    SubRules = ImmutableList.Create<IRule>(
        new ThresholdRule { /* P95 rule */ },
        new ThresholdRule { /* Error rate rule */ }
    )
};
```

---

## Custom Rules

Implement `IRule` interface to create custom evaluation logic:

```csharp
public sealed record CustomPercentileRule : IRule
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required double Percentile { get; init; }
    public required double MaxValue { get; init; }

    public EvaluationResult Evaluate(Metric metric)
    {
        var aggregationName = $"P{Percentile:F1}".Replace(".0", "");
        var aggregation = metric.AggregatedValues
            .FirstOrDefault(a => a.OperationName.Equals(aggregationName, 
                StringComparison.OrdinalIgnoreCase));

        if (aggregation == null)
        {
            var violation = Violation.Create(
                ruleId: Id,
                metricName: metric.MetricType,
                actualValue: double.NaN,
                threshold: MaxValue,
                message: $"{aggregationName} aggregation not found"
            );
            return EvaluationResult.Fail(
                ImmutableList.Create(violation), 
                DateTime.UtcNow
            );
        }

        var actualValue = aggregation.Value.GetValueIn(LatencyUnit.Milliseconds);
        if (actualValue > MaxValue)
        {
            var violation = Violation.Create(
                ruleId: Id,
                metricName: metric.MetricType,
                actualValue: actualValue,
                threshold: MaxValue,
                message: $"{aggregationName} {actualValue}ms exceeds {MaxValue}ms"
            );
            return EvaluationResult.Fail(
                ImmutableList.Create(violation), 
                DateTime.UtcNow
            );
        }

        return EvaluationResult.Pass(DateTime.UtcNow);
    }

    public bool Equals(IRule? other) =>
        other is CustomPercentileRule r &&
        Id == r.Id &&
        Percentile == r.Percentile &&
        MaxValue == r.MaxValue;

    public override int GetHashCode() =>
        HashCode.Combine(Id, Percentile, MaxValue);
}
```

**Usage**:
```csharp
var customRule = new CustomPercentileRule
{
    Id = "CUSTOM-P99.9",
    Name = "P99.9 Latency",
    Description = "P99.9 must be under 1000ms",
    Percentile = 99.9,
    MaxValue = 1000.0
};

var result = evaluator.Evaluate(metric, customRule);
```

---

## Batch Evaluation

Evaluate multiple metrics against multiple rules efficiently:

```csharp
var metrics = new[]
{
    metricA,  // IMetric instance
    metricB,
    metricC
};

var rules = new IRule[]
{
    latencyRule,
    errorRateRule,
    throughputRule
};

// Evaluate all metrics against all rules
var results = evaluator.EvaluateMultiple(metrics, rules);

// Process results
var failures = results.Where(r => r.Outcome == Severity.FAIL);
foreach (var failure in failures)
{
    Console.WriteLine($"Failed: {failure.Violations[0].MetricName}");
}
```

---

## Application Layer

Use `EvaluationService` facade for application-level operations:

```csharp
using PerformanceEngine.Evaluation.Domain.Application.Services;

var service = new EvaluationService();

// Single evaluation with error handling
var result = service.Evaluate(metric, rule);

// Batch evaluation
var results = service.EvaluateBatch(metrics, rules);
```

---

## Testing

### Run All Tests

```bash
dotnet test tests/PerformanceEngine.Evaluation.Domain.Tests/
```

### Run Specific Test Categories

```bash
# Unit tests only
dotnet test --filter "FullyQualifiedName~Domain"

# Determinism verification (1000+ iterations)
dotnet test --filter "FullyQualifiedName~Determinism"

# Architecture compliance
dotnet test --filter "FullyQualifiedName~Architecture"

# Integration tests
dotnet test --filter "FullyQualifiedName~Integration"
```

---

## Design Principles

### 1. Determinism First
- Pure functions throughout domain layer
- No `DateTime.Now` for logic decisions (only for timestamps)
- No `Random` or non-deterministic operations
- Epsilon-based floating-point equality

### 2. Extensibility via Strategy Pattern
- `IRule` interface allows unlimited custom rule types
- Evaluator delegates to rules; no type-based branching
- New rules added without modifying core logic

### 3. Clean Architecture
- Domain layer has zero infrastructure dependencies
- No file I/O, no database, no network calls in domain
- Application layer orchestrates; domain layer decides

### 4. Immutability
- All domain entities immutable after construction
- `ImmutableList` for collections
- Record types with `init` accessors

### 5. Explicit Over Implicit
- Clear violation messages with actual/expected values
- Named operators (LessThan) over symbols (<)
- Structured results over boolean returns

---

## Dependencies

**Required**:
- .NET 8.0
- PerformanceEngine.Metrics.Domain (input dependency)
- System.Collections.Immutable

**Development**:
- xUnit 2.6.2
- FluentAssertions 6.12.0

---

## Project Structure

```
src/PerformanceEngine.Evaluation.Domain/
├── Domain/
│   ├── Rules/
│   │   ├── IRule.cs                    # Strategy interface
│   │   ├── ThresholdRule.cs            # Single comparison
│   │   ├── RangeRule.cs                # Range validation
│   │   ├── CompositeRule.cs            # Logical combinations
│   │   └── ComparisonOperator.cs       # Enum: <, ≤, >, ≥, ==, !=
│   │
│   └── Evaluation/
│       ├── Evaluator.cs                # Pure evaluation service
│       ├── EvaluationResult.cs         # Immutable result entity
│       ├── Violation.cs                # Failure details
│       └── Severity.cs                 # PASS, WARN, FAIL
│
├── Application/
│   ├── Services/
│   │   └── EvaluationService.cs        # Application facade
│   │
│   ├── Dto/
│   │   ├── RuleDto.cs
│   │   ├── EvaluationResultDto.cs
│   │   └── ViolationDto.cs
│   │
│   └── UseCases/
│       └── EvaluateMultipleMetricsUseCase.cs
│
└── Ports/
    └── (Future: IEvaluationRepository, INotificationService)
```

---

## Further Reading

- **Implementation Guide**: [IMPLEMENTATION_GUIDE.md](IMPLEMENTATION_GUIDE.md) - Step-by-step walkthrough
- **Specification**: [specs/evaluation-domain/spec.md](../../specs/evaluation-domain/spec.md) - Full domain spec
- **Quick Start**: [specs/evaluation-domain/quickstart.md](../../specs/evaluation-domain/quickstart.md) - Setup guide
- **API Contracts**: [specs/evaluation-domain/contracts/](../../specs/evaluation-domain/contracts/) - Interface documentation

---

## License

[Your License Here]

---

## Contact

For questions or issues, please [open an issue](../../issues) on GitHub.
