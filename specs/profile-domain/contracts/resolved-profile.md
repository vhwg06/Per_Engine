# ResolvedProfile Contract

## Overview

`ResolvedProfile` is an **immutable entity** representing the result of configuration resolution. It contains the merged configuration dictionary, the requested scope, and an audit trail showing which scopes contributed to each configuration value.

**Key Characteristics**:
- **Immutable**: All properties are read-only; cannot be modified after creation
- **Complete**: Contains all configuration keys from applicable profiles
- **Auditable**: Tracks which scopes contributed to each key
- **Serializable**: Can be serialized to JSON for caching or logging

**Location**: `PerformanceEngine.Profile.Domain.Domain.Profiles.ResolvedProfile`

---

## Class Definition

```csharp
namespace PerformanceEngine.Profile.Domain.Domain.Profiles
{
    /// <summary>
    /// Represents the result of configuration resolution for a specific scope.
    /// Immutable - all properties are read-only.
    /// </summary>
    public sealed class ResolvedProfile
    {
        /// <summary>
        /// The scope that was requested during resolution.
        /// </summary>
        public IScope RequestedScope { get; }
        
        /// <summary>
        /// The merged configuration dictionary.
        /// Keys are ConfigKey, values are ConfigValue.
        /// Immutable - cannot be modified after creation.
        /// </summary>
        public ImmutableDictionary<ConfigKey, ConfigValue> Configuration { get; }
        
        /// <summary>
        /// Audit trail showing which scopes contributed to each configuration key.
        /// Keys are ConfigKey, values are lists of IScope in precedence order (lowest to highest).
        /// Immutable - cannot be modified after creation.
        /// </summary>
        public ImmutableDictionary<ConfigKey, ImmutableList<IScope>> AuditTrail { get; }
        
        /// <summary>
        /// Creates a new ResolvedProfile.
        /// </summary>
        /// <param name="requestedScope">The scope that was requested</param>
        /// <param name="configuration">Merged configuration dictionary</param>
        /// <param name="auditTrail">Audit trail of scope contributions</param>
        /// <exception cref="ArgumentNullException">
        /// If requestedScope, configuration, or auditTrail is null
        /// </exception>
        public ResolvedProfile(
            IScope requestedScope,
            ImmutableDictionary<ConfigKey, ConfigValue> configuration,
            ImmutableDictionary<ConfigKey, ImmutableList<IScope>> auditTrail)
        {
            RequestedScope = requestedScope ?? throw new ArgumentNullException(nameof(requestedScope));
            Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            AuditTrail = auditTrail ?? throw new ArgumentNullException(nameof(auditTrail));
        }
    }
}
```

---

## Contract Requirements

### 1. RequestedScope Property

**Requirements**:
- MUST NOT be null
- MUST be the scope that was passed to `ProfileResolver.Resolve()`
- MUST be immutable
- MAY be a CompositeScope if multiple scopes were requested

**Valid Examples**:
```csharp
RequestedScope = GlobalScope.Instance;
RequestedScope = new ApiScope("payment");
RequestedScope = new EnvironmentScope("prod");
RequestedScope = new CompositeScope(new ApiScope("payment"), new EnvironmentScope("prod"));
```

**Invalid Examples**:
```csharp
RequestedScope = null; // ❌ Violates non-null requirement
```

---

### 2. Configuration Property

**Requirements**:
- MUST NOT be null
- MUST be an ImmutableDictionary<ConfigKey, ConfigValue>
- MAY be empty if no profiles matched the requested scope
- MUST contain only keys from applicable profiles
- MUST reflect precedence-based merging (highest precedence wins)
- MUST be deterministic (same inputs produce identical configuration)

**Structure**:
```csharp
Configuration = ImmutableDictionary<ConfigKey, ConfigValue>.Empty
    .Add(new ConfigKey("timeout"), ConfigValue.Create("90s"))
    .Add(new ConfigKey("retries"), ConfigValue.Create(3))
    .Add(new ConfigKey("log_level"), ConfigValue.Create("warn"));
```

