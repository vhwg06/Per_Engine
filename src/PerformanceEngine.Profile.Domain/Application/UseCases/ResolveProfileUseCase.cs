namespace PerformanceEngine.Profile.Domain.Application.UseCases;

using PerformanceEngine.Profile.Domain.Domain.Configuration;
using PerformanceEngine.Profile.Domain.Domain.Profiles;
using PerformanceEngine.Profile.Domain.Domain.Scopes;

/// <summary>
/// Use case for resolving configuration profiles.
/// </summary>
public class ResolveProfileUseCase
{
    /// <summary>
    /// Resolves profiles for a requested scope.
    /// Validates for conflicts before resolution.
    /// </summary>
    public ResolvedProfile Execute(IEnumerable<Profile> profiles, IScope requestedScope)
    {
        try
        {
            // Validate no conflicts
            ConflictHandler.ValidateNoConflicts(profiles);

            // Resolve
            return ProfileResolver.Resolve(profiles, requestedScope);
        }
        catch (ConfigurationConflictException ex)
        {
            // Re-throw with additional context
            throw new InvalidOperationException(
                $"Cannot resolve profiles due to conflicts: {ex.Message}",
                ex
            );
        }
    }
}
