# Implementation Plan: Persist Results for Audit & Replay

**Branch**: `001-persist-results` | **Date**: 2026-01-16 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/001-persist-results/spec.md`

**Note**: This plan is filled in by the `/speckit.plan` command. See `.specify/templates/commands/plan.md` for the execution workflow.

## Summary

The system must establish a repository abstraction layer to persistently store immutable evaluation results for audit trails and replay capabilities. The feature defines append-only semantics, atomic persistence operations, and domain-focused repository ports that remain technology-agnostic while preserving all evaluation context (metrics, violations, evidence) needed for deterministic replay. Three priority levels are addressed: foundational result persistence (P1), historical result retrieval (P2), and evaluation replay validation (P3).

## Technical Context

**Language/Version**: C# 12 (.NET 8.0)
**Primary Dependencies**: Domain-Driven Design patterns, SOLID principles, existing PerformanceEngine.Metrics.Domain and PerformanceEngine.Evaluation.Domain
**Storage**: Technology-agnostic (port abstraction defined; implementation deferred to infrastructure layer)
**Testing**: xUnit (following existing project patterns)
**Target Platform**: .NET 8.0 (Linux, Windows, macOS compatible)
**Project Type**: Multi-project (domain, application, infrastructure layers per Clean Architecture)
**Performance Goals**: Atomic persistence with no partial writes; deterministic serialization ensuring byte-identical retrieval
**Constraints**: <10ms persistence latency target (P2 optimization); 100% consistency for concurrent operations
**Scale/Scope**: Foundation for audit/replay; supports concurrent persist operations without race conditions

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

**Specification-Driven Development**:
- [x] Feature defined through explicit, machine-readable specification
- [x] Specification version-controlled and precedes implementation
- [x] Functional requirements directly map to acceptance scenarios
- [x] Success criteria are measurable and technology-neutral

**Domain-Driven Design**:
- [x] Domain models (EvaluationResult, Violation, EvaluationEvidence, MetricReference) independent of storage implementation
- [x] Core logic expressed in ubiquitous language (append-only, immutable, atomic)
- [x] Persistence handled through repository port abstraction, not direct storage coupling
- [x] Consistency boundary explicitly defined around EvaluationResult and related entities

**Clean Architecture**:
- [x] Repository port (abstraction) defined in domain/application boundary
- [x] Infrastructure layer provides adapter implementations
- [x] No domain imports of storage-specific libraries
- [x] All external persistence interactions through domain-defined interfaces

**Layered Phase Independence**:
- [x] Persistence phase (P1) independent from evaluation phase (upstream)
- [x] Persistence phase separate from query/replay phases (P2, P3)
- [x] Phases communicate through serializable, technology-agnostic entity models
- [x] Changes to query implementation do not require persistence logic changes

**Determinism & Reproducibility**:
- [x] Identical evaluation results must produce identical persisted data (deterministic serialization)
- [x] Replay of persisted metrics + rules must yield byte-identical outcomes (SC-003)
- [x] Timestamp handling explicit (UTC-based, from evaluation process)
- [x] All inputs, metrics, and evidence explicitly captured for deterministic replay

**Engine-Agnostic Abstraction**:
- [x] Repository abstraction does not depend on execution engine or engine-specific result formats
- [x] Normalization of engine outputs to domain model occurs in evaluation phase (upstream)
- [x] Persisted entities use domain models, not engine-specific structures
- [x] Multiple engines supported without duplicating persistence logic

**Evolution-Friendly Design**:
- [x] No specific storage technology locked in (database, file system, cloud storage all supportable)
- [x] Repository port allows multiple implementations without domain changes
- [x] Append-only semantics extensible for future versioning/auditing needs
- [x] P1 persistence, P2 querying, P3 replay compose without architectural violations

✅ **PASSED - No constitutional violations detected**

## Project Structure

### Documentation (this feature)

```text
specs/001-persist-results/
├── spec.md              # User stories, requirements, acceptance criteria
├── plan.md              # This file (Phase 0-1 planning output)
├── research.md          # Phase 0 research: technology decisions and patterns
├── data-model.md        # Phase 1 entity models, value objects, consistency boundaries
├── quickstart.md        # Phase 1 developer getting started guide
├── contracts/           # Phase 1 port definitions and API contracts
│   ├── IEvaluationResultRepository.cs
│   ├── EvaluationResultDto.cs
│   ├── ViolationDto.cs
│   ├── EvaluationEvidenceDto.cs
│   └── MetricReferenceDto.cs
└── checklists/
    └── requirements.md  # Specification quality validation
