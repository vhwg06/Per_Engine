using PerformanceEngine.Profile.Domain.Domain.Scopes;
using System.Collections.Concurrent;

namespace PerformanceEngine.Profile.Domain.Application.Services;

/// <summary>
/// Registry for custom scope types at runtime.
/// Allows applications to register and query scope types dynamically.
/// </summary>
public class ScopeRegistry
{
    private readonly ConcurrentDictionary<string, Func<IScope>> _scopeFactories = new();

    /// <summary>
    /// Registers a scope factory for a given type name.
    /// </summary>
    /// <param name="scopeType">The scope type identifier (e.g., "PaymentMethod", "Region")</param>
    /// <param name="factory">Factory function to create scope instances</param>
    public void RegisterScope(string scopeType, Func<IScope> factory)
    {
        if (string.IsNullOrWhiteSpace(scopeType))
            throw new ArgumentException("Scope type cannot be null or empty", nameof(scopeType));

        if (factory == null)
            throw new ArgumentNullException(nameof(factory));

        if (!_scopeFactories.TryAdd(scopeType, factory))
        {
            throw new InvalidOperationException($"Scope type '{scopeType}' is already registered");
        }
    }

    /// <summary>
    /// Gets a scope by type name.
    /// </summary>
    /// <param name="scopeType">The scope type identifier</param>
    /// <returns>Scope instance, or null if not registered</returns>
    public IScope? GetScopeByType(string scopeType)
    {
        if (string.IsNullOrWhiteSpace(scopeType))
            return null;

        return _scopeFactories.TryGetValue(scopeType, out var factory)
            ? factory()
            : null;
    }

    /// <summary>
    /// Checks if a scope type is registered.
    /// </summary>
    public bool IsRegistered(string scopeType)
    {
        return !string.IsNullOrWhiteSpace(scopeType) &&
               _scopeFactories.ContainsKey(scopeType);
    }

    /// <summary>
    /// Gets all registered scope type names.
    /// </summary>
    public IReadOnlyCollection<string> GetRegisteredTypes()
    {
        return _scopeFactories.Keys.ToList();
    }

    /// <summary>
    /// Unregisters a scope type.
    /// </summary>
    public bool UnregisterScope(string scopeType)
    {
        if (string.IsNullOrWhiteSpace(scopeType))
            return false;

        return _scopeFactories.TryRemove(scopeType, out _);
    }

    /// <summary>
    /// Clears all registered scopes.
    /// </summary>
    public void Clear()
    {
        _scopeFactories.Clear();
    }
}
