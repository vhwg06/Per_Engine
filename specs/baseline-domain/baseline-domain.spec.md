# Baseline Domain Specification

**Feature**: Baseline Domain - Performance Result Comparison & Regression Detection  
**Status**: Design Phase  
**Version**: 0.1.0  
**Created**: January 15, 2026  
**Input**: Domain concept document

---

## Executive Summary

The **Baseline Domain** enables performance engineers to compare current test results against a previous established baseline to detect performance **regressions**, **improvements**, and **instability**. 

A baseline is an immutable snapshot of metrics and evaluation results from a specific test run, designated as the reference point. The domain provides deterministic comparison logic to evaluate whether current results represent meaningful changes from the baseline, using configurable tolerance thresholds and confidence levels to distinguish signal from noise.

---

## Purpose & Goals

### Why This Domain?

**Problem**: Performance test results vary run-to-run due to environmental factors, infrastructure differences, and natural measurement variance. Teams need to:
- Detect genuine performance regressions (not just normal variance)
- Avoid false positives that trigger unnecessary alarms
- Quantify improvements reliably
- Understand when variance is too high to draw conclusions

**Solution**: A dedicated baseline domain that:
- ✅ Compares current results deterministically against established baselines
- ✅ Supports configurable tolerance thresholds (absolute and relative)
- ✅ Produces clear comparison outcomes (regression/improvement/no change/inconclusive)
- ✅ Provides confidence assessments based on comparison magnitude
- ✅ Remains immutable (baselines cannot be modified)

### Strategic Outcomes

| Outcome | How Achieved | Success Metric |
|---------|-------------|-----------------|
| **Regression detection** | Deterministic comparison logic | Regressions detected vs. noise distinguished 95% of the time |
| **Immutable baselines** | Baseline version control | Audit trail preserved; baselines not retroactively modified |
| **Flexible tolerance** | Configurable threshold strategy | Teams can tune sensitivity for their context |
| **Reliable insights** | Clear outcome states | CI/CD can make automated decisions with high confidence |

---

## User Stories & Acceptance Criteria

### User Story 1: Establish a Baseline from Test Results (Priority: P1)

**Actor**: Performance Engineer / Release Engineer  
**Scenario**: After a test run produces metrics and evaluations, the engineer designates these results as the new performance baseline for future comparisons.

**Why this priority**: Foundation feature—all other baseline operations depend on having an established baseline to compare against.

**Independent Test**: Can verify by confirming a set of metrics/evaluations can be captured and stored as immutable baseline snapshot.

**Acceptance Scenarios**:

1. **Given** a test execution with recorded metrics and evaluation results, **When** I create a baseline from these results, **Then** the baseline is created with:
   - Snapshot timestamp
   - Complete metrics collection
   - Complete evaluation results
   - Immutable state (cannot be modified)

2. **Given** an established baseline, **When** I attempt to modify it, **Then** the operation fails (immutability enforced)

---

### User Story 2: Compare Current Results Against Baseline (Priority: P1)

**Actor**: Performance Engineer  
**Scenario**: After a new test run, the engineer wants to see how current results compare to the established baseline.

**Why this priority**: Core value delivery—enables regression detection and performance trend analysis.

**Independent Test**: Can verify by comparing two sets of metrics and confirming deterministic comparison outcome.

**Acceptance Scenarios**:

1. **Given** a baseline with known metrics (p95 latency = 150ms), **When** I compare against current results with identical p95 latency (150ms), **Then** comparison result is NO_SIGNIFICANT_CHANGE

2. **Given** a baseline with p95 latency = 150ms and tolerance = ±5%, **When** I compare against current p95 = 156ms, **Then** comparison result is NO_SIGNIFICANT_CHANGE

3. **Given** a baseline with p95 latency = 150ms and tolerance = ±5%, **When** I compare against current p95 = 200ms, **Then** comparison result is REGRESSION

4. **Given** a baseline with p95 latency = 150ms and tolerance = ±5%, **When** I compare against current p95 = 120ms, **Then** comparison result is IMPROVEMENT

---

### User Story 3: Handle Inconclusive Results When Variance is High (Priority: P1)

**Actor**: DevOps Engineer  
**Scenario**: Results show change magnitude that exceeds configured confidence threshold, making conclusions unreliable.

**Why this priority**: Prevents false alarms in high-variance environments; ensures only high-confidence decisions trigger automated actions.

