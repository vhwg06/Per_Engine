# Base Architecture Documentation

This directory contains the architectural design documentation for the Performance & Reliability Testing Platform's foundational capabilities.

## Overview

The base architecture establishes three critical specifications:

1. **Repository Port** - Persistence abstraction following Clean Architecture
2. **CLI Interface** - Deterministic command-line interface with standardized exit codes
3. **Configuration Format** - Explicit defaults with strict validation and fail-fast behavior

## Documentation Structure

### Planning Documents

- **[base-architecture-plan.md](base-architecture-plan.md)** - Comprehensive implementation plan covering all three specifications, including research tasks, design artifacts, implementation phases, and success criteria.

### Design Documents

- **[repository-contracts.md](repository-contracts.md)** - Repository port interfaces, audit trail, versioning, transactions, and query specifications. Defines `IRepository<TEntity, TId>`, `IAuditLog`, `IVersionStore<TEntity, TId>`, `IUnitOfWork`, and `IQuerySpecification<TEntity>`.

- **[cli-commands.md](cli-commands.md)** - CLI command structure, exit code semantics, output formatters, and orchestration patterns. Covers commands: `run`, `baseline save/compare/list`, `validate`, `export`.

- **[config-schema.md](config-schema.md)** - Configuration schema, validation rules, default values, multi-source composition (file, env, CLI), and schema versioning.

### Examples

- **[../examples/sample-config.yaml](../examples/sample-config.yaml)** - Complete configuration example with comments explaining all options, default values, and usage patterns.

## Architecture Principles

### Clean Architecture Compliance

```
┌─────────────────────────────────────────┐
│ Infrastructure Layer                    │
│ - CLI (Program.cs, Commands)            │
│ - Redis* (BaselineRepository, etc.)     │
│ - Output Formatters (JSON, Text)        │
└──────────────┬──────────────────────────┘
               │ implements ports
┌──────────────┴──────────────────────────┐
│ Application Layer                       │
│ - Use Cases                             │
│ - Configuration Validator               │
│ - Configuration Composer                │
└──────────────┬──────────────────────────┘
               │ uses domain
┌──────────────┴──────────────────────────┐
│ Domain Layer                            │
│ - Repository Ports                      │
│ - Domain Models                         │
│ - Configuration Schema                  │
└─────────────────────────────────────────┘
```

**Key Principles:**
- **Dependencies point inward** - Infrastructure → Application → Domain
- **Domain layer has no infrastructure dependencies** - Ports defined in domain, implemented in infrastructure
- **Thin adapters** - CLI and infrastructure are thin wrappers around application logic

### Determinism & Reproducibility

All three specifications enforce deterministic behavior:
- **Repository**: Same operations produce same results
- **CLI**: Same input produces same output and exit code
- **Configuration**: Same config sources produce same merged configuration

### Fail-Fast Philosophy

- **Configuration validation** - All errors detected before execution
- **Repository operations** - Clear error semantics (not found, conflict, system error)
- **CLI exit codes** - Distinct codes for different failure types (0, 1, 2, 3+)

## Repository Port Contracts

### Core Interfaces

```csharp
// Generic repository for aggregate roots
IRepository<TEntity, TId>
  - CreateAsync(TEntity) → TId
  - GetByIdAsync(TId) → TEntity?
  - UpdateAsync(TEntity) → bool
  - DeleteAsync(TId) → bool
  - ExistsAsync(TId) → bool
  - QueryAsync(IQuerySpecification<TEntity>) → IReadOnlyList<TEntity>

// Audit trail (cross-cutting concern)
IAuditLog
  - RecordAsync(AuditRecord)
  - GetByEntityAsync(entityId, entityType) → IReadOnlyList<AuditRecord>
  - GetByTimeRangeAsync(start, end) → IReadOnlyList<AuditRecord>

// Versioning (advanced feature)
IVersionStore<TEntity, TId>
  - StoreVersionAsync(TId, TEntity, timestamp) → long
  - GetVersionAsync(TId, versionId) → TEntity?
  - GetVersionAtTimeAsync(TId, pointInTime) → TEntity?
  - GetVersionHistoryAsync(TId) → IReadOnlyList<VersionMetadata>

// Transactions
IUnitOfWork
  - BeginAsync()
  - CommitAsync()
  - RollbackAsync()
```

### Separation of Concerns

- **CRUD** - Basic entity operations in `IRepository<T, TId>`
- **Audit** - Cross-cutting audit trail in `IAuditLog` (separate storage)
- **Versioning** - Advanced time-travel in `IVersionStore<T, TId>` (optional)
- **Transactions** - Atomic operations in `IUnitOfWork`

### Backward Compatibility

Existing repository interfaces (`IPersistenceRepository`, `IBaselineRepository`) will be extended to implement `IRepository<T, TId>` without breaking changes.

## CLI Interface

### Commands

