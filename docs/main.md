# Engineering Best Practices (Senior-Level)

This document summarizes the core engineering principles and best practices
that guide all design, specification, and implementation decisions in this repository.

It is not a specification, and it does not define behavior.
It defines how we think, reason, and judge code quality.

---

## 1. Think in Domains, Not Tools

- Always start from the domain problem, not from tools, frameworks, or libraries.
- JMeter, databases, file formats, and integrations are replaceable details.
- Domain concepts (metrics, evaluation, rules, profiles, baselines) are the source of truth.
- If a concept does not belong to the domain language, it does not belong in core code.

**Rule of thumb:**  
If removing a tool breaks your domain model, your design is wrong.

---

## 2. Domain Purity Is Non-Negotiable

- Domain code must be deterministic and side-effect free.
- No IO, no file access, no network calls, no clocks, no randomness in the domain.
- Domain objects enforce their own invariants.
- Prefer Value Objects over primitives.
- Avoid anemic domain models.

**Smell:**  
`if`, `switch`, or configuration-driven logic replacing explicit domain concepts.

---

## 3. Clean Architecture Is a Discipline, Not a Diagram

- Dependencies always point inward.
- Infrastructure depends on Application and Domain, never the reverse.
- Application code orchestrates; it does not calculate.
- Domain code decides; it does not orchestrate.

**Rule of thumb:**  
If domain logic knows how data is stored or executed, architecture is violated.

---

## 4. Specification Comes Before Code

- Specifications define behavior and constraints before implementation.
- Code exists only to fulfill a specification.
- If behavior is unclear, update the spec before writing code.
- Specs are contracts, not suggestions.

**Rule of thumb:**  
If you cannot point to a spec, the code probably should not exist.

---

## 5. Rules Are Code, Config Is Input

- Rules represent invariant logic and belong in code.
- Configuration provides data to rules; it must not replace rules.
- Avoid dynamic, configuration-driven decision trees when domain concepts are stable.
- Explicit rules are easier to reason about than flexible but opaque configurations.

**Smell:**  
Large YAML/JSON files controlling complex behavior.

---

## 6. One Concept Per File

- Each file represents a single, named concept.
- Avoid dumping logic into generic files (`Utils`, `Helpers`, `Common`).
- If a file becomes hard to name, it likely has too many responsibilities.

**Rule of thumb:**  
You should be able to explain a file’s purpose in one sentence.

---

## 7. Prefer Explicitness Over Cleverness

- Optimize for readability and maintainability, not brevity.
- Avoid “smart” code that requires mental decoding.
- Be explicit about intent, boundaries, and responsibilities.
- Small, intention-revealing methods are preferred.

**Smell:**  
Code that looks impressive but is hard to explain.

---

## 8. Ports Before Adapters

- Define abstractions (ports) before implementations (adapters).
- Databases, execution engines, and integrations must conform to domain-defined ports.
- Never let infrastructure choices leak into domain or application logic.

**Rule of thumb:**  
If you cannot swap an adapter without touching domain code, boundaries are wrong.

---

## 9. Code for Evolution, Not for the Current Sprint

- Favor designs that allow extension over modification.
- Anticipate new engines, new metrics, new rules, and new integrations.
- Avoid premature optimization, but do not block future growth.

**Rule of thumb:**  
A design is good if adding a new capability feels boring.

---

## 10. Determinism Enables Trust

- Given the same inputs, the system must produce the same outputs.
- Deterministic behavior is critical for testing, CI, and governance.
- Avoid hidden state and implicit dependencies.

**Smell:**  
Tests that fail intermittently without code changes.

---

## 11. Reviews Judge Intent, Not Just Syntax

- Code reviews focus on:
  - Domain correctness
  - Architectural alignment
  - Clarity of intent
- Style issues are secondary to conceptual correctness.
- Reject code that “works” but violates principles.

**Rule of thumb:**  
Correct code in the wrong place is still wrong.

---

## 12. Automation Is a Consumer, Not the Owner

- CI, tools, and AI assistants consume specifications and rules.
- They do not define architecture or domain behavior.
- Humans remain responsible for system design and evolution.

---

## Closing Principle

> Build systems that are easy to reason about,
> not systems that merely work today.

When in doubt:
- Favor clarity over flexibility
- Favor domain over tooling
- Favor long-term correctness over short-term speed
