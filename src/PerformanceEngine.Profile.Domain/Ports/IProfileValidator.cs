namespace PerformanceEngine.Profile.Domain.Ports;

using PerformanceEngine.Profile.Domain.Domain.Validation;
using PerformanceEngine.Profile.Domain.Domain.Profiles;

/// <summary>
/// Port interface for profile validation.
/// Validates profile configuration before use in evaluations.
/// Implementations must check for circular dependencies, required keys, type correctness, and scope validity.
/// </summary>
public interface IProfileValidator
{
    /// <summary>
    /// Validates the provided profile configuration.
    /// Performs comprehensive validation including:
    /// - Circular override dependency detection
    /// - Required keys presence verification
    /// - Type correctness validation for all values
    /// - Scope validity (global/api/endpoint only)
    /// - Range constraints per override definition
    /// 
    /// Uses non-early-exit strategy: collects all errors at once for complete feedback.
    /// </summary>
    /// <param name="profile">Profile to validate (required)</param>
    /// <returns>ValidationResult indicating success/failure and listing all errors</returns>
    /// <exception cref="ArgumentNullException">If profile is null</exception>
    ValidationResult Validate(Profile profile);
}