**Independent Test**: Can verify by testing comparison scenarios where variance indicates insufficient confidence.

**Acceptance Scenarios**:

1. **Given** a comparison with confidence level < minimum threshold (e.g., 0.6), **When** I evaluate the comparison, **Then** the result is INCONCLUSIVE (not REGRESSION or IMPROVEMENT)

2. **Given** baseline metrics with high inherent variance, **When** comparing against results within ±20% variance band, **Then** the result is INCONCLUSIVE

---

### User Story 4: Apply Tolerance-Based Thresholds (Priority: P2)

**Actor**: Performance Engineer  
**Scenario**: The engineer configures what counts as a significant change (e.g., ±10% relative, or ±50ms absolute).

**Why this priority**: Enables context-specific sensitivity; different services/metrics have different acceptable variance ranges.

**Independent Test**: Can verify by applying different tolerance configurations and confirming results change appropriately.

**Acceptance Scenarios**:

1. **Given** tolerance type = RELATIVE (±10%), baseline p95 = 150ms, **When** comparing current p95 = 165ms, **Then** result is NO_SIGNIFICANT_CHANGE (within ±10%)

2. **Given** tolerance type = ABSOLUTE (±20ms), baseline p95 = 150ms, **When** comparing current p95 = 175ms, **Then** result is REGRESSION (exceeds +20ms tolerance)

3. **Given** tolerance type = RELATIVE, baseline error_rate = 0.5%, **When** comparing current error_rate = 0.55%, **Then** result respects percentage-based calculation

---

### User Story 5: Compare Multi-Metric Results (Priority: P2)

**Actor**: Performance Engineer  
**Scenario**: A test produces multiple metrics (latency, throughput, error rate). The engineer wants an overall comparison result reflecting all metrics.

**Why this priority**: Real-world tests always produce multiple metrics; single metric comparison insufficient.

**Independent Test**: Can verify by comparing baseline with multiple metrics against current results with multiple metrics.

**Acceptance Scenarios**:

1. **Given** baseline with metrics: p95=150ms, error_rate=0.5%, throughput=1000req/s; all within tolerance, **When** I compare with identical current metrics, **Then** overall result is NO_SIGNIFICANT_CHANGE

2. **Given** baseline with metrics: p95=150ms, error_rate=0.5%; **When** comparing against current where p95 is REGRESSION but error_rate is NO_SIGNIFICANT_CHANGE, **Then** overall result is REGRESSION (worst outcome)

---

### Edge Cases

- What happens when baseline contains a metric that current results don't include?
- How is comparison handled when a new metric appears in current results that wasn't in baseline?
- What happens when comparing results from different engines/test configurations?
- How are null/missing values treated in tolerance calculations?
- What if tolerance is configured as 0% (exact match required)?

---

## Requirements *(mandatory)*

### Functional Requirements

#### Core Baseline Concepts

**FR-001**: System MUST define **Baseline** as an immutable snapshot containing:
- Reference timestamp (when baseline was established)
- Complete metrics collection (all metrics from that execution)
- Complete evaluation results
- Tolerance configuration
- Constraint: Baseline must be immutable after creation

**FR-002**: System MUST define **Comparison** as a deterministic operation comparing current results against a baseline with:
- Deterministic outcome (same inputs always produce same result)
- Support for metric-by-metric comparison
- Support for aggregate comparison result
- No dependency on system state or timing

**FR-003**: System MUST define **ComparisonResult** with four possible states:
- **IMPROVEMENT**: Current metrics better than baseline by more than tolerance
- **REGRESSION**: Current metrics worse than baseline by more than tolerance
- **NO_SIGNIFICANT_CHANGE**: Current metrics within tolerance of baseline
- **INCONCLUSIVE**: Variance or confidence insufficient to determine meaningful change

**FR-004**: System MUST define **Tolerance** configuration supporting:
- **RELATIVE** tolerance (e.g., ±10% relative change)
- **ABSOLUTE** tolerance (e.g., ±50ms absolute change)
- Per-metric tolerance rules
- Constraint: Tolerance values must be non-negative

**FR-005**: System MUST define **Confidence** level as:
- A value [0.0, 1.0] representing certainty in comparison result
- Calculated based on comparison magnitude and baseline variance
- Constraint: Cannot be negative or exceed 1.0

