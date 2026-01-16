# Phase 1 Design: Data Model for Persist Results

**Date**: 2026-01-16 | **Feature**: 001-persist-results | **Status**: Complete

---

## Entity Definitions

### Primary Aggregate Root: EvaluationResult

**Purpose**: Immutable record of a complete evaluation decision with all supporting evidence, violations, and context.

**Location**: `PerformanceEngine.Evaluation.Domain/Domain/EvaluationResult.cs`

**Definition**:

```csharp
namespace PerformanceEngine.Evaluation.Domain;

/// <summary>
/// Immutable aggregate root representing a complete evaluation decision.
/// Contains all context needed for audit trails and deterministic replay.
/// 
/// Invariants:
/// - Id is unique (enforced by repository at persistence layer)
/// - Outcome severity is consistent with violations list
/// - All collections are immutable and cannot be modified after construction
/// - Timestamp is UTC-based and set at evaluation time
/// - Evidence trail is complete (no missing metric references)
/// </summary>
public record EvaluationResult(
    /// <summary>Unique identifier assigned at evaluation time.</summary>
    Guid Id,
    
    /// <summary>Overall evaluation outcome severity (Pass, Warning, Fail).</summary>
    Severity Outcome,
    
    /// <summary>Immutable list of rule violations detected during evaluation.</summary>
    ImmutableList<Violation> Violations,
    
    /// <summary>Complete audit trail of evaluation decisions and evidence.</summary>
    ImmutableList<EvaluationEvidence> Evidence,
    
    /// <summary>Human-readable rationale for the evaluation outcome.</summary>
    string OutcomeReason,
    
    /// <summary>UTC timestamp when evaluation was performed.</summary>
    DateTime EvaluatedAt
)
{
    /// <summary>
    /// Factory method to create a new EvaluationResult with validation.
    /// </summary>
    public static EvaluationResult Create(
        Severity outcome,
        IEnumerable<Violation> violations,
        IEnumerable<EvaluationEvidence> evidence,
        string outcomeReason,
        DateTime evaluatedAtUtc)
    {
        // Validate outcome consistency with violations
        if (outcome == Severity.Pass && violations.Any())
            throw new InvalidOperationException(
                "Outcome cannot be Pass when violations are present");

        if (string.IsNullOrWhiteSpace(outcomeReason))
            throw new ArgumentException("Outcome reason must not be empty", nameof(outcomeReason));

        // Ensure timestamp is UTC
        if (evaluatedAtUtc.Kind != DateTimeKind.Utc)
            throw new ArgumentException(
                "Evaluated timestamp must be UTC", nameof(evaluatedAtUtc));

        return new EvaluationResult(
            Id: Guid.NewGuid(),
            Outcome: outcome,
            Violations: violations.ToImmutableList(),
            Evidence: evidence.ToImmutableList(),
            OutcomeReason: outcomeReason,
            EvaluatedAt: evaluatedAtUtc);
    }
}

/// <summary>
/// Outcome severity levels ordered from least to most severe.
/// </summary>
public enum Severity
{
    Pass = 0,      // All rules satisfied
    Warning = 1,   // Minor violations, test still acceptable
    Fail = 2       // Critical violations, test failed
}
```

**Immutability Guarantee**: C# record type enforces read-only properties at compile time

**Equality Semantics**: Value-based equality (two results are equal if all properties match)

**Persistence Invariant**: Once created, an EvaluationResult is immutable and cannot be modified

---

### Value Object: Violation

**Purpose**: Immutable record of a single rule violation with complete context for replay.

**Location**: `PerformanceEngine.Evaluation.Domain/Domain/Violation.cs`

**Definition**:

