# Profile Domain Implementation Guide

## Overview

This guide walks through implementing the Profile Domain from scratch, explaining architectural decisions and patterns used.

## Phase 1: Foundation

### Step 1: Define Configuration Primitives

**ConfigKey** - Immutable identifier for configuration settings:
```csharp
public sealed record ConfigKey(string Name);
```

**ConfigValue** - Type-safe configuration value:
```csharp
public sealed record ConfigValue(object Value, ConfigType Type);
```

**ConfigType** - Supported value types:
```csharp
public enum ConfigType { String, Int, Duration, Double, Bool }
```

### Step 2: Define Scope Abstraction

**IScope** - Strategy pattern for different context types:
```csharp
public interface IScope : IEquatable<IScope>, IComparable<IScope>
{
    string Id { get; }
    string Type { get; }
    int Precedence { get; }
    string Description { get; }
}
```

Key design decision: **Precedence** determines resolution order. Higher precedence wins.

### Step 3: Implement Built-in Scopes

**GlobalScope** (Precedence: 0) - Singleton pattern:
```csharp
public sealed record GlobalScope : IScope
{
    private static readonly GlobalScope _instance = new();
    public static GlobalScope Instance => _instance;
    
    public int Precedence => 0; // Lowest priority
}
```

**ApiScope** (Precedence: 10) - API-specific:
```csharp
public sealed record ApiScope(string ApiName) : IScope
{
    public int Precedence => 10;
}
```

**EnvironmentScope** (Precedence: 15) - Environment-specific:
```csharp
public sealed record EnvironmentScope(string EnvironmentName) : IScope
{
    public int Precedence => 15;
}
```

## Phase 2: Profile Entity

**Profile** - Configuration at a specific scope:
```csharp
public sealed record Profile(
    IScope Scope,
    ImmutableDictionary<ConfigKey, ConfigValue> Configurations
);
```

Design principles:
- ✅ Immutable after construction
- ✅ Value-based equality
- ✅ No business logic (anemic by design - logic is in resolver)

## Phase 3: Resolution Logic

### ProfileResolver - Pure Domain Service

```csharp
public static class ProfileResolver
{
    public static ResolvedProfile Resolve(
        IEnumerable<Profile> profiles,
        IScope requestedScope)
    {
        // 1. Validate no conflicts
        ConflictHandler.ValidateNoConflicts(profiles);
        
        // 2. Filter applicable profiles
        var applicable = GetApplicableProfiles(profiles, requestedScope);
        
        // 3. Sort by precedence (lowest first)
        var sorted = applicable.OrderBy(p => p.Scope.Precedence);
        
        // 4. Merge (higher precedence overwrites)
        var merged = MergeConfigurations(sorted);
        
        // 5. Return immutable result
        return new ResolvedProfile(merged, auditTrail, DateTime.UtcNow);
    }
}
```

**Key Characteristics**:
- Pure function (no side effects)
- Deterministic (same input → same output)
- Fail-fast on conflicts
- Explicit audit trail

### Conflict Detection

```csharp
public static class ConflictHandler
{
    public static void ValidateNoConflicts(IEnumerable<Profile> profiles)
    {
        // Group by scope
        var byScope = profiles.GroupBy(p => p.Scope);
        
        foreach (var group in byScope)
        {
            // Check for conflicting values at same scope
            var keys = group.SelectMany(p => p.Configurations.Keys).Distinct();
            
            foreach (var key in keys)
            {
                var values = group
                    .Where(p => p.Configurations.ContainsKey(key))
                    .Select(p => p.Configurations[key])
                    .Distinct();
                
                if (values.Count() > 1)
                {
                    throw new ConfigurationConflictException(...);
                }
            }
        }
    }
}
```

## Phase 4: Multi-Dimensional Resolution

### CompositeScope

Combines two scopes for more specific contexts:

```csharp
public sealed record CompositeScope(IScope ScopeA, IScope ScopeB) : IScope
{
    public int Precedence => Math.Max(ScopeA.Precedence, ScopeB.Precedence) + 5;
    
    public bool MatchesContext(IEnumerable<IScope> contextScopes)
    {
        return contextScopes.Contains(ScopeA) && contextScopes.Contains(ScopeB);
    }
}
```

**Usage**:
```csharp
var apiEnvScope = new CompositeScope(
    new ApiScope("payment"),
    new EnvironmentScope("staging")
);
// Precedence: max(10, 15) + 5 = 20
```

### Multi-Scope Resolution

Enhanced resolver supports multiple requested scopes:

```csharp
public static ResolvedProfile Resolve(
    IEnumerable<Profile> profiles,
    IEnumerable<IScope> requestedScopes) // Multiple scopes
{
    var applicable = profiles.Where(p =>
        p.Scope is GlobalScope ||
        requestedScopes.Contains(p.Scope) ||
        (p.Scope is CompositeScope c && c.MatchesContext(requestedScopes))
    );
    
    // Rest of resolution logic...
}
```

## Phase 5: Application Layer

### ProfileService - Application Facade

