namespace PerformanceEngine.Profile.Domain.Domain;

/// <summary>
/// State machine enum for Profile lifecycle management.
/// Controls when profiles can be modified, resolved, and used in evaluations.
/// Ensures deterministic resolution by enforcing clear state transitions.
/// </summary>
public enum ProfileState
{
    /// <summary>
    /// Initial state: Profile accepts overrides but cannot be used for evaluation.
    /// Overrides can be applied in any order.
    /// Transition to Resolved by calling Resolve().
    /// </summary>
    Unresolved = 1,

    /// <summary>
    /// Resolved state: Profile is ready for use in evaluations.
    /// Overrides are applied in deterministic order (scope priority → key alphabetical).
    /// No further modifications allowed after resolution.
    /// Transition from Unresolved only.
    /// </summary>
    Resolved = 2,

    /// <summary>
    /// Invalid state: Profile failed validation and cannot be used.
    /// Indicates validation errors that must be fixed before evaluation.
    /// Cannot transition back to Unresolved; new profile instance required.
    /// </summary>
    Invalid = 3
}
