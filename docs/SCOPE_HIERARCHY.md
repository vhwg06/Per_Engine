# Scope Hierarchy and Precedence Rules

## Overview

The Profile Domain uses a **precedence-based scope hierarchy** to determine which configuration values apply in different contexts. When multiple profiles define the same configuration key, the profile with the **highest precedence scope** wins.

This document explains:
- Built-in scope types and their precedence
- How precedence is calculated
- Multi-dimensional scope resolution
- Conflict detection rules

---

## Built-In Scope Types

### 1. GlobalScope (Precedence: 0)

**Purpose**: Default configuration that applies to all contexts.

**Usage**: Define baseline configuration values that should apply unless explicitly overridden.

**Example**:
```csharp
var globalScope = GlobalScope.Instance;
var profile = new Profile(
    globalScope,
    ImmutableDictionary<ConfigKey, ConfigValue>.Empty
        .Add(new ConfigKey("timeout"), ConfigValue.Create("30s"))
        .Add(new ConfigKey("retries"), ConfigValue.Create(3))
);
```

**Precedence**: `0` (lowest)

---

### 2. ApiScope (Precedence: 10)

**Purpose**: Configuration specific to a particular API or endpoint.

**Usage**: Override global configuration for specific APIs that have different requirements.

**Example**:
```csharp
var paymentApiScope = new ApiScope("payment");
var profile = new Profile(
    paymentApiScope,
    ImmutableDictionary<ConfigKey, ConfigValue>.Empty
        .Add(new ConfigKey("timeout"), ConfigValue.Create("60s")) // Override global
);
```

**Precedence**: `10`

---

### 3. EnvironmentScope (Precedence: 15)

**Purpose**: Configuration specific to an environment (prod, staging, dev).

**Usage**: Apply different configuration based on deployment environment.

**Example**:
```csharp
var prodScope = new EnvironmentScope("prod");
var profile = new Profile(
    prodScope,
    ImmutableDictionary<ConfigKey, ConfigValue>.Empty
        .Add(new ConfigKey("timeout"), ConfigValue.Create("90s"))
        .Add(new ConfigKey("retries"), ConfigValue.Create(5))
);
```

**Precedence**: `15`

---

### 4. TagScope (Precedence: 20, configurable)

**Purpose**: Configuration based on custom tags or labels.

**Usage**: Apply configuration to specific subsets of tests or scenarios.

**Example**:
```csharp
var criticalTag = new TagScope("critical", precedence: 20);
var profile = new Profile(
    criticalTag,
    ImmutableDictionary<ConfigKey, ConfigValue>.Empty
        .Add(new ConfigKey("retries"), ConfigValue.Create(10))
);
```

**Precedence**: `20` (default, can be customized)

---

### 5. CompositeScope (Precedence: max(A, B) + 5)

**Purpose**: Combine multiple scopes for multi-dimensional configuration.

**Usage**: Apply configuration that is specific to a combination of contexts (e.g., payment API in production).

**Example**:
```csharp
var apiScope = new ApiScope("payment");
var envScope = new EnvironmentScope("prod");
var compositeScope = new CompositeScope(apiScope, envScope);

var profile = new Profile(
    compositeScope,
    ImmutableDictionary<ConfigKey, ConfigValue>.Empty
        .Add(new ConfigKey("timeout"), ConfigValue.Create("120s"))
);
```

**Precedence**: `max(scopeA.Precedence, scopeB.Precedence) + 5`
- In this example: `max(10, 15) + 5 = 20`

**Note**: Nesting CompositeScope instances is **prevented by design** to avoid complex precedence calculations.

---

## Precedence Resolution Algorithm

When resolving configuration for a requested scope:

1. **Filter Applicable Profiles**: Select all profiles whose scopes match the requested scope or are more general (Global always applies)

2. **Sort by Precedence**: Order profiles from lowest to highest precedence

3. **Merge Configurations**: For each configuration key:
   - Start with the value from the lowest precedence profile
   - Override with values from higher precedence profiles
   - **Last value wins** (highest precedence)

4. **Build Audit Trail**: Track which scope contributed each configuration key

5. **Conflict Detection**: If two profiles at the **same scope** define different values for the same key, throw `ConfigurationConflictException`

---

## Resolution Examples

### Example 1: Simple Override

**Profiles**:
```csharp
var profiles = new[]
{
    // Global (precedence: 0)
    new Profile(GlobalScope.Instance, 
        ImmutableDictionary<ConfigKey, ConfigValue>.Empty
            .Add(new ConfigKey("timeout"), ConfigValue.Create("30s"))
            .Add(new ConfigKey("retries"), ConfigValue.Create(3))
    ),
    
    // Payment API (precedence: 10)
    new Profile(new ApiScope("payment"),
        ImmutableDictionary<ConfigKey, ConfigValue>.Empty
            .Add(new ConfigKey("timeout"), ConfigValue.Create("60s"))
    )
};
```

**Request**: Resolve for `ApiScope("payment")`

**Result**:
```csharp
{
    "timeout": "60s",   // From ApiScope (precedence 10) - overrides Global
    "retries": 3         // From GlobalScope (precedence 0) - not overridden
}
```

**Audit Trail**:
```csharp
{
    "timeout": [GlobalScope, ApiScope("payment")],
    "retries": [GlobalScope]
}
```

---

### Example 2: Multi-Dimensional Resolution