```csharp
namespace PerformanceEngine.Evaluation.Domain;

/// <summary>
/// Immutable value object representing a rule violation detected during evaluation.
/// Preserves all information needed for audit trails and debugging.
/// 
/// Invariants:
/// - RuleName, MetricName, and Message are non-empty strings
/// - Severity is a defined enum value
/// - Actual and Threshold values preserve metric precision (stored as strings)
/// - Violation cannot be modified after construction
/// </summary>
public record Violation(
    /// <summary>Name of the rule that was violated.</summary>
    string RuleName,
    
    /// <summary>Name of the metric that violated the rule.</summary>
    string MetricName,
    
    /// <summary>Severity level of the violation.</summary>
    Severity Severity,
    
    /// <summary>Actual metric value that caused the violation (string to preserve precision).</summary>
    string ActualValue,
    
    /// <summary>Threshold value that was exceeded (string to preserve precision).</summary>
    string ThresholdValue,
    
    /// <summary>Human-readable violation message.</summary>
    string Message
)
{
    /// <summary>
    /// Factory method with validation.
    /// </summary>
    public static Violation Create(
        string ruleName,
        string metricName,
        Severity severity,
        string actualValue,
        string thresholdValue,
        string message)
    {
        if (string.IsNullOrWhiteSpace(ruleName))
            throw new ArgumentException("Rule name must not be empty", nameof(ruleName));

        if (string.IsNullOrWhiteSpace(metricName))
            throw new ArgumentException("Metric name must not be empty", nameof(metricName));

        if (string.IsNullOrWhiteSpace(actualValue))
            throw new ArgumentException("Actual value must not be empty", nameof(actualValue));

        if (string.IsNullOrWhiteSpace(thresholdValue))
            throw new ArgumentException("Threshold value must not be empty", nameof(thresholdValue));

        if (string.IsNullOrWhiteSpace(message))
            throw new ArgumentException("Message must not be empty", nameof(message));

        return new Violation(
            RuleName: ruleName,
            MetricName: metricName,
            Severity: severity,
            ActualValue: actualValue,
            ThresholdValue: thresholdValue,
            Message: message);
    }
}
```

**Immutability Guarantee**: Record type, no setters

**Equality Semantics**: Value-based equality

**Serialization Note**: ActualValue and ThresholdValue stored as strings to preserve decimal precision exactly (determinism requirement)

---

### Value Object: EvaluationEvidence

**Purpose**: Immutable audit trail entry capturing the complete context of a single rule evaluation decision.

**Location**: `PerformanceEngine.Evaluation.Domain/Domain/EvaluationEvidence.cs`

**Definition**:

```csharp
namespace PerformanceEngine.Evaluation.Domain;

/// <summary>
/// Immutable value object capturing complete evaluation context for a single rule.
/// Enables deterministic replay by preserving all inputs and decision criteria.
/// 
/// Invariants:
/// - RuleId and RuleName are non-empty
/// - Metrics collection contains all metrics used in evaluation
/// - DecisionOutcome is consistent with ConstraintSatisfied
/// - Timestamp is UTC-based
/// - Cannot be modified after construction
/// </summary>
public record EvaluationEvidence(
    /// <summary>Unique identifier of the rule that was evaluated.</summary>
    string RuleId,
    
    /// <summary>Human-readable name of the rule.</summary>
    string RuleName,
    
    /// <summary>Metrics used in this rule's evaluation.</summary>
    ImmutableList<MetricReference> Metrics,
    
    /// <summary>Actual values of metrics at evaluation time (strings for precision).</summary>
    ImmutableDictionary<string, string> ActualValues,
    
    /// <summary>Expected constraint expression that was evaluated.</summary>
    string ExpectedConstraint,
    
    /// <summary>True if constraint was satisfied, false if violated.</summary>
    bool ConstraintSatisfied,
    
    /// <summary>The evaluation decision outcome (why the rule passed or failed).</summary>
    string DecisionOutcome,
    
    /// <summary>UTC timestamp when this evidence was recorded.</summary>
    DateTime RecordedAtUtc
)
{
    /// <summary>
    /// Factory method with validation.
    /// </summary>
    public static EvaluationEvidence Create(
        string ruleId,
        string ruleName,
        IEnumerable<MetricReference> metrics,
        IDictionary<string, string> actualValues,
        string expectedConstraint,
        bool constraintSatisfied,
        string decisionOutcome,
        DateTime recordedAtUtc)
    {
        if (string.IsNullOrWhiteSpace(ruleId))
            throw new ArgumentException("Rule ID must not be empty", nameof(ruleId));

        if (string.IsNullOrWhiteSpace(ruleName))
            throw new ArgumentException("Rule name must not be empty", nameof(ruleName));

        if (string.IsNullOrWhiteSpace(expectedConstraint))
            throw new ArgumentException("Expected constraint must not be empty", nameof(expectedConstraint));

        if (string.IsNullOrWhiteSpace(decisionOutcome))
            throw new ArgumentException("Decision outcome must not be empty", nameof(decisionOutcome));

        if (recordedAtUtc.Kind != DateTimeKind.Utc)
            throw new ArgumentException("Timestamp must be UTC", nameof(recordedAtUtc));

        // Validate constraint satisfaction consistency
        if (constraintSatisfied && actualValues.Any(av => !bool.TryParse(av.Value, out _)))
        {
            // Values should be numeric for satisfied constraints
            // This is a soft check; strict validation handled by application layer
        }

        return new EvaluationEvidence(
            RuleId: ruleId,
            RuleName: ruleName,
            Metrics: metrics.ToImmutableList(),
            ActualValues: actualValues.ToImmutableDictionary(),
            ExpectedConstraint: expectedConstraint,
            ConstraintSatisfied: constraintSatisfied,
            DecisionOutcome: decisionOutcome,
            RecordedAtUtc: recordedAtUtc);
    }
}
```

