namespace PerformanceEngine.Evaluation.Domain.ValueObjects;

/// <summary>
/// Base class for value objects in the Evaluation Domain.
/// Value objects use value-based equality (not identity-based).
/// Immutable after construction.
/// </summary>
public abstract record ValueObject;
