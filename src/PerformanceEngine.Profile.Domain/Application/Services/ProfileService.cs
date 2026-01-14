namespace PerformanceEngine.Profile.Domain.Application.Services;

using PerformanceEngine.Profile.Domain.Application.Dto;
using PerformanceEngine.Profile.Domain.Application.UseCases;
using PerformanceEngine.Profile.Domain.Domain.Configuration;
using PerformanceEngine.Profile.Domain.Domain.Profiles;
using PerformanceEngine.Profile.Domain.Domain.Scopes;

/// <summary>
/// Application service facade for profile resolution.
/// </summary>
public class ProfileService
{
    private readonly ResolveProfileUseCase _resolveUseCase;

    public ProfileService()
    {
        _resolveUseCase = new ResolveProfileUseCase();
    }

    /// <summary>
    /// Resolves profiles for a requested scope.
    /// </summary>
    public ResolvedProfile Resolve(IEnumerable<Profile> profiles, IScope requestedScope)
    {
        return _resolveUseCase.Execute(profiles, requestedScope);
    }

    /// <summary>
    /// Resolves profiles for multiple requested scopes (multi-dimensional).
    /// </summary>
    public ResolvedProfile Resolve(IEnumerable<Profile> profiles, IEnumerable<IScope> requestedScopes)
    {
        return ProfileResolver.Resolve(profiles, requestedScopes);
    }

    /// <summary>
    /// Resolves profiles and returns a DTO.
    /// </summary>
    public ResolvedProfileDto ResolveToDto(IEnumerable<Profile> profiles, IScope requestedScope)
    {
        var resolved = Resolve(profiles, requestedScope);
        return resolved.ToDto();
    }

    /// <summary>
    /// Validates profiles for conflicts without resolving.
    /// </summary>
    public void ValidateNoConflicts(IEnumerable<Profile> profiles)
    {
        ConflictHandler.ValidateNoConflicts(profiles);
    }

    /// <summary>
    /// Detects conflicts in profiles.
    /// </summary>
    public List<ConflictDetail> DetectConflicts(IEnumerable<Profile> profiles)
    {
        return ConflictHandler.DetectConflicts(profiles);
    }
}