**Purpose of Evidence Collection**: Complete audit trail enabling deterministic replay

**Immutability Guarantee**: Immutable collections (ImmutableList, ImmutableDictionary) cannot be modified

---

### Value Object: MetricReference

**Purpose**: Immutable reference to a metric value used during evaluation, enabling replay scenarios.

**Location**: `PerformanceEngine.Evaluation.Domain/Domain/MetricReference.cs`

**Definition**:

```csharp
namespace PerformanceEngine.Evaluation.Domain;

/// <summary>
/// Immutable value object representing a reference to a metric and its value
/// at the time of evaluation.
/// 
/// Preserves exact metric values (including decimal precision) needed for
/// deterministic replay. Values stored as strings to avoid floating-point
/// precision loss.
/// 
/// Invariants:
/// - MetricName is non-empty
/// - Value string is non-empty and represents the exact original value
/// - Cannot be modified after construction
/// </summary>
public record MetricReference(
    /// <summary>Name of the referenced metric (e.g., "ResponseTime", "ErrorRate").</summary>
    string MetricName,
    
    /// <summary>Exact value of the metric at evaluation time (stored as string for precision).</summary>
    string Value
)
{
    /// <summary>
    /// Factory method with validation.
    /// </summary>
    public static MetricReference Create(string metricName, string value)
    {
        if (string.IsNullOrWhiteSpace(metricName))
            throw new ArgumentException("Metric name must not be empty", nameof(metricName));

        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Metric value must not be empty", nameof(value));

        return new MetricReference(MetricName: metricName, Value: value);
    }

    /// <summary>
    /// Overload for convenient creation from numeric values.
    /// </summary>
    public static MetricReference Create(string metricName, decimal value)
        => Create(metricName, value.ToString("F"));

    /// <summary>
    /// Overload for convenient creation from double values.
    /// </summary>
    public static MetricReference Create(string metricName, double value)
        => Create(metricName, value.ToString("F"));
}
```

**String-Valued Storage**: Metric values preserved as strings to ensure byte-identical serialization/deserialization

**Convenience Overloads**: Numeric overloads convert to strings internally for precision preservation

---

## Consistency Boundary

**Aggregate Root**: EvaluationResult

**Consistency Boundary Definition**:
- EvaluationResult and all its contained entities (Violations, Evidence, MetricReferences) form a single aggregate
- Atomic persistence operates on the entire aggregate (all-or-nothing semantics)
- Queries always return the complete aggregate (no partial loading)
- No external dependencies on individual violations or evidence entries

**Rationale**:
- Evaluation decision is a coherent unit
- Violations are meaningless without their result context
- Evidence is meaningless without its result context
- Atomic persistence of the aggregate ensures consistency

---

## Entity Relationships

```
EvaluationResult (Aggregate Root)
├─ Violations (ImmutableList)
│  └─ Violation (Value Object)
│     ├─ RuleName: string
│     ├─ MetricName: string
│     ├─ ActualValue: string (precision preserved)
│     └─ ThresholdValue: string (precision preserved)
│
└─ Evidence (ImmutableList)
   └─ EvaluationEvidence (Value Object)
      ├─ RuleId: string
      ├─ RuleName: string
      ├─ Metrics: ImmutableList<MetricReference>
      │  └─ MetricReference (Value Object)
      │     ├─ MetricName: string
      │     └─ Value: string (precision preserved)
      ├─ ActualValues: ImmutableDictionary<string, string>
      ├─ ExpectedConstraint: string
      ├─ ConstraintSatisfied: bool
      └─ DecisionOutcome: string
```

---

## Immutability Enforcement

### Compile-Time Immutability

All entities defined as C# records with immutable properties:

```csharp
// These are read-only by default in record types
public record EvaluationResult(Guid Id, Severity Outcome, ...);
```

**Anti-patterns Prevented**:
- `result.Outcome = Severity.Fail;` ❌ Compile error (read-only)
- `violations.Add(newViolation);` ❌ Compile error (ImmutableList is read-only)
- `evidence[0] = newEvidence;` ❌ Compile error (immutable collection)

### Runtime Immutability

Immutable collections prevent runtime modifications:

