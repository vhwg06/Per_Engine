# Implementation Plan: Evaluate Performance Orchestration Use Case

**Branch**: `002-evaluate-performance` | **Date**: January 15, 2026 | **Spec**: [spec.md](spec.md)  
**Input**: Feature specification from `/specs/002-evaluate-performance/spec.md`

## Summary

Implement the Evaluate Performance use case as an application-layer orchestration service that coordinates the Metrics, Profile, and Evaluation domains to produce deterministic, immutable EvaluationResult objects. This use case is **purely orchestration** — it does not calculate metrics, evaluate rules in detail, or persist data. It translates domain inputs into deterministic evaluation workflows and ensures idempotency, traceability, and completeness transparency.

**Technical Approach**: 
- Clean Architecture positioned in **Application layer** with dependency flow: Application → Domain Ports (no Infrastructure)
- Uses existing domain ports for Metrics, Profile, and Evaluation domains
- Determinism guaranteed through:
  - Sorted/ordered iteration over all inputs (rules, metrics, profiles)
  - Immutable result objects with computed fingerprints
  - Seeded deterministic algorithms for fingerprint generation
- No persistence, storage, or infrastructure dependencies in this layer

---

## Technical Context

**Language/Version**: C# 13 (.NET 10 LTS)  
**Primary Dependencies**: 
- .NET 10 base libraries
- Existing domain packages: `PerformanceEngine.Metrics.Domain`, `PerformanceEngine.Profile.Domain`, `PerformanceEngine.Evaluation.Domain`
- xUnit 2.8+ (testing framework for orchestration tests)
- FluentAssertions (test readability)

**Storage**: N/A (orchestration layer does not persist; data flows through, not stored)  
**Testing**: xUnit (unit tests), integration tests with domain test doubles  
**Target Platform**: Linux server (.NET 10 cross-platform), container-ready  
**Project Type**: Single application library (PerformanceEngine.Application package)  
**Performance Goals**: 
- Orchestration overhead < 10% of total evaluation time (target: < 50ms overhead for typical workload)
- Support 1000+ metrics and 100+ rules without degradation
- No GC pressure from orchestration layer (immutable result objects, minimal allocations)

**Constraints**: 
- Zero infrastructure imports (no Entity Framework, no HTTP, no Redis, no file I/O)
- Deterministic: identical inputs → byte-identical outputs
- Immutable: all result objects must be immutable after construction
- Idempotent: evaluation does not modify input metrics, profiles, or rules
- No side effects: pure functional orchestration logic

**Scale/Scope**: 
- Single evaluation execution per invocation (no batching)
- Maximum 1000 metrics per execution
- Maximum 100 evaluation rules per execution
- Maximum 50 profiles available for selection
- Fingerprint computation must complete in < 1ms

---

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

**Specification-Driven Development**: ✅
- [x] Feature defined through explicit specification (spec.md with 4 user stories, 12 FRs, 8 success criteria)
- [x] Specification version-controlled and precedes all implementation
- [x] Implementation will follow specification requirements precisely

**Domain-Driven Design**: ✅
- [x] Orchestration layer works with domain models, not infrastructure concerns
- [x] Core logic expressed in ubiquitous language: EvaluationResult, CompletenessReport, Violation
- [x] Domain ports abstraction ensures no tight coupling to specific domain implementations
- [x] No domain rules redefined or extended; only orchestrated

**Clean Architecture**: ✅
- [x] Application layer (orchestration) depends only on Domain ports, not infrastructure
- [x] Direction of dependency: Application → Domain Ports (inward, clean)
- [x] External systems (Metrics, Profile, Evaluation) accessed only through domain-defined ports
- [x] No infrastructure imports or infrastructure-layer logic in application package

**Layered Phase Independence**: ✅
- [x] Clear phase boundary: specification → orchestration → (infrastructure adapters separate)
- [x] Orchestration communicates with domains through serializable, engine-agnostic interfaces
- [x] Changes to domain internals do not require orchestration changes (interface contract maintained)

**Determinism & Reproducibility**: ✅
- [x] Identical inputs (metrics, profile, rules) produce identical outputs
- [x] Non-deterministic factors (timestamps) are inputs, not generated internally
- [x] All rule evaluation order deterministic (sorted by rule ID)
- [x] Fingerprint algorithm deterministic (SHA256 or equivalent, seeded, not randomized)

**Engine-Agnostic Abstraction**: ✅
- [x] Orchestration works with domain models, not engine-specific formats
- [x] Results normalized into domain EvaluationResult (metrics domain, evaluation domain, profile domain)
- [x] No engine data structures leak into result objects