**FR-006**: System MUST define **ComparisonMetric** as per-metric comparison result containing:
- Metric name
- Baseline value
- Current value
- Tolerance applied
- Metric-level result (IMPROVEMENT/REGRESSION/NO_SIGNIFICANT_CHANGE/INCONCLUSIVE)
- Change magnitude (absolute and relative)

#### Comparison Semantics

**FR-007**: System MUST implement comparison logic as:
- Pure function (no side effects)
- Deterministic (identical inputs → identical outputs)
- Commutative for metrics (order of comparison doesn't matter)

**FR-008**: System MUST calculate change magnitude as:
- **Absolute Change** = Current Value - Baseline Value
- **Relative Change** = (Current Value - Baseline Value) / Baseline Value * 100%
- **For Better Direction** (lower is good): Negative relative change = improvement
- **For Worse Direction** (higher is good): Positive relative change = improvement

**FR-009**: System MUST classify comparison result based on:
- Change magnitude vs. configured tolerance
- Confidence level vs. minimum confidence threshold
- Result priority when multiple metrics present (REGRESSION > IMPROVEMENT > NO_SIGNIFICANT_CHANGE > INCONCLUSIVE)

**FR-010**: System MUST validate that:
- Current results have same metric set as baseline (or document differences)
- Tolerance configuration is applied consistently
- Comparison respects metric direction (latency: lower better; throughput: higher better)

#### Baseline Immutability

**FR-011**: System MUST enforce immutability by:
- Preventing modifications to established baselines
- Allowing new baseline creation
- Maintaining audit trail of baseline operations

**FR-012**: System MUST support baseline versioning conceptually (different baselines, not modifications to existing)

### Key Entities

- **Baseline**: Immutable snapshot of metrics + evaluations + configuration
- **ComparisonResult**: Outcome of comparing current against baseline
- **Tolerance**: Configuration for acceptable variance
- **ComparisonMetric**: Per-metric comparison details
- **ConfidenceLevel**: Certainty measure for comparison result

---

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Regression detection accuracy exceeds 95% (regressions detected vs. false positives)
- **SC-002**: All comparison operations complete in under 100ms (performance engineers can get results interactively)
- **SC-003**: Baseline immutability enforced 100% (no modifications allowed after creation)
- **SC-004**: Comparison results are deterministic 100% (identical inputs always produce identical results)
- **SC-005**: Multi-metric comparisons aggregate results correctly in all cases
- **SC-006**: System supports tolerance configuration from 0% to 100% without errors
- **SC-007**: Confidence calculations produce values in [0.0, 1.0] range consistently
- **SC-008**: Edge cases (missing metrics, null values) handled gracefully with clear error messages

---

## Assumptions & Constraints

### Assumptions

1. **Metrics are normalized**: Baseline domain assumes metrics from both baseline and current execution are already normalized (consistent units, valid value ranges)
2. **Metric direction is known**: System knows whether each metric type should trend higher or lower for improvement (e.g., latency lower=better, throughput higher=better)
3. **No statistical model**: Initial implementation uses simple threshold-based comparison; no advanced statistical modeling
4. **Baseline selection is external**: This domain doesn't decide which run becomes baseline—that's a higher-level process
5. **Single comparison context**: Comparison is isolated operation; doesn't depend on historical baselines or trends

### Constraints

- **Out of Scope**: Baseline storage, persistence, retrieval (handled by infrastructure)
- **Out of Scope**: Statistical models or hypothesis testing
- **Out of Scope**: Baseline versioning/evolution strategy
- **Out of Scope**: Reporting and visualization
- **Out of Scope**: Integration with CI/CD exit codes

---

## References & Dependencies

- **Depends on**: Metrics Domain (provides Metric concept)
- **Depends on**: Evaluation Domain (provides evaluation results)
- **Referenced by**: Persistence/Storage layer (for baseline retrieval)
- **Referenced by**: Reporting/Analytics layer (for trend analysis)

---

## In Scope

✅ Compare semantics between current and baseline  
✅ Regression detection logic  
✅ Improvement detection logic  
✅ Tolerance interpretation and application  
✅ Confidence level calculation  
✅ Immutability constraints  
✅ Determinism guarantees  

## Out of Scope

❌ Baseline storage and persistence  
❌ Statistical modeling  
❌ Baseline versioning strategy  
❌ Historical trend analysis  
❌ Reporting and visualization  
❌ CI/CD integration
