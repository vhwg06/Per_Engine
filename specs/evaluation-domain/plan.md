# Implementation Plan: Evaluation Domain

**Branch**: `evaluation-domain-implementation` | **Date**: 2026-01-14 | **Spec**: [evaluation-domain.spec.md](spec.md)  
**Input**: Feature specification from `/specs/evaluation-domain/spec.md`

---

## Summary

The Evaluation Domain implements a **deterministic rule engine** for performance metric evaluation. It provides pure, side-effect-free logic to evaluate metrics against extensible rules (ThresholdRule, RangeRule, custom implementations) and produce structured violation reports. Results are immutable and byte-identical for identical inputs, enabling automated CI/CD quality gates.

The domain operates independently of metrics infrastructure (K6, JMeter, Gatling) through clean architecture; it consumes a Metric interface and produces EvaluationResult entities. All evaluation is deterministic, order-independent, and supports extensible rule types via strategy pattern.

---

## Technical Context

**Language/Version**: C# 13 (.NET 10.0 LTS)  
**Primary Dependencies**: PerformanceEngine.Metrics.Domain (metrics models), xUnit & FluentAssertions (testing)  
**Storage**: N/A (in-memory, immutable domain models)  
**Testing**: xUnit with determinism test harness for 1000+ reproducibility runs  
**Target Platform**: .NET 10 runtime (.NET Standard 2.1 compatible for cross-platform support)  
**Project Type**: Single domain library (Clean Architecture layered structure)  
**Performance Goals**: Evaluate 100 rules over 10 metrics in <10ms  
**Constraints**: Deterministic byte-identical output; immutable results; zero persistence/infrastructure dependencies  
**Scale/Scope**: Foundation domain supporting batch evaluation and extensible rule strategies; estimated 2000-3000 LOC core domain  

---

## Constitution Check

✅ **Specification-Driven Development**:
- Feature defined through explicit specification (evaluation-domain.spec.md)
- Specification version-controlled and precedes all implementation

✅ **Domain-Driven Design**:
- Domain models (Rule, EvaluationResult, Violation, Severity) independent of execution engines, persistence, CI/CD
- Core logic in ubiquitous language: evaluate metrics deterministically against rules
- Extensibility via strategy pattern for custom rule implementations

✅ **Clean Architecture**:
- Dependencies flow inward: Domain ← Application (use cases) ← Adapters ← Infrastructure
- Evaluation domain has minimal inward dependencies (only Metrics Domain for Metric interface)
- No infrastructure imports in domain/application layers; persistence/CI logic separated

✅ **Layered Phase Independence**:
- Clear layer boundaries: Domain (rules, violations) → Application (evaluation orchestration) → Infrastructure (if needed: result persistence)
- Phases communicate through serializable DTOs and domain models
- Rule evaluation independent of how results are stored or reported

✅ **Determinism & Reproducibility**:
- Identical metric + rule set → byte-identical evaluation result every time
- All rule comparisons and outcome escalation fully deterministic
- No timestamps, randomness, or ordering ambiguity in evaluation logic

✅ **Engine-Agnostic Abstraction**:
- Evaluation logic operates on domain Metric interface, not engine-specific formats
- Rules are engine-agnostic (p95, error_rate, percentile work with K6, JMeter, Gatling)
- No execution engine data structures or APIs leak into domain