**Evolution-Friendly Design**: ✅
- [x] New rule types added without orchestration changes (rules are domain inputs)
- [x] New metric types supported without orchestration changes (metrics are domain inputs)
- [x] Profile resolution strategy changes do not break orchestration (interface-based)
- [x] Outcome determination rules extensible through evaluation domain

---

## Project Structure

### Documentation (this feature)

```text
specs/002-evaluate-performance/
├── spec.md                      # Feature specification (COMPLETE)
├── plan.md                      # This file
├── research.md                  # Phase 0 (research findings)
├── data-model.md                # Phase 1 (domain model details)
├── contracts/                   # Phase 1 (API contracts)
│   ├── orchestration-contracts.md
│   └── result-structures.md
├── quickstart.md                # Phase 1 (implementation quickstart)
├── checklists/
│   └── requirements.md          # Quality validation (COMPLETE)
└── tasks.md                     # Phase 2 (implementation tasks)
```

### Source Code

```text
src/
├── PerformanceEngine.Evaluation.Domain/          # Existing (not modified)
├── PerformanceEngine.Metrics.Domain/             # Existing (not modified)
├── PerformanceEngine.Profile.Domain/             # Existing (not modified)
└── PerformanceEngine.Application/                # NEW: Orchestration layer
    ├── PerformanceEngine.Application.csproj
    ├── Ports/                                    # Domain port abstractions
    │   ├── IMetricsProvider.cs                   # Receive metrics from Metrics Domain
    │   ├── IProfileResolver.cs                   # Resolve profiles from Profile Domain
    │   └── IEvaluationRulesProvider.cs           # Receive rules from Evaluation Domain
    ├── Orchestration/                            # Core orchestration logic
    │   ├── EvaluatePerformanceUseCase.cs         # Main orchestration entry point
    │   ├── EvaluationOrchestrator.cs             # Orchestration workflow
    │   ├── CompletenessAssessor.cs               # Assess metric availability
    │   ├── RuleEvaluationCoordinator.cs          # Coordinate rule application
    │   ├── OutcomeAggregator.cs                  # Aggregate rule outcomes
    │   └── ResultConstructor.cs                  # Build immutable EvaluationResult
    ├── Models/                                   # Application-level models
    │   ├── EvaluationResult.cs                   # Immutable result (orchestration view)
    │   ├── CompletenessReport.cs                 # Data availability report
    │   ├── Violation.cs                          # Rule violation details
    │   ├── ExecutionMetadata.cs                  # Traceability information
    │   └── EvaluationContext.cs                  # Internal orchestration context
    └── Services/                                 # Helper services
        └── DeterministicFingerprintGenerator.cs  # Generate data fingerprints

tests/
├── PerformanceEngine.Application.Tests/
    ├── Integration/
    │   ├── EvaluatePerformanceUseCaseTests.cs    # End-to-end orchestration tests
    │   ├── DeterminismTests.cs                   # Idempotency and reproducibility
    │   └── PartialMetricsTests.cs                # Partial data handling
    ├── Unit/
    │   ├── CompletenessAssessorTests.cs
    │   ├── RuleEvaluationCoordinatorTests.cs
    │   ├── OutcomeAggregatorTests.cs
    │   ├── ResultConstructorTests.cs
    │   └── DeterministicFingerprintGeneratorTests.cs
    └── Fixtures/
        ├── MetricsTestData.cs
        ├── ProfileTestData.cs
        └── EvaluationRulesTestData.cs
```

**Structure Decision**: Single library package `PerformanceEngine.Application` containing orchestration logic. This is positioned as the application layer that will eventually be wrapped by adapters (HTTP, CLI, etc.) in infrastructure packages. The design maintains clear separation: orchestration concerns here, domain concerns in existing domain packages.

---

## High-Level Architecture

### Responsibility Boundaries

