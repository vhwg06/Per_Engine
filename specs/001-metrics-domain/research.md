# Phase 0 Research: Metrics Domain Implementation Strategy

**Completed**: 2026-01-14  
**Scope**: Resolve technical open questions from plan.md to enable Phase 1 design

## Research Summary

This document consolidates findings on key technical decisions for the metrics domain implementation in C#/.NET 10. All six open questions from plan.md are addressed with concrete recommendations.

---

## Q1: Floating-Point Precision in Percentile Calculation

### Problem Statement

Percentile algorithms may produce different results due to floating-point rounding variance:
- Linear interpolation: `result = lower + (h - int(h)) * (upper - lower)`
- Nearest-rank: Different tie-breaking strategies
- Platform variance: .NET may optimize Math operations differently

**Impact**: Violates determinism constraint (SC-002) requiring byte-identical results.

### Decision

**Recommendation**: Implement **deterministic percentile via sorted index calculation** with explicit rounding semantics.

### Rationale & Implementation

1. **Algorithm Choice**: Use **nearest-rank method** (most deterministic)
   - Rank = ceil(P/100 * N) where P=percentile, N=sample count
   - Select sample at position Rank in sorted array
   - Avoids interpolation variance entirely

2. **Precision Semantics**:
   - For interpolation fallback (if needed): Use **decimal type** (128-bit, deterministic) instead of double
   - Document: "Percentiles calculated to 4 decimal places"
   - Tests verify: `Percentile(p95, samples1) == Percentile(p95, samples1)` exactly (same bytes)

3. **Implementation Structure**:
   ```
   PercentileAggregation
   ├── SortSamples(normalized)
   ├── CalculateRank(percentile, count)
   ├── SelectValue(rank)
   └── ReturnResult (no rounding variance)
   ```

4. **Edge Cases**:
   - Empty samples: Throw `InvalidOperationException` ("Cannot compute percentile of empty collection")
   - Single sample: Return that sample's value
   - Duplicate values: Handled by sort order (deterministic in .NET)

### Verification

- **Unit Test**: `PercentileTests.DeterminismAcross10000Runs()` - Run same aggregation 10,000 times, verify bitwise equality
- **Contract Test**: `PercentileAggregationContract.cs` - Property-based testing with QuickCheck-style generators
- **Stress Test**: `PercentileTests.LargeDataset()` - 1M samples, verify reproducibility

### Alternatives Considered

| Approach | Determinism | Performance | Complexity |
|----------|------------|-------------|-----------|
| **Nearest-rank (selected)** | ✅ Perfect | ✅ O(n log n) sort | Low |
| Linear interpolation + decimal | ✅ Good | ⚠️ Slower | Medium |
| Fixed-seed randomness | ❌ No | ✅ Fast | High |
| Quantile algorithm (R-type) | ✅ Good | ✅ Fast | High |

---

## Q2: Sample Normalization Rules

### Problem Statement

Converting samples from different engines to domain units requires explicit, lossless rules:
- JMeter: milliseconds, success/failure boolean, error code (numeric)
- k6: milliseconds, success/failure, error message (string)
- Custom engines: arbitrary units, error classifications

**Risk**: Lossy conversions or ambiguous semantics could cause data integrity issues.

### Decision

**Recommendation**: Define **bijective normalization mappings** per engine with **explicit error classification strategy**.

### Rationale & Implementation

1. **Normalization Contract** (in each adapter):
   - Input: Engine-specific result object (e.g., JMeter sample row)
   - Output: Domain `Sample` object with:
     - `Timestamp`: Engine timestamp or UTC.Now (engine-dependent)
     - `Duration`: Converted to milliseconds (with precision semantics)
     - `Status`: success/failure (boolean)
     - `ErrorClassification`: Mapped from engine error type
     - `ExecutionContext`: Engine name, run ID, scenario name

2. **Error Classification Mapping** (preserves information):
   ```csharp
   // Engine-specific error → Domain classification
   // With optional metadata preservation
   
   public ErrorClassification MapErrorType(EngineError engineError)
   {
       return engineError.Type switch
       {
           EngineErrorType.ConnectTimeout => ErrorClassification.Timeout,
           EngineErrorType.ReadTimeout => ErrorClassification.Timeout,
           EngineErrorType.ConnectionRefused => ErrorClassification.NetworkError,
           EngineErrorType.HTTP500 => ErrorClassification.ApplicationError,
           _ => ErrorClassification.UnknownError
       };
   }
   
   // Preserve original error code in Sample metadata
   Sample sample = new Sample(
       timestamp, duration, status,
       errorClassification,
       metadata: new Dictionary<string, object>
       {
           ["engine_error_code"] = engineError.Code,
           ["engine_error_message"] = engineError.Message
       }
   );
   ```

