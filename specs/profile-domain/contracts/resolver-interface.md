# ProfileResolver Contract

## Overview

The `ProfileResolver` is a **stateless domain service** that resolves configuration based on context (scope). It takes a collection of profiles and a requested scope, then produces a single `ResolvedProfile` with merged configuration and audit trail.

**Key Characteristics**:
- **Deterministic**: Same inputs always produce identical outputs
- **Pure Function**: No side effects, no I/O, no state mutation
- **Conflict Detection**: Fails fast if multiple profiles at the same scope define different values
- **Immutable Results**: ResolvedProfile is read-only

**Location**: `PerformanceEngine.Profile.Domain.Domain.Profiles.ProfileResolver`

---

## Interface Definition

```csharp
namespace PerformanceEngine.Profile.Domain.Domain.Profiles
{
    /// <summary>
    /// Stateless domain service for resolving configuration profiles based on scope.
    /// </summary>
    public static class ProfileResolver
    {
        /// <summary>
        /// Resolves configuration for a single requested scope.
        /// </summary>
        /// <param name="profiles">Collection of configuration profiles to resolve from</param>
        /// <param name="requestedScope">The scope to resolve configuration for</param>
        /// <returns>ResolvedProfile with merged configuration and audit trail</returns>
        /// <exception cref="ArgumentNullException">If profiles or requestedScope is null</exception>
        /// <exception cref="ConfigurationConflictException">
        /// If multiple profiles at the same scope define different values for the same key
        /// </exception>
        public static ResolvedProfile Resolve(
            IEnumerable<Profile> profiles, 
            IScope requestedScope);

        /// <summary>
        /// Resolves configuration for multiple requested scopes (multi-dimensional resolution).
        /// Creates a CompositeScope from the requested scopes and resolves accordingly.
        /// </summary>
        /// <param name="profiles">Collection of configuration profiles to resolve from</param>
        /// <param name="requestedScopes">Multiple scopes to resolve configuration for</param>
        /// <returns>ResolvedProfile with merged configuration and audit trail</returns>
        /// <exception cref="ArgumentNullException">If profiles or requestedScopes is null</exception>
        /// <exception cref="ArgumentException">If requestedScopes is empty</exception>
        /// <exception cref="ConfigurationConflictException">
        /// If multiple profiles at the same scope define different values for the same key
        /// </exception>
        public static ResolvedProfile Resolve(
            IEnumerable<Profile> profiles, 
            IEnumerable<IScope> requestedScopes);
    }
}
```

---

## Contract Requirements

### 1. Input Validation

**Requirements**:
- MUST throw `ArgumentNullException` if `profiles` is null
- MUST throw `ArgumentNullException` if `requestedScope(s)` is null
- MUST throw `ArgumentException` if `requestedScopes` is empty (multi-dimensional overload)
- SHOULD accept empty `profiles` collection (returns empty configuration)

**Test Cases**:
```csharp
// ❌ Null profiles
ProfileResolver.Resolve(null, GlobalScope.Instance); 
// Throws: ArgumentNullException("profiles")

// ❌ Null requested scope
ProfileResolver.Resolve(profiles, (IScope)null);
// Throws: ArgumentNullException("requestedScope")

// ❌ Empty requested scopes
ProfileResolver.Resolve(profiles, Array.Empty<IScope>());
// Throws: ArgumentException("requestedScopes cannot be empty")

// ✅ Empty profiles (valid)
ProfileResolver.Resolve(Array.Empty<Profile>(), GlobalScope.Instance);
// Returns: ResolvedProfile with empty configuration
```

---

### 2. Resolution Algorithm

**Steps**:

1. **Filter Applicable Profiles**:
   - For each profile, call `profile.Scope.Matches(requestedScope)`
   - Include profiles where `Matches()` returns true
   - GlobalScope always matches (applies to all contexts)

2. **Sort by Precedence**:
   - Order profiles by `Scope.Precedence` ascending (lowest to highest)
   - If same precedence, preserve input order (stable sort)