**Evaluate Performance Use Case (This Plan)** — Orchestration Only
```
┌─────────────────────────────────────────────────────────────────┐
│  APPLICATION LAYER: Orchestration                               │
│  ┌─────────────────────────────────────────────────────────────┐
│  │ Purpose: Coordinate Metrics → Profile → Rules → Result     │
│  │ • Resolve profile configuration                             │
│  │ • Assess completeness (which metrics available)             │
│  │ • Order rules deterministically                             │
│  │ • Aggregate outcomes                                        │
│  │ • Generate immutable EvaluationResult                        │
│  │                                                             │
│  │ NOT Responsible For:                                        │
│  │ ✗ Calculating metrics (delegated to Metrics Domain)         │
│  │ ✗ Evaluating rules in detail (delegated to Eval Domain)    │
│  │ ✗ Storing results (deferred to infrastructure)             │
│  │ ✗ Comparing to baseline (out of scope)                     │
│  │ ✗ Integrating with external systems (out of scope)         │
│  └─────────────────────────────────────────────────────────────┘
└─────────────────────────────────────────────────────────────────┘
          ↓ uses domain ports (abstraction) ↓
┌─────────────────────────────────────────────────────────────────┐
│  DOMAIN LAYER: Existing Domains (NOT Modified Here)             │
├─────────────────────────────────────────────────────────────────┤
│ ↙             ↙                ↙                                │
│ Metrics       Profile           Evaluation                       │
│ Domain        Domain            Domain                           │
│ • Sample      • ProfileConf     • Rule                          │
│ • Metric      • Threshold       • Outcome enum                  │
│ • Latency     • Selector        • Violation                     │
│ • Aggregation • Rules ref       • RuleEvaluation                │
└─────────────────────────────────────────────────────────────────┘
          ↓ adapted from infrastructure ↓
┌─────────────────────────────────────────────────────────────────┐
│  INFRASTRUCTURE LAYER: External Adapters                        │
│  (HTTP API, Event Sourcing, Database, File I/O)                │
└─────────────────────────────────────────────────────────────────┘
```

### Dependency Direction
```
Infrastructure (HTTP, DB, Events)
    ↑
    │ implements
    │
Application (Orchestration) → ports ← Domain (Metrics, Profile, Eval)
                    │
                    └─ imports ONLY (inward dependency, clean)
```

### Key Interaction Points

**With Metrics Domain** (IMetricsProvider port):
- **Input**: Collection of `Sample` objects (immutable, engine-agnostic)
- **Usage**: Query available metrics and their values for completeness assessment
- **Responsibility boundary**: Orchestration does not calculate; uses provided samples as-is
- **Determinism**: Metrics are inputs, not generated; ordering deterministic

**With Profile Domain** (IProfileResolver port):
- **Input**: Requested profile identifier, available profiles
- **Usage**: Resolve which configuration/thresholds apply to this evaluation
- **Responsibility boundary**: Orchestration does not define profiles; uses provided configuration
- **Determinism**: Profile resolution is deterministic given same ID and available profiles

**With Evaluation Domain** (IEvaluationRulesProvider port):
- **Input**: Set of evaluation rules with metric dependencies
- **Usage**: Apply rules to metrics in deterministic order, collect violations
- **Responsibility boundary**: Orchestration does not implement rule logic; delegates to domain
- **Determinism**: Rule order deterministic (sorted by rule ID); rule evaluation is pure (same inputs → same violations)

---

## Input and Output Contracts

### Inputs (Conceptual)

The orchestration receives four distinct inputs:

#### 1. Collected Metrics (from Metrics Domain)
```
Type: IReadOnlyCollection<Sample>
Content:
  - Immutable Sample objects (metric name, value, unit, timestamp)
  - May be empty or partial (missing expected metrics)
  - No modifications during orchestration
  
Contract: Metrics are read-only references; orchestration may iterate but not mutate
```

#### 2. Execution Context (calling code)
```
Type: ExecutionContext (value object)
Content:
  - Execution ID (for traceability)
  - Execution timestamp (when evaluation occurs)
  - Environment/metadata (not used for rules, only metadata)
  
Contract: Context is input; does not affect rule outcomes, only metadata
```

#### 3. Available Profiles (from Profile Domain)
```
Type: IReadOnlyCollection<ProfileConfiguration>
Content:
  - Profile ID, name, thresholds, rule references
  - Immutable configuration objects
  
Contract: Profiles are read-only; orchestration selects but does not modify
```

#### 4. Evaluation Rules (from Evaluation Domain)
```
Type: IReadOnlyCollection<EvaluationRule>
Content:
  - Rule ID, name, metric dependencies, rule logic (abstracted)
  - Severity level (critical → FAIL, non-critical → WARN)
  
Contract: Rules are read-only; orchestration applies in deterministic order
```

### Output Contract

#### EvaluationResult (Immutable, Primary Output)
```csharp
public sealed class EvaluationResult
{
    // Outcome determination
    public Outcome Outcome { get; }                      // PASS | WARN | FAIL | INCONCLUSIVE
    
    // Detailed violation information
    public IReadOnlyList<Violation> Violations { get; } // What failed and why
    
    // Traceability and metadata
    public ExecutionMetadata Metadata { get; }           // Profile applied, thresholds used, timestamps
    
    // Data transparency
    public CompletenessReport Completeness { get; }      // Which metrics available, which missing
    
    // Data integrity
    public string DataFingerprint { get; }               // SHA256 of actual collected data
}
```

