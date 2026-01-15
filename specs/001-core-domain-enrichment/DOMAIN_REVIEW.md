# Phase 1 T003: Domain Implementation Review & Documentation

**Date**: 2026-01-15  
**Purpose**: Document existing Metric, Evaluation, and Profile domain implementations to inform enrichment architecture decisions

---

## Executive Summary

All three domain projects (Metrics, Evaluation, Profile) have mature implementations with established patterns. Each domain follows clean architecture principles with clear separation of concerns. ValueObject patterns are partially implemented; the Profile domain has a base class, but Metrics and Evaluation domains need ValueObject base classes created.

**Status**: ✅ Ready for Phase 2 foundation work

---

## Metrics Domain Analysis

### Current Structure

**Location**: `src/PerformanceEngine.Metrics.Domain/Domain/Metrics/`

**Key Entities & Value Objects**:
1. **Metric** (entity) - Aggregated measurement representation
   - `Id: Guid`
   - `Samples: SampleCollection`
   - `Window: AggregationWindow`
   - `MetricType: string`
   - `ComputedAt: DateTime`
   - `AggregatedValues: IReadOnlyList<AggregationResult>`
   - Currently immutable; suitable for enrichment extension

2. **Sample** (entity) - Individual measurement
   - `Timestamp: DateTime`
   - `Value: double`
   - `Status: SampleStatus`
   - Status indicates if sample was valid/error/timeout

3. **SampleCollection** (aggregate) - Collection of samples
   - `IsEmpty: bool` property
   - `SampleCount: int` property
   - Enforces "no empty metrics" invariant

4. **AggregationWindow** (value object) - Time window for aggregation
   - Represents duration or sample count window
   - Immutable; comparable

5. **AggregationResult** (value object) - Aggregation computation output
   - Stores computed value with aggregation name
   - Examples: p50, p95, p99, mean, min, max

6. **Percentile** (value object) - Percentile specification
   - Value: 0-100
   - Used in aggregation definitions

7. **Latency** (value object) - Latency measurement
   - Value with unit (ms, sec, μs)
   - Unit conversion support

8. **LatencyUnit** (enum) - Unit specification
   - Values: Milliseconds, Seconds, Microseconds

9. **ErrorClassification** (enum) - Error categorization
   - Values: Unknown, Timeout, BadRequest, ServerError

10. **ExecutionContext** (value object) - Metadata context
    - Stores execution environment information

### Architectural Observations

✅ **Strengths**:
- Immutable design: All entities use init-only properties or records
- Value objects well-defined with equality-based comparison
- No null values in collections (SampleCollection enforces non-empty)
- Clear invariants (metrics cannot exist without samples)
- Sample status tracking for error handling

⚠️ **For Enrichment**:
- No ValueObject base class (unlike Profile domain) - Will create in Phase 2 T004
- Metric class can be extended with new properties (CompletessStatus, MetricEvidence)
- AggregationResult already captures aggregation name + value pattern needed for enrichment

### Enrichment Extension Points

1. **Metric.cs** - Add `CompletessStatus` and `MetricEvidence` properties
   - Current: 6 properties → New: 8 properties
   - Factory method: Add `sampleCount`, `requiredSampleCount`, `aggregationWindow` parameters
   - No modification to sample collection logic

2. **New: CompletessStatus.cs** - Enum for reliability marking
   - COMPLETE (all required samples)
   - PARTIAL (incomplete data)

3. **New: MetricEvidence.cs** - Value object for reliability metadata
   - SampleCount, RequiredSampleCount, AggregationWindow, IsComplete

---

## Evaluation Domain Analysis

### Current Structure

**Location**: `src/PerformanceEngine.Evaluation.Domain/Domain/`

**Key Entities & Value Objects**:
1. **EvaluationResult** (record aggregate)
   - `Outcome: Severity` (PASS, WARN, FAIL)
   - `Violations: ImmutableList<Violation>`
   - `EvaluatedAt: DateTime`
   - Factory methods: Pass(), Warning(), Fail(), FromViolations()

2. **Violation** (value object)
   - Records rule violations with metadata
   - Immutable

3. **Severity** (enum)
   - Values: PASS, WARN, FAIL

4. **Evaluator** (service)
   - Stateless evaluation orchestrator
   - Applies rules to metrics
   - Returns EvaluationResult