3. **Detect Conflicts**:
   - For each configuration key, check if multiple profiles at the **exact same scope** define different values
   - If conflict detected, throw `ConfigurationConflictException` immediately (fail-fast)
   - Same scope = same `Type`, `Value`, and `Precedence`

4. **Merge Configuration**:
   - Start with empty configuration
   - For each profile (in precedence order):
     - Add/override configuration values
     - Track which scope contributed each key (audit trail)
   - **Last value wins** (highest precedence)

5. **Build Audit Trail**:
   - For each configuration key, record all scopes that provided a value
   - Order: Global → API → Environment → Tag → Composite (precedence order)

6. **Return Immutable Result**:
   - Create `ResolvedProfile` with:
     - `RequestedScope`: The scope requested
     - `Configuration`: ImmutableDictionary of merged values
     - `AuditTrail`: ImmutableDictionary tracking scope contributions

**Determinism Requirements**:
- MUST produce byte-identical results across 1000+ invocations
- MUST be order-independent (profile input order doesn't affect result, only precedence)
- MUST NOT depend on system time, random values, or environment variables

---

### 3. Conflict Detection

**Conflict Definition**:
A conflict occurs when:
- Two or more profiles have **identical scopes** (same Type, Value, Precedence)
- They define **different values** for the **same ConfigKey**

**Conflict Examples**:

**❌ Conflict** (same scope, different values):
```csharp
var profile1 = new Profile(
    new ApiScope("payment"),
    ImmutableDictionary<ConfigKey, ConfigValue>.Empty
        .Add(new ConfigKey("timeout"), ConfigValue.Create("30s"))
);

var profile2 = new Profile(
    new ApiScope("payment"), // SAME SCOPE
    ImmutableDictionary<ConfigKey, ConfigValue>.Empty
        .Add(new ConfigKey("timeout"), ConfigValue.Create("60s")) // DIFFERENT VALUE
);

// Throws: ConfigurationConflictException
ProfileResolver.Resolve(new[] { profile1, profile2 }, new ApiScope("payment"));
```

**✅ Not a Conflict** (different scopes):
```csharp
var globalProfile = new Profile(
    GlobalScope.Instance, // Precedence 0
    ImmutableDictionary<ConfigKey, ConfigValue>.Empty
        .Add(new ConfigKey("timeout"), ConfigValue.Create("30s"))
);

var apiProfile = new Profile(
    new ApiScope("payment"), // Precedence 10 (different scope)
    ImmutableDictionary<ConfigKey, ConfigValue>.Empty
        .Add(new ConfigKey("timeout"), ConfigValue.Create("60s"))
);

// ✅ No conflict - precedence resolves ambiguity
var result = ProfileResolver.Resolve(new[] { globalProfile, apiProfile }, new ApiScope("payment"));
// Result: timeout = "60s" (API scope wins)
```

**✅ Not a Conflict** (same scope, same value):
```csharp
var profile1 = new Profile(
    new ApiScope("payment"),
    ImmutableDictionary<ConfigKey, ConfigValue>.Empty
        .Add(new ConfigKey("timeout"), ConfigValue.Create("30s"))
);

var profile2 = new Profile(
    new ApiScope("payment"), // SAME SCOPE
    ImmutableDictionary<ConfigKey, ConfigValue>.Empty
        .Add(new ConfigKey("timeout"), ConfigValue.Create("30s")) // SAME VALUE
);

// ✅ No conflict - identical values
var result = ProfileResolver.Resolve(new[] { profile1, profile2 }, new ApiScope("payment"));
```

**Exception Format**:
```csharp
throw new ConfigurationConflictException(
    $"Configuration conflicts detected: {conflictCount} conflict(s)\n" +
    $"  - Key '{key}' has conflicting values in scope {scope}: {value1} vs {value2}"
);
```

---

### 4. Multi-Dimensional Resolution

When resolving with multiple scopes:

1. **Create CompositeScope**: Combine requested scopes into a single CompositeScope
2. **Calculate Precedence**: `max(scope1.Precedence, scope2.Precedence, ...) + 5`
3. **Partial Matching**: Profiles matching any dimension are included
4. **Apply Standard Algorithm**: Same conflict detection and merging logic

**Example**:
```csharp
var profiles = new[]
{
    new Profile(GlobalScope.Instance, ...),                  // Precedence 0
    new Profile(new ApiScope("payment"), ...),               // Precedence 10
    new Profile(new EnvironmentScope("prod"), ...),          // Precedence 15
    new Profile(new CompositeScope(
        new ApiScope("payment"),
        new EnvironmentScope("prod")
    ), ...)                                                   // Precedence 20
};

var requestedScopes = new IScope[]
{
    new ApiScope("payment"),
    new EnvironmentScope("prod")
};

// Resolves to CompositeScope(payment, prod) with precedence 20
var result = ProfileResolver.Resolve(profiles, requestedScopes);

// Resolution order:
// 1. GlobalScope (0)
// 2. ApiScope("payment") (10)
// 3. EnvironmentScope("prod") (15)
// 4. CompositeScope (20) - highest precedence wins
```

---

### 5. Return Value Contract

**ResolvedProfile Structure**:
```csharp
public sealed class ResolvedProfile
{
    /// <summary>
    /// The scope that was requested during resolution.
    /// </summary>
    public IScope RequestedScope { get; }
    
    /// <summary>
    /// Merged configuration dictionary. Keys are ConfigKey, values are ConfigValue.
    /// Immutable - cannot be modified after creation.
    /// </summary>
    public ImmutableDictionary<ConfigKey, ConfigValue> Configuration { get; }
    
    /// <summary>
    /// Audit trail showing which scopes contributed to each configuration key.
    /// Keys are ConfigKey, values are lists of IScope (in precedence order).
    /// </summary>
    public ImmutableDictionary<ConfigKey, ImmutableList<IScope>> AuditTrail { get; }
}
```

**Guarantees**:
- Configuration is **immutable** (ImmutableDictionary)
- AuditTrail lists scopes in **precedence order** (lowest to highest)
- If a key appears in Configuration, it **must** appear in AuditTrail
- AuditTrail may contain keys not in Configuration (if overridden multiple times)

---

## Usage Examples

### Example 1: Simple Resolution

```csharp
// Setup
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

// Resolve
var result = ProfileResolver.Resolve(profiles, new EnvironmentScope("prod"));

// Access configuration
var timeout = result.Configuration[new ConfigKey("timeout")];
Console.WriteLine(timeout.Value); // Output: "90s"

var retries = result.Configuration[new ConfigKey("retries")];
Console.WriteLine(retries.Value); // Output: 3

// Inspect audit trail
var timeoutTrail = result.AuditTrail[new ConfigKey("timeout")];
Console.WriteLine(string.Join(" → ", timeoutTrail)); 
// Output: "Global → Environment:prod"
```

### Example 2: Multi-Dimensional Resolution

```csharp
var profiles = new[]
{
    new Profile(GlobalScope.Instance, 
        ImmutableDictionary<ConfigKey, ConfigValue>.Empty
            .Add(new ConfigKey("timeout"), ConfigValue.Create("30s"))
    ),
    
    new Profile(new ApiScope("payment"),
        ImmutableDictionary<ConfigKey, ConfigValue>.Empty
            .Add(new ConfigKey("timeout"), ConfigValue.Create("60s"))
    ),
    
    new Profile(new CompositeScope(
        new ApiScope("payment"),
        new EnvironmentScope("prod")
    ),
        ImmutableDictionary<ConfigKey, ConfigValue>.Empty
            .Add(new ConfigKey("timeout"), ConfigValue.Create("120s"))
    )
};

var requestedScopes = new IScope[]
{
    new ApiScope("payment"),
    new EnvironmentScope("prod")
};

var result = ProfileResolver.Resolve(profiles, requestedScopes);

// Result: timeout = "120s" (composite scope wins)
Console.WriteLine(result.Configuration[new ConfigKey("timeout")].Value); // "120s"
```

### Example 3: Conflict Detection

```csharp
var conflictingProfiles = new[]
{
    new Profile(new ApiScope("payment"),
        ImmutableDictionary<ConfigKey, ConfigValue>.Empty
            .Add(new ConfigKey("timeout"), ConfigValue.Create("30s"))
    ),
    
    new Profile(new ApiScope("payment"), // Same scope
        ImmutableDictionary<ConfigKey, ConfigValue>.Empty
            .Add(new ConfigKey("timeout"), ConfigValue.Create("60s")) // Different value
    )
};

try
{
    ProfileResolver.Resolve(conflictingProfiles, new ApiScope("payment"));
}
catch (ConfigurationConflictException ex)
{
    Console.WriteLine(ex.Message);
    // Output: Configuration conflicts detected: 1 conflict(s)
    //   - Key 'timeout' has conflicting values in scope Api:payment: 30s vs 60s
}
```

---

## Performance Characteristics

**Time Complexity**:
- **Filtering**: O(P) where P = number of profiles
- **Sorting**: O(P log P)
- **Conflict Detection**: O(P × K) where K = average keys per profile
- **Merging**: O(P × K)
- **Overall**: O(P log P + P × K)

**Space Complexity**:
- **Configuration**: O(K) where K = total unique keys
- **Audit Trail**: O(K × S) where S = average scopes per key
- **Overall**: O(K × S)

**Target Performance**:
- 100 profiles, 100 keys, 10 dimensions: **< 5ms**
- 1000 profiles, 1000 keys: **< 50ms**
- No dynamic allocation in hot path

---

## Testing Requirements

### Unit Tests

1. **Null Validation**:
   - Null profiles throws ArgumentNullException
   - Null scope throws ArgumentNullException
   - Empty scopes throws ArgumentException (multi-dimensional)

2. **Empty Input Handling**:
   - Empty profiles returns empty configuration
   - Empty configuration in profiles is handled

3. **Single Profile Resolution**:
   - Global profile applies to any scope
   - API profile only applies to matching API scope
   - Non-matching scopes return empty configuration

4. **Multi-Profile Resolution**:
   - Lower precedence overridden by higher precedence
   - Keys from multiple profiles are merged
   - Audit trail tracks all contributing scopes

5. **Conflict Detection**:
   - Same scope + different values throws exception
   - Same scope + same values does not throw
   - Different scopes + different values does not throw

6. **Multi-Dimensional Resolution**:
   - CompositeScope created from multiple requested scopes
   - Partial matching works (single dimension match)
   - Precedence calculated correctly (max + 5)

### Integration Tests

1. **End-to-End Resolution**:
   - Complete resolution pipeline from profiles to ResolvedProfile
   - Audit trail correctly populated

2. **Determinism**:
   - 1000+ invocations produce byte-identical results
   - Order-independent (profile input order doesn't matter)
   - Thread-safe (parallel resolution doesn't corrupt state)

3. **Performance**:
   - 100 profiles, 100 keys resolves in < 5ms
   - 1000 profiles, 1000 keys resolves in < 50ms

---

## Design Rationale

### Why Static Class?

- **Stateless**: No instance state needed
- **Pure Function**: Emphasizes deterministic behavior
- **Simple API**: No construction or lifecycle management
- **Thread-Safe**: No shared state = no synchronization

### Why Fail-Fast on Conflicts?

- **Correctness**: Ambiguous configuration is a bug, not a feature
- **Debugging**: Clear error messages at resolution time
- **Explicit**: Forces developers to resolve conflicts intentionally

### Why Audit Trail?

- **Debugging**: Understand why a particular value was chosen
- **Compliance**: Track configuration sources for auditing
- **Transparency**: Users can see the resolution logic

---

## See Also

- [scope-interface.md](scope-interface.md) - IScope contract
- [resolved-profile.md](resolved-profile.md) - ResolvedProfile contract
- [conflict-handling.md](conflict-handling.md) - ConflictHandler contract
- [SCOPE_HIERARCHY.md](../../../docs/SCOPE_HIERARCHY.md) - Precedence rules
- [Profile Domain README](../../../src/PerformanceEngine.Profile.Domain/README.md) - Architecture overview
