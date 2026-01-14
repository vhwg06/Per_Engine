# Implementation Guide: Evaluation Domain

**Purpose**: Step-by-step guide for implementing the evaluation domain from scratch  
**Audience**: Developers new to the domain, architects reviewing the design  
**Time to Read**: 25 minutes | **Time to Implement**: 8-12 days

---

## Table of Contents

1. [Overview](#overview)
2. [User Story 1: Simple Rule Evaluation](#user-story-1-simple-rule-evaluation)
3. [User Story 2: Batch Evaluation](#user-story-2-batch-evaluation)
4. [User Story 3: Custom Rules](#user-story-3-custom-rules)
5. [Testing Strategy](#testing-strategy)
6. [Integration with Metrics Domain](#integration-with-metrics-domain)

---

## Overview

This guide walks through the **three user stories** that define the evaluation domain:

| User Story | Purpose | Priority | Implementation Time |
|------------|---------|----------|-------------------|
| **US1** | Evaluate single metric against simple rules | **P1 - MVP** | 2 days |
| **US2** | Batch evaluation (multiple metrics & rules) | **P1 - MVP** | 1.5 days |
| **US3** | Custom rule extensibility | **P2 - Extension** | 1.5 days |

**Foundation**: Severity, Violation, EvaluationResult, IRule interface (prerequisite for all user stories)

---

## Prerequisites

Before starting implementation:

1. ✅ **Metrics Domain**: Must be implemented and tested (provides `IMetric` interface)
2. ✅ **Clean Architecture Understanding**: Know layering rules (Domain → Application → Infrastructure)
3. ✅ **DDD Patterns**: Familiarity with entities, value objects, domain services
4. ✅ **C# 12 Features**: Records, init-only properties, pattern matching

---

## Phase 1: Foundation (Blocking Prerequisites)

All user stories depend on these foundational types.

### Step 1: Create Severity Enum

```csharp
// src/Domain/Evaluation/Severity.cs
namespace PerformanceEngine.Evaluation.Domain.Domain.Evaluation;

/// <summary>
/// Evaluation outcome severity levels.
/// </summary>
public enum Severity
{
    /// <summary>All rules passed</summary>
    PASS = 0,
    
    /// <summary>Warning - non-critical rule violation</summary>
    WARN = 1,
    
    /// <summary>Critical failure - rule violation</summary>
    FAIL = 2
}

/// <summary>
/// Severity escalation logic (FAIL > WARN > PASS).
/// </summary>
public static class SeverityExtensions
{
    public static Severity Escalate(this Severity current, Severity other) =>
        (Severity)Math.Max((int)current, (int)other));
}
```

**Invariants**:
- FAIL > WARN > PASS (ordered by severity)
- Escalation is commutative: `Escalate(A, B) == Escalate(B, A)`

#### Test Coverage

```csharp
// tests/Domain/Evaluation/SeverityTests.cs
[Fact]
public void Severity_Escalation_ReturnsHighestSeverity()
{
    // PASS < WARN < FAIL
    Severity.FAIL.Escalate(Severity.WARN).Should().Be(Severity.FAIL);
    Severity.WARN.Escalate(Severity.PASS).Should().Be(Severity.WARN);
}
```

---

## User Story 2: Evaluate Metrics Against Simple Rules

**Goal**: Single metric + single rule → EvaluationResult with violations if rule fails.

### Phase 1: Define Rules

#### 2.1 IRule Interface (Strategy Pattern)

```csharp
// src/Domain/Rules/IRule.cs
namespace PerformanceEngine.Evaluation.Domain.Domain.Rules;

public interface IRule : IEquatable<IRule>
{
    string Id { get; }
    string Name { get; }
    string Description { get; }
    
    /// <summary>
    /// Evaluates the given metric against this rule's constraints.
    /// Must be deterministic: identical metric always produces identical result.
    /// </summary>
    EvaluationResult Evaluate(Metric metric);
}
```

**Key Points**:
- Interface-based extensibility (strategy pattern)
- `IEquatable<IRule>` for comparison/deduplication
- `Evaluate(Metric)` returns immutable result

#### Step 2: Implement ThresholdRule

```csharp
// src/Domain/Rules/ThresholdRule.cs
public sealed record ThresholdRule : IRule
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required string AggregationName { get; init; }
    public required double Threshold { get; init; }
    public required ComparisonOperator Operator { get; init; }

    public EvaluationResult Evaluate(Metric metric)
    {
        // 1. Find aggregation
        var aggregation = metric.AggregatedValues
            .FirstOrDefault(a => a.OperationName.Equals(AggregationName, 
                StringComparison.OrdinalIgnoreCase));

        if (aggregation == null)
        {
            // Aggregation missing → FAIL
            var violation = Violation.Create(
                ruleId: Id,
                metricName: metric.MetricType,
                actualValue: double.NaN,
                threshold: Threshold,
                message: $"{AggregationName} aggregation not found"
            );
            return EvaluationResult.Fail(
                ImmutableList.Create(violation),
                DateTime.UtcNow
            );
        }

        // 2. Convert to comparable value
        var actualValue = aggregation.Value.GetValueIn(LatencyUnit.Milliseconds);

        // 3. Compare with threshold
        if (CompareValues(actualValue, Threshold, Operator))
        {
            return EvaluationResult.Pass(DateTime.UtcNow);
        }

        // 4. Create violation for failure
        var violation = Violation.Create(
            ruleId: Id,
            metricName: metric.MetricType,
            actualValue: actualValue,
            threshold: Threshold,
            message: $"{AggregationName} {actualValue} does not meet {Operator} {Threshold}"
        );

        return EvaluationResult.Fail(ImmutableList.Create(violation), DateTime.UtcNow);
    }
}
```

**Takeaways**:
- Business logic encoded in rule classes, not scattered across system
- Deterministic: Same metric + rule always produces same result
- Extensible via IRule interface - no modification needed for new rule types

---

## User Story 2: Batch Evaluation

**Goal**: Evaluate multiple metrics against multiple rules efficiently in a single operation.

### Phase 1: Batch Use Case

```csharp
// src/Application/UseCases/EvaluateMultipleMetricsUseCase.cs
namespace PerformanceEngine.Evaluation.Domain.Application.UseCases;

public sealed class EvaluateMultipleMetricsUseCase
{
    private readonly Evaluator _evaluator;

    public EvaluateMultipleMetricsUseCase()
    {
        _evaluator = new Evaluator();
    }

    public IEnumerable<EvaluationResult> Execute(
        IEnumerable<Metric> metrics,
        IEnumerable<IRule> rules)
    {
        if (metrics == null || !metrics.Any())
            return Enumerable.Empty<EvaluationResult>();

        if (rules == null || !rules.Any())
            return Enumerable.Empty<EvaluationResult>();

        return _evaluator.EvaluateMultiple(metrics, rules);
    }
}
```

#### Testing Strategy

**Unit Tests** (162 total):
- Value object invariants (Latency, Sample)
- Aggregation determinism (1000+ runs)
- Percentile accuracy vs reference implementation
- Domain service purity (no side effects)

```bash
# Run all tests
dotnet test

# Run specific categories
dotnet test --filter "FullyQualifiedName~Aggregations"
dotnet test --filter "FullyQualifiedName~Determinism"
```

---

## Next Steps

1. **Review Documentation**: Read [spec.md](../../specs/evaluation-domain/spec.md) for complete requirements
2. **Explore Tests**: Browse test files to see expected behavior
3. **Run Quick Start**: Follow [quickstart.md](../../specs/evaluation-domain/quickstart.md)
4. **Create Custom Rules**: Implement `IRule` for domain-specific logic

---

**Document Status**: ✅ Complete  
**Last Updated**: January 2026