5. **IRule** (interface)
   - Strategy pattern for rule evaluation
   - Implementations: RangeRule, ThresholdRule, CompositeRule

6. **RuleFactory** (factory)
   - Creates rule instances

### Architectural Observations

✅ **Strengths**:
- Record-based immutability (C# 9+ feature)
- Clear separation of concerns (Rules vs Evaluator vs Results)
- Factory methods for different result types
- IRule strategy pattern enables rule extensibility
- Determinism-friendly: No non-deterministic operations observed

⚠️ **For Enrichment**:
- No ValueObject base class - Will create in Phase 2 T005
- Severity enum needs extension with INCONCLUSIVE value
- EvaluationResult needs Evidence field
- Evaluator service needs determinism guarantees (sorted violations)

### Enrichment Extension Points

1. **Outcome/Severity** - Extend enum from 3 values (PASS, WARN, FAIL) to 4 (add INCONCLUSIVE)
   - Backward compatible: INCONCLUSIVE is new path
   - Existing PASS/FAIL logic unchanged

2. **EvaluationResult.cs** - Add Evidence field
   - New: `Evidence: EvaluationEvidence`
   - New: `OutcomeReason: string`
   - Factory method: CreateInconclusive()

3. **New: EvaluationEvidence.cs** - Value object for decision trail
   - RuleId, RuleName, MetricsUsed, ActualValues, ExpectedConstraint, ConstraintSatisfied, Decision, EvaluatedAt

4. **New: MetricReference.cs** - Value object for metric references in evidence
   - AggregationName, Value, Unit, CompletessStatus

5. **Evaluator.cs** - Update logic for evidence capture + determinism guarantee
   - Sort violations by (RuleId, MetricName) for determinism
   - Capture DateTime.UtcNow once at evaluation start
   - Build EvaluationEvidence with all details

---

## Profile Domain Analysis

### Current Structure

**Location**: `src/PerformanceEngine.Profile.Domain/Domain/`

**Key Entities & Value Objects**:
1. **Profile** (aggregate)
   - Container for configuration overrides
   - Methods: Apply scope-based overrides, resolve final configuration

2. **ResolvedProfile** (value object)
   - Immutable representation of resolved profile state
   - Contains final configuration values

3. **Scopes** - Hierarchy of override contexts
   - **IScope** (interface) - Base scope abstraction
   - **GlobalScope** - Global settings (priority 1)
   - **ApiScope** - API-specific settings (priority 2)
   - **EnvironmentScope** - Environment-specific settings
   - **TagScope** - Tag-based scoping
   - **CompositeScope** - Multiple scopes combined

4. **Configuration** - Configuration management
   - **ConfigKey** - Key identifier
   - **ConfigValue** - Value container with type
   - **ConfigType** - Type specification
   - **ConflictHandler** - Handles resolution conflicts

5. **ProfileResolver** (service)
   - Already exists! Performs profile resolution
   - Applies overrides in scope order
   - Returns resolved configuration

6. **ValueObject** (base class)
   - Abstract record base
   - Enables value-based equality

### Architectural Observations

✅ **Strengths**:
- ValueObject base class already established (unlike other domains)
- Scope hierarchy clearly defined with priorities
- ProfileResolver service already implements resolution logic
- ConflictHandler manages scope conflicts
- Immutable resolved profiles

⚠️ **For Enrichment**:
- No state machine enforcement (Unresolved → Resolved → Invalid)
- Current ProfileResolver may need determinism verification
- No validation gates before evaluation use
- No explicit immutability enforcement post-resolution

### Enrichment Extension Points

1. **Profile.cs** - Add state machine
   - New: `State: ProfileState` property
   - Gate ApplyOverride() to Unresolved state only
   - Gate Get() to Resolved state only

2. **New: ProfileState.cs** - Enum for lifecycle states
   - Unresolved (accepting overrides)
   - Resolved (immutable)
   - Invalid (failed validation)

3. **ProfileResolver.cs** - Verify/ensure determinism guarantee
   - Sort overrides by (scope priority DESC, key ASC)
   - Test with 1000+ iterations for byte-identical output
   - Ensure no runtime context dependency

4. **New: IProfileValidator.cs** - Port for validation
   - ValidationResult Validate(Profile)
   - Enables pluggable validators

5. **New: ProfileValidator.cs** - Implementation
   - Check circular override dependencies
   - Validate required keys present
   - Type correctness
   - Scope validity
   - Range constraints

6. **New: ValidationError.cs** - Value object for errors
7. **New: ValidationResult.cs** - Value object for validation results

---

## Cross-Domain Analysis

### Immutability Pattern Consistency

| Domain | Pattern | Status |
|--------|---------|--------|
| Metrics | Record + Init properties | ✅ Consistent |
| Evaluation | C# Record | ✅ Consistent |
| Profile | Abstract record ValueObject | ✅ Consistent |

**Recommendation**: Ensure all new value objects use C# records with init-only properties for consistency.

### Error Handling Pattern

| Domain | Approach | Status |
|--------|----------|--------|
| Metrics | Exceptions for invariant violations | ✅ Consistent |
| Evaluation | Result type for violations | ⚠️ Different (intentional) |
| Profile | Exceptions for configuration errors | ✅ Consistent |

**Recommendation**: Evaluation domain appropriately uses Result type for domain violations (that's the pattern). Domain layer throws exceptions for invariant violations (correct).

### Testing Structure

**Existing**:
- Unit tests in `tests/PerformanceEngine.*.Domain.Tests/`
- Tests organized by layer (Domain, Application, Integration)
- No determinism test utilities yet

**For Enrichment**:
- Will create determinism test utilities in Phase 2 (T007-T009)
- 1000+ iteration tests for all enriched entities
- Byte-identical JSON serialization verification

---

## Readiness Assessment

### ✅ Ready for Phase 2 Foundation Work

**Pre-requisites Met**:
1. Solution structure verified (3 domain projects + 3 test projects)
2. Existing implementations reviewed and documented
3. ValueObject pattern identified (profile has base; metrics/evaluation need creation)
4. Extension points clearly mapped
5. Architecture principles aligned with enrichment design

### Foundation Work (Phase 2) To-Dos

**T004-T006**: Create ValueObject base classes
- **Metrics Domain**: Abstract base class similar to Profile
- **Evaluation Domain**: Abstract base class similar to Profile
- **Profile Domain**: Already has; verify consistency

**T007-T009**: Create determinism test utilities
- Pattern: Run operation N times (1000+), verify identical outputs
- Serialize to JSON, compare byte-for-byte
- Support for metrics, evaluation, profile entities

**T010**: Create shared test fixtures
- Test doubles for metrics
- Test doubles for evaluation rules
- Test doubles for profiles

---

## Implementation Recommendations

### 1. Shared ValueObject Base Class (Optional)

Consider creating a shared `ValueObject` abstract base in a common location:

```csharp
namespace PerformanceEngine.Domain.Common;

public abstract record ValueObject
{
    // Optional: Override GetHashCode() for deterministic hashing if needed
}
```

**Decision**: ✅ Create domain-specific base classes (current approach) to maintain independence

### 2. Enrichment Extension Backward Compatibility

All extensions should be backward compatible:
- New properties on existing entities: Use init-only accessors
- New enum values: Add without removing existing values
- New factory methods: Create alternatives; keep existing constructors

**Status**: ✅ All planned extensions maintain backward compatibility

### 3. Determinism Test Pattern

Establish standard determinism test pattern:

```csharp
[Fact]
public void CreateMetric_Determinism_1000Iterations_Identical()
{
    var metric = new Metric(...);
    var results = new List<string>();
    
    for (int i = 0; i < 1000; i++)
    {
        var copy = new Metric(...);
        results.Add(JsonSerializer.Serialize(copy));
    }
    
    var firstJson = results[0];
    Assert.All(results, json => Assert.Equal(firstJson, json));
}
```

**Status**: ✅ Will implement in Phase 2 T007-T009

---

## Conclusion

All three domains are architecturally sound and ready for enrichment extensions. The implementations follow clean architecture principles, maintain immutability, and are well-structured for the planned enrichments. Phase 2 foundation work will establish ValueObject base classes and determinism test utilities, enabling parallel implementation of User Stories 1-5 (Phases 3-7).

**Next Step**: Proceed to Phase 2: Foundational work (T004-T010)
