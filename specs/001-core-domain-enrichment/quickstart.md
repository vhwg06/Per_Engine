# Quick Start Guide: Core Domain Enrichment Implementation

**Completed**: 2026-01-15  
**Audience**: Developers implementing enrichments in Metrics, Evaluation, and Profile domains  
**Scope**: Step-by-step guidance for building enriched domain models

---

## Overview

This guide walks through implementing the Core Domain Enrichment feature across three domains:
- **Metrics Domain**: Add completeness metadata
- **Evaluation Domain**: Add evidence trail and INCONCLUSIVE outcome
- **Profile Domain**: Add deterministic resolution and validation gates

**Prerequisite Knowledge**:
- Familiarity with Clean Architecture and DDD
- Understanding of C# records, value objects, and immutability patterns
- Experience with xUnit testing and determinism verification

---

## Phase 1: Metrics Domain Enrichment

### Step 1: Create CompletessStatus Enum

**File**: `src/PerformanceEngine.Metrics.Domain/Domain/CompletessStatus.cs`

```csharp
namespace PerformanceEngine.Metrics.Domain.Domain
{
    /// <summary>
    /// Indicates reliability of metric data collection.
    /// </summary>
    public enum CompletessStatus
    {
        /// <summary>
        /// All required samples collected; metric is reliable.
        /// </summary>
        COMPLETE = 1,
        
        /// <summary>
        /// Incomplete data; metric should be used with caution or skipped.
        /// </summary>
        PARTIAL = 2
    }
}
```

### Step 2: Create MetricEvidence Value Object

**File**: `src/PerformanceEngine.Metrics.Domain/Domain/Metrics/MetricEvidence.cs`

```csharp
namespace PerformanceEngine.Metrics.Domain.Domain.Metrics
{
    public sealed class MetricEvidence : ValueObject
    {
        public int SampleCount { get; }
        public int RequiredSampleCount { get; }
        public string AggregationWindow { get; }
        
        public bool IsComplete => SampleCount >= RequiredSampleCount;
        
        public MetricEvidence(
            int sampleCount,
            int requiredSampleCount,
            string aggregationWindow)
        {
            if (sampleCount < 0)
                throw new ArgumentException("SampleCount must be non-negative", nameof(sampleCount));
            
            if (requiredSampleCount <= 0)
                throw new ArgumentException("RequiredSampleCount must be positive", nameof(requiredSampleCount));
            
            if (string.IsNullOrWhiteSpace(aggregationWindow))
                throw new ArgumentException("AggregationWindow required", nameof(aggregationWindow));
            
            SampleCount = sampleCount;
            RequiredSampleCount = requiredSampleCount;
            AggregationWindow = aggregationWindow;
        }
        
        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return SampleCount;
            yield return RequiredSampleCount;
            yield return AggregationWindow;
        }
    }
}
```

### Step 3: Extend IMetric Interface

**File**: `src/PerformanceEngine.Metrics.Domain/Domain/Ports/IMetric.cs`

```csharp
namespace PerformanceEngine.Metrics.Domain.Domain.Ports
{
    public interface IMetric
    {
        // Existing properties...
        Guid Id { get; }
        string AggregationName { get; }
        double Value { get; }
        string Unit { get; }
        DateTime ComputedAt { get; }
        
        // NEW properties for enrichment
        CompletessStatus CompletessStatus { get; }
        MetricEvidence GetEvidence();
    }
}
```

### Step 4: Update Metric Entity

**File**: `src/PerformanceEngine.Metrics.Domain/Domain/Metrics/Metric.cs`