#### Outcome Enum
```
PASS         → All evaluation rules satisfied; no violations
WARN         → One or more non-critical rules failed (incomplete requirements)
FAIL         → One or more critical rules failed (requirements not met)
INCONCLUSIVE → Insufficient data (>50% metrics missing) or conflicting rule results
```

#### Violation Details
```csharp
public sealed class Violation
{
    public string RuleId { get; }              // Which rule failed
    public string RuleName { get; }            
    public double ExpectedThreshold { get; }  // What was expected
    public double ActualValue { get; }         // What was measured
    public string AffectedMetricName { get; }  // Which metric caused failure
    public SeverityLevel Severity { get; }     // Critical → FAIL, Non-critical → WARN
}
```

#### CompletenessReport
```csharp
public sealed class CompletenessReport
{
    public int MetricsProvidedCount { get; }   // How many metrics available
    public int MetricsExpectedCount { get; }   // How many expected
    public double Completeness { get; }        // 0.0 → 1.0 (percentage)
    public IReadOnlyList<string> MissingMetrics { get; } // Which specific metrics missing
    public IReadOnlyList<string> UnevaluatedRules { get; } // Rules skipped due to missing data
}
```

### Immutability & Determinism Guarantees

1. **Immutability**: EvaluationResult and all nested objects are immutable after construction (sealed records, no setters)
2. **Idempotency**: Same inputs (metrics, profile, rules) executed twice produce byte-identical EvaluationResult
3. **Fingerprinting**: DataFingerprint computed from actual collected metrics (not expected metrics), deterministic (SHA256 with seeded order)
4. **No Side Effects**: Inputs are never modified; evaluation is pure functional

---

## Orchestration Flow

### Step-by-Step Workflow

```
INPUTS: metrics, executionContext, availableProfiles, evaluationRules
   ↓
1. VALIDATE INPUTS
   └─ Fail if no available profiles
   └─ Fail if no evaluation rules
   └─ Metrics can be empty (partial data allowed)
   ↓
2. RESOLVE PROFILE
   └─ Select profile from availableProfiles (determined by context or config)
   └─ Extract: thresholds, rule references, severity definitions
   └─ If profile not found → FAIL with clear error
   ↓
3. FILTER RULES FOR PROFILE
   └─ Only evaluate rules referenced by selected profile
   └─ Sort rules deterministically by rule ID (ASCII sort)
   ↓
4. ASSESS COMPLETENESS
   └─ For each rule, check if all required metrics are available in collection
   └─ Count: metricsAvailable, metricsExpected, missingMetrics
   └─ Determine completeness percentage
   └─ Identify unevaluated rules (rules missing required metrics)
   ↓
5. EVALUATE RULES (DETERMINISTIC ORDER)
   ├─ For each rule in sorted order:
   │  ├─ If required metrics missing → SKIP (mark as unevaluated)
   │  ├─ If metrics available → DELEGATE TO EVALUATION DOMAIN
   │  │  └─ Domain evaluates rule, returns: passed OR violation details
   │  ├─ Collect: all violations with rule ID, threshold, actual value, affected metric, severity
   └─ Sort violations by rule ID for deterministic output
   ↓
6. AGGREGATE OUTCOME
   ├─ If any critical rule violated → FAIL
   ├─ Else if any non-critical rule violated → WARN
   ├─ Else if completeness < threshold (e.g., 50%) → INCONCLUSIVE
   ├─ Else if all rules satisfied AND completeness sufficient → PASS
   └─ Sort outcome determination rules in clear precedence
   ↓
7. GENERATE FINGERPRINT
   └─ Collect actual metric values in deterministic order (sorted by metric name)
   └─ Create string representation: "metric1=value1|metric2=value2|..."
   └─ Compute SHA256 hash of string (seeded, not randomized)
   ↓
8. CONSTRUCT IMMUTABLE RESULT
   └─ Build EvaluationResult:
      ├─ Outcome (from step 6)
      ├─ Violations (sorted by rule ID from step 5)
      ├─ CompletenessReport (from step 4)
      ├─ ExecutionMetadata (from inputs + profile selected)
      └─ DataFingerprint (from step 7)
   └─ Result is sealed/immutable; no further modifications
   ↓
OUTPUT: EvaluationResult (immutable, deterministic, traceable)
```

### Orchestration Responsibilities (What This Layer Does)