```

### Source Code (repository root)

```text
src/
├── PerformanceEngine.Metrics.Domain/       # Existing: metric domain models
├── PerformanceEngine.Evaluation.Domain/    # Existing: evaluation results, violations
├── PerformanceEngine.Evaluation.Infrastructure/  # NEW: repository implementations
│   ├── Persistence/
│   │   ├── InMemoryEvaluationResultRepository.cs    # In-memory adapter for testing
│   │   └── SqlEvaluationResultRepository.cs         # SQL adapter (future implementation)
│   └── ServiceCollectionExtensions.cs               # DI registration
└── PerformanceEngine.Application/          # Existing: use cases, orchestration

tests/
├── PerformanceEngine.Evaluation.Domain.Tests/
│   └── PersistenceScenarios/
│       ├── AtomicPersistenceTests.cs       # NEW: Verify atomic writes
│       ├── ImmutabilityTests.cs            # NEW: Enforce immutability
│       ├── AppendOnlyTests.cs              # NEW: Append-only semantics
│       └── DeterministicReplayTests.cs     # NEW: Deterministic serialization
└── PerformanceEngine.Evaluation.Infrastructure.Tests/
    ├── InMemoryRepositoryTests.cs          # NEW: In-memory adapter tests
    └── ConcurrencyTests.cs                 # NEW: Concurrent persistence validation
```

**Structure Decision**: Multi-project structure following existing Clean Architecture pattern. 
- Domain models and repository ports defined in `PerformanceEngine.Evaluation.Domain` 
- Infrastructure adapters in new `PerformanceEngine.Evaluation.Infrastructure` project
- Integration tests verify cross-layer contracts
- In-memory repository provides testability; SQL implementation deferred to infrastructure hardening phase

## Phase 0: Research & Clarifications

### No Clarifications Required

All technical context is explicit and derivable from specification and existing project patterns:

1. **Language/Framework**: C# 12, .NET 8.0 ✅
2. **Repository Pattern**: Existing project demonstrates port abstraction usage ✅
3. **Serialization**: C# standard mechanisms (JSON with deterministic ordering) ✅
4. **Concurrency**: Task-based async patterns following .NET conventions ✅
5. **Testing**: xUnit with existing test infrastructure ✅

### Research Findings: SOLID Patterns in Existing Codebase

**Single Responsibility**: Each domain entity has one reason to change (business rule changes)
**Open/Closed**: Repository port allows extension (new adapter implementations) without modification
**Liskov Substitution**: All repository implementations substitutable for `IEvaluationResultRepository`
**Interface Segregation**: Repository port focused on persistence, not query complexity (queries handled at application layer)
**Dependency Inversion**: Domain depends on repository abstraction; infrastructure implements abstraction

### Research Findings: Deterministic Serialization Strategy

C# serialization considerations:
- Use `System.Text.Json` with deterministic ordering for DTOs
- Timestamp handling: UTC, ISO 8601 string format
- Decimal precision: Explicit (metric values as string to preserve precision)
- Collection ordering: Violations and evidence persisted in definition order, not hash-based ordering
- GUIDs: ToString("D") format for consistency

### Key Decisions

1. **Repository abstraction lives in domain layer** - Port defined in `PerformanceEngine.Evaluation.Domain/Ports/`
2. **In-memory implementation first** - Enables rapid testing; SQL adapter in future phase
3. **Immutability enforced at domain level** - Entity constructors private; only read-only properties exposed
4. **Atomic writes via application service** - Orchestration layer ensures all-or-nothing semantics
5. **No-op delete/update operations** - Repository interface omits these; violations of append-only manifest as compile-time errors

**Phase 0 Status**: ✅ Complete - All clarifications resolved, research findings consolidated

---

## Phase 1: Design & Contracts

### 1. Domain Model Extraction

**Primary Entity**: `EvaluationResult`
- `Id`: Unique identifier (GUID)
- `Outcome`: Severity enum (Pass, Warning, Fail)
- `Violations`: ImmutableList<Violation>
- `Evidence`: ImmutableList<EvaluationEvidence>
- `OutcomeReason`: string (rationale for outcome)
- `EvaluatedAt`: DateTime (UTC, from evaluation process)

**Supporting Entities**:
- `Violation`: Rule name, metric name, severity, actual value, threshold, message
- `EvaluationEvidence`: Rule ID, rule name, metrics, actual values, constraint, satisfaction status, outcome, timestamp
- `MetricReference`: Immutable reference (name + value) for replay purposes

All entities: read-only properties, value-based equality, immutable after construction

### 2. Repository Port Contract

**File**: `specs/001-persist-results/contracts/IEvaluationResultRepository.cs`

```csharp
namespace PerformanceEngine.Evaluation.Ports;

