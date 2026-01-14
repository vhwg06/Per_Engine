# Evaluation Domain Specification

**Feature**: Evaluation Domain - Deterministic Rule Engine  
**Status**: Design Phase  
**Version**: 0.1.0  
**Last Updated**: January 14, 2026

---

## Executive Summary

The **Evaluation Domain** provides deterministic rule evaluation over performance metrics. It answers the question: **"Does this performance result meet our requirements, and why?"**

The domain establishes ubiquitous language for evaluation concepts (Rule, EvaluationResult, Violation, Severity) that are independent of persistence, reporting, or CI/CD exit codes. All evaluation logic is pure, deterministic, and immutable.

---

## Purpose & Goals

### Why This Domain?

**Problem**: Performance test results need objective evaluation against predefined criteria. Current ad-hoc evaluation approaches:
- Lack reproducibility (same results, different conclusions)
- Mix domain logic with infrastructure (CI exit codes, reporting formats)
- Cannot evolve rules without redeployment

**Solution**: A dedicated evaluation domain that:
- ✅ Evaluates metrics deterministically (byte-identical results)
- ✅ Operates on pure domain logic (no CI/CD or persistence dependencies)
- ✅ Supports extensible rules (strategic extension point)
- ✅ Produces clear violation reports

### Strategic Outcomes

| Outcome | How Achieved | Success Metric |
|---------|-------------|-----------------|
| **Deterministic evaluation** | Pure functions, immutable rules | Same metric + rule = same result every time |
| **Rule extensibility** | Strategy pattern for custom rules | New rule type added without modifying core |
| **Cross-engine evaluation** | Works with any metric (K6, JMeter, Gatling) | Same rule applies to all engines |
| **Clear failure diagnosis** | Structured violation reports | CI can provide detailed failure reasons |

---

## User Stories & Acceptance Criteria

### User Story 1: Evaluate Metrics Against Simple Rules
**Actor**: Performance Engineer  
**Priority**: P1 (MVP)

**Scenario**: A test produces a p95 latency metric. The engineer wants to know if it meets the SLO threshold.

```gherkin
Given a metric with p95 latency = 150ms
And a rule "p95 < 200ms"
When I evaluate the metric against the rule
Then the result is PASS
And no violations are recorded

Given a metric with p95 latency = 250ms
And a rule "p95 < 200ms"
When I evaluate the metric against the rule
Then the result is FAIL
And 1 violation is recorded: "p95 latency 250ms exceeds threshold 200ms"
```

**Acceptance Criteria**:
- FR-001: Rule evaluation produces deterministic outcome
- FR-002: Violation includes metric name, actual value, threshold
- FR-003: Outcome is one of: PASS, WARN, FAIL

---

### User Story 2: Evaluate Multiple Rules in Sequence
**Actor**: Performance Engineer  
**Priority**: P1 (MVP)

**Scenario**: A test produces multiple metrics. The engineer wants to evaluate all metrics against their corresponding rules in a single operation.

```gherkin
Given metrics:
  - p95 latency = 150ms
  - error rate = 0.5%
And rules:
  - "p95 < 200ms"
  - "error_rate < 1%"
When I evaluate all metrics against all rules
Then the result is PASS (both rules pass)
And violations list is empty

Given metrics:
  - p95 latency = 250ms
  - error rate = 2.5%
When I evaluate all metrics against all rules
Then the result is FAIL (at least one rule fails)
And violations list contains 2 violations
```

**Acceptance Criteria**:
- FR-004: Multiple rules evaluated in deterministic order
- FR-005: Overall outcome reflects worst violation severity
- FR-006: Violations list preserves all rule failures

---

### User Story 3: Support Custom Rule Types
**Actor**: System Architect  
**Priority**: P2

**Scenario**: The default rules (threshold-based) don't cover all cases. The architect wants to add custom rule types (e.g., trend analysis, statistical tests) without modifying the core evaluation engine.

```gherkin
Given a custom rule type "TrendAnalysis"
And implementation of custom rule
When evaluation engine encounters this rule
Then it evaluates the rule via strategy pattern
And result is properly integrated into overall evaluation outcome
```

**Acceptance Criteria**:
- FR-007: Rule interface supports custom implementations
- FR-008: Custom rules produce same result format (EvaluationResult)
- FR-009: Custom rules are evaluated deterministically

---

## Functional Requirements

| ID | Requirement | Rationale |
|----|-------------|-----------|
| **FR-001** | Rule evaluation is deterministic | Same metric + rule → identical result every run |
| **FR-002** | Violation includes: metric name, actual value, threshold, rule name | Clear diagnosis of failure reason |
| **FR-003** | EvaluationResult includes: outcome (PASS/WARN/FAIL), violations[], timestamp | Structured format for downstream use |
| **FR-004** | Multiple rules evaluated in stable, deterministic order | Reproducible batch evaluations |
| **FR-005** | Overall outcome reflects worst violation: FAIL > WARN > PASS | Intuitive severity escalation |
| **FR-006** | Rule evaluation does NOT mutate the metric | Immutability principle |
| **FR-007** | Rule interface supports custom implementations (strategy pattern) | Extensibility without core changes |
| **FR-008** | Evaluation domain does NOT depend on metrics domain | Clean architecture |
| **FR-009** | Evaluation results are immutable after creation | Thread-safe, shareable results |
| **FR-010** | Violation references the rule that caused it | Audit trail for debugging |
| **FR-011** | Rules support comparison operators: <, ≤, >, ≥, ==, != | Flexible threshold definitions |
| **FR-012** | Rules support logical operators: AND, OR, NOT | Complex rule composition |

---

## Success Criteria