3. **Unit Handling**:
   - Internal standard: All latencies stored in **nanoseconds** (highest precision)
   - Conversion rule: `latencyNs = latencyUnit.ToNanoseconds(value)`
   - Output: `Latency` value object specifies unit at creation
   - Example: `new Latency(1500, LatencyUnit.Nanoseconds)` or `new Latency(1.5, LatencyUnit.Milliseconds)`

4. **Verification Matrix** (per adapter):
   - Sample with known engine values → normalized Sample → re-export → matches original (bijective)
   - Test data: Actual engine output samples (saved fixtures)

### Verification

- **Adapter Unit Tests**: Each adapter test verifies round-trip conversion
  - `K6AdapterTests.NormalizationRoundTrip()`
  - `JMeterAdapterTests.ErrorClassificationMapping()`
- **Contract Test**: `EngineAdapterContract.cs` - All adapters must preserve data fidelity
- **Fixtures**: Real engine sample outputs stored in `tests/Fixtures/engine-outputs/`

### Alternatives Considered

| Approach | Fidelity | Complexity | Risk |
|----------|----------|-----------|------|
| **Bijective mapping + metadata (selected)** | ✅ High | Low | Low |
| Lossy mapping (discard engine error details) | ❌ Low | Low | High (data loss) |
| Lossless with nested objects | ✅ High | High | Medium (complexity) |
| Configurable mappings | ✅ High | High | Medium (hard to test) |

---

## Q3: Thread Safety for Concurrent Sample Collection

### Problem Statement

Multiple execution engines may report samples concurrently. `SampleCollection` must:
- Accept samples without blocking (high throughput)
- Ensure no samples lost
- Provide snapshot consistency for aggregation
- Support concurrent reads + writes

**Risk**: Race conditions, lost samples, or non-deterministic ordering.

### Decision

**Recommendation**: Use **immutable collections (ImmutableList<T>)** with **atomic snapshot semantics**.

### Rationale & Implementation

1. **Data Structure**: `ImmutableList<Sample>` from `System.Collections.Immutable`
   - Immutable: Thread-safe without locks
   - Copy-on-write: Efficient memory usage
   - Snapshot consistency: Readers see consistent state

2. **Implementation Pattern**:
   ```csharp
   public class SampleCollection
   {
       private volatile ImmutableList<Sample> _samples = ImmutableList<Sample>.Empty;
       
       public void AddSample(Sample sample)
       {
           // Lock-free atomic operation
           ImmutableList<Sample> newList;
           ImmutableList<Sample> oldList;
           
           do
           {
               oldList = _samples;
               newList = oldList.Add(sample);
           }
           while (Interlocked.CompareExchange(ref _samples, newList, oldList) != oldList);
       }
       
       public ImmutableList<Sample> GetSnapshot()
       {
           return _samples; // Atomic read
       }
   }
   ```

3. **Performance Characteristics**:
   - Add: O(log N) append-and-share
   - Snapshot: O(1) atomic read
   - Memory: 30% overhead vs array (acceptable for determinism benefit)
   - No GC pauses from lock contention

4. **Ordering Semantics**:
   - Samples ordered by add sequence (FIFO)
   - For deterministic aggregation: Sort by timestamp before processing
   - Document: "Aggregations may reorder samples; determinism depends on timestamp-ordered input"

### Verification

- **Unit Test**: `SampleCollectionTests.ConcurrentAdditions()` - 1000 concurrent threads, verify no samples lost
- **Stress Test**: `SampleCollectionTests.StressTest()` - 10M samples added concurrently, measure performance
- **Determinism Test**: `AggregationDeterminismTests.ConcurrentVsSequentialAddition()` - Verify same results regardless of thread scheduling

### Alternatives Considered

| Approach | Thread-Safety | Performance | Complexity |
|----------|---------------|-------------|-----------|
| **ImmutableList (selected)** | ✅ Lock-free | ✅ O(log N) | Low |
| ReaderWriterLockSlim | ✅ High contention | ⚠️ Variable | Medium |
| Channel<Sample> | ✅ Queue-based | ✅ O(1) | Medium |
| Concurrent bag | ✅ Unordered | ✅ O(1) | Low (but no order) |

---

## Q4: Aggregation Result Composition

### Problem Statement

Can aggregation results be chained (e.g., max of percentiles)?
- Example: Compute p95 latencies per engine, then max across engines
- Semantic ambiguity: "max of 3 p95 values" loses context