- **`run`** - Execute performance tests
- **`baseline save`** - Save test results as baseline
- **`baseline compare`** - Compare against baseline
- **`baseline list`** - List available baselines
- **`validate`** - Validate configuration without running
- **`export`** - Export results to external formats

### Exit Codes (Deterministic)

| Code | Name | Meaning |
|------|------|---------|
| 0 | SUCCESS | Tests passed |
| 1 | FAIL | Tests failed (thresholds violated) |
| 2 | INCONCLUSIVE | Insufficient data, partial results |
| 3 | CONFIG_ERROR | Invalid configuration |
| 4 | VALIDATION_ERROR | Validation failed |
| 5 | RUNTIME_ERROR | System error during execution |
| 130 | INTERRUPTED | SIGINT (Ctrl+C) |
| 143 | TERMINATED | SIGTERM |

### Output Formats

- **JSON** - Valid, parseable JSON for automation
- **Text** - Human-readable tables with color support
- **Stdout/Stderr** - Results to stdout, diagnostics to stderr

### Thin Adapter Pattern

CLI delegates all logic to application use cases:
```
CLI Command → Application Use Case → Domain Logic → Repository
```

## Configuration Format

### Schema Structure

```yaml
schemaVersion: "1.0"              # Required

testExecution:                    # Required
  duration: "10m"
  virtualUsers: 500
  rampUpPeriod: "2m"
  target: "https://api.example.com"

requestSettings:                  # Optional
  timeout: "60s"
  retries: 5
  headers: { }

rateLimits:                       # Optional
  requestsPerSecond: 5000
  concurrency: 200

thresholds:                       # Optional
  - metric: "p95"
    operator: "<="
    value: 300

profiles:                         # Optional
  smoke-test: { }
  load-test: { }
```

### Multi-Source Composition

**Precedence** (highest to lowest):
1. CLI arguments (`--duration 20m`)
2. Environment variables (`PERF_ENGINE_TEST_EXECUTION_DURATION=20m`)
3. Configuration file (`duration: "20m"`)
4. Default values (defined in `DefaultValuesRegistry`)

### Validation Phases

1. **Structural** - Schema version, required fields
2. **Type** - Field types (enforced by strong typing in C#)
3. **Range** - Numeric constraints (positive, non-negative)
4. **Logic** - Cross-field consistency (min ≤ max, rampUp ≤ duration)
5. **Dependency** - Required field combinations

### Default Values

All defaults are explicit and documented:
- `testExecution.rampUpPeriod`: `0s`
- `requestSettings.timeout`: `30s`
- `requestSettings.retries`: `3`
- `rateLimits.concurrency`: `100`
- See `DefaultValuesRegistry` for complete list

## Implementation Phases

### Phase 0: Research ✅
- Repository pattern extensions
- CLI framework selection
- Configuration validation strategy
- Audit trail storage pattern

### Phase 1: Design ✅
- Repository port contracts (this document)
- CLI command structure (this document)
- Configuration schema (this document)

### Phase 2: Implementation (Next)
- **Domain Layer**: Repository ports, configuration models
- **Application Layer**: Validators, use cases, composers
- **Infrastructure Layer**: CLI, repository implementations, formatters

### Phase 3: Verification
- Unit tests (per layer)
- Integration tests (cross-layer)
- Acceptance tests (specification compliance)

## Success Criteria

### Repository Port
- ✅ Domain layer has no infrastructure dependencies
- ✅ Repository ports support multiple storage technologies
- ✅ Audit trail captures changes within 100ms
- ✅ Versioning with <500ms retrieval latency
- ✅ Transactions succeed or rollback atomically

### CLI Interface
- ✅ Exit codes deterministic (0, 1, 2, 3+)
- ✅ JSON output valid and parseable (100%)
- ✅ Tests execute within 5 seconds of completion
- ✅ CI/CD integration with zero configuration

### Configuration Format
- ✅ 100% of errors detected before execution (fail-fast)
- ✅ Validation completes in <100ms
- ✅ Error messages include field, value, constraint, source
- ✅ Source attribution for all values

## References

- **Specifications**:
  - [/specs/001-repository-port/spec.md](../../specs/001-repository-port/spec.md)
  - [/specs/001-cli-interface/spec.md](../../specs/001-cli-interface/spec.md)
  - [/specs/001-config-format/spec.md](../../specs/001-config-format/spec.md)

- **Constitution**: [/.specify/memory/constitution.md](../../.specify/memory/constitution.md)

- **Project README**: [/README.md](../../README.md)

## Next Steps

1. **Review** - Review design documents with stakeholders
2. **Implement Domain Layer** - Repository ports, configuration models
3. **Implement Application Layer** - Validators, use cases, composers
4. **Implement Infrastructure Layer** - CLI, repository implementations
5. **Test** - Unit, integration, acceptance tests
6. **Verify** - Architecture compliance, specification compliance, success criteria

---

**Version**: 1.0  
**Created**: 2026-01-16  
**Status**: Design Complete, Ready for Implementation
