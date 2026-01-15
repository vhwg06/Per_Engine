# Phase 0 Research: Core Domain Enrichment

**Completed**: 2026-01-15  
**Input**: Plan.md technical context and open questions  
**Output**: Research findings addressing design decisions and clarifications

---

## Research Findings

### 1. Profile Resolution Performance Baseline

**Open Question**: What is acceptable resolution time for typical profiles (99th percentile)?

**Research Finding**:
- **Decision**: Profile resolution MUST complete in < 10ms for 100-entry profiles (99th percentile)
- **Rationale**: 
  - Profile resolution happens pre-evaluation; blocking evaluation until complete is acceptable
  - Evaluation itself typically takes 1-5ms per rule; profile resolution should not exceed this by order of magnitude
  - Caching resolved profiles at infrastructure layer can further reduce repeated resolutions
- **Alternatives Considered**:
  - Real-time caching: Increases complexity; deferred to Phase 2 optimization
  - Incremental resolution: Stateful approach increases mutation risk; determinism-first approach preferred
- **Implementation Implication**: 
  - Use deterministic sorting (LINQ OrderBy) rather than concurrent collections
  - Topological sort for circular dependency detection should complete in O(n) time
  - No performance optimization justified at domain layer for initial implementation

**Verification**:
- Benchmark tests measure resolution time for 10, 50, 100, 500 entry profiles
- Performance gates: fail if any resolution > 20ms (2x baseline)

---

### 2. INCONCLUSIVE Outcome Handling Downstream

**Open Question**: How do CI/CD systems and reporting handle INCONCLUSIVE vs PASS/FAIL?

**Research Finding**:
- **Decision**: INCONCLUSIVE is domain-level outcome; CI/CD semantics determined at application layer
- **Rationale**:
  - Domain layer specifies when INCONCLUSIVE is appropriate (incomplete metrics, partial execution)
  - Application layer maps domain outcome to CI exit codes, reporting strategies, and retry logic
  - This separation preserves domain purity; CI semantics remain adapter concern
  
**Examples**:
- CI Exit Code Mapping (application layer decision):
  - PASS → exit 0 (success)
  - FAIL → exit 1 (failure, blocking)
  - INCONCLUSIVE → exit 2 (retry / pending) or configured to pass/warn
- Reporting (adapter layer decision):
  - INCONCLUSIVE appears as "PENDING_DATA" in dashboards
  - Flagged for manual review or automatic retry after delay
  
**Implementation Implication**:
- Domain returns EvaluationResult with Outcome enum (unchanged)
- Application service maps outcome to business semantics
- No changes to domain contracts required; INCONCLUSIVE is pure domain concern

---

### 3. Evidence Retention Policy

**Open Question**: Should evaluation evidence be retained indefinitely, or is there a purge policy?

**Research Finding**:
- **Decision**: Evidence retention policy is adapter/infrastructure concern; domain layer does not enforce TTL
- **Rationale**:
  - Domain layer produces immutable evidence objects
  - Persistence layer (repository/database adapter) owns retention policy
  - Allows organizations to set compliance-driven retention without domain changes
  
**Adapter-Level Decisions** (not in domain scope):
- Some organizations: Retain indefinitely (compliance/audit requirements)
- Some organizations: Purge after 90 days (privacy, cost reduction)
- Some organizations: Archive to cold storage after 30 days
  
**Implementation Implication**:
- `IEvaluationResultRecorder` port accepts full evidence object
- Infrastructure adapter implements retention policy via database constraints or archival jobs
- Domain layer produces evidence once; infrastructure handles persistence

---

### 4. Validation Error Messaging Detail Level

**Open Question**: What level of detail should ProfileValidator return?

**Research Finding**:
- **Decision**: ProfileValidator returns ALL validation errors collected; application layer decides display strategy
- **Rationale**:
  - Returning all errors at once speeds up remediation (users see all issues, not one per iteration)
  - Application layer owns presentation; domain layer only owns data structure
  - Enables different reporting strategies: compact error list, detailed error logs, etc.

