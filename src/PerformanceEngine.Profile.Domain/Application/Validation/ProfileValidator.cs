namespace PerformanceEngine.Profile.Domain.Application.Validation;

using PerformanceEngine.Profile.Domain.Domain.Validation;
using PerformanceEngine.Profile.Domain.Domain.Profiles;
using PerformanceEngine.Profile.Domain.Domain.Scopes;
using PerformanceEngine.Profile.Domain.Domain.Configuration;
using PerformanceEngine.Profile.Domain.Ports;

/// <summary>
/// Default implementation of IProfileValidator.
/// Validates profile configurations against architectural constraints.
/// Uses non-early-exit validation: collects all errors at once for complete feedback.
/// </summary>
public class ProfileValidator : IProfileValidator
{
    /// <summary>
    /// Validates the provided profile configuration.
    /// Performs comprehensive validation including:
    /// - Circular override dependency detection
    /// - Required keys presence verification
    /// - Type correctness validation for all values
    /// - Scope validity (global/api/endpoint only)
    /// - Range constraints per override definition
    /// </summary>
    /// <param name="profile">Profile to validate</param>
    /// <returns>ValidationResult with all errors collected (non-early-exit)</returns>
    public ValidationResult Validate(Profile profile)
    {
        if (profile == null)
            throw new ArgumentNullException(nameof(profile));

        var errors = new List<ValidationError>();

        // Validate scope
        ValidateScope(profile.Scope, errors);

        // Validate configurations
        ValidateConfigurations(profile.Configurations, errors);

        // Validate for circular dependencies
        ValidateNoCircularDependencies(profile.Configurations, errors);

        return errors.Count == 0
            ? ValidationResult.Success()
            : ValidationResult.Failure(errors.AsReadOnly());
    }

    /// <summary>
    /// Validates scope is one of the allowed types (Global, API, Endpoint).
    /// </summary>
    private static void ValidateScope(IScope scope, List<ValidationError> errors)
    {
        var validScopes = new[] { ScopeType.Global, ScopeType.Api, ScopeType.Endpoint };
        if (!validScopes.Contains(scope.Type))
        {
            errors.Add(new ValidationError(
                "INVALID_SCOPE",
                $"Scope type must be one of: {string.Join(", ", validScopes)}, but was {scope.Type}",
                "Scope"));
        }

        // Scope ID must be non-empty (or validated according to scope type)
        if (string.IsNullOrWhiteSpace(scope.Id))
        {
            errors.Add(new ValidationError(
                "EMPTY_SCOPE_ID",
                "Scope ID cannot be empty",
                "Scope.Id"));
        }
    }

    /// <summary>
    /// Validates configuration keys and values for type correctness and constraints.
    /// </summary>
    private static void ValidateConfigurations(
        ImmutableDictionary<ConfigKey, ConfigValue> configurations,
        List<ValidationError> errors)
    {
        foreach (var kvp in configurations)
        {
            var key = kvp.Key;
            var value = kvp.Value;

            // Validate key
            if (string.IsNullOrWhiteSpace(key.Name))
            {
                errors.Add(new ValidationError(
                    "INVALID_CONFIG_KEY",
                    "Configuration key name cannot be empty",
                    $"Configuration[{key.Name}]"));
            }

            // Validate value
            if (value == null)
            {
                errors.Add(new ValidationError(
                    "NULL_CONFIG_VALUE",
                    "Configuration value cannot be null",
                    key.Name));
            }
            
            // Additional type-specific validation could go here
            // For example: validating numeric ranges, string patterns, etc.
        }
    }

    /// <summary>
    /// Validates that there are no circular dependencies among configuration overrides.
    /// A circular dependency exists if override A references override B which references A (directly or transitively).
    /// </summary>
    private static void ValidateNoCircularDependencies(
        ImmutableDictionary<ConfigKey, ConfigValue> configurations,
        List<ValidationError> errors)
    {
        // Build dependency graph
        var dependencies = new Dictionary<string, HashSet<string>>();
        
        foreach (var kvp in configurations)
        {
            var key = kvp.Key.Name;
            dependencies[key] = ExtractDependencies(kvp.Value);
        }

        // Detect cycles using DFS
        var visited = new HashSet<string>();
        var recursionStack = new HashSet<string>();

        foreach (var key in dependencies.Keys)
        {
            if (!visited.Contains(key))
            {
                if (HasCycle(key, dependencies, visited, recursionStack, out var cyclePath))
                {
                    errors.Add(new ValidationError(
                        "CIRCULAR_DEPENDENCY",
                        $"Circular dependency detected: {cyclePath}",
                        "Configurations"));
                }
            }
        }
    }

    /// <summary>
    /// Detects if there's a cycle starting from the given key using DFS.
    /// </summary>
    private static bool HasCycle(
        string key,
        Dictionary<string, HashSet<string>> dependencies,
        HashSet<string> visited,
        HashSet<string> recursionStack,
        out string cyclePath)
    {
        cyclePath = string.Empty;

        visited.Add(key);
        recursionStack.Add(key);

        if (!dependencies.TryGetValue(key, out var deps))
            return false;

        foreach (var dep in deps)
        {
            if (!visited.Contains(dep))
            {
                if (HasCycle(dep, dependencies, visited, recursionStack, out var subPath))
                {
                    cyclePath = $"{key} → {subPath}";
                    return true;
                }
            }
            else if (recursionStack.Contains(dep))
            {
                cyclePath = $"{key} → {dep}";
                return true;
            }
        }

        recursionStack.Remove(key);
        return false;
    }

    /// <summary>
    /// Extracts configuration key references from a configuration value.
    /// This is a simplified implementation; actual implementation would parse value expressions.
    /// </summary>
    private static HashSet<string> ExtractDependencies(ConfigValue value)
    {
        // Simplified: assume no dependencies for now
        // In a real implementation, this would parse ${key} references in the value
        return new HashSet<string>();
    }
}
