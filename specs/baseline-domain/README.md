# Baseline Domain: Complete Planning Index

**Status**: ✅ Implementation Plan Phase 1 Complete  
**Date**: 2026-01-15  
**Next**: Phase 2 Task Breakdown & Implementation  

---

## Quick Navigation

| Document | Purpose | Audience | Length |
|----------|---------|----------|--------|
| [baseline-domain.spec.md](baseline-domain.spec.md) | User-facing feature specification | Product, QA | ~300 lines |
| [plan.md](plan.md) | Technical implementation strategy | Tech leads, architects | ~500 lines |
| [research.md](research.md) | Decision research & rationale | Reviewers, stakeholders | ~300 lines |
| [data-model.md](data-model.md) | Domain model design (entities, VOs, services) | Developers, architects | ~600 lines |
| [quickstart.md](quickstart.md) | Developer onboarding & code examples | Dev team | ~400 lines |
| [contracts/domain-contracts.md](contracts/domain-contracts.md) | Domain-level interfaces & contracts | All teams | ~400 lines |
| [PLAN_COMPLETE.md](PLAN_COMPLETE.md) | Completion summary & approval gates | Decision makers | ~300 lines |

---

## By Audience

### 👤 Product Managers & Stakeholders
1. Start: [baseline-domain.spec.md](baseline-domain.spec.md) - User stories, requirements
2. Then: [PLAN_COMPLETE.md](PLAN_COMPLETE.md#success-metrics-phase-2-validation) - Success criteria

### 👨‍💼 Tech Leads & Architects
1. Start: [plan.md](plan.md) - Executive summary, architecture
2. Review: [research.md](research.md) - Technical decisions
3. Detailed: [data-model.md](data-model.md) - Domain design

### 👨‍💻 Developers
1. Start: [quickstart.md](quickstart.md) - Setup & code examples
2. Reference: [data-model.md](data-model.md) - Domain model details
3. Integration: [contracts/domain-contracts.md](contracts/domain-contracts.md) - Domain boundaries

### 🎯 QA / Test Engineers
1. Start: [baseline-domain.spec.md](baseline-domain.spec.md) - Acceptance criteria
2. Test scenarios: [research.md](research.md#research-3-multi-metric-outcome-aggregation) - Outcome aggregation
3. Edge cases: [data-model.md](data-model.md#validation--invariant-enforcement) - Invariants

---

## Content Map

### Feature Specification
- **File**: [baseline-domain.spec.md](baseline-domain.spec.md)
- **Contains**:
  - Executive summary (what problem does baseline solve)
  - Purpose & goals (why we build this)
  - 5 prioritized user stories (P1 core, P2 enhancements)
  - 12 functional requirements (FR-001 through FR-012)
  - 5 edge cases to handle
  - 8 measurable success criteria
  - Assumptions & constraints
  - In-Scope vs Out-of-Scope

### Technical Implementation Plan
- **File**: [plan.md](plan.md)
- **Contains**:
  - Technology choices & rationale (C#, .NET 10, Redis)
  - High-level architecture (Clean Architecture + DDD)
  - Project structure (src/, tests/ layout)
  - Core concepts (Baseline, Comparison, Tolerance, Confidence)
  - Required interfaces (IBaselineRepository port)
  - Cross-cutting constraints (determinism, immutability)
  - Non-goals & assumptions
  - Risk assessment
  - Success metrics for implementation

### Research & Decisions
- **File**: [research.md](research.md)
- **Contains**:
  - **Decision 1**: Confidence level calculation (magnitude-based)
  - **Decision 2**: Metric direction metadata (from Metrics Domain)
  - **Decision 3**: Multi-metric aggregation (worst-case)
  - **Decision 4**: Baseline TTL policy (24h default)
  - **Decision 5**: Concurrent versioning (Phase 2 deferral)
  - Open questions for Phase 1 design review

### Domain Model Design
- **File**: [data-model.md](data-model.md)
- **Contains**:
  - Ubiquitous language (core terms defined)
  - **Aggregate Roots**:
    - Baseline (immutable snapshot aggregate)
    - ComparisonResult (immutable outcome aggregate)
  - **Value Objects**:
    - BaselineId (unique identifier)
    - Tolerance (acceptable variance config)
    - ConfidenceLevel (certainty measure)
    - ComparisonMetric (per-metric comparison details)
  - **Domain Services**:
    - ComparisonCalculator (pure comparison logic)
    - BaselineFactory (baseline creation)
  - **Invariant enforcement** (BaselineInvariants, ComparisonResultInvariants)
  - **Extension points** (Phase 2+ enhancements)
  - Domain model diagram (entity relationships)

### Domain Contracts
- **File**: [contracts/domain-contracts.md](contracts/domain-contracts.md)
- **Contains**:
  - **Contract 1**: Baseline aggregate interface
  - **Contract 2**: Comparison & result interfaces
  - **Contract 3**: Tolerance & configuration contracts
  - **Contract 4**: Confidence level contract
  - **Contract 5**: Repository port (infrastructure boundary)
  - Exception hierarchy (8 exception types)
  - Contract versioning & evolution strategy
  - Testing contracts (determinism, immutability, etc.)

### Developer Quick Start
- **File**: [quickstart.md](quickstart.md)
- **Contains**:
  - **Setup**: Project structure, csproj files
  - **Domain classes**: C# code examples (Baseline, Tolerance, ConfidenceLevel, ComparisonResult)
  - **Repository port**: IBaselineRepository interface
  - **Test patterns**: Determinism test harness
  - **Integration example**: ComparisonOrchestrator service
  - **Build & test**: dotnet commands
  - **Configuration**: appsettings.json example
  - **Common tasks**: Adding tolerance types, debugging

### Quality Checklist
- **File**: [checklists/requirements.md](checklists/requirements.md)
- **Contains**:
  - Content quality validation
  - Requirement completeness
  - Feature readiness assessment
  - All mandatory sections confirmed complete

### Completion Summary
- **File**: [PLAN_COMPLETE.md](PLAN_COMPLETE.md)
- **Contains**:
  - List of all artifacts generated
  - Coverage validation (requirements → design mapping)
  - Architecture & technology decisions
  - Constitution check results
  - Critical design decisions
  - Known unknowns for Phase 2
  - Success metrics
  - Approval gates before implementation

---

## How to Use This Plan

### For Implementation (Developers)

1. **Week 1: Setup & Domain**
   - Read: [quickstart.md](quickstart.md) (setup section)
   - Read: [data-model.md](data-model.md) (Baseline, Tolerance, ConfidenceLevel)
   - Create domain project structure
   - Implement core value objects

2. **Week 2: Comparison Logic**
   - Read: [data-model.md](data-model.md) (ComparisonCalculator section)
   - Read: [research.md](research.md) (confidence formula, outcome aggregation)
   - Implement comparison logic
   - Implement determinism test harness

3. **Week 3: Infrastructure & Integration**
   - Read: [plan.md](plan.md) (architecture section)
   - Read: [quickstart.md](quickstart.md) (integration example)
   - Implement Redis adapter
   - Integrate with application layer

4. **Week 4: Testing & Validation**
   - Complete unit tests
   - Run 1000-run determinism verification
   - Edge case testing
   - Performance validation

### For Architecture Review

1. Read: [plan.md](plan.md) (summary + architecture overview)
2. Review: [research.md](research.md) (decisions section)
3. Check: [PLAN_COMPLETE.md](PLAN_COMPLETE.md#constitution-check-results)
4. Validate: [PLAN_COMPLETE.md](PLAN_COMPLETE.md#approval-gates)

### For Stakeholder Approval

1. Read: [baseline-domain.spec.md](baseline-domain.spec.md) (entire)
2. Check: [PLAN_COMPLETE.md](PLAN_COMPLETE.md#success-metrics-phase-2-validation)
3. Review: [PLAN_COMPLETE.md](PLAN_COMPLETE.md#approval-gates)

---

## Key Concepts Quick Reference

### Baseline
- Immutable snapshot of metrics + evaluation results
- Created at point in time; used as reference for comparisons
- Cannot be modified after creation
- Expires after TTL (24h default)
- **In**: [data-model.md](data-model.md#aggregate-root-baseline)

### Comparison
- Deterministic operation comparing current metrics against baseline
- Produces ComparisonResult with outcomes (REGRESSION/IMPROVEMENT/NO_CHANGE/INCONCLUSIVE)
- Pure function; no side effects; identical inputs → identical outputs
- **In**: [data-model.md](data-model.md#domain-service-comparison-calculator), [research.md](research.md)

### Tolerance
- Configuration specifying acceptable variance for a metric
- Types: RELATIVE (±10%) or ABSOLUTE (±50ms)
- Evaluated per-metric to determine if change is significant
- **In**: [data-model.md](data-model.md#value-object-tolerance), [contracts/domain-contracts.md](contracts/domain-contracts.md#contract-3-tolerance--configuration)

### Confidence
- Measure [0.0, 1.0] of certainty in comparison outcome
- Calculated based on how far result exceeds tolerance
- Below threshold (0.7) → result marked INCONCLUSIVE
- **In**: [data-model.md](data-model.md#value-object-confidencelevel), [research.md](research.md#research-1-confidence-level-calculation-strategy)

### ComparisonResult
- Immutable outcome of single comparison operation
- Contains overall outcome + per-metric details
- Overall outcome = worst-case aggregation (REGRESSION > IMPROVEMENT > ...)
- **In**: [data-model.md](data-model.md#aggregate-root-comparison-result), [research.md](research.md#research-3-multi-metric-outcome-aggregation)

---

## Decision Quick Reference

### 5 Critical Decisions (Phase 0 Research)

| # | Decision | Chosen | Rationale | Location |
|---|----------|--------|-----------|----------|
| 1 | Confidence formula | Magnitude-based | No historical variance; immutable baseline | [research.md](research.md#research-1-confidence-level-calculation-strategy) |
| 2 | Metric direction | From Metrics Domain | Direction is metric semantics | [research.md](research.md#research-2-metric-direction-metadata) |
| 3 | Outcome aggregation | Worst-case (REGRESSION first) | Conservative; safe for CI/CD | [research.md](research.md#research-3-multi-metric-outcome-aggregation) |
| 4 | Baseline TTL | 24h default; configurable | Operational concern; graceful expiration | [research.md](research.md#research-4-baseline-ttl--retention-policy) |
| 5 | Versioning | Phase 2 (Phase 1: single baseline) | Simplifies initial implementation | [research.md](research.md#research-5-concurrent-baseline-versioning) |

---

## Dependency & Integration Points

### External Dependencies (Must Confirm)
- **Metrics Domain**: Expects IMetric interface with value/direction metadata
  - Status: ✓ Dependency defined; design review needed
  - Action: Confirm Metric.Direction availability with Metrics team

### Outgoing Integrations
- **Evaluation Domain**: Optional; baseline can store evaluation results
  - Status: ✓ Optional; not required for Phase 1
- **Repository (Infrastructure)**: Redis adapter implements IBaselineRepository
  - Status: ✓ Port defined; adapter pattern ready

---

## Glossary (For Reference)

See [data-model.md](data-model.md#ubiquitous-language) for complete ubiquitous language definitions:
- Baseline, Comparison, ComparisonResult, Tolerance, Confidence, Regression, Improvement, Inconclusive

---

## Questions & Escalation

### Questions During Implementation?
1. Check [research.md](research.md#open-questions-for-design-phase) (open questions section)
2. Review [plan.md](plan.md#non-goals-assumptions--open-questions) (assumptions)
3. Consult [data-model.md](data-model.md#extension-points-phase-2) (extension points)

### Design Review Needed?
1. Check [PLAN_COMPLETE.md](PLAN_COMPLETE.md#approval-gates) for approval gates
2. Schedule review of [plan.md](plan.md) & [research.md](research.md) with architects

### Performance Concerns?
1. See [plan.md](plan.md#technical-decisions--rationale) (Redis rationale)
2. See [PLAN_COMPLETE.md](PLAN_COMPLETE.md#success-metrics-phase-2-validation) (latency <20ms target)

---

## Status & Next Steps

**Current Status**: ✅ Phase 1 Implementation Plan Complete

**Artifacts**:
- 8 markdown documents (spec + plan + research + design + contracts + quickstart + checklist + summary)
- 25+ domain entities/services designed
- 5 critical decisions resolved
- Constitution compliance verified

**Next Action**: Phase 2 Task Breakdown

To generate detailed task breakdown:
```bash
cd /Users/cynus/Per_Engine
speckit.tasks baseline-domain
```

This will create `tasks.md` with:
- Ordered task list (dependencies, complexity, categories)
- Validation criteria per task
- Estimated effort

**Branch**: `baseline-domain-implementation`

---

## Contact & Support

For questions about specific sections:
- **Architecture/Technology**: Review [plan.md](plan.md)
- **Domain Design**: Review [data-model.md](data-model.md)
- **Implementation Details**: Review [quickstart.md](quickstart.md)
- **Decision Rationale**: Review [research.md](research.md)
- **Integration Boundaries**: Review [contracts/domain-contracts.md](contracts/domain-contracts.md)