public interface IEvaluationResultRepository
{
    /// <summary>
    /// Persist an evaluation result atomically.
    /// Either the entire result is persisted or nothing is persisted (no partial writes).
    /// </summary>
    Task<EvaluationResult> PersistAsync(EvaluationResult result, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieve a persisted evaluation result by unique identifier.
    /// Returns the exact immutable result with all original metadata.
    /// </summary>
    Task<EvaluationResult?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Query persisted evaluation results by timestamp range.
    /// Results returned in chronological order.
    /// </summary>
    Task<IAsyncEnumerable<EvaluationResult>> QueryByTimestampRangeAsync(
        DateTime startUtc, DateTime endUtc, CancellationToken cancellationToken = default);

    /// <summary>
    /// Query persisted evaluation results by test identifier.
    /// Only results matching the identifier are returned.
    /// </summary>
    Task<IAsyncEnumerable<EvaluationResult>> QueryByTestIdAsync(
        string testId, CancellationToken cancellationToken = default);
}
```

**Key Properties**:
- Technology-agnostic (no SQL, no file paths, no engine-specific details)
- Domain-focused method names (PersistAsync, GetByIdAsync, not SaveToDB, ExecuteQuery)
- Append-only semantics enforced (no Update, Delete methods)
- Async-first for scalability
- CancellationToken support for graceful shutdown

### 3. Data Transfer Object Contracts

**Files**: `specs/001-persist-results/contracts/*.cs`

DTO layer ensures serialization contract stability:
- `EvaluationResultDto`: Serializable representation with deterministic JSON ordering
- `ViolationDto`: Rule/metric/threshold/actual immutable snapshot
- `EvaluationEvidenceDto`: Complete audit trail snapshot
- `MetricReferenceDto`: Metric name + value (replay enablement)

All DTOs: immutable properties, JSON-serializable, deterministic ordering

### 4. Entity and Value Object Definitions

**File**: `specs/001-persist-results/data-model.md`

Complete entity definitions with:
- Properties and types
- Validation rules
- Immutability constraints
- Equality semantics (value-based vs identity-based)
- State transitions (if applicable)
- Consistency boundaries

### 5. Quickstart Guide for Developers

**File**: `specs/001-persist-results/quickstart.md`

Covers:
- How to use the repository port
- How to construct immutable EvaluationResult entities
- How to register repository implementations in DI
- How to test with in-memory repository
- How to verify atomic persistence
- How to query and replay results

### 6. Agent Context Update

Run `.specify/scripts/bash/update-agent-context.sh copilot` to inject learned technology patterns into agent context for code generation phase.

**Phase 1 Status**: ✅ Complete - Ports defined, contracts established, data model specified

---

## Complexity Tracking

> No constitutional violations requiring justification.

All architectural decisions align with principles:
| Decision | Principle Served | Rationale |
|----------|------------------|-----------|
| Repository port abstraction | Clean Architecture + Engine-Agnostic Abstraction | Enables multiple storage implementations without domain coupling |
| Immutable entities + value-based equality | Domain Purity + Determinism | Ensures replay produces identical results; prevents accidental mutations |
| Append-only semantics enforcement | Specification-Driven Development | Explicit from requirements; compile-time safety through interface design |
| Separate in-memory and SQL adapters | Evolution-Friendly Design | Supports testing without DB dependency; extensible to new storage technologies |

---

## Next Steps (Phase 2: Implementation via /speckit.tasks)

This plan establishes the design foundation. Phase 2 implementation will be driven by `/speckit.tasks`:

1. **Generate tasks.md** with implementation subtasks aligned to port contracts
2. **Create domain entities** with immutability enforced at compile time
3. **Implement in-memory repository** for rapid testing and validation
4. **Add comprehensive tests** covering atomic persistence, immutability, append-only semantics
5. **Validate deterministic replay** with same-input-same-output verification
6. **Integrate with application layer** via use cases and services
7. **Document completion** with audit trail and CI validation

---

**Plan Status**: ✅ Complete - Ready for Phase 2 implementation via `/speckit.tasks`
