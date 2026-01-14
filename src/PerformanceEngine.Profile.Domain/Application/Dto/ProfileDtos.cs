namespace PerformanceEngine.Profile.Domain.Application.Dto;

/// <summary>
/// Data transfer object for ConfigKey.
/// </summary>
public sealed record ConfigKeyDto(string Name);

/// <summary>
/// Data transfer object for ConfigValue.
/// </summary>
public sealed record ConfigValueDto(object Value, string Type);

/// <summary>
/// Data transfer object for Scope.
/// </summary>
public sealed record ScopeDto(string Id, string Type, int Precedence, string Description);

/// <summary>
/// Data transfer object for Profile.
/// </summary>
public sealed record ProfileDto(
    ScopeDto Scope,
    Dictionary<string, ConfigValueDto> Configurations);

/// <summary>
/// Data transfer object for ResolvedProfile.
/// </summary>
public sealed record ResolvedProfileDto(
    Dictionary<string, ConfigValueDto> Configuration,
    Dictionary<string, List<ScopeDto>> AuditTrail,
    DateTime ResolvedAt);