```csharp
namespace PerformanceEngine.Metrics.Domain.Domain.Metrics
{
    public sealed record Metric : IMetric
    {
        public Guid Id { get; init; }
        public string AggregationName { get; init; }
        public double Value { get; init; }
        public string Unit { get; init; }
        public DateTime ComputedAt { get; init; }
        
        // NEW enrichment properties
        public CompletessStatus CompletessStatus { get; init; }
        private MetricEvidence _evidence = null!;
        
        public MetricEvidence GetEvidence() => _evidence;
        
        public static Metric Create(
            string aggregationName,
            double value,
            string unit,
            DateTime computedAt,
            int sampleCount,
            int requiredSampleCount,
            string aggregationWindow,
            CompletessStatus? overrideStatus = null)
        {
            // Validation...
            if (string.IsNullOrWhiteSpace(aggregationName))
                throw new ArgumentException("AggregationName required", nameof(aggregationName));
            
            var status = overrideStatus ?? 
                (sampleCount >= requiredSampleCount ? CompletessStatus.COMPLETE : CompletessStatus.PARTIAL);
            
            var evidence = new MetricEvidence(sampleCount, requiredSampleCount, aggregationWindow);
            
            return new Metric
            {
                Id = Guid.NewGuid(),
                AggregationName = aggregationName,
                Value = value,
                Unit = unit,
                ComputedAt = computedAt,
                CompletessStatus = status,
                _evidence = evidence
            };
        }
    }
}
```

### Step 5: Test Metrics Enrichment

**File**: `tests/PerformanceEngine.Metrics.Domain.Tests/Domain/MetricEnrichmentTests.cs`

```csharp
namespace PerformanceEngine.Metrics.Domain.Tests.Domain
{
    public class MetricEnrichmentTests
    {
        [Fact]
        public void Metric_WithCompleteData_HasCompleteStatus()
        {
            var metric = Metric.Create(
                aggregationName: "p95",
                value: 195.5,
                unit: "ms",
                computedAt: DateTime.UtcNow,
                sampleCount: 100,
                requiredSampleCount: 100,
                aggregationWindow: "5m");
            
            Assert.Equal(CompletessStatus.COMPLETE, metric.CompletessStatus);
            Assert.True(metric.GetEvidence().IsComplete);
        }
        
        [Fact]
        public void Metric_WithPartialData_HasPartialStatus()
        {
            var metric = Metric.Create(
                aggregationName: "p95",
                value: 195.5,
                unit: "ms",
                computedAt: DateTime.UtcNow,
                sampleCount: 45,
                requiredSampleCount: 100,
                aggregationWindow: "5m");
            
            Assert.Equal(CompletessStatus.PARTIAL, metric.CompletessStatus);
            Assert.False(metric.GetEvidence().IsComplete);
        }
    }
}
```

---

## Phase 2: Evaluation Domain Enrichment

### Step 1: Extend Outcome Enum

**File**: `src/PerformanceEngine.Evaluation.Domain/Domain/Evaluation/Outcome.cs`

```csharp
namespace PerformanceEngine.Evaluation.Domain.Domain.Evaluation
{
    public enum Outcome
    {
        PASS = 1,
        FAIL = 2,
        INCONCLUSIVE = 3  // NEW
    }
}
```

### Step 2: Create MetricReference Value Object

**File**: `src/PerformanceEngine.Evaluation.Domain/Domain/Evaluation/MetricReference.cs`

```csharp
namespace PerformanceEngine.Evaluation.Domain.Domain.Evaluation
{
    public sealed class MetricReference : ValueObject
    {
        public string AggregationName { get; }
        public CompletessStatus Completeness { get; }
        public double Value { get; }
        
        public MetricReference(string aggregationName, CompletessStatus completeness, double value)
        {
            AggregationName = aggregationName ?? throw new ArgumentNullException(nameof(aggregationName));
            Completeness = completeness;
            Value = value;
        }
        
        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return AggregationName;
            yield return Completeness;
            yield return Value;
        }
    }
}
```

### Step 3: Create EvaluationEvidence Value Object

**File**: `src/PerformanceEngine.Evaluation.Domain/Domain/Evaluation/EvaluationEvidence.cs`