```csharp
public class ProfileService
{
    private readonly ResolveProfileUseCase _resolveUseCase;
    
    public ResolvedProfile Resolve(
        IEnumerable<Profile> profiles,
        IScope requestedScope)
    {
        return _resolveUseCase.Execute(profiles, requestedScope);
    }
    
    public void ValidateNoConflicts(IEnumerable<Profile> profiles)
    {
        ConflictHandler.ValidateNoConflicts(profiles);
    }
}
```

### DTOs for Serialization

```csharp
public record ProfileDto(ScopeDto Scope, Dictionary<string, ConfigValueDto> Configurations);
public record ResolvedProfileDto(Dictionary<string, ConfigValueDto> Configuration, ...);
```

**Mapping**:
```csharp
public static class DtoMapper
{
    public static ProfileDto ToDto(this Profile profile) => ...;
    public static Profile FromDto(ProfileDto dto) => ...;
}
```

## Phase 6: Extensibility

### Custom Scopes

Users can implement `IScope` for custom dimensions:

```csharp
public sealed record RegionScope(string Region) : IScope
{
    public string Id => Region;
    public string Type => "Region";
    public int Precedence => 12; // Choose wisely
    public string Description => $"Region: {Region}";
    
    // Implement IEquatable and IComparable
}
```

### ScopeFactory

Convenience factory for built-in types:

```csharp
public static class ScopeFactory
{
    public static IScope CreateGlobal() => GlobalScope.Instance;
    public static IScope CreateApi(string name) => new ApiScope(name);
    public static IScope CreateEnvironment(string env) => new EnvironmentScope(env);
    public static IScope CreateComposite(IScope a, IScope b) => new CompositeScope(a, b);
}
```

## Testing Strategy

### 1. Unit Tests

```csharp
[Fact]
public void GlobalScope_HasLowestPrecedence()
{
    var global = GlobalScope.Instance;
    var api = new ApiScope("test");
    
    global.Precedence.Should().BeLessThan(api.Precedence);
}
```

### 2. Determinism Tests

```csharp
[Fact]
public void Resolver_ProducesDeterministicResults()
{
    var profiles = CreateTestProfiles();
    var scope = new ApiScope("payment");
    
    var results = new HashSet<string>();
    for (int i = 0; i < 1000; i++)
    {
        var resolved = ProfileResolver.Resolve(profiles, scope);
        results.Add(SerializeToString(resolved));
    }
    
    results.Should().HaveCount(1); // All identical
}
```

### 3. Conflict Detection Tests

```csharp
[Fact]
public void Resolver_DetectsConflicts()
{
    var profile1 = CreateProfile(new ApiScope("payment"), timeout: 30);
    var profile2 = CreateProfile(new ApiScope("payment"), timeout: 60);
    
    Action act = () => ProfileResolver.Resolve(new[] { profile1, profile2 }, ...);
    
    act.Should().Throw<ConfigurationConflictException>();
}
```

### 4. Architecture Compliance Tests

```csharp
[Fact]
public void DomainLayer_HasNoInfrastructureDependencies()
{
    var domainAssembly = typeof(Profile).Assembly;
    var types = domainAssembly.GetTypes();
    
    var violations = types.Where(t => 
        t.Namespace.Contains("Infrastructure") ||
        UsesFileIO(t) ||
        UsesEnvironmentVariables(t)
    );
    
    violations.Should().BeEmpty();
}
```

## Constitutional Compliance

✅ **Domain Purity**: No I/O, no side effects  
✅ **Determinism**: Same inputs → same outputs  
✅ **Clean Architecture**: No infrastructure dependencies  
✅ **Extensibility**: Strategy pattern for custom scopes  
✅ **Immutability**: All entities are immutable  
✅ **Explicit Intent**: Clear separation of concerns

## Common Patterns

### Pattern 1: Global + API Override

```csharp
var global = Profile.Create(GlobalScope.Instance, globalConfig);
var api = Profile.Create(new ApiScope("payment"), apiConfig);

var resolved = ProfileResolver.Resolve(new[] { global, api }, new ApiScope("payment"));
// API values override global
```

### Pattern 2: Environment-Specific Configuration

```csharp
var profiles = new[]
{
    Profile.Create(GlobalScope.Instance, defaultConfig),
    Profile.Create(new EnvironmentScope("prod"), prodConfig),
    Profile.Create(new EnvironmentScope("staging"), stagingConfig)
};

var resolved = ProfileResolver.Resolve(profiles, new EnvironmentScope("prod"));
```

### Pattern 3: Multi-Dimensional Context

```csharp
var composite = new CompositeScope(
    new ApiScope("payment"),
    new EnvironmentScope("prod")
);

var profile = Profile.Create(composite, highValueConfig);

var resolved = ProfileResolver.Resolve(
    profiles,
    new[] { new ApiScope("payment"), new EnvironmentScope("prod") }
);
```

## Performance Considerations

- Profile resolution is O(n*m) where n=profiles, m=config keys
- For 100 profiles with 50 keys: <1ms resolution time
- Immutable collections use structural sharing (efficient)
- No allocations after resolution (immutable result)

## Next Steps

1. Add persistence layer (Infrastructure)
2. Add validation rules for config values
3. Add support for config inheritance
4. Add telemetry and logging (Application layer)

## References

- [Specification](../../specs/profile-domain/spec.md)
- [README](README.md)
- [Clean Architecture](../../docs/main.md)