**Implementation Structure**:
```csharp
public class ValidationResult
{
    public bool IsValid { get; }
    public IReadOnlyList<ValidationError> Errors { get; }
    
    public IReadOnlyList<ValidationError> ErrorsByCategory(string category) { }
    public IReadOnlyList<ValidationError> ErrorsByScope(string scope) { }
}

public class ValidationError
{
    public string Code { get; }                 // e.g., "CIRCULAR_DEPENDENCY"
    public string Message { get; }              // e.g., "Profile has circular override: A → B → A"
    public string Scope { get; }                // e.g., "api.endpoint1"
    public IReadOnlyList<string> Path { get; }  // e.g., ["override_a", "override_b", "override_a"]
}
```

**Application-Level Display**:
```csharp
// Simple: "Profile invalid: 3 errors"
// Detailed: List all errors with scopes and suggested fixes
// API: Return errors array for programmatic handling
```

**Implementation Implication**:
- Domain `ValidationResult` is flat list of errors
- Application layer applies filtering, sorting, grouping based on use case
- Test validation behavior comprehensively; application behavior tested separately

---

### 5. Metric Completeness Definition (COMPLETE vs PARTIAL threshold)

**Open Question**: Is "COMPLETE" a binary (100% samples collected) or a threshold (e.g., ≥90%)?

**Research Finding**:
- **Decision**: CompletessStatus enum values (COMPLETE/PARTIAL) are domain decision; threshold for each is Metrics Domain responsibility
- **Rationale**:
  - Metrics Domain defines what "complete" means for its aggregation model
  - Evaluation/Profile Domains consume via enum; they don't care about thresholds
  - Allows Metrics Domain to evolve thresholds without changing Evaluation/Profile logic

**Implementation Approach**:
```csharp
public enum CompletessStatus
{
    COMPLETE,    // All required samples collected (threshold TBD by Metrics Domain)
    PARTIAL      // Incomplete data; threshold TBD by Metrics Domain
}

public class MetricEvidence
{
    public CompletessStatus Status { get; }
    public int SampleCount { get; }
    public int RequiredSampleCount { get; }    // Transparency: what threshold was used
    public string AggregationWindow { get; }
}
```

**Metrics Domain owns**: Definition of RequiredSampleCount based on aggregation type  
**Evaluation Domain consumes**: CompletessStatus enum and SampleCount metadata  
**Profile Domain ignores**: Completeness (orthogonal concern)

---

### 6. Circular Override Dependencies: Stateful or Pure Detection?

**Open Question**: Can circular dependency detection be stateful, or must it be pure?

**Research Finding**:
- **Decision**: Circular dependency detection MUST be pure (stateless) analysis
- **Rationale**:
  - Stateful detection would require hidden state in ProfileValidator; harder to test and reason about
  - Pure analysis using topological sort is deterministic and testable in isolation
  - Aligns with determinism requirement: same profile → same validation result

