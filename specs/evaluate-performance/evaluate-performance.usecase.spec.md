pecs/evaluate-performance.usecase.spec.md (ENRICHED BASE)
1. Purpose
Điều phối toàn bộ flow đánh giá performance bằng cách phối hợp các domain:Metrics, Profile, và Evaluation.
Use case này trả lời:
“Với dữ liệu execution và yêu cầu hiện tại, performance có đạt không,dựa trên dữ liệu nào, và mức độ tin cậy ra sao?”

2. References
* specs/metrics-domain.spec.md
* specs/evaluation-domain.spec.md
* specs/profile-domain.spec.md
* docs/coding-rules/application.md

3. Responsibilities
Evaluate Performance Use Case CHỈ chịu trách nhiệm orchestration, bao gồm:
* Resolve cấu hình evaluation thông qua Profile Domain
* Điều phối việc áp dụng evaluation rules lên metrics
* Tổng hợp kết quả evaluation thành EvaluationResult bất biến
* Expose completeness và traceability metadata
Use case này KHÔNG chứa:
* logic tính metric
* logic rule evaluation chi tiết
* logic persistence hay integration

4. Input & Output (Conceptual)
Input
* Collected metrics (from Metrics Domain)
* Execution context (API, environment, tags)
* Available profiles
* Evaluation rules (domain objects)
Output
* EvaluationResult (immutable), bao gồm:
    * Outcome (PASS / WARN / FAIL / INCONCLUSIVE)
    * Violations (if any)
    * ExecutionMetadata (profile applied, thresholds used)
    * CompletenessReport
    * Deterministic fingerprint of actual collected data

5. Flow
metrics
  → resolve profile
    → apply evaluation rules
      → aggregate outcome
        → build EvaluationResult
Flow này:
* deterministic
* idempotent
* independent of infrastructure

6. Semantics
* Idempotent: Cùng input → cùng output (byte-identical)
* Deterministic ordering:
    * Rules MUST be evaluated in a deterministic order
* Partial metrics allowed:
    * Rules thiếu metric MAY be skipped or marked inconclusive
* Completeness required:
    * EvaluationResult MUST expose what data was actually used
* Fingerprint required:
    * Fingerprint MUST reflect actual collected data, not expectations
* No mutation:
    * Metrics, profiles, and rules MUST NOT be mutated

7. Error Handling Semantics
* Missing metrics:
    * MUST NOT crash evaluation
    * MUST be reflected in CompletenessReport
* Invalid configuration:
    * MUST fail fast with explicit error
* Rule evaluation errors:
    * MUST be captured as violations or evaluation errors,not infrastructure failures

8. Out of Scope
* Metric calculation
* Persistence
* Baseline comparison
* Integration / export
* CI/CD exit codes