- ✅ Select profile based on execution context
- ✅ Determine rule evaluation order (deterministically)
- ✅ Assess which metrics are available vs. expected
- ✅ Orchestrate calls to Evaluation Domain rule evaluation (order, error handling)
- ✅ Collect violations and violations details
- ✅ Aggregate rules outcomes into single Outcome
- ✅ Generate deterministic fingerprint of actual data
- ✅ Construct immutable result object
- ✅ Provide traceability (which profile, which data, which rules evaluated)

### Orchestration Non-Responsibilities (What Domains Do)

- ❌ Calculate metrics (Metrics Domain)
- ❌ Implement rule evaluation logic (Evaluation Domain)
- ❌ Store or persist results (Infrastructure)
- ❌ Compare to baselines (Out of scope)
- ❌ Determine CI/CD exit codes (Out of scope)

---

## Determinism and Idempotency Guarantees

### How Deterministic Ordering is Ensured

#### Rule Evaluation Order
- **Rule Collection**: Collect all rules to evaluate (filtered by profile)
- **Sorting**: Sort by rule ID using **string comparison (ASCII order)** — stable, language-independent
- **Iteration**: Evaluate in sorted order, always
- **Result**: Same rules, same order, every execution

#### Metric Ordering (for fingerprint and completeness)
- **Metric Collection**: Collect all provided metrics
- **Sorting**: Sort by metric name (ASCII order) before processing
- **Aggregation**: Aggregate (count, check existence) in sorted order
- **Result**: Same metrics, same order, every execution

#### Violation Ordering
- **Collection**: Collect all violations as rules are evaluated
- **Sorting**: Sort by rule ID (same as rule evaluation order)
- **Result**: Same violations, same order, every execution

### How Byte-Identical Output is Achieved

#### Immutable Result Objects
- All result objects are `sealed records` or immutable classes
- No setters; properties set only during construction
- No mutable collections; only `IReadOnlyList` exposed
- No `DateTime.Now` or random values used; all timestamps are inputs

#### Deterministic Fingerprint
- **Input**: Collected metrics (provided, not generated)
- **Ordering**: Sort metrics by name (stable)
- **Serialization**: Deterministic string format: `metric1=value1|metric2=value2|...`
- **Hashing**: SHA256 with fixed seed (not randomized)
- **Output**: Same hash string every execution with same metrics

#### Deterministic Outcome Aggregation
- **Rule Application**: Evaluate in sorted order; collect violations
- **Outcome Rules** (deterministic precedence):
  1. If any critical violation → FAIL
  2. Else if any non-critical violation → WARN
  3. Else if completeness < 50% → INCONCLUSIVE
  4. Else → PASS
- **Result**: Outcome determined by rules, not by order or timing

### Idempotency Guarantee

**Idempotent Contract**: 
```
Execute(metrics₁, profile₁, rules₁) = result₁
Execute(metrics₁, profile₁, rules₁) = result₁  (byte-identical)
Execute(metrics₁, profile₁, rules₁) = result₁  (byte-identical, N times)
```

**How Achieved**:
- All inputs are read-only (not modified during orchestration)
- No external I/O (no timestamps, no random, no database calls)
- All computations are pure functions (same input → same output)
- Result objects immutable; no re-evaluation or caching

---

## Partial Metrics Handling Strategy

### Detection of Missing Metrics

```csharp
// Pseudo-code logic
foreach (rule in sortedRules)
{
    var requiredMetrics = rule.GetRequiredMetricNames();  // e.g., ["latency_p99", "cpu_usage"]
    var availableMetrics = collectedMetrics.Select(m => m.Name).ToSet();
    
    var missingForThisRule = requiredMetrics.Except(availableMetrics).ToList();
    
    if (missingForThisRule.Count > 0)
    {
        // Metrics missing for this rule
        unevaluatedRules.Add(rule.Id, missingForThisRule);
    }
}
```

### Handling Rules with Missing Dependencies

**Strategy**: Graceful Degradation (Not Crash)

1. **Detection Phase**: When assessing completeness, identify which rules have missing metrics
2. **Skipping**: Rules with missing metrics are **not evaluated** (not called to Evaluation Domain)
3. **Tracking**: Add rule ID to `unevaluatedRules` list in CompletenessReport
4. **Outcome Impact**: Skipped rules do not contribute to outcome (assumed satisfied, not included in violation count)
5. **Traceability**: CompletenessReport explicitly lists which rules could not be evaluated

### CompletenessReport Details

```
MetricsProvidedCount: 8       // How many samples collected
MetricsExpectedCount: 10      // How many samples typical
Completeness: 0.80            // 80% available
MissingMetrics: 
  - "memory_peak"
  - "gc_collection_time"
UnevaluatedRules:             // Rules not evaluated due to missing metrics
  - "memory_rule_1"
  - "gc_rule_2"
```

