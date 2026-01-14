# Domain Layer Coding Rules

The domain layer represents the core business and analytical logic.
These rules are strict and non-negotiable.

## Purity and Determinism
- Domain code must be deterministic and side-effect free.
- No IO, no file access, no network calls.
- No clocks (DateTime.Now), randomness, environment access, or static state.
- Domain behavior must depend only on explicit inputs.

## Responsibilities
- Domain objects enforce their own invariants.
- Validation logic belongs in domain objects, not in application or infrastructure.
- Domain rules must be explicit classes or value objects, not conditionals.

## Modeling Guidelines
- Prefer Value Objects over primitives.
- Avoid primitive obsession (string, int, double without meaning).
- Domain methods must be small and intention-revealing.
- Anemic domain models are forbidden.

## Error Handling
- Domain errors must be explicit and meaningful.
- Prefer domain-specific exceptions or result types.
- Do not leak technical error types into the domain.

## Prohibited
- No configuration parsing.
- No framework annotations.
- No serialization attributes.
- No dependency on execution engines, databases, or integrations.
