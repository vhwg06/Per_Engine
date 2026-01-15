# Research Phase: Baseline Domain (Phase 0)

**Status**: Research & Clarification  
**Date**: 2026-01-15  
**Goal**: Resolve all "NEEDS CLARIFICATION" items from Technical Context and answer open questions from plan.md  

---

## Research Agenda

This research phase addresses five critical decisions that determine the implementation approach for baseline domain:

1. Confidence Level Calculation Strategy
2. Metric Direction Metadata & Aggregation
3. Multi-Metric Outcome Aggregation Rules
4. Baseline TTL & Retention Policy
5. Concurrent Baseline Versioning Model

---

## Research 1: Confidence Level Calculation Strategy

### Problem Statement

**Spec Quote**:
> "Confidence – mức độ tin cậy của kết luận" (Confidence – certainty of conclusion)
> 
> "Calculated based on comparison magnitude and baseline variance"

**Ambiguity**: 
- How is "baseline variance" measured? (Not stored in immutable baseline snapshot)
- What formula converts comparison magnitude + variance → confidence [0.0, 1.0]?
- Is variance estimated from single current comparison, or historical data (out of scope)?

### Decision: Simplified Magnitude-Based Approach

**Chosen Strategy**: Confidence determined purely by comparison magnitude, independent of baseline variance (historical data deferred to Analytics domain).

**Rationale**:
1. Baseline domain has no access to historical variance (single snapshot, no history)
2. Spec marks "statistical model" as out-of-scope; confidence should remain deterministic
3. Simpler approach: confidence = how far result deviates from tolerance threshold
4. Aligns with immutability constraint (no external state required)

**Algorithm** (illustrative; exact formula finalized in Design phase):

```
confidence = min(1.0, abs(change_magnitude - tolerance_threshold) / tolerance_threshold)

Example:
- Baseline p95 = 150ms, tolerance = ±10ms (absolute)
- Current p95 = 160ms
- Change = +10ms (at tolerance boundary)
- Confidence = 0.0 (inconclusive; on the line)

- Current p95 = 170ms (20ms beyond tolerance)
- Change = +20ms (at 2x tolerance)
- Confidence = min(1.0, 20/10) = 1.0 (very confident)

- Current p95 = 155ms (within tolerance)
- Confidence = (5/10) = 0.5 (moderate confidence)
```

**Confidence Thresholds** (configurable per domain/context):
- Confidence ≥ 0.7 → REGRESSION/IMPROVEMENT (high confidence)
- Confidence < 0.7 → INCONCLUSIVE (insufficient confidence)

**Trade-off Accepted**:
- ❌ No historical variance weighting (requires analytics context)
- ✅ Enables single-baseline comparison in CI/CD (no historical data needed)
- ✅ Deterministic calculation (same baseline + metrics = same confidence)

---

## Research 2: Metric Direction Metadata

### Problem Statement

**Spec Requirement** (FR-008):
> "For Better Direction (lower is good): Negative relative change = improvement"
> "For Worse Direction (higher is good): Positive relative change = improvement"

**Ambiguity**:
- Where does the system know "latency: lower is good" vs "throughput: higher is good"?
- Is this encoded in Metric schema (Metrics Domain)?
- Or in Tolerance configuration (Baseline Domain)?
- Or external config?

### Decision: Metric Direction from Metrics Domain Contract

**Chosen Approach**: Baseline domain assumes Metric interface includes direction metadata.

**Contract** (from Metrics Domain):
```csharp
interface IMetric
{
    string MetricName { get; }
    double Value { get; }
    
    // NEW: Proposed extension
    MetricDirection Direction { get; }  // LowerIsBetter, HigherIsBetter
}

enum MetricDirection
{
    LowerIsBetter,   // Latency, error rate, memory usage
    HigherIsBetter   // Throughput, success rate, profit
}
```

**Rationale**:
1. Metric type (latency, throughput) is Metrics Domain concern
2. Direction is intrinsic to metric semantics (not tolerances)
3. Cleaner separation: Metrics Domain = "what is this metric?", Baseline Domain = "did it change acceptably?"

**Fallback** (if Metrics Domain cannot extend immediately):
- Tolerance configuration includes direction hint per metric
- Less ideal (couples domain knowledge to tolerance rules) but workable

**Recommendation**: 
- Propose Metric.Direction property to Metrics Domain team (Phase 0)
- If accepted: Use in baseline comparison logic
- If not: Document tolerance rule as including direction specification

---

## Research 3: Multi-Metric Outcome Aggregation

### Problem Statement

**Spec Quote**:
> "ComparisonResult có 4 trạng thái: IMPROVEMENT, REGRESSION, NO_SIGNIFICANT_CHANGE, INCONCLUSIVE"
> 
> "Result priority when multiple metrics present (REGRESSION > IMPROVEMENT > NO_SIGNIFICANT_CHANGE > INCONCLUSIVE)"

**Ambiguity**:
- Is overall outcome always "worst case" (any REGRESSION → result is REGRESSION)?
- What if 2 metrics show REGRESSION, 1 shows IMPROVEMENT? Still REGRESSION?
- Should some metrics be weighted/critical?

### Decision: Worst-Case Aggregation (Phase 1)

**Chosen Strategy**: Overall comparison outcome = worst-case metric outcome.

**Priority Order**:
1. REGRESSION (worst; indicates performance degradation)
2. IMPROVEMENT (positive; but unexpected can indicate measurement variance)
3. NO_SIGNIFICANT_CHANGE (expected; no action needed)
4. INCONCLUSIVE (uncertain; insufficient data to decide)

