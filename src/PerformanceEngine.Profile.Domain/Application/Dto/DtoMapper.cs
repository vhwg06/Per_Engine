namespace PerformanceEngine.Profile.Domain.Application.Dto;

using PerformanceEngine.Profile.Domain.Domain.Configuration;
using PerformanceEngine.Profile.Domain.Domain.Profiles;
using PerformanceEngine.Profile.Domain.Domain.Scopes;

/// <summary>
/// Mapping utilities between domain models and DTOs.
/// </summary>
public static class DtoMapper
{
    // Domain → DTO

    public static ConfigKeyDto ToDto(this ConfigKey key) =>
        new(key.Name);

    public static ConfigValueDto ToDto(this ConfigValue value) =>
        new(value.Value, value.Type.ToString());

    public static ScopeDto ToDto(this IScope scope) =>
        new(scope.Id, scope.Type, scope.Precedence, scope.Description);

    public static ProfileDto ToDto(this Profile profile) =>
        new(
            profile.Scope.ToDto(),
            profile.Configurations.ToDictionary(
                kvp => kvp.Key.Name,
                kvp => kvp.Value.ToDto()
            )
        );

    public static ResolvedProfileDto ToDto(this ResolvedProfile resolved) =>
        new(
            resolved.Configuration.ToDictionary(
                kvp => kvp.Key.Name,
                kvp => kvp.Value.ToDto()
            ),
            resolved.AuditTrail.ToDictionary(
                kvp => kvp.Key.Name,
                kvp => kvp.Value.Select(s => s.ToDto()).ToList()
            ),
            resolved.ResolvedAt
        );

    // DTO → Domain

    public static ConfigKey FromDto(ConfigKeyDto dto) =>
        new(dto.Name);

    public static ConfigValue FromDto(ConfigValueDto dto)
    {
        var type = Enum.Parse<ConfigType>(dto.Type);
        return new ConfigValue(dto.Value, type);
    }

    public static ConfigValue FromDto(string keyName, ConfigValueDto dto) =>
        FromDto(dto);
}
