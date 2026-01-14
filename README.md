# Performance & Reliability Engine (Per_Engine)

A specification-driven platform for defining, generating, executing, evaluating, and persisting performance and reliability test data in an automated and evolvable way.

## Overview

Per_Engine establishes foundational infrastructure for:
- **Specification-driven test definition**: Machine-readable specs drive behavior
- **Multi-phase architecture**: Specification → Generation → Execution → Analysis → Persistence → Reporting
- **Engine-agnostic abstraction**: Support multiple execution engines without domain coupling
- **Deterministic, reproducible outputs**: Suitable for CI/CD quality gates and historical analysis
- **Domain-driven design**: Performance and reliability concepts independent of infrastructure

## Project Governance

This project follows strict architectural principles defined in the **Project Constitution**:

📜 **[Constitution v1.0.0](.specify/memory/constitution.md)**

All specifications, implementations, and generated artifacts must conform to constitutional principles including:
- Specification-Driven Development
- Domain-Driven Design (DDD)
- Clean Architecture
- Layered Phase Independence
- Determinism & Reproducibility
- Engine-Agnostic Abstraction
- Evolution-Friendly Design

## Getting Started

### Quick Setup

```bash
# Clone repository
git clone <repository-url>
cd Per_Engine

# Build all projects
dotnet build

# Run all tests
dotnet test
```

### Domain Implementation Status

| Domain | Status | Tests | Documentation |
|--------|--------|-------|---------------|
| **Metrics Domain** | ✅ Complete | 162 passing | [README](src/PerformanceEngine.Metrics.Domain/README.md) |
| **Evaluation Domain** | ✅ Complete | 120+ | [README](src/PerformanceEngine.Evaluation.Domain/README.md) |
| **Profile Domain** | ✅ Complete | Infrastructure ready | [README](src/PerformanceEngine.Profile.Domain/README.md) |

### Implemented Features

#### Metrics Domain
- ✅ Engine-agnostic performance metric model
- ✅ Deterministic aggregations (Average, Min, Max, Percentiles)
- ✅ Immutable samples and metrics
- ✅ Clean Architecture boundaries

[Quick Start Guide](specs/001-metrics-domain/quickstart.md)

#### Evaluation Domain  
- ✅ Deterministic rule evaluation (1000+ iteration tested)
- ✅ Extensible rule types (Threshold, Range, Composite, Custom)
- ✅ Batch evaluation support
- ✅ Structured violation reporting

[Quick Start Guide](specs/evaluation-domain/quickstart.md)

#### Profile Domain
- ✅ Deterministic configuration resolution  
- ✅ Hierarchical scope precedence (Global < API < Environment < Tag)
- ✅ Multi-dimensional contexts (Composite scopes)
- ✅ Extensible custom scopes
- ✅ Conflict detection with fail-fast behavior

[README](src/PerformanceEngine.Profile.Domain/README.md) | [Spec](specs/profile-domain/spec.md)

## Project Structure

```
Per_Engine/
├── .specify/               # Project governance and templates
│   ├── memory/
│   │   └── constitution.md # Project constitution (v1.0.0)
│   ├── templates/          # Feature specification templates
│   └── scripts/            # Automation scripts
├── .github/
│   ├── agents/             # Agent-driven workflow definitions
│   └── prompts/            # Prompt templates
└── README.md               # This file
```

## Development Workflow

This project uses a spec-driven development workflow powered by Speckit agents:

1. `/speckit.specify` - Define feature specifications
2. `/speckit.plan` - Generate implementation plans with constitution checks
3. `/speckit.tasks` - Break down into actionable tasks
4. `/speckit.implement` - Execute implementation
5. `/speckit.analyze` - Validate against constitution and requirements

See [.github/agents/](.github/agents/) for agent definitions.

## Contributing

All contributions must:
- Begin with a feature specification following constitution principles
- Pass constitution compliance checks before implementation
- Maintain clean architecture boundaries
- Respect domain independence from infrastructure

## License

*(To be determined)*

---

**Version**: 1.0.0 (Initial Project Setup)  
**Last Updated**: 2026-01-14
