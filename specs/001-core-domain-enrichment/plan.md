# Implementation Plan: Core Domain Enrichment

**Branch**: `001-core-domain-enrichment` | **Date**: 2026-01-15 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification enriching three existing domains: Metrics, Evaluation, Profile

## Summary

This plan translates the Core Domain Enrichment specification into a concrete, build-ready technical design. The enrichment adds reliability, explainability, and determinism guarantees to three existing domains (Metrics, Evaluation, Profile) through clean, backward-compatible extensions.

**Strategic Approach**: Implement enrichments as additive value objects and extended aggregates in each domain layer. Validation and orchestration occur at the application layer. All changes preserve backward compatibility and can be phased incrementally.

## Technical Context

**Language/Version**: C# with .NET 10 (latest LTS)  
**Primary Dependencies**: None new (leverages existing .NET ecosystem; xUnit for testing)  
**Storage**: N/A (domain-layer enrichments; storage integration via adapter layer only)  
**Testing**: xUnit, with determinism verification tests  
**Target Platform**: .NET 10 runtime (Windows, Linux, macOS capable)  
**Project Type**: Clean Architecture multi-domain library suite (3 domains + shared)  
**Performance Goals**: Zero performance degradation; enrichments are metadata-only  
**Constraints**: Deterministic outcomes; order-independent rule/profile processing; immutability post-resolution  
**Scale/Scope**: Extends 3 existing production domains; backward compatible; phased adoption enabled

---

## Constitution Check

**GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.**

### Specification-Driven Development

- ✅ Feature defined through explicit, machine-readable specification
- ✅ Specification version-controlled and precedes implementation
- ✅ Enrichments derive from spec requirements, not ad-hoc decisions

### Domain-Driven Design

- ✅ Enrichments extend existing domain models (Metric, Evaluation, Profile, Rule)
- ✅ Core logic remains expressed in ubiquitous language (no technical leakage)
- ✅ Infrastructure concerns (persistence) remain in adapter layer
- ✅ No domain imports of execution engines, databases, or file formats

### Clean Architecture

- ✅ Dependencies point inward: Domain ← Application ← Adapters ← Infrastructure
- ✅ External systems access domain only through defined interfaces (ports)
- ✅ No infrastructure imports in domain or application layers
- ✅ Enriched domain objects immutable after construction

### Layered Phase Independence

- ✅ Each domain (Metrics, Evaluation, Profile) independently enriched
- ✅ Communication via serializable contracts (documented in Phase 1)
- ✅ Phases remain independent: specification → design → implementation → testing
- ✅ Execution engines remain replaceable (engine-agnostic enrichments)

### Determinism & Reproducibility

- ✅ Identical metrics + rules + profiles always produce identical evaluation results
- ✅ Profile resolution deterministic regardless of input order or runtime context
- ✅ Controlled factors: no `DateTime.Now`, `Random`, or concurrent ordering in domain logic
- ✅ All inputs, configurations, and enrichment rules explicitly versioned

### Engine-Agnostic Abstraction

- ✅ Enrichments work with any IMetric implementation (K6, JMeter, Gatling, etc.)
- ✅ Evidence structure captures domain-level data, not engine-specific formats
- ✅ Profile resolution independent of execution engine
- ✅ No engine-specific APIs in domain models

### Evolution-Friendly Design