**Algorithm**:
```csharp
ComparisonOutcome AggregateOutcome(List<ComparisonMetric> metrics)
{
    // Return worst outcome from all metrics
    if (metrics.Any(m => m.Outcome == ComparisonOutcome.REGRESSION))
        return ComparisonOutcome.REGRESSION;
    if (metrics.Any(m => m.Outcome == ComparisonOutcome.IMPROVEMENT))
        return ComparisonOutcome.IMPROVEMENT;
    if (metrics.Any(m => m.Outcome == ComparisonOutcome.NO_SIGNIFICANT_CHANGE))
        return ComparisonOutcome.NO_SIGNIFICANT_CHANGE;
    return ComparisonOutcome.INCONCLUSIVE;
}
```

**Rationale**:
- Conservative: Any regression in any metric signals test failure
- Simple: No weighting complexity; deterministic
- Safe for CI/CD: Prevents passing if any metric regresses

**Phase 2 Extension** (deferred):
- Support metric weighting (critical metrics with higher priority)
- Support metric group outcomes (e.g., "latency metrics" vs "reliability metrics")
- Requires configuration; out of scope for Phase 1

---

## Research 4: Baseline TTL & Retention Policy

### Problem Statement

**Assumption**: 
> "Redis is used as a short-term, fast-access persistence mechanism. No long-term audit or historical retention is required."

**Questions**:
- What is "short-term"? (5 min? 1 hour? 1 day?)
- Who configures TTL? (Ops team? Application code?)
- What happens if baseline expires during comparison? (Error? Graceful?)

### Decision: Configurable TTL, Graceful Expiration Handling

**TTL Strategy**:
- **Default**: 24 hours (1 full day of CI/CD cycles)
- **Configurable**: Application environment setting (not domain concern)
- **Rationale**: 
  - Long enough: Covers typical baseline lifetime in CI/CD
  - Short enough: Prevents stale baselines from accumulating in Redis
  - Operational: TTL policy driven by operations/DevOps, not domain

**Expiration Handling**:
- Repository.GetByIdAsync(id) returns null if baseline expired
- Consumer (ComparisonOrchestrator) handles: throw BaselineNotFoundException
- CI/CD integration catches exception: "Baseline expired, create new baseline from this run"

**No Domain-Level Concerns**:
- ❌ Domain doesn't enforce TTL
- ❌ Domain doesn't migrate old baselines
- ✅ Domain recognizes baseline may be unavailable (null semantics)

**Configuration** (Infrastructure layer):
```csharp
// appsettings.json
{
  "Redis": {
    "ConnectionString": "...",
    "BaselineTtl": "1.00:00:00"  // 24 hours
  }
}
```

---

## Research 5: Concurrent Baseline Versioning

### Problem Statement

**Scenario**: 
- CI run #1 creates baseline V1 at 10:00
- CI run #2 (parallel) compares against V1 at 10:01
- CI run #3 creates baseline V2 at 10:02
- Can V1 and V2 coexist? Or does V2 replace V1?

**Implications**:
- If V2 replaces V1: Old comparisons fail (baseline expired)
- If coexist: Need baseline ID versioning; version pinning in CI/CD

### Decision: Coexistence via Versioning (Deferred to Phase 2)

**Phase 1 Assumption**:
- Single "current" baseline per test suite (no versioning)
- Each new baseline creation overwrites previous
- Comparisons always reference "latest" baseline

**Phase 2 Enhancement** (deferred):
- Support multiple baseline versions
- Explicit version pinning in comparison (compare against V1, not "latest")
- Baseline ID = (suite, version) tuple

**Rationale for Phase 1 Deferral**:
- Simplifies initial implementation
- Works for typical CI/CD pattern (one baseline per branch)
- Versioning is organizational policy, not domain requirement
- Can add without breaking existing baseline/comparison logic

**Implementation Note**:
- BaselineId currently opaque string (UUID)
- Phase 2: Extend to (suite:version) semantics if needed
- Domain logic unchanged; repository adapter enhanced

---

## Research Summary & Decisions

| Decision | Status | Impact |
|----------|--------|--------|
| **Confidence Formula** | ✅ Decided | Simple magnitude-based; no historical variance |
| **Metric Direction** | 🔄 Propose | Recommend Metric.Direction in Metrics Domain |
| **Outcome Aggregation** | ✅ Decided | Worst-case strategy; simple & safe |
| **Baseline TTL** | ✅ Decided | 24h default; configurable; graceful expiration |
| **Versioning** | 🕐 Deferred | Phase 2; single-baseline Phase 1 |

---

## Recommendations for Phase 1 Design

1. **High Priority**:
   - Finalize confidence calculation formula (decision made; implement)
   - Clarify Metric.Direction availability (propose to Metrics Domain; fallback to tolerance config)
   - Implement worst-case outcome aggregation (decision made)

2. **Medium Priority**:
   - Document TTL configuration in quickstart
   - Implement graceful baseline expiration handling
   - Design BaselineId semantics (prepare for Phase 2 versioning)

3. **Low Priority** (Phase 2+):
   - Metric weighting / outcome groups
   - Baseline versioning / version pinning
   - Historical variance weighting (if Analytics domain available)

---

## Open Questions for Design Phase

1. **Metric Direction**: Can Metrics Domain add Direction property? Or fallback to tolerance config?
2. **Confidence Threshold**: What confidence value triggers INCONCLUSIVE? (Recommendation: 0.7, configurable)
3. **Baseline Naming**: Convention for baseline identifiers? (e.g., "main-branch-latest", "v2.0.0-baseline")
4. **Comparison Caching**: Should comparison results be cached? Lifetime? (Deferred to Phase 2 optimization)