```csharp
namespace PerformanceEngine.Evaluation.Domain.Domain.Evaluation
{
    public sealed class EvaluationEvidence : ValueObject
    {
        public string RuleId { get; }
        public string RuleName { get; }
        public IReadOnlyList<MetricReference> MetricsUsed { get; }
        public IReadOnlyDictionary<string, double> ActualValues { get; }
        public string ExpectedConstraint { get; }
        public bool ConstraintSatisfied { get; }
        public string Decision { get; }
        public DateTime EvaluatedAt { get; }
        
        public EvaluationEvidence(
            string ruleId,
            string ruleName,
            IReadOnlyList<MetricReference> metricsUsed,
            IReadOnlyDictionary<string, double> actualValues,
            string expectedConstraint,
            bool constraintSatisfied,
            string decision,
            DateTime evaluatedAt)
        {
            RuleId = ruleId ?? throw new ArgumentNullException(nameof(ruleId));
            RuleName = ruleName ?? throw new ArgumentNullException(nameof(ruleName));
            MetricsUsed = metricsUsed ?? throw new ArgumentNullException(nameof(metricsUsed));
            ActualValues = new Dictionary<string, double>(actualValues ?? new Dictionary<string, double>());
            ExpectedConstraint = expectedConstraint ?? throw new ArgumentNullException(nameof(expectedConstraint));
            ConstraintSatisfied = constraintSatisfied;
            Decision = decision ?? throw new ArgumentNullException(nameof(decision));
            EvaluatedAt = evaluatedAt;
        }
        
        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return RuleId;
            yield return RuleName;
            yield return string.Join(",", MetricsUsed.OrderBy(m => m.AggregationName).Select(m => m.AggregationName));
            yield return string.Join(",", ActualValues.OrderBy(kv => kv.Key).Select(kv => $"{kv.Key}:{kv.Value}"));
            yield return ExpectedConstraint;
            yield return ConstraintSatisfied;
            yield return Decision;
            yield return EvaluatedAt;
        }
    }
}
```

### Step 4: Update EvaluationResult Entity

**File**: `src/PerformanceEngine.Evaluation.Domain/Domain/Evaluation/EvaluationResult.cs`

```csharp
namespace PerformanceEngine.Evaluation.Domain.Domain.Evaluation
{
    public sealed record EvaluationResult
    {
        public required Guid Id { get; init; }
        public required Outcome Outcome { get; init; }  // EXTENDED
        public required IReadOnlyList<Violation> Violations { get; init; }
        public required EvaluationEvidence Evidence { get; init; }  // NEW
        public required string OutcomeReason { get; init; }  // NEW
        public required DateTime EvaluatedAt { get; init; }
        
        public static EvaluationResult CreatePass(
            IReadOnlyList<EvaluationEvidence> evidences,
            DateTime evaluatedAt)
        {
            return new EvaluationResult
            {
                Id = Guid.NewGuid(),
                Outcome = Outcome.PASS,
                Violations = ImmutableList<Violation>.Empty,
                Evidence = CombineEvidences(evidences),
                OutcomeReason = "All constraints satisfied with complete data.",
                EvaluatedAt = evaluatedAt
            };
        }
        
        public static EvaluationResult CreateFail(
            IReadOnlyList<Violation> violations,
            IReadOnlyList<EvaluationEvidence> evidences,
            DateTime evaluatedAt)
        {
            var sorted = violations
                .OrderBy(v => v.RuleId)
                .ThenBy(v => v.MetricName)
                .ToImmutableList();
            
            return new EvaluationResult
            {
                Id = Guid.NewGuid(),
                Outcome = Outcome.FAIL,
                Violations = sorted,
                Evidence = CombineEvidences(evidences),
                OutcomeReason = $"{violations.Count} constraint(s) violated.",
                EvaluatedAt = evaluatedAt
            };
        }
        
        public static EvaluationResult CreateInconclusive(
            IReadOnlyList<EvaluationEvidence> evidences,
            string reason,
            DateTime evaluatedAt)
        {
            return new EvaluationResult
            {
                Id = Guid.NewGuid(),
                Outcome = Outcome.INCONCLUSIVE,
                Violations = ImmutableList<Violation>.Empty,
                Evidence = CombineEvidences(evidences),
                OutcomeReason = reason,
                EvaluatedAt = evaluatedAt
            };
        }
    }
}
```

### Step 5: Update Evaluator Service

