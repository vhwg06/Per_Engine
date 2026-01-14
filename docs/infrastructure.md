# Infrastructure Layer Coding Rules

Infrastructure adapts external systems to the application.
It must remain boring and predictable.

## Responsibilities
- Implement ports defined by the application layer.
- Handle IO, persistence, serialization, execution engines, and integrations.
- Translate external representations into domain-neutral forms.

## Constraints
- No business or domain decisions.
- No SLA or evaluation logic.
- No implicit retries or hidden behavior.

## Error Handling
- Fail fast on invalid or unexpected input.
- Translate technical errors into neutral failures.
- Never throw raw infrastructure exceptions across boundaries.

## Design Guidelines
- Adapters should be thin and replaceable.
- Prefer composition over inheritance.
- Avoid caching or optimization unless explicitly required by a spec.