### Completeness Thresholds

- **Sufficient Data**: Completeness > 50% → proceed with evaluation, report inconclusive if needed
- **Insufficient Data**: Completeness ≤ 50% → outcome = INCONCLUSIVE (even if no violations in evaluated rules)
- **Extreme Case**: Zero metrics provided → Completeness = 0%, outcome = INCONCLUSIVE, CompletenessReport lists all expected metrics as missing

### Example Scenarios

**Scenario 1**: 9 of 10 metrics available
```
✅ 90% complete
→ Evaluate all rules with available metrics
→ Skip rules needing the 1 missing metric
→ Outcome: PASS/WARN/FAIL (based on evaluated rules)
→ CompletenessReport: Indicates 1 metric missing, 1 rule unevaluated
```

**Scenario 2**: 4 of 10 metrics available (40%)
```
⚠️ 40% complete (below 50% threshold)
→ Evaluate all possible rules
→ Outcome: INCONCLUSIVE (even if all evaluated rules pass)
→ CompletenessReport: Indicates 6 metrics missing, completeness < threshold
```

**Scenario 3**: Zero metrics available
```
🔴 0% complete
→ No rules can be evaluated
→ All rules listed as unevaluated
→ Outcome: INCONCLUSIVE
→ CompletenessReport: All metrics listed as missing
```

---

## Error Handling Semantics

### Invalid Configuration (Fail Fast)

**Errors that MUST fail before evaluation begins**:

1. **No Profile Found**
   ```
   Input: profileId that doesn't exist in availableProfiles
   Action: Throw explicit error IMMEDIATELY
   Message: "Profile '{profileId}' not found in available profiles: [{list}]"
   ```

2. **No Evaluation Rules**
   ```
   Input: evaluationRules collection is empty
   Action: Throw explicit error IMMEDIATELY
   Message: "No evaluation rules provided; cannot evaluate"
   ```

3. **Invalid Rule Configuration**
   ```
   Input: Rule references metric that doesn't exist in metrics domain vocabulary
   Action: Throw explicit error IMMEDIATELY (domain validation)
   Message: "Rule '{ruleId}' references unknown metric '{metricName}'"
   ```

4. **Incompatible Threshold**
   ```
   Input: Profile threshold incompatible with metric unit or type
   Action: Throw explicit error IMMEDIATELY (domain validation)
   Message: "Profile threshold {value} {unit} incompatible with metric {metricName}"
   ```

### Missing Data (Graceful Degradation)

**Not errors; handled gracefully**:

- Partial metrics available → Skip rules needing missing metrics, report in CompletenessReport
- No metrics available → Evaluate no rules, outcome INCONCLUSIVE, report in CompletenessReport
- Some expected metrics missing → Report in CompletenessReport, continue evaluation

### Rule Evaluation Errors (Captured, Not Crashing)

**If Evaluation Domain throws error during rule evaluation**:

1. **Catch exception** from domain rule evaluation
2. **Create error Violation entry** with:
   - RuleId, RuleName
   - ErrorMessage (from exception)
   - Severity: Critical (error treated as critical failure)
3. **Add to Violations list** (treat as violation, not infrastructure failure)
4. **Continue evaluation** of remaining rules
5. **Outcome**: At least FAIL (error counts as critical violation)

**Example**:
```
Evaluating rule "cpu_threshold_rule"
  → Domain returns violation (threshold exceeded) → Add to violations
Evaluating rule "memory_rule"
  → Domain throws exception (metric value invalid) → Create error violation, add to violations
Evaluating rule "latency_rule"
  → Domain returns pass → Continue
Final outcome: FAIL (due to violations + error)
```

---

## Non-Goals, Assumptions, and Open Questions

### Explicitly Deferred (Out of Scope)

1. **Metric Calculation**
   - Metrics are provided by caller or Metrics Domain; orchestration does not calculate
   - Deferred: How to calculate latency percentiles, aggregations, etc.

2. **Baseline Comparison**
   - Evaluating "improved" vs "degraded" compared to previous execution
   - Deferred: Baseline storage, comparison logic, historical data correlation

3. **Persistence**
   - Storing evaluation results, metrics, profiles, or execution history
   - Deferred: Database schema, cache invalidation, archival strategy

4. **Integration with External Systems**
   - Sending results to CI/CD, monitoring systems, Slack, PagerDuty, etc.
   - Deferred: Integration adapter design, webhook schemas

5. **CI/CD Exit Code Determination**
   - Determining if a process should exit with code 0 (success) vs 1 (failure)
   - Deferred: Business logic for exit code mapping, CI/CD semantics

