# Performance Engine - Application Layer

## Purpose

The Application layer provides orchestration services that coordinate between domain boundaries (Metrics, Profile, Evaluation) to produce deterministic, immutable performance evaluation results.

This layer follows Clean Architecture principles:
- **Dependencies flow inward**: Application → Domain Ports (no infrastructure dependencies)
- **Orchestration only**: Does not implement business rules; coordinates domain services
- **Immutable results**: All outputs are immutable, thread-safe records
- **Deterministic**: Same inputs always produce byte-identical outputs

## Key Components

### Orchestration

- **EvaluatePerformanceUseCase**: Main entry point for performance evaluation flow
- **RuleEvaluationCoordinator**: Manages deterministic rule ordering and evaluation
- **CompletenessAssessor**: Analyzes metric availability and data gaps
- **OutcomeAggregator**: Determines final outcome using precedence rules (FAIL > WARN > INCONCLUSIVE > PASS)
- **ResultConstructor**: Assembles immutable EvaluationResult objects

### Services

- **DeterministicFingerprintGenerator**: Generates SHA256 fingerprints of metric data for integrity verification

### Models

Core application models representing evaluation results:

- **EvaluationResult**: Complete evaluation outcome with violations, completeness, and metadata
- **Outcome**: Final verdict enum (PASS, WARN, FAIL, INCONCLUSIVE)
- **Violation**: Rule violation details with threshold/actual values
- **CompletenessReport**: Data availability transparency
- **ExecutionMetadata**: Traceability information

### Ports

Domain port abstractions for accessing external domain services:

- **IMetricsProvider**: Access to collected performance metrics
- **IProfileResolver**: Profile configuration resolution
- **IEvaluationRulesProvider**: Evaluation rules and rule execution

See [Ports/README.md](Ports/README.md) for detailed port contracts.

## Usage Example

```csharp
// Create orchestration use case with domain port implementations
var useCase = new EvaluatePerformanceUseCase(
    metricsProvider,
    profileResolver,
    rulesProvider);

// Create execution context
var context = ExecutionContext.Create("production");

// Execute evaluation
var result = useCase.Execute("my-profile", context);

// Inspect results
Console.WriteLine($"Outcome: {result.Outcome}");
Console.WriteLine($"Violations: {result.Violations.Count}");
Console.WriteLine($"Completeness: {result.CompletenessReport.CompletenessPercentage:P0}");
Console.WriteLine($"Fingerprint: {result.DataFingerprint}");
```

## Determinism Guarantees

1. **Rule Ordering**: Rules always evaluated in ASCII-sorted order by Rule ID
2. **Metric Ordering**: Metrics always processed in sorted order for fingerprinting
3. **Violation Ordering**: Violations always sorted by Rule ID
4. **Idempotency**: Same inputs → byte-identical outputs
5. **Immutability**: Result objects cannot be modified after construction

## Architecture Constraints

**What this layer DOES:**
- ✅ Coordinate between domains
- ✅ Orchestrate evaluation workflow
- ✅ Aggregate outcomes deterministically
- ✅ Generate data integrity fingerprints
- ✅ Provide traceability metadata

**What this layer DOES NOT do:**
- ❌ Calculate metrics (delegated to Metrics Domain)
- ❌ Evaluate rule logic (delegated to Evaluation Domain)
- ❌ Store or persist data (no infrastructure dependencies)
- ❌ Integrate with external systems (HTTP, databases, etc.)
- ❌ Compare to baselines (out of scope)

## Testing

Tests are organized by purpose:

- **Unit Tests**: Test individual orchestration components in isolation
- **Integration Tests**: Test complete orchestration flow with test doubles
- **Determinism Tests**: Verify idempotency and reproducibility guarantees

Run tests:
```bash
dotnet test tests/PerformanceEngine.Application.Tests/
```

## Design Principles

1. **Clean Architecture**: Dependencies point inward (Application → Domain)
2. **SOLID Principles**: Single responsibility, open/closed, dependency inversion
3. **Immutability**: All results immutable after construction
4. **Determinism**: Predictable, reproducible behavior
5. **Fail-Fast**: Invalid configuration caught before evaluation
6. **Graceful Degradation**: Partial metrics handled without crashing

## Future Enhancements

Potential extensions (out of current scope):

- Multiple profile evaluation (compare profiles)
- Historical trend analysis
- Baseline comparison orchestration
- Weighted scoring algorithms
- ML-based anomaly detection integration