| Criterion | Definition | How Verified |
|-----------|-----------|--------------|
| **SC-001** | Determinism | 1000 consecutive evaluations of same metric + rule produce identical results |
| **SC-002** | Engine independence | Same rule evaluates K6, JMeter, and Gatling metrics correctly |
| **SC-003** | Extensibility | New rule type added and integrated in <200 lines of code |
| **SC-004** | Performance | Evaluation of 100 rules over 10 metrics completes in <10ms |
| **SC-005** | Zero violations | No engine/persistence/CI references in domain layer |

---

## Domain Model

### Core Entities & Value Objects

```
Rule (interface, value-object-like)
├── Comparable rule instances
├── Immutable after creation
└── Multiple implementations:
    ├── ThresholdRule (e.g., p95 < 200ms)
    ├── RangeRule (e.g., 10% < error_rate < 20%)
    └── Custom implementations (extensible)

EvaluationResult (entity)
├── outcome: Severity (PASS, WARN, FAIL)
├── violations: ImmutableList<Violation>
├── evaluatedAt: DateTime
└── Immutable after creation

Violation (value object)
├── ruleId: string
├── metricName: string
├── actualValue: double
├── threshold: double
├── message: string
└── Immutable

Severity (enum)
├── PASS
├── WARN
└── FAIL

Evaluator (domain service)
├── EvaluateMetric(metric, rule) → EvaluationResult
├── EvaluateMultiple(metrics, rules) → List<EvaluationResult>
└── Pure functions, no side effects
```

### Architectural Constraints

- **No metrics dependency**: Domain cannot import from Metrics domain
- **No persistence**: No database or file operations
- **No CI/CD**: No exit codes, environment variables, or shell commands
- **No reporting**: No JSON/XML/HTML generation
- **Immutable results**: EvaluationResult cannot be modified after creation
- **Deterministic evaluation**: Identical inputs → byte-identical output

---

## Out of Scope

- ❌ **SLA syntax**: SLA formatting (YAML, DSL, etc.) - applies to Rule definitions, not domain
- ❌ **CI exit codes**: Process exit codes based on evaluation results
- ❌ **Reporting**: JSON reports, HTML dashboards, notifications
- ❌ **Persistence**: Saving/loading rules or evaluation results
- ❌ **Metrics consumption**: Details of how to parse Metric objects (Metrics domain responsibility)
- ❌ **Rule scheduling**: When/how often to evaluate rules (orchestration layer)

---

## Invariants & Rules

### Evaluation Invariants

1. **Determinism**: Same metric + rule set → same evaluation result (byte-identical)
2. **Immutability**: Evaluation results cannot be modified after creation
3. **Outcome consistency**: `outcome == FAIL` ⟹ `violations.Count > 0`
4. **No evaluation side effects**: Evaluating a rule must not modify the metric
5. **Severity escalation**: FAIL > WARN > PASS (worst outcome wins)

### Rule Invariants

- Rules must be comparable (two rules with same parameters are equivalent)
- Rules must be evaluable without external dependencies
- Custom rules must implement the Rule interface contract

### Violation Invariants

- Each violation references exactly one rule and one metric
- Violation message must clearly describe why the rule failed
- Violation includes both actual and expected values

---

## Technical Approach

### Design Principles

1. **Pure Functions**: All evaluation logic is deterministic and side-effect-free
2. **Strategy Pattern**: Rules are strategies; evaluator delegates to them
3. **Value Objects**: Rules and violations are value-object-like (immutable, comparable)
4. **Immutable Results**: EvaluationResult is immutable after creation
5. **Composition Over Inheritance**: Complex rules composed from simple rules

### Technology Stack

- **Language**: C# 13 (.NET 8.0)
- **Testing**: xUnit, FluentAssertions
- **Dependencies**: Metrics Domain (input), none else

### Dependency Graph

```
Evaluation Domain (depends on)
└── Metrics Domain (provides Metric interface)
    └── Domain foundations (ValueObject, DomainEvent, etc.)
```

No other domain depends on Evaluation Domain.

---

## Implementation Phases

### Phase 1: Foundation (4 tasks)
- [ ] Rule interface and basic implementations (Threshold, Range)
- [ ] Violation value object with immutability
- [ ] Severity enum and escalation logic
- [ ] Evaluator service (pure functions)

### Phase 2: Application Layer (3 tasks)
- [ ] EvaluationRequestDto and EvaluationResultDto
- [ ] EvaluationService application facade
- [ ] Use cases for single and batch evaluation

### Phase 3: Testing & Validation (4 tasks)
- [ ] Unit tests for all rule types
- [ ] Determinism tests (reproducibility)
- [ ] Integration tests with Metrics domain
- [ ] Cross-engine evaluation tests

### Phase 4: Documentation (2 tasks)
- [ ] README and quick start guide
- [ ] Architecture documentation

---

## Conformance to Constitutional Principles

**Specification-Driven Development**: ✅  
All implementation will be derived from this specification.

**Domain-Driven Design**: ✅  
Pure domain logic expressed in ubiquitous language (Rule, Violation, Severity).

**Clean Architecture**: ✅  
Evaluation domain has minimal dependencies; no infrastructure leakage.

**Determinism & Reproducibility**: ✅  
All evaluation is deterministic; same inputs always produce same outputs.

**Engine-Agnostic Abstraction**: ✅  
Rules work with any metrics from any engine.

**Evolution-Friendly Design**: ✅  
Strategy pattern enables extensibility without core changes.

---

## Acceptance Gates

- [ ] All user stories have passing tests
- [ ] 100+ unit tests covering all rule types
- [ ] Determinism tests pass 1000 consecutive runs
- [ ] Zero engine/persistence/CI references in domain
- [ ] Cross-engine evaluation verified
- [ ] Documentation complete (README, guides, examples)