**File**: `src/PerformanceEngine.Evaluation.Domain/Domain/Evaluation/Evaluator.cs`

Update the `Evaluate` method to check metric completeness and return enriched results with evidence:

```csharp
public EvaluationResult Evaluate(
    IMetric metric,
    IRule rule,
    IReadOnlyList<string>? partialMetricAllowlist = null)
{
    var evaluatedAt = DateTime.UtcNow;
    
    // Check completeness
    if (metric.CompletessStatus == CompletessStatus.PARTIAL &&
        (partialMetricAllowlist == null || !partialMetricAllowlist.Contains(metric.AggregationName)))
    {
        // Return INCONCLUSIVE for incomplete metrics not explicitly allowed
        var evidence = new EvaluationEvidence(
            ruleId: rule.Id,
            ruleName: rule.Name,
            metricsUsed: new[] { new MetricReference(metric.AggregationName, metric.CompletessStatus, metric.Value) },
            actualValues: new Dictionary<string, double> { { metric.AggregationName, metric.Value } },
            expectedConstraint: rule.GetConstraintDescription(),
            constraintSatisfied: false,
            decision: $"Metric {metric.AggregationName} is partial; rule does not allow partial metrics.",
            evaluatedAt: evaluatedAt);
        
        return EvaluationResult.CreateInconclusive(
            new[] { evidence },
            $"Incomplete data for {metric.AggregationName}",
            evaluatedAt);
    }
    
    // Proceed with evaluation
    var ruleResult = rule.Evaluate(metric);
    
    var decisionEvidence = new EvaluationEvidence(
        ruleId: rule.Id,
        ruleName: rule.Name,
        metricsUsed: new[] { new MetricReference(metric.AggregationName, metric.CompletessStatus, metric.Value) },
        actualValues: new Dictionary<string, double> { { metric.AggregationName, metric.Value } },
        expectedConstraint: rule.GetConstraintDescription(),
        constraintSatisfied: ruleResult.IsPassing,
        decision: ruleResult.Explanation,
        evaluatedAt: evaluatedAt);
    
    return ruleResult.IsPassing
        ? EvaluationResult.CreatePass(new[] { decisionEvidence }, evaluatedAt)
        : EvaluationResult.CreateFail(ruleResult.Violations, new[] { decisionEvidence }, evaluatedAt);
}
```

### Step 6: Test Evaluation Enrichment

**File**: `tests/PerformanceEngine.Evaluation.Domain.Tests/Domain/EvaluationEnrichmentTests.cs`

```csharp
public class EvaluationEnrichmentTests
{
    [Fact]
    public void Evaluation_ReturnsInconclusive_WhenMetricPartial()
    {
        var metric = new Metric { /* ..., CompletessStatus = PARTIAL ... */ };
        var evaluator = new Evaluator();
        
        var result = evaluator.Evaluate(metric, rule);
        
        Assert.Equal(Outcome.INCONCLUSIVE, result.Outcome);
        Assert.NotNull(result.Evidence);
        Assert.Contains("partial", result.Evidence.Decision, StringComparison.OrdinalIgnoreCase);
    }
    
    [Fact]
    public void EvaluationResult_IsDeterministic_Across1000Runs()
    {
        var results = new List<string>();
        
        for (int i = 0; i < 1000; i++)
        {
            var result = evaluator.Evaluate(metric, rule);
            results.Add(JsonConvert.SerializeObject(result));
        }
        
        var first = results[0];
        Assert.All(results, json => Assert.Equal(first, json));
    }
}
```

---

## Phase 3: Profile Domain Enrichment

### Step 1: Create ProfileState Enum

**File**: `src/PerformanceEngine.Profile.Domain/Domain/Profiles/ProfileState.cs`

```csharp
namespace PerformanceEngine.Profile.Domain.Domain.Profiles
{
    public enum ProfileState
    {
        Unresolved = 1,
        Resolved = 2,
        Invalid = 3
    }
}
```

### Step 2: Create ProfileResolver Service