- ✅ No technology locked at constitutional level (C# and .NET 10 are plan-level choices)
- ✅ New enrichments extend existing layers without bypassing architecture
- ✅ Backward compatibility ensured: existing code works unchanged
- ✅ Enrichments can be adopted incrementally per domain

**Status**: ✅ **PASS** – All constitutional principles satisfied.

---

## High-Level System Architecture

### Current Baseline (Pre-Enrichment)

```
Domain Layer
├── Metrics Domain
│   ├── Sample (measurement entity)
│   ├── Metric (aggregated result)
│   ├── Aggregation (calculation specification)
│   └── IMetric (engine-agnostic interface)
│
├── Evaluation Domain
│   ├── Rule (strategy pattern for evaluation logic)
│   ├── Evaluator (stateless evaluation service)
│   ├── EvaluationResult (outcome with violations)
│   └── Violation (failure report)
│
└── Profile Domain
    ├── Profile (configuration container)
    ├── Scope (override context: global/api/endpoint)
    ├── Override (value + scope pair)
    └── Configuration (resolved profile state)

Application Layer
└── EvaluationService (use case: evaluate metrics against rules with profiles)

Adapter Layer
└── Repository implementations, engine adapters, persistence mappers
```

### Enriched Architecture (Post-Implementation)

```
Domain Layer
├── Metrics Domain (ENRICHED)
│   ├── Sample
│   ├── Metric + CompletessStatus enum
│   ├── MetricEvidence (NEW: sample count, window reference)
│   ├── Aggregation
│   └── IMetric (extended with completeness metadata)
│
├── Evaluation Domain (ENRICHED)
│   ├── Rule (unchanged)
│   ├── Evaluator (unchanged, deterministic guarantee explicit)
│   ├── EvaluationResult + Outcome enum (CHANGED: PASS/FAIL/INCONCLUSIVE)
│   ├── EvaluationEvidence (NEW: rule, metrics, values, constraints, decision)
│   ├── Violation (unchanged)
│   └── Evidence (value object capturing domain-level decision data)
│
└── Profile Domain (ENRICHED)
    ├── Profile + immutability guard
    ├── Scope
    ├── Override
    ├── Configuration (determinism guarantee explicit)
    ├── ProfileValidationResult (NEW: validation gates)
    └── IProfileValidator (NEW: validation abstraction)

Application Layer (ENRICHED)
├── EvaluationService
│   ├── ValidateProfile(profile) → result with errors
│   ├── ResolveProfile(profile, inputs) → deterministic resolution
│   └── Evaluate(metric, rule, profile) → enriched result with evidence
│
└── EnrichmentOrchestrator (NEW: cross-domain validation and enrichment)

Adapter Layer
└── Repositories, validators, persistence mappers (no changes to contracts)
```

### Dependency Flow

```
Adapters/Infrastructure
        ↑
   Application Layer (orchestration, validation)
        ↑
   Domain Layer (pure logic, enriched contracts)
        ↓
   (no downward dependencies)
```

---

## Mapping: Domain Concepts to Implementation Boundaries

### Metrics Domain Enrichment

#### Concept: Metric Completeness & Reliability

**Specification Requirement** (FR-001, FR-002):
- Each metric MUST declare completeness status (COMPLETE or PARTIAL)
- Each metric MUST expose evidence metadata (sample count, aggregation window reference)

**Implementation Boundary**:
- **Entity**: Extend existing `Metric` aggregate with `CompletessStatus` enum
- **Value Object**: Create `MetricEvidence` containing:
  - `SampleCount: int` (number of samples aggregated)
  - `AggregationWindow: string` (reference to aggregation window, e.g., "5m", "1h")
  - `IsComplete: bool` (convenience property derived from CompletessStatus)
- **Port**: Extend `IMetric` interface to expose `CompletessStatus` and `MetricEvidence` properties
- **Responsibility**: Metric domain declares reliability; evaluation domain decides usage
- **Boundary Protection**: Evidence metadata is read-only; changes only via domain factory methods

**Invariants**:
- COMPLETE metrics have SampleCount ≥ threshold (domain constant)
- PARTIAL metrics have SampleCount ≥ 1 (at least some data)
- AggregationWindow always present and non-empty

**No Change To**: Metric calculation logic, aggregation mathematics, or sample collection

---

### Evaluation Domain Enrichment

#### Concept 1: Extended Evaluation Outcomes

**Specification Requirement** (FR-005, FR-006):
- Evaluation results MUST support three outcomes: PASS, FAIL, INCONCLUSIVE
- INCONCLUSIVE MUST be used when metrics incomplete, execution partial, or insufficient evidence exists

**Implementation Boundary**:
- **Enum**: Extend existing `Outcome` enum from 2 values (PASS/FAIL) to 3 (PASS/FAIL/INCONCLUSIVE)
- **EvaluationResult**: Add `OutcomeReason: string` field explaining outcome choice
- **Responsibility**: Evaluator determines outcome based on:
  - Complete metrics + passing constraint → PASS
  - Complete metrics + failing constraint → FAIL
  - Incomplete metrics (any PARTIAL) + rule doesn't explicitly allow partials → INCONCLUSIVE
  - Execution partial (e.g., insufficient samples) → INCONCLUSIVE
  
**Backward Compatibility**: Existing code treating PASS/FAIL continues unchanged; INCONCLUSIVE is new path

---

#### Concept 2: Evaluation Evidence Trail

**Specification Requirement** (FR-007, FR-008, FR-009):
- Each evaluation result MUST include complete evidence (rule, metrics used, values, constraints, outcome)
- Evidence MUST be domain-level (not logs or infrastructure data)
- Given identical inputs, evaluations MUST produce identical evidence (determinism)

**Implementation Boundary**:
- **Value Object**: Create `EvaluationEvidence` capturing:
  - `RuleId: string` (rule evaluated)
  - `RuleName: string` (human-readable name)
  - `MetricsUsed: IReadOnlyList<MetricReference>` (which metrics evaluated)
  - `ActualValues: Dictionary<string, double>` (aggregation name → actual value)
  - `ExpectedConstraint: string` (e.g., "p95 < 200ms")
  - `ConstraintSatisfied: bool` (pass/fail of constraint)
  - `Decision: string` (human-readable outcome explanation)
  - `EvaluatedAt: DateTime` (deterministic timestamp, UTC)
  
- **Value Object**: `MetricReference` (lightweight reference to metric used):
  - `AggregationName: string` (e.g., "p95", "error_rate")
  - `Completeness: CompletessStatus`
  - `Value: double`
  
- **EvaluationResult**: Add `Evidence: EvaluationEvidence` field (never null)
- **Determinism**: Use `DateTime.UtcNow` captured at evaluation start; sort evidence deterministically

**Invariants**:
- Evidence must explain the outcome without requiring external logs
- ActualValues keys must align with MetricReference entries
- EvaluatedAt timestamp must be UTC-based

---

### Profile Domain Enrichment

#### Concept 1: Deterministic Resolution Guarantee

**Specification Requirement** (FR-010):
- Profile resolution MUST be deterministic and independent of input order and runtime context

**Implementation Boundary**:
- **Service**: Extend or create `ProfileResolver` with determinism guarantee
  - Input: Profile + override inputs (in any order)
  - Output: Resolved configuration (deterministically identical regardless of input order)
  - Method: Sort overrides by (scope, key) before applying, deterministically process conflicts
  
- **Value Object**: Create `ResolutionStrategy` capturing:
  - Override application order (scope hierarchy: global → api → endpoint)
  - Conflict resolution rules (last-write-wins within scope, higher scope wins)
  - Deterministic iteration order (sorted keys)
  
- **Responsibility**: Profile domain ensures resolution is byte-identical; infrastructure layer may cache results

**Invariants**:
- Order-independent: {A, B, C} = {C, A, B} = {B, C, A}
- Deterministic: profile + inputs always produce identical resolved state
- No runtime context (CPU timing, load, etc.) affects resolution

---

#### Concept 2: Post-Resolution Immutability

**Specification Requirement** (FR-011, FR-012):
- After resolution, a profile MUST be immutable
- All overrides MUST occur before profile resolution

**Implementation Boundary**:
- **Aggregate State**: Profile progresses through states:
  - `Unresolved` (accepting overrides)
  - `Resolved` (immutable; reads return resolved values)
  - `Invalid` (failed validation; cannot evaluate)
  
- **Entity Methods**:
  - `ApplyOverride(scope, key, value)` → throws if already resolved
  - `Resolve(inputs) → ResolutionResult` → transitions state to Resolved
  - `Get(key) → ResolvedValue` → throws if not resolved
  
- **Responsibility**: Profile domain enforces state machine; application layer calls methods in correct order

---

#### Concept 3: Profile Validation Gates

**Specification Requirement** (FR-013, FR-014):
- Profiles MUST be validated before evaluation use
- Invalid profiles MUST block evaluation execution

**Implementation Boundary**:
- **Port**: Create `IProfileValidator` interface:
  - `Validate(profile) → ValidationResult`
  - Result contains: `IsValid: bool`, `Errors: IReadOnlyList<ValidationError>`
  
- **Validation Rules**:
  - No circular override dependencies
  - All required configuration keys present (per profile type)
  - Value types match expected schema
  - Scope references valid (no unknown API/endpoint scopes)
  
- **Application Gate**: `EvaluationService.Evaluate()`
  - Pre-condition: ValidateProfile(profile).IsValid == true
  - Throws `InvalidProfileException` if validation fails
  - Includes error details for debugging
  
- **Responsibility**: Domain provides validation abstraction; adapters implement validators; application enforces gate

---

## Required Interfaces / Ports

### Ports (Domain-Defined Abstractions)

#### 1. Metric Input Port (Metrics Domain)

```
interface IMetricProvider
{
    IMetric GetMetric(string aggregationName);
    IReadOnlyList<IMetric> GetAllMetrics();
}
```

**Contract**: Provides access to calculated metrics with completeness metadata.

**Implementations**: Engine adapters (K6, JMeter, Gatling), test doubles, mock metrics.

**Changes for Enrichment**: Return type remains `IMetric`, but `IMetric` now includes:
```
CompletessStatus GetCompleteness();
MetricEvidence GetEvidence();
```

---

#### 2. Profile Input Port (Profile Domain)

```
interface IProfileProvider
{
    Profile GetProfile(string profileId);
    IReadOnlyList<Profile> GetProfilesByScope(string scope);
}
```

**Contract**: Provides profiles for evaluation.

**Implementations**: Configuration adapters, file-based loaders, database repositories.

**Changes for Enrichment**: Profiles returned are post-validation; application layer validates before returning.

---

#### 3. Evaluation Output Port (Evaluation Domain)

```
interface IEvaluationResultRecorder
{
    void RecordResult(EvaluationResult result);
    void RecordEvidence(EvaluationEvidence evidence);
}
```

**Contract**: Persists evaluation results and evidence for audit trails.

**Implementations**: Event logs, audit databases, compliance systems.

**Changes for Enrichment**: New `RecordEvidence` method captures enriched data; result includes full evidence object.

---

#### 4. Profile Validation Port (Profile Domain)

```
interface IProfileValidator
{
    ValidationResult Validate(Profile profile);
    IReadOnlyList<ValidationRule> GetApplicableRules(string profileType);
}
```

**Contract**: Validates profiles before use.

**Implementations**: Schema validators, business rule validators, custom validators.

**New for Enrichment**: Entirely new port enabling pluggable validation strategies.

---

### Contracts Summary

| Data Flow | From | To | Contract | Changes for Enrichment |
|-----------|------|-----|----------|----------------------|
| Metrics Input | Engine | Evaluation | `IMetric` with aggregations | + `CompletessStatus`, `MetricEvidence` properties |
| Evaluation Output | Evaluation | Reporting | `EvaluationResult` | + `Evidence` field, `Outcome` enum extended |
| Profile Input | Config Storage | Evaluation | `Profile` with overrides | + Resolved state; validation result |
| Enrichment Metadata | Any | Reporting/Audit | New value objects | `EvaluationEvidence`, `MetricEvidence`, `ValidationResult` |

---

## Cross-Cutting Constraints

### 1. Deterministic Evaluation Outcomes

**Constraint**: Identical metrics + rules + profiles always produce identical outcomes, violations, and evidence.

**Implementation Strategy**:
- Metrics: No `DateTime.Now`, `Random`, or concurrent ordering in aggregation
- Rules: Strategy implementations must be pure functions (no side effects)
- Evaluation: Sort violations by rule ID, then metric name (deterministic order)
- Evidence: Captured at evaluation start (single `DateTime.UtcNow`); serializable to JSON

**Verification**: Unit tests run evaluation 1000+ times; verify byte-identical JSON serialization

---

### 2. Order-Independent Rule Processing

**Constraint**: Rule evaluation results identical regardless of rule processing order.

**Implementation Strategy**:
- Evaluator sorts rules by ID before processing
- Violations collected in deterministic order
- EvaluationResult.Violations is `IReadOnlyList` sorted by (RuleId, MetricName)

**Verification**: Property-based tests vary rule order; verify identical outcomes

---

### 3. Deterministic Profile Resolution

**Constraint**: Profile resolution deterministic and independent of input order and runtime context.

**Implementation Strategy**:
- ProfileResolver: Sort overrides by (scope, key) before applying
- Scope hierarchy fixed: Global < API < Endpoint (no context-dependent ordering)
- No environment-based decisions (CPU load, timing, thread scheduling)
- Determinism validation: Resolve identical profile + inputs 100+ times; verify byte-identical state

**Verification**: Determinism tests exercise order permutations; verify identical resolved config

---

### 4. Immutability After Evaluation or Profile Resolution

**Constraint**: EvaluationResult and resolved Profile cannot be modified post-creation.

**Implementation Strategy**:
- **EvaluationResult**: Immutable record (C# 9+ records with init accessors or readonly fields)
- **Resolved Profile**: State machine prevents modifications; throws if modification attempted
- **EvaluationEvidence**: Immutable value object; no setters
- **Collections**: All returned collections are `IReadOnlyList`, `IReadOnlyDictionary`

**Verification**: Unit tests attempt mutation; verify exceptions thrown

---

### 5. Testability at Unit, Contract, and Integration Levels

**Constraint**: Enrichments must be testable without infrastructure; contracts testable independently; full integration testable.

**Implementation Strategy**:
- **Unit Tests**: Mock `IMetricProvider`, `IProfileProvider`, `IProfileValidator`
- **Contract Tests**: Define expected behaviors for `IMetric.GetCompleteness()`, `EvaluationResult.Evidence`, etc.
- **Integration Tests**: Real profile resolution, real evaluation, real enrichment flow
- **Determinism Tests**: 1000+ iteration determinism checks at unit and integration level

**Verification**: xUnit test suite organized by layer; coverage includes all three levels

---

## Non-Goals, Assumptions, and Open Questions

### Non-Goals

What this enrichment explicitly does **NOT** handle:

1. **Changing metric calculation logic**: Enrichments only add completeness metadata; aggregations unchanged
2. **Introducing new rule types**: Enrichments extend existing rule evaluation; no new strategists in this phase
3. **Changing profile storage format**: Profiles remain engine-agnostic; storage format is adapter concern
4. **Adding new visualization or reporting**: Evidence is structured data; presentation is downstream
5. **Changing CI/CD integration**: Exit codes and automation logic remain application concern
6. **Optimizing profile resolution performance**: Determinism takes priority; optimization deferred
7. **Supporting real-time profile updates**: All overrides applied before resolution; runtime changes out of scope

### Assumptions

1. **Metrics are already standardized**: All IMetric implementations follow existing Metrics Domain spec
2. **Rules are stateless**: Rule evaluation has no side effects; rules are pure functions
3. **Profiles are relatively small**: <10,000 override entries; no sharding or partitioning needed
4. **Validation rules are known**: Profile validation rules are stable; no machine learning or dynamic rules
5. **Storage layer handles durability**: Domain layer assumes persistence layer works; doesn't add checksums
6. **No breaking changes to existing code**: All enrichments are additive; backward compatibility maintained
7. **Infrastructure layer is already in place**: Repositories, adapters, logging already available

### Open Questions

Questions requiring resolution before implementation or early in task breakdown:

1. **Profile Resolution Performance Baseline**: What is acceptable resolution time for typical profiles (99th percentile)? Helps inform caching decisions.
   - **Recommendation**: Establish baseline in early unit tests; aim for < 10ms for 100-entry profiles

2. **INCONCLUSIVE Outcome Handling Downstream**: How do CI/CD systems and reporting handle INCONCLUSIVE vs PASS/FAIL? Does it require exit code changes?
   - **Recommendation**: Document as application concern; domain returns INCONCLUSIVE; application layer decides semantics

3. **Evidence Retention Policy**: Should evaluation evidence be retained indefinitely, or is there a purge policy? Impacts audit trail completeness.
   - **Recommendation**: Implement as adapter concern (database retention policy); domain layer doesn't enforce TTL

4. **Validation Error Messaging**: What level of detail should ProfileValidator return? One error per violation, or aggregated?
   - **Recommendation**: Return all errors at once; allow application to decide display strategy

5. **Metric Completeness Thresholds**: Is "COMPLETE" a binary (100% samples collected) or a threshold (e.g., ≥90%)?
   - **Recommendation**: Metrics Domain owns definition; Profile/Evaluation Domains consume via CompletessStatus enum

6. **Circular Override Dependencies**: Can Profile Domain circular dependency detection be stateful, or must it be pure?
   - **Recommendation**: Pure analysis recommended; use topological sort algorithm for acyclic validation

---

## Project Structure

### Documentation (this feature)

```text
specs/001-core-domain-enrichment/
├── plan.md              # This file (Phase 1 plan)
├── research.md          # Phase 0: Design research (to be generated)
├── data-model.md        # Phase 1: Entity specifications (to be generated)
├── quickstart.md        # Phase 1: Developer quickstart (to be generated)
├── contracts/           # Phase 1: API contracts (to be generated)
│   ├── metrics-enrichment.contract.md
│   ├── evaluation-enrichment.contract.md
│   └── profile-enrichment.contract.md
└── tasks.md             # Phase 2: Task breakdown (not created by /speckit.plan)
```

### Source Code (repository root)

```text
src/
├── PerformanceEngine.Metrics.Domain/          # ENRICHED
│   ├── Domain/
│   │   ├── Metrics/
│   │   │   ├── Metric.cs                      # Extended with CompletessStatus, MetricEvidence
│   │   │   ├── CompletessStatus.cs            # NEW enum
│   │   │   └── MetricEvidence.cs              # NEW value object
│   │   └── Ports/
│   │       ├── IMetric.cs                     # Extended interface
│   │       └── IMetricProvider.cs             # Existing port (unchanged)
│   ├── Application/
│   │   └── Services/
│   │       └── MetricService.cs               # Existing service (unchanged)
│   └── Ports/
│       ├── IMetricProvider.cs
│       └── IEvidenceRecorder.cs               # NEW port for enrichment data
│
├── PerformanceEngine.Evaluation.Domain/        # ENRICHED
│   ├── Domain/
│   │   ├── Evaluation/
│   │   │   ├── EvaluationResult.cs            # Extended with Evidence field
│   │   │   ├── EvaluationEvidence.cs          # NEW value object
│   │   │   ├── MetricReference.cs             # NEW value object
│   │   │   ├── Outcome.cs                     # Extended enum (PASS/FAIL/INCONCLUSIVE)
│   │   │   ├── OutcomeReason.cs               # NEW value object
│   │   │   └── Evaluator.cs                   # Existing service (logic unchanged, evidence added)
│   │   ├── Rules/
│   │   │   └── IRule.cs                       # Existing interface (unchanged)
│   │   └── Ports/
│   │       └── IEvaluationResultRecorder.cs   # Extended port (new evidence method)
│   ├── Application/
│   │   └── Services/
│   │       └── EvaluationService.cs           # Existing service (profile validation added)
│   └── Ports/
│       ├── IMetricProvider.cs
│       ├── IProfileProvider.cs
│       └── IEvaluationResultRecorder.cs
│
├── PerformanceEngine.Profile.Domain/           # ENRICHED
│   ├── Domain/
│   │   ├── Profiles/
│   │   │   ├── Profile.cs                     # Extended with state machine + immutability
│   │   │   ├── ProfileState.cs                # NEW enum
│   │   │   ├── ProfileResolver.cs             # NEW service for deterministic resolution
│   │   │   └── ResolutionStrategy.cs          # NEW value object
│   │   ├── Validation/
│   │   │   ├── ValidationResult.cs            # NEW value object
│   │   │   ├── ValidationError.cs             # NEW value object
│   │   │   ├── ValidationRule.cs              # NEW interface
│   │   │   └── IProfileValidator.cs           # NEW port
│   │   ├── Configuration/
│   │   │   └── ConfigurationValue.cs          # Existing value object (unchanged)
│   │   └── Ports/
│   │       ├── IProfileProvider.cs            # Existing port (unchanged)
│   │       └── IProfileValidator.cs           # NEW port
│   ├── Application/
│   │   └── Services/
│   │       ├── ProfileService.cs              # Existing service
│   │       └── ProfileValidationService.cs    # NEW service for validation orchestration
│   └── Infrastructure/
│       └── Validators/
│           └── ProfileValidator.cs            # NEW implementation of IProfileValidator
│
└── PerformanceEngine.Shared/                  # SHARED (if created)
    ├── Domain/
    │   └── ValueObjects/
    │       ├── CompletessStatus.cs            # If shared across domains
    │       └── Evidence.cs                    # Base evidence class (optional)
    └── Ports/
        └── IEnrichmentPort.cs                 # Optional shared abstraction

tests/
├── PerformanceEngine.Metrics.Domain.Tests/
│   ├── Domain/
│   │   ├── MetricEnrichmentTests.cs           # NEW: CompletessStatus, MetricEvidence
│   │   └── MetricEvidenceTests.cs             # NEW: Evidence value object tests
│   └── Determinism/
│       └── MetricDeterminismTests.cs          # Existing + enrichment verification
│
├── PerformanceEngine.Evaluation.Domain.Tests/
│   ├── Domain/
│   │   ├── EvaluationEnrichmentTests.cs       # NEW: Outcome enum, Evidence
│   │   ├── EvaluationEvidenceTests.cs         # NEW: Evidence structure tests
│   │   ├── InconclusivenessTests.cs           # NEW: INCONCLUSIVE outcome tests
│   │   └── EvidenceCompletenessTests.cs       # NEW: Evidence adequacy validation
│   └── Determinism/
│       ├── DeterminismTests.cs                # Existing (unchanged)
│       └── EvidenceDeterminismTests.cs        # NEW: Evidence determinism verification
│
└── PerformanceEngine.Profile.Domain.Tests/
    ├── Domain/
    │   ├── ProfileEnrichmentTests.cs          # NEW: State machine, immutability
    │   ├── ProfileResolutionTests.cs          # NEW: Deterministic resolution
    │   ├── ProfileValidationTests.cs          # NEW: Validation gate tests
    │   ├── CircularDependencyTests.cs         # NEW: Validation rule tests
    │   └── ImmutabilityTests.cs               # NEW: Immutability enforcement
    └── Determinism/
        └── ResolutionDeterminismTests.cs      # NEW: Order-independent resolution
```

---

## Complexity Tracking

| Architectural Decision | Why Needed | Simpler Alternative Rejected Because |
|------------------------|-----------|--------------------------------------|
| Three separate domain projects | Each domain independently enriched; clear boundaries | Single project would couple domains, violating DDD boundaries |
| State machine for Profile (Unresolved → Resolved) | Enforce immutability after resolution | Simple flags too permissive; incorrect state transitions possible |
| ProfileValidator port (strategy pattern) | Support pluggable validators; testability without infrastructure | Hard-coded validation rules couples domain to specific validators |
| EvaluationEvidence value object | Separate enrichment concern; enables future audit/reporting features | Embedding evidence in EvaluationResult conflates concerns |
| Order-independent processing (sorted overrides/rules) | Determinism guarantee independent of input order | Context-dependent ordering non-deterministic; violates requirement |

---

## Technology Rationale

### Why .NET 10 for Core Domain Enrichment?

1. **Determinism Excellence**: .NET's type system and immutability patterns (records, init accessors, readonly) provide compile-time guarantees for deterministic implementation
2. **Clean Architecture Native Support**: Dependency injection, interface-based design, and stratified project structure align naturally with DDD/Clean Architecture principles
3. **Performance & Reproducibility**: Deterministic GC, IL compilation, and no hidden runtime surprises make performance testing logic reliable
4. **Alignment with Existing Stack**: Three domains already implemented in C#/.NET 8+; .NET 10 LTS provides stability and latest features
5. **Test Infrastructure**: xUnit combined with determinism testing patterns (Hypothesis.net-style property tests) enables comprehensive coverage
6. **Long-Term Evolution**: LTS release ensures compatibility for multi-year platform evolution without forced upgrades

### Why Clean Architecture + DDD?

1. **Domain Purity**: Core evaluation, profile resolution, and metric aggregation remain independent of infrastructure (databases, APIs, engines)
2. **Port/Adapter Pattern**: Validation, persistence, and enrichment logic plug into domain through well-defined interfaces; swappable implementations
3. **Testability**: Pure domain logic testable without mocks; adapters testable independently; full integration testable end-to-end
4. **Evolution Path**: New domains, capabilities, and integrations add without modifying existing domain logic

---

## Next Steps

### Phase 0: Research (Blocking)
- [ ] Resolve open questions (performance baselines, INCONCLUSIVE handling, etc.)
- [ ] Validate technology choices (C#/.NET 10 appropriate for enrichment scope)
- [ ] Generate `research.md` documenting findings

### Phase 1: Design & Contracts
- [ ] Generate `data-model.md` with detailed entity specifications
- [ ] Generate API contracts in `/contracts/` directory
- [ ] Generate `quickstart.md` for developer onboarding
- [ ] Update Copilot agent context with enrichment design
- [ ] Re-validate Constitution Check post-design

### Phase 2: Task Breakdown (Separate `/speckit.tasks` command)
- [ ] Decompose enrichments into granular implementation tasks
- [ ] Sequence tasks by dependency and risk
- [ ] Assign complexity estimates and acceptance criteria

---

**Status**: Plan complete. Ready for Phase 0 research.
