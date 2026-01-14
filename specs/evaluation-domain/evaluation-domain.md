📘 specs/evaluation-domain.spec.md
1. Purpose
Định nghĩa logic đánh giá (evaluation) dựa trên metrics domain.
Evaluation domain trả lời câu hỏi:
“Kết quả performance này có đạt yêu cầu không, và vì sao?”

2. References
* speckit.constitution
* specs/metrics-domain.spec.md
* docs/coding-rules/domain.md

3. Core Concepts
3.1 Evaluation
* Quá trình áp dụng Rule lên Metric
* Evaluation là pure domain logic

3.2 Rule
* Điều kiện logic được áp dụng lên metric
* Rule không biết engine
* Rule không biết persistence
Ví dụ (conceptual):
* p95 latency < threshold
* error rate ≤ threshold

3.3 EvaluationResult
* Kết quả deterministic
* Gồm:
    * outcome
    * violations (nếu có)

3.4 Violation
* Đại diện cho một rule bị vi phạm
* Gắn với:
    * metric
    * rule
    * actual value
    * expected constraint

3.5 Severity
* Mức độ đánh giá:
    * PASS
    * WARN
    * FAIL
Severity là domain concept, không phải CI exit code.

4. Evaluation Semantics
* Một metric có thể có nhiều rule
* Một evaluation có thể bao gồm nhiều metric
* Kết quả evaluation:
    * phải deterministic
    * không phụ thuộc thứ tự rule

5. Invariants
* Rule evaluation không mutate metric
* Evaluation outcome phải nhất quán với violations
* Severity escalation phải deterministic

6. Out of Scope
* SLA syntax
* CI exit codes
* Reporting / visualization
* Persistence

7. Architectural Notes
* Evaluation domain phụ thuộc metrics domain
* Không domain nào được phụ thuộc evaluation
* Rule là strategic extension point cho tương lai