✅ **Evolution-Friendly Design**:
- No specific technology locked (C# version, database, framework are plan-level decisions, not constitutional)
- Custom rules extend via Strategy pattern without modifying core evaluator
- Future integration with Profile Domain, Baseline Domain, etc. planned as separate bounded contexts

---

## Project Structure

### Documentation (this feature)

```text
specs/evaluation-domain/
├── spec.md              # Feature specification
├── plan.md              # This file (implementation plan)
├── research.md          # Phase 0: Technology decisions + best practices
├── data-model.md        # Phase 1: Domain entities, value objects, services
├── quickstart.md        # Phase 1: Developer quick start guide
├── contracts/           # Phase 1: Domain-level interface contracts
│   ├── rule-interface.md
│   ├── evaluator-interface.md
│   └── domain-events.md
└── tasks.md             # Phase 2+: Task breakdown (generated later)
```

### Source Code (repository root)

```text
src/
├── PerformanceEngine.Metrics.Domain/     # Existing: foundation domain
├── PerformanceEngine.Evaluation.Domain/  # NEW: evaluation domain
│   ├── Domain/
│   │   ├── Rules/
│   │   │   ├── IRule.cs                  # Rule interface (strategy pattern)
│   │   │   ├── ThresholdRule.cs          # Simple threshold: metric < threshold
│   │   │   ├── RangeRule.cs              # Range: min < metric < max
│   │   │   └── RuleComparison.cs         # Rule equality/comparison utilities
│   │   │
│   │   ├── Evaluation/
│   │   │   ├── EvaluationResult.cs       # Immutable result entity
│   │   │   ├── Violation.cs              # Immutable violation value object
│   │   │   ├── Severity.cs               # Enum: PASS, WARN, FAIL
│   │   │   └── Evaluator.cs              # Domain service: pure evaluation logic
│   │   │
│   │   └── Events/
│   │       └── MetricEvaluatedEvent.cs   # Domain event (optional)
│   │
│   ├── Application/
│   │   ├── Services/
│   │   │   └── EvaluationService.cs      # Application facade
│   │   │
│   │   ├── Dto/
│   │   │   ├── RuleDto.cs
│   │   │   ├── EvaluationRequestDto.cs
│   │   │   └── EvaluationResultDto.cs
│   │   │
│   │   └── UseCases/
│   │       ├── EvaluateSingleMetricUseCase.cs
│   │       └── EvaluateMultipleMetricsUseCase.cs
│   │
│   └── Ports/
│       └── IPersistenceRepository.cs     # (Optional: for result storage)
│
tests/
├── PerformanceEngine.Evaluation.Domain.Tests/
│   ├── Domain/
│   │   ├── Rules/
│   │   │   ├── ThresholdRuleTests.cs
│   │   │   ├── RangeRuleTests.cs
│   │   │   └── CustomRuleTests.cs        # Examples of extensibility
│   │   │
│   │   ├── Evaluation/
│   │   │   ├── EvaluatorTests.cs         # Core evaluation logic
│   │   │   ├── ViolationInvariantTests.cs
│   │   │   ├── DeterminismTests.cs       # 1000 consecutive runs identical
│   │   │   └── SeverityEscalationTests.cs
│   │   │
│   │   └── Integration/
│   │       └── MetricsDomainIntegrationTests.cs
│   │
│   └── Application/
│       ├── EvaluationServiceTests.cs
│       └── UseCaseTests.cs
```

**Structure Decision**: Clean Architecture with Domain-Driven Design. Single domain library (PerformanceEngine.Evaluation.Domain) following existing Metrics Domain pattern. Separated into Domain (pure logic) → Application (orchestration) → Ports (infrastructure boundaries). Tests mirror source structure with determinism test harness for reproducibility verification.
│       ├── RuleDto.cs
│       └── ViolationDto.cs
│
└── Ports/
    └── IRuleRepository.cs (deferred)

tests/PerformanceEngine.Evaluation.Domain.Tests/
├── Domain/
│   ├── RuleTests.cs
│   ├── EvaluationTests.cs
│   └── ViolationTests.cs
├── Rules/
│   ├── ThresholdRuleTests.cs
│   ├── RangeRuleTests.cs
│   └── CustomRuleTests.cs
└── Integration/
    ├── EvaluationServiceTests.cs
    └── CrossEngineEvaluationTests.cs
```

---

## Architecture Overview

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

**Rule**: A condition that can be evaluated against a metric. Implementations:
- `ThresholdRule`: Single comparison (p95 < 200ms)
- `RangeRule`: Range constraint (10% < error_rate < 20%)
- Custom implementations via `IRule` interface

**EvaluationResult**: Immutable result of evaluating a rule against a metric:
- `outcome`: PASS, WARN, or FAIL
- `violations`: List of all rule failures
- `evaluatedAt`: Timestamp

**Violation**: Represents a single rule failure:
- `ruleId`: Identifier of failing rule
- `metricName`: Which metric failed
- `actualValue`: What we measured
- `threshold`: What was expected
- `message`: Human-readable explanation

**Evaluator**: Pure function that performs evaluation (domain service, not entity):
- `EvaluateMetric(metric, rule) → EvaluationResult`
- `EvaluateMultiple(metrics, rules) → List<EvaluationResult>`

---

## Implementation Phases

### Phase 1: Domain Foundations (4 tasks)

**Purpose**: Core evaluation logic

#### Task 1.1: Create Rule Interface & Base Implementations
- `IRule` interface (method: `Evaluate(Metric) → bool`)
- `ThresholdRule` (p95 < 200ms pattern)
- `RangeRule` (10% < error_rate < 20% pattern)
- Unit tests for each rule type

#### Task 1.2: Create Violation Value Object
- Properties: `ruleId`, `metricName`, `actualValue`, `threshold`, `message`
- Immutable construction with validation
- Equals/GetHashCode implementation
- Unit tests for immutability and equality

#### Task 1.3: Create Severity Enum & Escalation Logic
- `Severity` enum: `PASS`, `WARN`, `FAIL`
- Escalation function: `Escalate(Severity, Severity) → Severity`
- Rule: FAIL > WARN > PASS
- Unit tests

#### Task 1.4: Create Evaluator Service & EvaluationResult
- `EvaluationResult` entity with immutable properties
- `Evaluator` service with deterministic evaluation logic
- Methods:
  - `EvaluateMetric(metric, rule) → EvaluationResult`
  - `EvaluateMultiple(metrics, rules) → List<EvaluationResult>`
- Determinism tests (1000+ runs, identical results)

---

### Phase 2: Application Layer (3 tasks)

**Purpose**: Service facade and data transfer

#### Task 2.1: Create DTOs & Mapping
- `RuleDto`: Serializable rule representation
- `EvaluationResultDto`: Result transfer object
- `ViolationDto`: Violation transfer object
- Bidirectional mapping (domain ↔ DTO)

#### Task 2.2: Create Use Cases
- `EvaluateSingleRuleUseCase`: Evaluate one metric against one rule
- `EvaluateBatchRulesUseCase`: Evaluate multiple metrics against multiple rules
- `ValidateRuleUseCase`: Validate rule syntax before evaluation

#### Task 2.3: Create EvaluationService Application Facade
- Public methods:
  - `Evaluate(EvaluationRequestDto) → EvaluationResultDto`
  - `EvaluateBatch(List<EvaluationRequestDto>) → List<EvaluationResultDto>`
- Error handling and graceful degradation
- Integration with use cases

---

### Phase 3: Testing & Validation (4 tasks)

**Purpose**: Comprehensive test coverage and verification

#### Task 3.1: Unit Tests - Rules
- `ThresholdRuleTests`: Various comparison operators (<, ≤, >, ≥, ==, !=)
- `RangeRuleTests`: Lower and upper bounds
- `CustomRuleTests`: Strategy pattern verification
- Coverage: 20+ tests

#### Task 3.2: Determinism & Cross-Engine Tests
- `DeterminismTests`: 1000+ consecutive evaluations produce identical results
- `CrossEngineEvaluationTests`: Same rule evaluates K6, JMeter, Gatling metrics
- Edge cases: boundary values, null handling, extreme metrics

#### Task 3.3: Integration Tests
- `EvaluationServiceTests`: End-to-end via service facade
- `RuleCompositionTests`: Complex rules with AND/OR logic
- `BatchEvaluationTests`: Multiple rules and metrics

#### Task 3.4: Architecture Compliance Tests
- Verify no metrics domain references leak into results
- Verify immutability of all entities
- Verify determinism across different machines

---

### Phase 4: Documentation (2 tasks)

**Purpose**: Guides and API documentation

#### Task 4.1: Create README & Quick Start
- Architecture overview
- Quick start: evaluate a single metric
- Rule type examples
- Extension guide (custom rules)

#### Task 4.2: Create Implementation Guide
- Step-by-step walkthrough (similar to Metrics domain)
- Complete code examples
- Rule template for new types
- Testing strategy

---

## Task List (Detailed)

```markdown
# Evaluation Domain Tasks (13 total)

## Phase 1: Domain Foundations (4 tasks)

- [ ] T001 Create Rule interface: `src/Domain/Rules/IRule.cs`
- [ ] T002 Create rule implementations: `ThresholdRule`, `RangeRule`
- [ ] T003 Create Violation value object: `src/Domain/Evaluation/Violation.cs`
- [ ] T004 Create Evaluator & EvaluationResult: `src/Domain/Evaluation/Evaluator.cs`

## Phase 2: Application Layer (3 tasks)

- [ ] T005 Create DTOs: `RuleDto`, `EvaluationResultDto`, `ViolationDto`
- [ ] T006 Create use cases: `EvaluateSingleRuleUseCase`, etc.
- [ ] T007 Create EvaluationService facade: `src/Application/Services/EvaluationService.cs`

## Phase 3: Testing (4 tasks)

- [ ] T008 Create rule unit tests (ThresholdRule, RangeRule)
- [ ] T009 Create determinism and cross-engine tests
- [ ] T010 Create integration tests
- [ ] T011 Create architecture compliance tests

## Phase 4: Documentation (2 tasks)

- [ ] T012 Create README.md and quick start
- [ ] T013 Create IMPLEMENTATION_GUIDE.md
```

---

## Testing Strategy

### Test Pyramid

```
         ┌─────────┐
         │Integration  │ 15 tests
      ┌──┴────────┴──┐
      │  Contract    │  25 tests
   ┌──┴─────────────┴──┐
   │    Unit Tests      │  80 tests
   └────────────────────┘
```

### Determinism Testing

Critical for evaluation domain:

```csharp
[Fact]
public void ThresholdRule_ProducesDeterministicResults()
{
    var rule = new ThresholdRule("p95", 200.0, ComparisonOperator.LessThan);
    var metric = CreateMetricWithP95(150.0);
    
    var results = new HashSet<bool>();
    for (int i = 0; i < 1000; i++)
    {
        var result = rule.Evaluate(metric);
        results.Add(result);
    }
    
    // MUST be single result (all identical)
    Assert.Single(results);
    Assert.True(results.First());
}
```

### Cross-Engine Testing

Verify rules work with metrics from any engine:

```csharp
[Fact]
public void Rule_EvaluatesEquivalentlyAcrossEngines()
{
    var rule = new ThresholdRule("p95", 200.0, ComparisonOperator.LessThan);
    
    var k6Metric = CreateK6Metric(p95: 150.0);
    var jmeterMetric = CreateJMeterMetric(p95: 150.0);
    
    var k6Result = rule.Evaluate(k6Metric);
    var jmeterResult = rule.Evaluate(jmeterMetric);
    
    Assert.Equal(k6Result, jmeterResult);
}
```

---

## Constitutional Compliance

### Specification-Driven Development ✅
- Specification precedes implementation
- All tasks derived from functional requirements

### Domain-Driven Design ✅
- Pure domain logic (Rule, Violation, Evaluator)
- Ubiquitous language (Severity, EvaluationResult)
- Aggregate structure: EvaluationResult is root

### Clean Architecture ✅
- Evaluation domain has no infrastructure dependencies
- One-way dependency: Evaluation → Metrics (only for input)
- Application layer orchestrates domain logic

### Determinism & Reproducibility ✅
- No randomness, timestamps, or external dependencies
- Identical inputs → byte-identical outputs
- Critical for reproducible evaluations

### Engine-Agnostic Design ✅
- Rules work with any metric, any engine
- No K6/JMeter/Gatling-specific code in domain

### Evolution-Friendly ✅
- Strategy pattern for custom rule types
- New rules added without modifying core
- Open/closed principle enforced

---

## Success Criteria

- ✅ All 13 tasks completed
- ✅ 120+ tests passing
- ✅ Determinism verified (1000+ identical runs)
- ✅ Cross-engine evaluation working
- ✅ Zero infrastructure dependencies
- ✅ Documentation complete

---

## Timeline Estimate

- **Phase 1**: 2-3 days (domain foundations)
- **Phase 2**: 1-2 days (application layer)
- **Phase 3**: 2-3 days (testing)
- **Phase 4**: 1 day (documentation)

**Total**: 6-9 days

---

## References

- **Specification**: [spec.md](spec.md)
- **Metrics Domain**: ../metrics-domain/
- **Constitution**: docs/coding-rules/constitution.md
