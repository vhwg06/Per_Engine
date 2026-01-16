namespace PerformanceEngine.Application.Ports;

using PerformanceEngine.Profile.Domain.Domain.Profiles;

/// <summary>
/// Port abstraction for resolving performance profiles.
/// Provides access to profile configurations based on execution context.
/// </summary>
public interface IProfileResolver
{
    /// <summary>
    /// Resolves a profile by its identifier.
    /// </summary>
    /// <param name="profileId">Unique identifier of the profile to resolve.</param>
    /// <returns>Resolved profile configuration.</returns>
    /// <exception cref="ArgumentException">Thrown when profile not found.</exception>
    ResolvedProfile ResolveProfile(string profileId);

    /// <summary>
    /// Gets all available profile identifiers.
    /// </summary>
    /// <returns>Collection of available profile IDs.</returns>
    IReadOnlyCollection<string> GetAvailableProfileIds();

    /// <summary>
    /// Checks if a profile exists with the given identifier.
    /// </summary>
    /// <param name="profileId">Profile identifier to check.</param>
    /// <returns>True if profile exists; otherwise false.</returns>
    bool ProfileExists(string profileId);
}