**Risk**: Misleading results or incorrect interpretations.

### Decision

**Recommendation**: **Disallow direct aggregation composition; require explicit data projection instead.**

### Rationale & Implementation

1. **Composition Rules** (what IS allowed):
   - ✅ Aggregate samples → metric
   - ✅ Aggregate metrics using new sample collection (re-project data)
   - ✅ Chain aggregations on different dimensions (e.g., max per engine, then avg across engines)

2. **Composition Rules** (what IS NOT allowed):
   - ❌ Directly: `Max(Percentile(samples1), Percentile(samples2))`
   - ✅ Instead: `Max(samples1 ∪ samples2)` or `Max(p95_metric1, p95_metric2)` with clear semantics

3. **Implementation**:
   ```csharp
   // Disallowed: AggregationResult as input to aggregation
   public interface IAggregationOperation
   {
       // Input MUST be sample collection, never AggregationResult
       AggregationResult Aggregate(SampleCollection samples);
       
       // Composition via re-aggregation
       public static AggregationResult ComposeMax(
           IEnumerable<SampleCollection> collections,
           IAggregationOperation innerAggregation)
       {
           var aggregatedValues = collections
               .Select(c => innerAggregation.Aggregate(c))
               .ToList();
           
           // Now aggregate the aggregated values
           var reprojectedSamples = aggregatedValues
               .Select((val, idx) => new Sample(
                   timestamp: DateTime.UtcNow,
                   duration: val.Value,
                   status: true,
                   errorClassification: null
               ))
               .ToList();
           
           return new MaxAggregation().Aggregate(
               new SampleCollection(reprojectedSamples));
       }
   }
   ```

4. **Semantic Contract**:
   - Document each composition clearly: "Max p95 latencies = maximum 95th-percentile latency across [N] engines"
   - Versioning: CompositionStrategy enum for future expansion

### Verification

- **Contract Test**: `AggregationCompositionContract.cs` - Explicit whitelist of valid compositions
- **Semantics Test**: `AggregationCompositionTests.MaxOfPercentilesPreservesSemantics()`
- **Documentation**: Examples in `data-model.md`

### Alternatives Considered

| Approach | Safety | Usability | Complexity |
|----------|--------|-----------|-----------|
| **Explicit re-projection (selected)** | ✅ High | Medium | Low |
| Allow composition, document carefully | ⚠️ Medium | ✅ High | Low |
| Type system prevents invalid composition | ✅ High | ✅ High | High |

---

## Q5: Error Classification Extensibility

### Problem Statement

Domain specifies Timeout, NetworkError, ApplicationError, UnknownError. Engine-specific error types may not fit cleanly:
- Engine A: "ConnectionPoolExhausted" (network or timeout?)
- Engine B: "TLSHandshakeFailed" (network)
- Custom engine: proprietary error classification

**Risk**: Adapters forced into Unknown category, losing information.

### Decision

**Recommendation**: **Fixed domain classifications + optional engine-specific metadata** in Sample context.

### Rationale & Implementation

1. **Domain Classifications** (immutable, core):
   - `Timeout`: Request exceeded time limit (deterministic)
   - `NetworkError`: Connectivity/transport failure (deterministic)
   - `ApplicationError`: Application-level exception or business rule violation (deterministic)
   - `UnknownError`: Type cannot be determined; fallback (deterministic)

2. **Metadata Preservation** (extensible):
   ```csharp
   public class Sample
   {
       public ErrorClassification Classification { get; }
       
       public Dictionary<string, object> Metadata { get; }
       // Metadata example:
       // {
       //   "engine_error_type": "ConnectionPoolExhausted",
       //   "engine_error_code": 7,
       //   "engine_name": "k6",
       //   "raw_error_message": "Connection pool limit reached"
       // }
   }
   ```

3. **Adapter Mapping Strategy**:
   - Best-fit mapping: Each adapter maps engine errors to closest domain classification
   - Preserve context: Store engine error details in metadata
   - Bidirectional: When exporting sample to reporting/evaluation, include metadata

4. **Example Implementations**:
   ```csharp
   // k6 adapter
   public Sample MapK6Error(K6SampleResult result)
   {
       return new Sample(
           errorClassification: result.ErrorCode == "connection_timeout"
               ? ErrorClassification.Timeout
               : result.ErrorCode?.StartsWith("network_")
                   ? ErrorClassification.NetworkError
                   : ErrorClassification.UnknownError,
           metadata: new Dictionary<string, object>
           {
               ["engine_error_code"] = result.ErrorCode,
               ["engine_error_description"] = result.ErrorString
           }
       );
   }
   ```

