
📘 specs/profile-domain.spec.md
1. Purpose
Định nghĩa cách cấu hình hành vi test & evaluation ở domain level.
Profile trả lời câu hỏi:
“Trong context này, hệ thống nên cư xử như thế nào?”

2. References
* speckit.constitution
* docs/coding-rules/domain.md

3. Core Concepts
3.1 Profile
* Tập hợp các configuration decision
* Immutable sau khi resolved

3.2 Scope
* Phạm vi áp dụng của config
    * Global
    * Per API
    * Per tag
    * Custom scope (extensible)

3.3 Override
* Cơ chế ghi đè config
* Override không phá invariant

3.4 Default
* Giá trị mặc định khi không có override
* Default là explicit, không implicit

4. Resolution Rules
* Override luôn thắng default
* Scope hẹp hơn thắng scope rộng hơn
* Conflict phải resolve deterministic
* Không được tồn tại ambiguity sau resolution