**File**: `src/PerformanceEngine.Profile.Domain/Domain/Profiles/ProfileResolver.cs`

```csharp
namespace PerformanceEngine.Profile.Domain.Domain.Profiles
{
    public sealed class ProfileResolver
    {
        public IReadOnlyDictionary<string, ConfigurationValue> Resolve(
            IReadOnlyDictionary<string, ConfigurationValue> overrides,
            IReadOnlyDictionary<string, ConfigurationValue> inputs)
        {
            var resolved = new Dictionary<string, ConfigurationValue>(
                inputs ?? new Dictionary<string, ConfigurationValue>());
            
            var sortedOverrides = overrides
                .OrderBy(kv => GetScopePriority(ExtractScope(kv.Key)))
                .ThenBy(kv => ExtractKey(kv.Key), StringComparer.Ordinal)
                .ToList();
            
            foreach (var (compositeKey, value) in sortedOverrides)
            {
                var key = ExtractKey(compositeKey);
                resolved[key] = value;
            }
            
            return new ReadOnlyDictionary<string, ConfigurationValue>(
                resolved.OrderBy(kv => kv.Key, StringComparer.Ordinal)
                        .ToDictionary(kv => kv.Key, kv => kv.Value));
        }
        
        private int GetScopePriority(string scope) =>
            scope switch
            {
                "global" => 1,
                "api" => 2,
                "endpoint" => 3,
                _ => 0
            };
        
        private string ExtractScope(string compositeKey) =>
            compositeKey.Contains(':') ? compositeKey.Substring(0, compositeKey.IndexOf(':')) : compositeKey;
        
        private string ExtractKey(string compositeKey) =>
            compositeKey.Contains(':') ? compositeKey.Substring(compositeKey.IndexOf(':') + 1) : compositeKey;
    }
}
```

### Step 3: Create Validation Value Objects

**File**: `src/PerformanceEngine.Profile.Domain/Domain/Validation/ValidationError.cs`

```csharp
namespace PerformanceEngine.Profile.Domain.Domain.Validation
{
    public sealed class ValidationError : ValueObject
    {
        public string Code { get; }
        public string Message { get; }
        public string Category { get; }
        public string? Scope { get; }
        public IReadOnlyList<string>? Path { get; }
        
        public ValidationError(
            string code,
            string message,
            string category,
            string? scope = null,
            IReadOnlyList<string>? path = null)
        {
            Code = code ?? throw new ArgumentNullException(nameof(code));
            Message = message ?? throw new ArgumentNullException(nameof(message));
            Category = category;
            Scope = scope;
            Path = path ?? ImmutableList<string>.Empty;
        }
        
        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return Code;
            yield return Message;
            yield return Category;
            yield return Scope;
            yield return string.Join(",", Path ?? new List<string>());
        }
    }
}
```

**File**: `src/PerformanceEngine.Profile.Domain/Domain/Validation/ValidationResult.cs`

```csharp
namespace PerformanceEngine.Profile.Domain.Domain.Validation
{
    public sealed class ValidationResult : ValueObject
    {
        public bool IsValid { get; }
        public IReadOnlyList<ValidationError> Errors { get; }
        
        public static ValidationResult Success() =>
            new ValidationResult(true, ImmutableList<ValidationError>.Empty);
        
        public static ValidationResult Failure(IReadOnlyList<ValidationError> errors) =>
            new ValidationResult(
                errors?.Count > 0 ? false : true,
                errors ?? ImmutableList<ValidationError>.Empty);
        
        private ValidationResult(bool isValid, IReadOnlyList<ValidationError> errors)
        {
            IsValid = isValid;
            Errors = errors;
        }
    }
}
```

### Step 4: Update Profile Entity

**File**: `src/PerformanceEngine.Profile.Domain/Domain/Profiles/Profile.cs`