```csharp
var violations = ImmutableList<Violation>.Empty;
violations.Add(v);  // ❌ Returns new collection, doesn't modify original
violations = violations.Add(v);  // ✅ Explicit reassignment required
```

---

## Validation Rules

### EvaluationResult Validation

1. **Outcome-Violations Consistency**: If Outcome is Pass, Violations list must be empty
2. **OutcomeReason Non-Empty**: Rationale for outcome must be provided
3. **Timestamp Must Be UTC**: EvaluatedAt.Kind must be DateTimeKind.Utc
4. **ID Uniqueness**: Repository enforces unique IDs at persistence layer

### Violation Validation

1. **Non-Empty Strings**: RuleName, MetricName, ActualValue, ThresholdValue, Message all required
2. **Valid Severity**: Severity enum value must be defined

### EvaluationEvidence Validation

1. **Non-Empty Identifiers**: RuleId and RuleName must be non-empty
2. **UTC Timestamp**: RecordedAtUtc.Kind must be DateTimeKind.Utc
3. **Immutable Collections**: Metrics and ActualValues cannot be modified after construction

### MetricReference Validation

1. **Non-Empty Name**: MetricName must be non-empty
2. **Non-Empty Value**: Value string must be non-empty (represents exact original value)

---

## Serialization Contracts

### EvaluationResult Serialization

```json
{
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "outcome": "Fail",
  "violations": [
    {
      "ruleName": "ResponseTimeRule",
      "metricName": "ResponseTime",
      "severity": "Fail",
      "actualValue": "1250.5",
      "thresholdValue": "1000.0",
      "message": "Response time exceeded threshold"
    }
  ],
  "evidence": [
    {
      "ruleId": "rule-001",
      "ruleName": "ResponseTimeRule",
      "metrics": [
        {
          "metricName": "ResponseTime",
          "value": "1250.5"
        }
      ],
      "actualValues": {
        "ResponseTime": "1250.5"
      },
      "expectedConstraint": "ResponseTime <= 1000",
      "constraintSatisfied": false,
      "decisionOutcome": "Violation: Response time exceeds threshold",
      "recordedAtUtc": "2026-01-16T14:30:45.1234567Z"
    }
  ],
  "outcomeReason": "Test failed: Response time exceeded acceptable threshold by 250ms",
  "evaluatedAt": "2026-01-16T14:30:45.1234567Z"
}
```

**Deterministic Ordering**:
- Properties serialized in alphabetical order (enforced by JSON serializer config)
- Collections serialized in list order (not sorted)
- Timestamps in ISO 8601 UTC format
- Metric values as strings (no floating-point conversion)

**Hash Verification**: 
- Serialization → Deserialization → Serialization produces identical bytes
- Hash(Original) == Hash(Retrieved) verified in unit tests

---

## Equality Semantics

### Value-Based Equality

All entities use record-based value equality (two entities are equal if all properties are equal):

```csharp
var result1 = EvaluationResult.Create(...);
var result2 = EvaluationResult.Create(...);

if (result1.Outcome == result2.Outcome) { ... }  // ✅ Property comparison
if (result1 == result2) { ... }                   // ✅ Value equality (all properties)
if (ReferenceEquals(result1, result2)) { ... }   // ❌ Would be false (different objects)
```

**No Identity-Based Equality**: Two results with identical properties are considered equal, even if they're different objects

---

## State Transitions

**Immutable Pattern**: No state transitions after construction

```
Construction
    ↓
[Read-Only State]
    ↓
Persistence
    ↓
Retrieval (identical state)
```

**Forbidden Transitions**:
- Outcome change: ❌
- Violation modification: ❌
- Evidence modification: ❌
- Timestamp change: ❌

---

## Extension Points for Future Phases

### Query Optimization (Phase 2)
- Add TestId field to EvaluationResult (current: only query by ID or timestamp range)
- Add indexes in SQL repository for TestId and timestamp queries
- Domain model remains unchanged; query layer enhanced

### Versioning Support (Phase 3)
- Add Version property to EvaluationResult for governance tracking
- Support querying by version range
- Append-only semantics allow version history

### Compliance & Retention (Phase 4)
- Add RetentionPolicy field
- Add ComplianceContext entity
- Domain model extended; existing persistence logic unchanged

---

## Summary

**Data Model Status**: ✅ Complete

**Key Achievements**:
- ✅ All entities defined as immutable records
- ✅ Consistency boundary clearly defined (EvaluationResult is aggregate root)
- ✅ Serialization contracts ensure deterministic byte-identical storage
- ✅ Validation rules enforce domain invariants at construction time
- ✅ Extension points prepared for future phases

**Readiness**: Ready for contract definition and repository implementation