**Access Patterns**:
```csharp
// Check if key exists
bool hasTimeout = resolvedProfile.Configuration.ContainsKey(new ConfigKey("timeout"));

// Get value (throws KeyNotFoundException if not found)
var timeout = resolvedProfile.Configuration[new ConfigKey("timeout")];

// Try get value (safe)
if (resolvedProfile.Configuration.TryGetValue(new ConfigKey("timeout"), out var value))
{
    Console.WriteLine(value.Value);
}

// Enumerate all keys
foreach (var (key, value) in resolvedProfile.Configuration)
{
    Console.WriteLine($"{key.Name} = {value.Value}");
}
```

---

### 3. AuditTrail Property

**Requirements**:
- MUST NOT be null
- MUST be an ImmutableDictionary<ConfigKey, ImmutableList<IScope>>
- Keys SHOULD correspond to keys in Configuration (though may include overridden keys)
- Scope lists MUST be ordered by precedence (lowest to highest)
- MUST track ALL scopes that contributed to each key (including overridden values)
- MUST be immutable

**Structure**:
```csharp
AuditTrail = ImmutableDictionary<ConfigKey, ImmutableList<IScope>>.Empty
    .Add(new ConfigKey("timeout"), ImmutableList.Create<IScope>(
        GlobalScope.Instance,           // First contributed: 30s (overridden)
        new EnvironmentScope("prod")    // Final value: 90s (won)
    ))
    .Add(new ConfigKey("retries"), ImmutableList.Create<IScope>(
        GlobalScope.Instance            // Only contribution: 3
    ))
    .Add(new ConfigKey("log_level"), ImmutableList.Create<IScope>(
        GlobalScope.Instance,           // First contributed: info (overridden)
        new EnvironmentScope("prod")    // Final value: warn (won)
    ));
```

**Interpretation**:
- **Last scope in list** = final value (highest precedence that won)
- **Earlier scopes** = overridden values (historical contributions)
- **Single scope** = no overrides, only one profile provided this key

**Access Patterns**:
```csharp
// Get audit trail for a key
var timeoutTrail = resolvedProfile.AuditTrail[new ConfigKey("timeout")];

// Print scope progression
Console.WriteLine(string.Join(" → ", timeoutTrail));
// Output: "Global → Environment:prod"

// Get final scope (highest precedence)
var finalScope = timeoutTrail.Last();
Console.WriteLine($"Final value from: {finalScope}");
// Output: "Final value from: Environment:prod"

// Count contributors
int contributorCount = timeoutTrail.Count;
Console.WriteLine($"Key was defined in {contributorCount} profiles");

// Check if key was overridden
bool wasOverridden = timeoutTrail.Count > 1;
```

---

## Invariants

### Invariant 1: Configuration-AuditTrail Consistency

**Rule**: Every key in `Configuration` MUST have a corresponding entry in `AuditTrail`.

**Rationale**: If a configuration key exists, we must know which scope(s) contributed it.

**Validation**:
```csharp
foreach (var key in resolvedProfile.Configuration.Keys)
{
    Debug.Assert(resolvedProfile.AuditTrail.ContainsKey(key), 
        $"Configuration key '{key}' missing from AuditTrail");
}
```