```csharp
namespace PerformanceEngine.Profile.Domain.Domain.Profiles
{
    public sealed class Profile
    {
        public Guid Id { get; }
        public string Name { get; }
        public ProfileState State { get; private set; }
        
        private IDictionary<string, ConfigurationValue> _overrides;
        private IReadOnlyDictionary<string, ConfigurationValue>? _resolved;
        private IReadOnlyList<ValidationError>? _validationErrors;
        
        public IReadOnlyDictionary<string, ConfigurationValue>? ResolvedConfiguration => _resolved;
        public IReadOnlyList<ValidationError>? ValidationErrors => _validationErrors;
        
        public Profile(Guid id, string name)
        {
            Id = id;
            Name = name;
            State = ProfileState.Unresolved;
            _overrides = new Dictionary<string, ConfigurationValue>();
        }
        
        public void ApplyOverride(string scope, string key, ConfigurationValue value)
        {
            if (State != ProfileState.Unresolved)
                throw new InvalidOperationException(
                    $"Cannot apply overrides when profile is {State}.");
            
            _overrides[$"{scope}:{key}"] = value;
        }
        
        public void Resolve(IReadOnlyDictionary<string, ConfigurationValue>? inputs = null)
        {
            if (State != ProfileState.Unresolved)
                throw new InvalidOperationException(
                    $"Cannot resolve profile in {State} state.");
            
            var resolver = new ProfileResolver();
            _resolved = resolver.Resolve(_overrides, inputs ?? new Dictionary<string, ConfigurationValue>());
            State = ProfileState.Resolved;
        }
        
        public ConfigurationValue Get(string key)
        {
            if (State != ProfileState.Resolved)
                throw new InvalidOperationException(
                    $"Profile must be Resolved (current state: {State}) before reading values.");
            
            if (!_resolved!.TryGetValue(key, out var value))
                throw new KeyNotFoundException($"Configuration key '{key}' not found.");
            
            return value;
        }
        
        public ValidationResult Validate(IProfileValidator validator)
        {
            var result = validator.Validate(this);
            
            if (!result.IsValid)
            {
                State = ProfileState.Invalid;
                _validationErrors = result.Errors;
            }
            
            return result;
        }
    }
}
```

### Step 5: Create IProfileValidator Port

**File**: `src/PerformanceEngine.Profile.Domain/Domain/Validation/IProfileValidator.cs`

```csharp
namespace PerformanceEngine.Profile.Domain.Domain.Validation
{
    public interface IProfileValidator
    {
        ValidationResult Validate(Profile profile);
    }
}
```

### Step 6: Test Profile Enrichment

**File**: `tests/PerformanceEngine.Profile.Domain.Tests/Domain/ProfileEnrichmentTests.cs`

```csharp
public class ProfileEnrichmentTests
{
    [Fact]
    public void Profile_Resolve_IsDeterministic_AndOrderIndependent()
    {
        var profile1 = new Profile(Guid.NewGuid(), "Config");
        profile1.ApplyOverride("global", "timeout", new ConfigurationValue(30));
        profile1.ApplyOverride("api", "timeout", new ConfigurationValue(60));
        profile1.ApplyOverride("endpoint", "timeout", new ConfigurationValue(120));
        profile1.Resolve();
        
        var profile2 = new Profile(Guid.NewGuid(), "Config");
        profile2.ApplyOverride("endpoint", "timeout", new ConfigurationValue(120));
        profile2.ApplyOverride("global", "timeout", new ConfigurationValue(30));
        profile2.ApplyOverride("api", "timeout", new ConfigurationValue(60));
        profile2.Resolve();
        
        Assert.Equal(120, profile1.Get("timeout").AsInt());
        Assert.Equal(120, profile2.Get("timeout").AsInt());
    }
    
    [Fact]
    public void Profile_Immutable_AfterResolution()
    {
        var profile = new Profile(Guid.NewGuid(), "Config");
        profile.ApplyOverride("global", "timeout", new ConfigurationValue(30));
        profile.Resolve();
        
        Assert.Throws<InvalidOperationException>(() =>
            profile.ApplyOverride("api", "timeout", new ConfigurationValue(60)));
    }
}
```

---

## Testing Strategy

### Unit Tests
- Test each domain enrichment in isolation
- Mock dependencies (MetricProvider, ProfileValidator, etc.)
- Focus on invariants and state transitions