6. **Profile Storage/Retrieval**
   - Persisting profile configurations, versioning, fetching
   - Deferred: Profile repository, versioning strategy, rollback

7. **Advanced Outcome Logic**
   - Weighted scoring, trend analysis, machine learning-based prediction
   - Deferred: Future enhancement, not in MVP

### Assumptions

1. **Metrics Already Collected**
   - Caller provides metrics already collected from execution
   - Orchestration receives finished metric collection, does not collect
   - Assumption: `IMetricsProvider` returns immutable, complete collection

2. **Profile Configuration Valid**
   - Profile selected is already valid and contains:
     - Referenced rules exist in Evaluation Domain
     - Thresholds are compatible with metric types
     - No circular dependencies or conflicts
   - Assumption: Profile resolved by Profile Domain before passing to orchestration

3. **Evaluation Rules Immutable**
   - Rules do not change during evaluation
   - Same rule evaluated twice produces same result (given same metrics)
   - Assumption: Rules are read-only, deterministic domain objects

4. **Evaluation Domain is Pure**
   - Evaluation Domain rule application is pure: same metrics + rule → same violation result
   - No side effects during rule evaluation
   - Assumption: Domain layer follows pure functional semantics

5. **Standard Cryptography Available**
   - SHA256 or equivalent deterministic hashing function available
   - Hashing is seeded (not randomized)
   - Assumption: .NET 10 `System.Security.Cryptography` available

6. **Execution Environment Deterministic for Our Purposes**
   - Floating-point arithmetic produces same results across runs (within .NET platform)
   - No timing-dependent outcomes
   - Assumption: Orchestration does not depend on system clock or environment-specific behavior

7. **Maximum Scale Assumptions**
   - < 1000 metrics per execution
   - < 100 evaluation rules per execution
   - < 50 available profiles to select from
   - Assumption: Performance targets met within these bounds

### Open Questions for Stakeholder/Architect Review

1. **Profile Selection Logic**
   - Q: How is the profile determined from execution context?
   - A: [NEEDS CLARIFICATION - implementation decision required]
   - Options: 
     - By environment name (prod, staging, dev)?
     - By execution tags/labels?
     - By API parameter?
     - By last-successful profile?

2. **Fingerprint Algorithm**
   - Q: Should fingerprint include metric timestamps, or only values?
   - A: [NEEDS CLARIFICATION - specification decision]
   - Impact: Affects whether same metrics collected at different times have same fingerprint

3. **Completeness Threshold**
   - Q: The spec says "50% missing metrics" → INCONCLUSIVE; is this the final threshold?
   - A: [NEEDS CLARIFICATION - specification decision]
   - Impact: Rules how partial data is handled

4. **Rule Evaluation Error Handling**
   - Q: If domain rule throws exception, should entire evaluation fail or just that rule?
   - A: [NEEDS CLARIFICATION - specified as "captured as violation" but needs test coverage]
   - Impact: Failure mode for corrupt/invalid rules

5. **Outcome Precedence**
   - Q: If evaluation produces BOTH WARN-level violations AND insufficient metrics, should outcome be WARN or INCONCLUSIVE?
   - A: [NEEDS CLARIFICATION - precedence rules for edge cases]
   - Current assumption: Insufficient completeness → INCONCLUSIVE (overrides violations)

6. **Non-Critical vs Critical Rules**
   - Q: How does orchestration know which rules are critical vs non-critical?
   - A: Specified in rule metadata via domain port
   - Confirmation: Verify domain rule interface exposes `IsCritical` or `Severity` property

---

## Technology Decisions & Justifications

### Why Application Layer (Not Domain Layer)

**Decision**: Orchestration implemented as **Application Layer**, not extending existing domains

**Justification**:
- Domains (Metrics, Profile, Evaluation) are independently valuable
- Orchestration is a **workflow** that uses domains, not domain logic itself
- Clean Architecture principle: Application layer coordinates between domains
- Allows future orchestrations (e.g., multiple profiles, comparison evaluations) without modifying domains
- Easier to test orchestration separately from domain logic

**Alternative Rejected**: Putting orchestration in Evaluation Domain would couple domains together, violating separation of concerns

### Why No Infrastructure in This Layer

**Decision**: Zero infrastructure imports (no Entity Framework, HTTP, Redis, files)

**Justification**:
- Ensures orchestration logic is reusable across infrastructure boundaries
- Simplifies testing (no mocks needed for databases or HTTP clients)
- Follows Clean Architecture principle: dependencies point inward only
- Allows multiple infrastructure implementations (HTTP API, gRPC, Event-driven, CLI) all using same orchestration

