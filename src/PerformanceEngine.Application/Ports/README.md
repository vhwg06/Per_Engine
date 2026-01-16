# Application Layer - Domain Ports

## Overview

Domain ports are abstraction interfaces that define how the Application layer interacts with domain services. They follow the **Dependency Inversion Principle**: Application layer defines the interfaces it needs, and infrastructure/domain layers implement them.

This inverts the traditional dependency direction:
```
Traditional:  Application → Domain Implementation
With Ports:   Application → Port Interface ← Domain Implementation
```

## Port Contracts

### IMetricsProvider

**Purpose**: Provides access to collected performance metrics.

**Responsibilities**:
- Return immutable collection of metric samples
- Check metric availability by name
- Provide list of available metric names

**Usage**:
```csharp
var samples = metricsProvider.GetAvailableSamples();
var hasLatency = metricsProvider.IsMetricAvailable("latency_p99");
var available = metricsProvider.GetAvailableMetricNames();
```

**Constraints**:
- Returns read-only collections (no mutation)
- Metrics are provided, not computed by this interface
- Empty collection is valid (partial metrics scenario)

---

### IProfileResolver

**Purpose**: Resolves profile configurations by identifier.

**Responsibilities**:
- Resolve profile configuration by ID
- List available profile IDs
- Check if profile exists

**Usage**:
```csharp
var profile = profileResolver.ResolveProfile("production-api");
var profiles = profileResolver.GetAvailableProfileIds();
var exists = profileResolver.ProfileExists("staging");
```

**Constraints**:
- Throws ArgumentException if profile not found
- Returns ResolvedProfile (immutable domain object)
- Profile must be resolved before use in evaluation

**Error Handling**:
- Profile not found → ArgumentException with clear message
- Invalid profile → Domain validation exception

---

### IEvaluationRulesProvider

**Purpose**: Provides evaluation rules and delegates rule execution to domain.

**Responsibilities**:
- Return collection of rule definitions
- Evaluate individual rule against metrics
- Provide rule metadata (required metrics, severity)

**Usage**:
```csharp
var rules = rulesProvider.GetRules();
var requiredMetrics = rulesProvider.GetRequiredMetrics("latency-rule-1");
var result = rulesProvider.EvaluateRule(rule, samples);
```

**Constraints**:
- Rules are immutable definitions
- Evaluation is pure (no side effects)
- Returns domain EvaluationResult with violations
- Same rule + same metrics → same result (deterministic)

**Rule Definition**:
- **RuleId**: Unique identifier (used for deterministic ordering)
- **RuleName**: Human-readable name
- **Severity**: Critical (FAIL) or NonCritical (WARN)
- **RequiredMetrics**: Metrics needed for evaluation

---

## Implementation Guidelines

### For Infrastructure Layer

When implementing these ports in infrastructure:

1. **IMetricsProvider Implementation**:
   ```csharp
   public class InMemoryMetricsProvider : IMetricsProvider
   {
       private readonly IReadOnlyCollection<Sample> _samples;
       
       public IReadOnlyCollection<Sample> GetAvailableSamples() => _samples;
       
       public bool IsMetricAvailable(string metricName)
       {
           // Check if samples contain this metric type
       }
   }
   ```

2. **IProfileResolver Implementation**:
   ```csharp
   public class FileBasedProfileResolver : IProfileResolver
   {
       private readonly Dictionary<string, ResolvedProfile> _profiles;
       
       public ResolvedProfile ResolveProfile(string profileId)
       {
           if (!_profiles.TryGetValue(profileId, out var profile))
           {
               throw new ArgumentException($"Profile not found: {profileId}");
           }
           return profile;
       }
   }
   ```

3. **IEvaluationRulesProvider Implementation**:
   ```csharp
   public class DomainRulesProvider : IEvaluationRulesProvider
   {
       public EvaluationResult EvaluateRule(EvaluationRuleDefinition rule, IReadOnlyCollection<Sample> samples)
       {
           // Delegate to Evaluation Domain service
           return _evaluationService.Evaluate(rule, samples);
       }
   }
   ```

### Testing with Test Doubles

Use test doubles for isolated unit/integration testing:

```csharp
public class MockMetricsProvider : IMetricsProvider
{
    private readonly List<Sample> _samples = new();
    
    public void AddTestSample(Sample sample) => _samples.Add(sample);
    
    public IReadOnlyCollection<Sample> GetAvailableSamples() => _samples;
}
```

---

## Design Rationale

**Why Ports?**

1. **Testability**: Application logic testable without infrastructure
2. **Flexibility**: Swap implementations (file-based, HTTP, in-memory)
3. **Clean Architecture**: Dependencies point inward (Application defines needs)
4. **Decoupling**: Domain changes don't break Application layer
5. **Contract Clarity**: Explicit interface contracts

**Port vs Direct Dependency**:

❌ **Without Ports** (Tight Coupling):
```
Application → Metrics.Domain.Service
Application → Profile.Domain.Service
Application → Evaluation.Domain.Service
```

✅ **With Ports** (Loose Coupling):
```
Application → IMetricsProvider ← MetricsAdapter → Metrics.Domain
Application → IProfileResolver ← ProfileAdapter → Profile.Domain
Application → IEvaluationRulesProvider ← EvaluationAdapter → Evaluation.Domain
```

---

## Contract Guarantees

All ports must guarantee:

1. **Immutability**: Returned objects are immutable
2. **Thread-Safety**: Safe for concurrent access
3. **Determinism**: Same input → same output (no randomness, no DateTime.Now)
4. **No Side Effects**: Queries don't modify state
5. **Clear Errors**: Meaningful exceptions with context

---

## Evolution Strategy

### Adding New Ports

1. Define interface in `Ports/` directory
2. Document contract in this README
3. Implement in infrastructure layer
4. Create test doubles for testing
5. Wire up in dependency injection container

### Modifying Existing Ports

1. Add new methods (backward compatible)
2. Deprecate old methods (don't remove immediately)
3. Update documentation
4. Update all implementations
5. Update tests

### Versioning

Ports follow semantic versioning:
- Major version: Breaking changes (method signature changes)
- Minor version: New methods (backward compatible)
- Patch version: Documentation/comments only

---

## References

- [Clean Architecture by Robert C. Martin](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html)
- [Hexagonal Architecture (Ports & Adapters)](https://alistair.cockburn.us/hexagonal-architecture/)
- [Dependency Inversion Principle](https://en.wikipedia.org/wiki/Dependency_inversion_principle)