### Contract Tests
- Verify interface contracts (JSON serialization, determinism)
- Test across all implementations

### Integration Tests
- Test end-to-end evaluation flow with enrichments
- Verify validation gates prevent invalid profiles from evaluation
- Confirm evidence trail is complete and sufficient

### Determinism Tests
- Run evaluations 1000+ times with identical inputs
- Verify identical JSON serialization of results
- Test with different input orders for profile resolution

---

## Implementation Checklist

### Metrics Domain
- [ ] Create CompletessStatus enum
- [ ] Create MetricEvidence value object
- [ ] Extend IMetric interface
- [ ] Update Metric entity
- [ ] Add unit tests for completeness
- [ ] Verify backward compatibility

### Evaluation Domain
- [ ] Extend Outcome enum (INCONCLUSIVE)
- [ ] Create MetricReference value object
- [ ] Create EvaluationEvidence value object
- [ ] Update EvaluationResult entity
- [ ] Update Evaluator service (partial metric handling)
- [ ] Add unit tests for INCONCLUSIVE outcome
- [ ] Verify determinism across 1000+ runs

### Profile Domain
- [ ] Create ProfileState enum
- [ ] Create ProfileResolver service
- [ ] Create ValidationError and ValidationResult value objects
- [ ] Create IProfileValidator port
- [ ] Update Profile entity (state machine)
- [ ] Add unit tests for deterministic resolution
- [ ] Verify immutability after resolution
- [ ] Add validation tests

### Application Layer
- [ ] Update EvaluationService to use validation gates
- [ ] Add profile resolution to evaluation flow
- [ ] Verify backward compatibility

### Documentation
- [ ] Update domain README files with enrichment examples
- [ ] Document validation rules
- [ ] Add troubleshooting guide

---

## Common Issues and Solutions

### Issue: Metric Completeness Not Exposed in Engine Adapter

**Solution**: Update engine adapter to populate MetricEvidence when creating Metric instances:
```csharp
public Metric MapToMetric(EngineMetricData data)
{
    return Metric.Create(
        aggregationName: data.Name,
        value: data.Value,
        unit: data.Unit,
        computedAt: data.ComputedAt,
        sampleCount: data.SampleCount,
        requiredSampleCount: 100,  // Or from config
        aggregationWindow: "5m");   // Or from config
}
```

### Issue: INCONCLUSIVE Outcomes Not Handled in CI

**Solution**: Update CI integration to handle INCONCLUSIVE as configurable action (retry/pass/fail):
```csharp
public static int MapOutcomeToExitCode(Outcome outcome, EvaluationPolicy policy)
{
    return outcome switch
    {
        Outcome.PASS => 0,
        Outcome.FAIL => 1,
        Outcome.INCONCLUSIVE => policy.InconclusiveAction switch
        {
            InconclusiveAction.Fail => 1,
            InconclusiveAction.Pass => 0,
            InconclusiveAction.Retry => -1,  // Signal retry
            _ => 0
        },
        _ => 1
    };
}
```

### Issue: Profile Resolution Not Deterministic

**Solution**: Verify ProfileResolver sorts all keys before application:
```csharp
// Correct: sorted keys ensure determinism
var sorted = overrides
    .OrderBy(kv => GetScopePriority(ExtractScope(kv.Key)))
    .ThenBy(kv => ExtractKey(kv.Key), StringComparer.Ordinal)  // String order
    .ToList();

// Incorrect: dictionary iteration order is non-deterministic
foreach (var (key, value) in overrides)  // BAD: dictionary order undefined
{
    // ...
}
```

---

## Next Steps

1. **Implement in Order**: Metrics → Evaluation → Profile (dependencies flow down)
2. **Test Each Domain**: Unit tests, contract tests, integration tests
3. **Verify Determinism**: 1000+ iteration tests for each domain
4. **Document API Changes**: Update external documentation with enrichment details
5. **Gradual Rollout**: Enable enrichments incrementally in application layer

---

**Status**: ✅ Quick Start Guide Complete