**Note**: The reverse is NOT required - `AuditTrail` may contain keys not in `Configuration` (e.g., if a key was overridden multiple times but the final scope didn't match).

---

### Invariant 2: Precedence Ordering in AuditTrail

**Rule**: Scopes in each `AuditTrail` list MUST be ordered by precedence (ascending).

**Rationale**: Audit trail shows resolution history from lowest to highest precedence.

**Validation**:
```csharp
foreach (var (key, scopes) in resolvedProfile.AuditTrail)
{
    for (int i = 1; i < scopes.Count; i++)
    {
        Debug.Assert(scopes[i-1].Precedence <= scopes[i].Precedence,
            $"AuditTrail for '{key}' not sorted by precedence");
    }
}
```

---

### Invariant 3: Non-Empty AuditTrail Lists

**Rule**: Each `AuditTrail` list MUST contain at least one scope.

**Rationale**: Every configuration key came from at least one profile.

**Validation**:
```csharp
foreach (var (key, scopes) in resolvedProfile.AuditTrail)
{
    Debug.Assert(scopes.Count > 0, 
        $"AuditTrail for '{key}' is empty");
}
```

---

### Invariant 4: Immutability

**Rule**: All properties MUST be immutable (read-only).

**Rationale**: ResolvedProfile represents a point-in-time snapshot of configuration. Modifications would violate determinism.

**Validation**:
```csharp
// ✅ Valid (creates new instance)
var updatedConfig = resolvedProfile.Configuration.Add(
    new ConfigKey("new_key"), 
    ConfigValue.Create("new_value")
);

// ✅ Original is unchanged
Debug.Assert(!resolvedProfile.Configuration.ContainsKey(new ConfigKey("new_key")));

// ❌ Invalid (not possible - no setter)
// resolvedProfile.Configuration = newConfig; // Compile error
```

---

## Usage Examples

### Example 1: Simple Resolution

```csharp
var profiles = new[]
{
    new Profile(GlobalScope.Instance, 
        ImmutableDictionary<ConfigKey, ConfigValue>.Empty
            .Add(new ConfigKey("timeout"), ConfigValue.Create("30s"))
            .Add(new ConfigKey("retries"), ConfigValue.Create(3))
    ),
    
    new Profile(new EnvironmentScope("prod"),
        ImmutableDictionary<ConfigKey, ConfigValue>.Empty
            .Add(new ConfigKey("timeout"), ConfigValue.Create("90s"))
    )
};

var result = ProfileResolver.Resolve(profiles, new EnvironmentScope("prod"));

// Inspect result
Console.WriteLine($"Requested Scope: {result.RequestedScope}");
// Output: "Requested Scope: Environment:prod"

Console.WriteLine($"Configuration Keys: {result.Configuration.Count}");
// Output: "Configuration Keys: 2"

// Access configuration
var timeout = result.Configuration[new ConfigKey("timeout")];
Console.WriteLine($"Timeout: {timeout.Value}");
// Output: "Timeout: 90s"

// Inspect audit trail
var timeoutTrail = result.AuditTrail[new ConfigKey("timeout")];
Console.WriteLine($"Timeout sources: {string.Join(" → ", timeoutTrail)}");
// Output: "Timeout sources: Global → Environment:prod"
```

---

### Example 2: Debugging Configuration Resolution

```csharp
public void PrintResolutionDetails(ResolvedProfile resolved)
{
    Console.WriteLine($"=== Configuration for {resolved.RequestedScope} ===\n");
    
    foreach (var (key, value) in resolved.Configuration)
    {
        Console.WriteLine($"Key: {key.Name}");
        Console.WriteLine($"  Value: {value.Value} ({value.Type})");
        
        if (resolved.AuditTrail.TryGetValue(key, out var trail))
        {
            Console.WriteLine($"  Sources: {string.Join(" → ", trail)}");
            
            if (trail.Count > 1)
            {
                Console.WriteLine($"  ⚠️  Overridden {trail.Count - 1} time(s)");
            }
        }
        
        Console.WriteLine();
    }
}
```

**Output**:
```
=== Configuration for Environment:prod ===

Key: timeout
  Value: 90s (string)
  Sources: Global → Environment:prod
  ⚠️  Overridden 1 time(s)

Key: retries
  Value: 3 (integer)
  Sources: Global

Key: log_level
  Value: warn (string)
  Sources: Global → Environment:prod
  ⚠️  Overridden 1 time(s)
```

---

### Example 3: Caching Resolved Profiles

```csharp
using System.Text.Json;

public class ProfileCache
{
    private readonly Dictionary<string, string> _cache = new();
    
    public void CacheResolved(ResolvedProfile resolved)
    {
        var cacheKey = $"{resolved.RequestedScope.Type}:{resolved.RequestedScope.Value}";
        
        // Serialize to JSON
        var json = JsonSerializer.Serialize(new
        {
            RequestedScope = resolved.RequestedScope.ToString(),
            Configuration = resolved.Configuration.ToDictionary(
                kvp => kvp.Key.Name,
                kvp => kvp.Value.Value
            ),
            AuditTrail = resolved.AuditTrail.ToDictionary(
                kvp => kvp.Key.Name,
                kvp => kvp.Value.Select(s => s.ToString()).ToArray()
            )
        });
        
        _cache[cacheKey] = json;
    }
    
    public string? GetCached(IScope scope)
    {
        var cacheKey = $"{scope.Type}:{scope.Value}";
        return _cache.TryGetValue(cacheKey, out var json) ? json : null;
    }
}
```

---

### Example 4: Comparing Resolved Profiles

```csharp
public bool AreEquivalent(ResolvedProfile a, ResolvedProfile b)
{
    // Check if configurations are identical
    if (a.Configuration.Count != b.Configuration.Count)
        return false;
    
    foreach (var (key, valueA) in a.Configuration)
    {
        if (!b.Configuration.TryGetValue(key, out var valueB))
            return false;
        
        if (!valueA.Equals(valueB))
            return false;
    }
    
    return true;
}

public void CompareConfigurations(ResolvedProfile prod, ResolvedProfile staging)
{
    Console.WriteLine("=== Configuration Differences ===\n");
    
    var allKeys = prod.Configuration.Keys
        .Union(staging.Configuration.Keys)
        .Distinct();
    
    foreach (var key in allKeys)
    {
        var hasProd = prod.Configuration.TryGetValue(key, out var prodValue);
        var hasStaging = staging.Configuration.TryGetValue(key, out var stagingValue);
        
        if (!hasProd)
        {
            Console.WriteLine($"❌ {key.Name}: Missing in prod");
        }
        else if (!hasStaging)
        {
            Console.WriteLine($"❌ {key.Name}: Missing in staging");
        }
        else if (!prodValue!.Equals(stagingValue!))
        {
            Console.WriteLine($"⚠️  {key.Name}: {prodValue.Value} (prod) vs {stagingValue.Value} (staging)");
        }
        else
        {
            Console.WriteLine($"✅ {key.Name}: {prodValue.Value} (identical)");
        }
    }
}
```

---

## Testing Requirements

### Unit Tests

1. **Constructor Validation**:
   - Null requestedScope throws ArgumentNullException
   - Null configuration throws ArgumentNullException
   - Null auditTrail throws ArgumentNullException

2. **Property Access**:
   - RequestedScope returns correct scope
   - Configuration returns immutable dictionary
   - AuditTrail returns immutable dictionary

3. **Immutability**:
   - Adding to Configuration returns new instance
   - Original Configuration unchanged

4. **Configuration Access**:
   - ContainsKey works correctly
   - Indexer returns correct value
   - TryGetValue works correctly
   - Enumeration iterates all keys

5. **AuditTrail Consistency**:
   - All Configuration keys present in AuditTrail
   - AuditTrail lists ordered by precedence
   - AuditTrail lists non-empty

---

## Design Rationale

### Why Immutable?

- **Thread-Safety**: No synchronization needed for concurrent reads
- **Determinism**: Resolved profile never changes after creation
- **Caching**: Safe to cache and reuse
- **Debugging**: Snapshot of resolution state at a point in time

### Why Include AuditTrail?

- **Debugging**: Understand why a particular value was chosen
- **Compliance**: Track configuration sources for auditing
- **Transparency**: Users can see the resolution logic
- **Override Detection**: Identify when values are being overridden

### Why Separate Configuration and AuditTrail?

- **Performance**: Don't force audit trail processing if not needed
- **Flexibility**: Audit trail can include overridden values
- **Clarity**: Separate concerns (final config vs. resolution history)

### Why ImmutableDictionary?

- **Performance**: O(log n) lookup, efficient for large configurations
- **Immutability**: Guaranteed thread-safe, no defensive copying
- **Structural Sharing**: Memory-efficient when creating variants

---

## See Also

- [scope-interface.md](scope-interface.md) - IScope contract
- [resolver-interface.md](resolver-interface.md) - ProfileResolver contract
- [conflict-handling.md](conflict-handling.md) - ConflictHandler contract
- [SCOPE_HIERARCHY.md](../../../docs/SCOPE_HIERARCHY.md) - Precedence rules
- [Profile Domain README](../../../src/PerformanceEngine.Profile.Domain/README.md) - Architecture overview