5. **Evaluation Logic** (application layer):
   - Evaluation rules read `Classification` (domain concept)
   - For debugging: `Sample.Metadata["engine_error_code"]` available
   - Reporting: Can include metadata for transparency

### Verification

- **Adapter Tests**: Each adapter verifies mapping + metadata preservation
  - `K6AdapterTests.ErrorMetadataPreserved()`
  - `JMeterAdapterTests.ErrorMappingStrategy()`
- **Contract Test**: `EngineAdapterContract.cs` - All adapters preserve metadata
- **Integration Test**: Verify metadata propagates through aggregation

### Alternatives Considered

| Approach | Fidelity | Extensibility | Complexity |
|----------|----------|--------------|-----------|
| **Fixed classifications + metadata (selected)** | ✅ High | ✅ High | Low |
| Extensible enum (enum + string) | ✅ High | ✅ Very High | High |
| Nested error hierarchy | ✅ High | ✅ Medium | High |
| Discard metadata, accept loss | ❌ Low | ✅ Low | Low |

---

## Q6: Metrics Retention Policy

### Problem Statement

When should old metrics be deleted or archived?
- Performance concern: Unbounded storage growth
- Compliance: Data retention regulations
- Operational: Cost management

**Risk**: Storage exhaustion, compliance violations.

### Decision

**Recommendation**: **Deferred to Phase 2+ (infrastructure/persistence); define interface contract in Phase 1.**

### Rationale & Implementation

1. **Phase 1 Design**:
   - Define `IRetentionPolicy` interface in ports
   - Sample contract: `Task<IEnumerable<Metric>> ApplyRetentionPolicyAsync()`
   - No implementation: Deferred to infrastructure adapters

2. **Example Interface**:
   ```csharp
   namespace Domain.Ports;
   
   public interface IRetentionPolicy
   {
       /// <summary>
       /// Determine which metrics to archive/delete based on policy.
       /// </summary>
       Task<IEnumerable<Guid>> GetMetricsToArchiveAsync(DateTime before, DateTime after);
       
       /// <summary>
       /// Archive metrics (move to cold storage).
       /// </summary>
       Task ArchiveMetricsAsync(IEnumerable<Guid> metricIds);
   }
   ```

3. **Phase 2+ Implementations**:
   - Time-based: Delete metrics older than N days
   - Size-based: Delete oldest when storage > threshold
   - Compliance-based: GDPR/HIPAA retention rules
   - Smart archival: Hot/cold storage tiering

4. **Specification Notes**:
   - Application layer calls retention policy periodically
   - Domain remains unaware of retention (pure business logic)
   - Audit trail preserved (domain events logged before deletion)

### Verification

- **Phase 1**: Interface contract + documentation
- **Phase 2**: Implement time-based retention policy
- **Phase 3+**: Add compliance and storage policies

### Alternatives Considered

| Approach | Scope | Urgency | Status |
|----------|-------|---------|--------|
| **Defer to Phase 2+ (selected)** | Infrastructure | Not critical | Deferred |
| Implement now | Phase 1 | Adds scope | High risk |
| Hard-code forever retention | Domain | Simplistic | ❌ Unacceptable |

---

## Summary of Decisions

| Question | Recommendation | Phase | Risk |
|----------|---|---|---|
| **Q1: Percentile Precision** | Nearest-rank with decimal precision | 1 (Design) | Low |
| **Q2: Normalization Rules** | Bijective mappings + metadata | 1 (Contracts) | Low |
| **Q3: Thread Safety** | ImmutableList lock-free | 1 (Data Model) | Low |
| **Q4: Composition** | Explicit re-projection, no direct chaining | 1 (Contracts) | Low |
| **Q5: Error Classification** | Fixed domain + metadata | 1 (Data Model) | Low |
| **Q6: Retention Policy** | Defer to Phase 2+ via interface | 1 (Ports) | Low |

**Overall Risk Assessment**: ✅ LOW - All decisions align with constitutional principles and have clear implementation paths.

---

## Conformance Verification

All research findings conform to constitutional principles:

✅ **Specification-Driven**: Decisions driven by spec requirements (determinism, engine-agnostic)  
✅ **Domain-Driven Design**: Domain classifications fixed; engine specifics in adapters  
✅ **Clean Architecture**: Thread safety via immutable collections (no external dependencies)  
✅ **Determinism**: Floating-point precision and composition semantics ensure reproducibility  
✅ **Engine-Agnostic**: Normalization and error mapping preserve domain independence  
✅ **Evolution-Friendly**: Extensible via metadata and interface ports; no breaking changes  

**Ready for Phase 1 Design Implementation**.