**Profiles**:
```csharp
var profiles = new[]
{
    // Global (precedence: 0)
    new Profile(GlobalScope.Instance, 
        ImmutableDictionary<ConfigKey, ConfigValue>.Empty
            .Add(new ConfigKey("timeout"), ConfigValue.Create("30s"))
            .Add(new ConfigKey("retries"), ConfigValue.Create(3))
    ),
    
    // Environment: prod (precedence: 15)
    new Profile(new EnvironmentScope("prod"),
        ImmutableDictionary<ConfigKey, ConfigValue>.Empty
            .Add(new ConfigKey("timeout"), ConfigValue.Create("90s"))
    ),
    
    // API: payment + Environment: prod (precedence: max(10,15)+5 = 20)
    new Profile(new CompositeScope(new ApiScope("payment"), new EnvironmentScope("prod")),
        ImmutableDictionary<ConfigKey, ConfigValue>.Empty
            .Add(new ConfigKey("timeout"), ConfigValue.Create("120s"))
    )
};
```

**Request**: Resolve for `CompositeScope(ApiScope("payment"), EnvironmentScope("prod"))`

**Result**:
```csharp
{
    "timeout": "120s",   // From CompositeScope (precedence 20) - most specific
    "retries": 3          // From GlobalScope (precedence 0)
}
```

**Resolution Order**:
1. GlobalScope (0): timeout=30s, retries=3
2. EnvironmentScope("prod") (15): timeout=90s (overrides global)
3. CompositeScope (20): timeout=120s (overrides environment)

---

### Example 3: Partial Dimension Match

**Profiles**:
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
    )
};
```

**Request**: Resolve for `CompositeScope(ApiScope("payment"), EnvironmentScope("prod"))`

**Result**:
```csharp
{
    "timeout": "60s"  // From ApiScope (precedence 10)
}
```

**Explanation**: Even though we requested a CompositeScope, the ApiScope profile still applies because it matches one dimension of the request. The EnvironmentScope("prod") dimension doesn't have a matching profile, so it doesn't contribute any overrides.

---

## Conflict Detection

### What is a Conflict?

A conflict occurs when **two or more profiles at the exact same scope** define **different values** for the **same configuration key**.

### Example: Conflict

```csharp
var scope = new ApiScope("payment");

var profiles = new[]
{
    new Profile(scope,
        ImmutableDictionary<ConfigKey, ConfigValue>.Empty
            .Add(new ConfigKey("timeout"), ConfigValue.Create("30s"))
    ),
    
    new Profile(scope,  // SAME SCOPE
        ImmutableDictionary<ConfigKey, ConfigValue>.Empty
            .Add(new ConfigKey("timeout"), ConfigValue.Create("60s"))  // DIFFERENT VALUE
    )
};

// ❌ Throws ConfigurationConflictException
var result = ProfileResolver.Resolve(profiles, scope);
```

**Error Message**:
```
Configuration conflicts detected: 1 conflict(s)
  - Key 'timeout' has conflicting values in scope Api:payment: 30s vs 60s
```

### Example: NOT a Conflict

```csharp
var profiles = new[]
{
    // Global scope (precedence: 0)
    new Profile(GlobalScope.Instance,
        ImmutableDictionary<ConfigKey, ConfigValue>.Empty
            .Add(new ConfigKey("timeout"), ConfigValue.Create("30s"))
    ),
    
    // API scope (precedence: 10) - DIFFERENT SCOPE
    new Profile(new ApiScope("payment"),
        ImmutableDictionary<ConfigKey, ConfigValue>.Empty
            .Add(new ConfigKey("timeout"), ConfigValue.Create("60s"))
    )
};

// ✅ No conflict - different scopes, precedence resolves ambiguity
var result = ProfileResolver.Resolve(profiles, new ApiScope("payment"));
// Result: timeout = "60s" (API scope wins)
```

---

## Precedence Summary Table

| Scope Type | Precedence | Description | Example |
|------------|------------|-------------|---------|
| **GlobalScope** | 0 | Applies to all contexts | Default configuration |
| **ApiScope** | 10 | API-specific configuration | `ApiScope("payment")` |
| **EnvironmentScope** | 15 | Environment-specific configuration | `EnvironmentScope("prod")` |
| **TagScope** | 20 (default) | Tag-based configuration | `TagScope("critical")` |
| **CompositeScope** | max(A, B) + 5 | Multi-dimensional configuration | `CompositeScope(api, env)` |
| **Custom Scopes** | User-defined | Extensible scope types | Any value via `IScope` |

---

## Best Practices

### 1. Use Global for Defaults

Always define a global profile with sensible defaults. This ensures every configuration key has a value, even if no specific overrides exist.

### 2. Prefer Specific Scopes Over Composite

Only use CompositeScope when configuration truly depends on multiple dimensions simultaneously. If configuration can be expressed with single-dimension scopes, keep it simple.

### 3. Avoid Same-Scope Conflicts

Design your configuration structure so that each scope has at most one profile. If you need multiple "versions" of configuration for the same scope, use different scopes or tags.

### 4. Document Precedence Choices

When defining custom scopes, document why you chose a particular precedence value and how it interacts with built-in scopes.

### 5. Test Multi-Dimensional Resolution

Write tests that verify your configuration behaves correctly when multiple dimensions interact. Use the determinism test harness to ensure reproducibility.

---

## See Also

- [CUSTOM_SCOPES.md](CUSTOM_SCOPES.md) - How to implement custom scope types
- [Profile Domain README](../src/PerformanceEngine.Profile.Domain/README.md) - Architecture overview
- [Implementation Guide](../src/PerformanceEngine.Profile.Domain/IMPLEMENTATION_GUIDE.md) - Step-by-step examples