**Alternative Rejected**: Embedding infrastructure (e.g., caching, logging) would lock orchestration to specific infrastructure choice

### Why C# 13 (.NET 10 LTS)

**Decision**: Use C# 13 with .NET 10 LTS

**Justification**:
- Consistent with existing domain projects in this repository
- .NET 10 LTS: 3-year support window, production-ready
- C# 13: Record types enable immutable result objects elegantly
- Strong null-safety features (`#nullable enable`) prevent NullReferenceException errors
- Cross-platform (.NET 10 runs on Linux, Windows, macOS)

---

## Testing Strategy

### Unit Tests (Orchestration Logic)

**What to test**:
- Rule sorting determinism (same rules always same order)
- Completeness assessment (correct counting of available metrics)
- Outcome aggregation (correct precedence: FAIL > WARN > INCONCLUSIVE > PASS)
- Violation collection (all violations captured, none lost)
- Fingerprint generation (same metrics → same hash, different → different)
- Immutability (result objects cannot be modified after construction)

**Test Framework**: xUnit with FluentAssertions

**Example tests**:
- `DeterministicRuleOrdering_SameRulesAlwaysSameOrder`
- `CompletenessAssessment_9Of10Metrics_Returns90Percent`
- `CompletenessThreshold_40PercentComplete_OutcomeInconclusive`
- `FingerprintDeterminism_SameMetricsProduceSameHash`
- `ViolationCollection_AllRulesEvaluated_AllViolationsCaptured`

### Integration Tests (With Domain Test Doubles)

**What to test**:
- Full orchestration flow (inputs → result)
- Interaction with domain ports (calls domain in correct order, handles returns)
- Error handling (invalid config caught before evaluation)
- Partial metrics handling (skip rules, report completeness)

**Setup**: Use test doubles for domain ports (not real domain implementation)

**Example scenarios**:
- `FullOrchestration_AllRulesPass_OutcomePass`
- `FullOrchestration_SomeRulesFail_OutcomeFail`
- `FullOrchestration_50PercentMetricsMissing_OutcomeInconclusive`
- `InvalidProfile_NotFound_ThrowsBeforeEvaluation`
- `RuleEvaluationError_CaughtAndCaptured_NotCrash`

### Determinism Tests (Reproducibility Validation)

**What to test**:
- Execute same scenario 10 times, all results byte-identical
- Change one metric value, fingerprint changes
- Restore metric value, fingerprint matches original

**Example**:
```csharp
[Fact]
public void OrchestrationIdempotency_SameInputProducesByteIdenticalOutput()
{
    // Arrange
    var metrics = CreateTestMetrics();
    var profile = CreateTestProfile();
    var rules = CreateTestRules();
    
    // Act
    var result1 = orchestrator.Evaluate(metrics, profile, rules);
    var result2 = orchestrator.Evaluate(metrics, profile, rules);
    var result3 = orchestrator.Evaluate(metrics, profile, rules);
    
    // Assert
    Assert.Equal(SerializeToJson(result1), SerializeToJson(result2));
    Assert.Equal(SerializeToJson(result1), SerializeToJson(result3));
}
```

---

## Summary Table

| Aspect | Details |
|--------|---------|
| **Layer** | Application (Clean Architecture) |
| **Responsibility** | Orchestration only (coordinate, not calculate) |
| **Input** | Metrics, profile, rules, execution context |
| **Output** | Immutable EvaluationResult |
| **Determinism** | Deterministic sorting, immutable objects, seeded fingerprints |
| **Idempotency** | Same input → byte-identical output, always |
| **Partial Data** | Gracefully degrade (skip rules, report completeness) |
| **Error Handling** | Fail fast on invalid config; capture domain errors as violations |
| **Dependencies** | Domain ports only (inward dependency, clean architecture) |
| **No Infrastructure** | No persistence, HTTP, Redis, file I/O in this layer |
| **Testing** | Unit (logic), Integration (domains), Determinism (reproducibility) |
| **Scale** | < 1000 metrics, < 100 rules, < 50 profiles per execution |
| **Performance** | Orchestration < 50ms overhead for typical workloads |

---

## Next Steps (Phase 1: Design)

This plan will be followed by Phase 1 activities:

1. **data-model.md**: Define orchestration domain models (EvaluationResult, Violation, etc.)
2. **contracts/**: API contracts for domain ports (IMetricsProvider, IProfileResolver, IEvaluationRulesProvider)
3. **quickstart.md**: Step-by-step guide to implementing the use case
4. **tasks.md** (Phase 2): Break plan into concrete implementation tasks

---

**Plan Status**: ✅ READY FOR PHASE 1 DESIGN
