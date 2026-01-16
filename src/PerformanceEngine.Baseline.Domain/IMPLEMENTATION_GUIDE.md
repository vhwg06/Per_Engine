# Baseline Domain - Implementation Guide

## Architecture Overview

The Baseline Domain follows Clean Architecture principles with clear separation of concerns:

### Domain Layer (Pure Logic)
- **Aggregate Roots**: `Baseline`, `ComparisonResult`
- **Value Objects**: `BaselineId`, `Tolerance`, `ConfidenceLevel`, `ComparisonOutcome`
- **Domain Services**: `ComparisonCalculator`, `ConfidenceCalculator`, `OutcomeAggregator`
- **Ports**: `IBaselineRepository` (infrastructure boundary)

### Application Layer
- **Services**: `ComparisonOrchestrator` orchestrates domain logic
- **DTOs**: Data transfer objects for API/external communication
- **Use Cases**: Optional higher-level business operations

### Infrastructure Layer
- **Redis Adapter**: `RedisBaselineRepository` implements `IBaselineRepository`
- **Configuration**: Dependency injection setup and Redis connection management

## Key Classes & Responsibilities

### `Baseline` (Aggregate Root)
Immutable snapshot of performance metrics and configuration.

```csharp
public class Baseline
{
    public BaselineId Id { get; }
    public DateTime CreatedAt { get; }
    public IReadOnlyList<IMetric> Metrics { get; }
    public ToleranceConfiguration ToleranceConfig { get; }
    
    public IMetric? GetMetric(string name);
}
```

### `ComparisonCalculator` (Domain Service)
Pure functions for comparison logic.

```csharp
public class ComparisonCalculator
{
    public ComparisonMetric CalculateMetric(
        Baseline baseline,
        IMetric current,
        Tolerance tolerance,
        ConfidenceLevel threshold);
    
    public ComparisonOutcome DetermineOutcome(
        Tolerance tolerance,
        ConfidenceLevel confidence);
}
```

### `ComparisonResult` (Aggregate Root)
Immutable result of baseline comparison.

```csharp
public class ComparisonResult
{
    public ComparisonResultId Id { get; }
    public BaselineId BaselineId { get; }
    public DateTime ComparedAt { get; }
    public ComparisonOutcome OverallOutcome { get; }
    public ConfidenceLevel OverallConfidence { get; }
    public IReadOnlyList<ComparisonMetric> MetricResults { get; }
    
    public bool HasRegression();
}
```

### `IBaselineRepository` (Port)
Infrastructure abstraction for baseline persistence.

```csharp
public interface IBaselineRepository
{
    Task<BaselineId> CreateAsync(Baseline baseline);
    Task<Baseline?> GetByIdAsync(BaselineId id);
    Task<IReadOnlyList<Baseline>> ListRecentAsync(int count);
}
```

## Extension Points

### Custom Tolerance Strategies
Extend `Tolerance` class to support new evaluation logic:

```csharp
public class CustomTolerance : Tolerance
{
    public override bool IsWithinTolerance(decimal baseline, decimal current)
    {
        // Custom logic
    }
}
```

### Alternative Confidence Algorithms
Override `ConfidenceCalculator.CalculateConfidence()`:

```csharp
public class CustomConfidenceCalculator : ConfidenceCalculator
{
    protected override ConfidenceLevel Calculate(decimal magnitude, Tolerance tolerance)
    {
        // Your algorithm
    }
}
```

### Different Persistence Layers
Implement `IBaselineRepository` for alternative storage:

```csharp
public class PostgresBaselineRepository : IBaselineRepository
{
    // PostgreSQL implementation
}
```

## Error Handling

Common exceptions to handle:

- **`BaselineDomainException`**: Base exception for all domain errors
- **`BaselineNotFoundException`**: Baseline not found or expired
- **`ToleranceValidationException`**: Invalid tolerance configuration
- **`DomainInvariantViolatedException`**: Constraint violation (immutability, consistency)
- **`RepositoryException`**: Storage/infrastructure failure

## Testing Strategy

### Unit Tests
Test individual domain classes in isolation:
- Value object equality and immutability
- Domain service calculations
- Invariant enforcement

### Determinism Tests
Verify reproducibility (1000+ runs with identical input → identical output):
- Floating-point precision handling
- No ordering effects
- No timestamp-based logic

### Integration Tests
Test complete workflows:
- Create baseline → compare → inspect results
- Redis persistence → retrieval → comparison
- Cross-domain integration (Metrics Domain objects)

### Performance Tests
Validate performance targets:
- Individual comparison: < 20ms
- Batch operations: handle 100+ concurrent requests
- Redis latency: < 15ms p95

## Troubleshooting

### Floating-Point Precision Issues
Baseline Domain uses `decimal` type for precise calculations. Ensure tolerance thresholds are specified with appropriate precision.

### Comparison Determinism Failures
Check for:
- Non-deterministic random number generation (should not exist)
- Floating-point comparisons without rounding
- Ordering dependencies (should use sorted collections)

### Redis Connection Issues
Verify:
- Redis server is running
- Connection string in `appsettings.json`
- Network connectivity and firewall rules
- TTL configuration doesn't expire baselines prematurely

## Performance Optimization

### Caching
Consider caching frequently-accessed baselines in-memory:

```csharp
public class CachedBaselineRepository : IBaselineRepository
{
    private readonly Dictionary<BaselineId, Baseline> _cache = new();
    private readonly IBaselineRepository _inner;
    
    public async Task<Baseline?> GetByIdAsync(BaselineId id)
    {
        if (_cache.TryGetValue(id, out var baseline))
            return baseline;
        
        var result = await _inner.GetByIdAsync(id);
        if (result != null)
            _cache[id] = result;
        
        return result;
    }
}
```

### Batch Comparisons
For multiple comparisons with same baseline:

```csharp
public async Task<List<ComparisonResult>> CompareMultiple(
    BaselineId baselineId,
    IEnumerable<IReadOnlyList<IMetric>> currentMetricsList)
{
    var baseline = await _repository.GetByIdAsync(baselineId);
    return currentMetricsList
        .Select(metrics => Compare(baseline, metrics))
        .ToList();
}
```

## Integration with Other Domains

### Metrics Domain
Baseline accepts `IMetric` interface from Metrics Domain. No conversion needed.

### Evaluation Domain
Optional: Store evaluation results alongside baseline for context:

```csharp
public class BaselineSnapshot
{
    public Baseline Baseline { get; }
    public EvaluationResult? EvaluationContext { get; }
}
```

## Next Steps (Phase 2)

Planned enhancements:
- Metric weighting (prioritize critical metrics)
- Baseline versioning (track changes over time)
- Statistical confidence (hypothesis testing)
- Trend analysis and historical comparison
- Advanced tolerance strategies

## See Also

- [baseline-domain.spec.md](../../specs/baseline-domain/baseline-domain.spec.md) - Feature specification
- [plan.md](../../specs/baseline-domain/plan.md) - Implementation plan
- [research.md](../../specs/baseline-domain/research.md) - Technical decisions
- [data-model.md](../../specs/baseline-domain/data-model.md) - Domain entity details