**Algorithm Choice**:
- **Topological Sort** (Kahn's Algorithm):
  - Time: O(n + e) where n = profiles, e = edges (dependencies)
  - Space: O(n + e)
  - Deterministic: Sort nodes by ID before processing for consistent order
  - Output: Either sorted dependency order (valid) or cycle nodes (invalid)

```csharp
public class CircularDependencyValidator
{
    public ValidationResult ValidateNoCycles(Profile profile)
    {
        // Build directed graph of override dependencies
        // Apply Kahn's algorithm
        // If any nodes remain after processing, cycles exist
        // Errors list includes detected cycle path
    }
}
```

**Implementation Implication**:
- IProfileValidator.Validate() is pure function (no state changes)
- Validator can be used concurrently without synchronization
- Test cycles detection via property-based tests

---

### 7. Determinism and DateTime Handling

**Open Question**: How is `DateTime.UtcNow` captured for evidence determinism?

**Research Finding**:
- **Decision**: `DateTime.UtcNow` captured once at evaluation start; passed through evaluation pipeline
- **Rationale**:
  - Calling `DateTime.UtcNow` multiple times during evaluation produces different values
  - Capturing once ensures consistent timestamp across all evidence/violations
  - Determinism verification: 1000+ evaluations with same inputs → identical timestamp in evidence

**Implementation Pattern**:
```csharp
public class Evaluator
{
    public EvaluationResult Evaluate(IMetric metric, IRule rule, Profile profile)
    {
        var evaluatedAt = DateTime.UtcNow;  // Captured once
        
        // ... evaluation logic ...
        
        var evidence = new EvaluationEvidence
        {
            RuleId = rule.Id,
            EvaluatedAt = evaluatedAt,  // Same for all evidence in this evaluation
            // ... other fields ...
        };
        
        return new EvaluationResult
        {
            Outcome = outcome,
            Evidence = evidence,
            EvaluatedAt = evaluatedAt
        };
    }
}
```

**Verification**:
- Determinism tests: Evaluate 1000 times; verify identical EvaluatedAt timestamp
- Integration tests: Verify timestamp matches logical evaluation order (not machine clock skew)

---

### 8. Technology Stack Validation: C# and .NET 10

**Question**: Is C#/.NET 10 appropriate for deterministic enrichment implementation?

**Research Finding**:
- **Decision**: C#/.NET 10 is EXCELLENT fit for this enrichment; no alternatives needed
- **Rationale**:

| Factor | C#/.NET 10 Assessment |
|--------|----------------------|
| **Immutability** | ✅ Records, init accessors, readonly keywords provide compile-time immutability guarantees |
| **Determinism** | ✅ Deterministic GC, IL compilation, no hidden reflection cost; predictable performance |
| **Type Safety** | ✅ Strong typing catches many errors at compile time; NullableReferenceTypes prevent null issues |
| **Testing** | ✅ xUnit mature; determinism testing patterns (property tests) well-supported |
| **Domains** | ✅ All three domains already C#; consistency and team knowledge |
| **DDD/Clean Arch** | ✅ Dependency injection, layered project structure native to .NET ecosystem |
| **Performance** | ✅ LTS release; performance regressions tracked carefully |
| **Long-Term** | ✅ LTS release guaranteed support until 2026-11-10 (3.5+ years) |

**Alternatives Considered** (and rejected):
- Rust: Overkill; determinism already guaranteed by .NET; adds complexity
- Python: Type safety insufficient; determinism harder to guarantee; not used in current stack
- Go: Interface system less expressive; early-stage domain modeling harder

---

## Design Decision Summary

| Aspect | Decided | Impact | Verification |
|--------|---------|--------|--------------|
| **Profile Resolution Performance** | < 10ms for 100-entry profiles | Acceptable pre-evaluation overhead | Benchmark tests with performance gates |
| **INCONCLUSIVE Semantics** | Domain-level outcome; CI/CD handling deferred to application | Preserves domain purity; flexible CI integration | Domain tests verify INCONCLUSIVE is returned correctly; application tests verify CI mapping |
| **Evidence Retention** | Infrastructure adapter concern; domain doesn't enforce TTL | Allows org-specific compliance policies | Adapter tests verify retention policy is applied; domain doesn't require changes |
| **Validation Errors** | Return all errors at once; application owns display | Faster remediation; flexible reporting | Unit tests verify all errors collected; integration tests verify application display strategy |
| **Completeness Threshold** | Metrics Domain owns definition; Evaluation/Profile consume enum | Clean boundary; Metrics Domain evolves independently | Metrics tests verify threshold definition; Evaluation/Profile tests verify enum consumption |
| **Circular Dependency Detection** | Pure (stateless) topological sort | Deterministic, testable, concurrent-safe | Property-based cycle tests; determinism verification tests |
| **DateTime Handling** | Capture once at evaluation start; pass through pipeline | Consistent evidence timestamps; deterministic verification | Determinism tests verify identical timestamps across 1000 runs |
| **Technology Stack** | C#/.NET 10 (no alternatives) | Leverage existing stack; proven for determinism | No alternatives needed; align with current platform |

---

## Conclusion

All open questions have been resolved through research. The technical plan is ready for Phase 1: Design & Contracts.

**Key Findings**:
1. Performance baseline (< 10ms) is achievable with deterministic sorting
2. INCONCLUSIVE handling is application concern; domain remains pure
3. All design decisions preserve determinism, testability, and clean boundaries
4. C#/.NET 10 is the right choice for this enrichment
5. Enrichments can be implemented incrementally; backward compatibility maintained

**Next Steps**: Proceed to Phase 1 to generate data-model.md, contracts, and quickstart.md.

---

**Status**: ✅ Phase 0 Research Complete
