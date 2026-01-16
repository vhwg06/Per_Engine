# Baseline Domain

## Overview

The Baseline Domain implements deterministic comparison logic between performance test results and an immutable baseline snapshot. It enables automated regression detection by comparing current metrics against a baseline with configurable tolerance thresholds and confidence assessments.

## Key Features

- **Immutable Baseline Snapshots**: Once created, baselines cannot be modified
- **Deterministic Comparisons**: Identical inputs always produce identical results
- **Flexible Tolerance Configuration**: Support for both absolute and relative tolerance rules
- **Confidence Levels**: Quantifies certainty in comparison outcomes
- **Domain-Driven Design**: Pure domain logic independent of infrastructure

## Architecture

```
Domain/
├── Baselines/          # Baseline aggregate root and value objects
├── Comparisons/        # Comparison logic and result aggregation
├── Tolerances/         # Tolerance configuration and evaluation
├── Confidence/         # Confidence calculation and assessment
└── Events/             # Domain events

Application/
├── Services/           # Use case orchestration
├── Dto/                # Data transfer objects
└── UseCases/           # Business use cases

Ports/
└── IBaselineRepository # Repository abstraction for persistence
```

## Quick Start

See [IMPLEMENTATION_GUIDE.md](IMPLEMENTATION_GUIDE.md) for integration details.

## Domain Concepts

### Baseline
An immutable snapshot of performance metrics at a specific point in time. Contains metric values and tolerance configuration for comparisons.

### Tolerance
Rules that define acceptable variance for each metric. Can be:
- **ABSOLUTE**: Fixed deviation amount (e.g., ±50ms)
- **RELATIVE**: Percentage-based deviation (e.g., ±10%)

### Confidence Level
A value in [0.0, 1.0] representing certainty in a comparison outcome. Used to distinguish between conclusive and inconclusive results.

### Comparison Outcome
The result of comparing current metrics against a baseline:
- **IMPROVEMENT**: Current performance better than baseline
- **REGRESSION**: Current performance worse than baseline
- **NO_SIGNIFICANT_CHANGE**: Within tolerance threshold
- **INCONCLUSIVE**: Confidence too low to determine

## Testing

Run tests with:
```bash
dotnet test tests/PerformanceEngine.Baseline.Domain.Tests
dotnet test tests/PerformanceEngine.Baseline.Infrastructure.Tests
```

## Performance Goals

- Comparison operations: < 20ms p95
- 100+ concurrent comparisons supported
- Deterministic byte-identical output for identical inputs
