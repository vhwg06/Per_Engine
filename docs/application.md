# Application Layer Coding Rules

The application layer coordinates domain logic.
It does not implement business rules.

## Responsibilities
- Application code orchestrates domain objects and use cases.
- It coordinates input, domain execution, persistence, and integration.
- Each use case handles exactly one application concern.

## Boundaries
- Application code may depend on domain abstractions.
- Application code defines ports (interfaces) for infrastructure.
- Infrastructure implements these ports.

## What Application Code Must Not Do
- No business rule calculations.
- No domain invariants.
- No metric or SLA evaluation logic.
- No infrastructure-specific logic.

## Error Handling
- Translate technical failures into application-level outcomes.
- Do not swallow domain exceptions.
- Maintain deterministic execution flow.

## Structure
- One use case per file.
- Avoid god services or generic coordinators.
