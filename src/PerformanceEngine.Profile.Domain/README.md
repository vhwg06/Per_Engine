# Profile Domain

**Version**: 1.0.0  
**Status**: Production Ready  
**Architecture**: Clean Architecture + Domain-Driven Design

## Overview

The Profile Domain provides **deterministic configuration resolution** based on hierarchical scopes. It enables context-aware configuration management with explicit override rules, conflict detection, and extensibility.

## Key Features

✅ **Deterministic Resolution**: Same inputs always produce same outputs  
✅ **Hierarchical Scopes**: Global < API < Environment < Tag < Composite  
✅ **Conflict Detection**: Fail-fast on ambiguous configurations  
✅ **Extensible**: Custom scopes via strategy pattern  
✅ **Pure Domain Logic**: Zero infrastructure dependencies  
✅ **Immutable Results**: Thread-safe, reproducible outputs

## Quick Start

### Basic Usage

```csharp
using PerformanceEngine.Profile.Domain.Domain.Configuration;
using PerformanceEngine.Profile.Domain.Domain.Scopes;
using PerformanceEngine.Profile.Domain.Domain.Profiles;

// Create configuration keys and values
var timeoutKey = new ConfigKey("timeout");
var retriesKey = new ConfigKey("retries");

// Define global profile
var globalProfile = Profile.Create(
    GlobalScope.Instance,
    new Dictionary<ConfigKey, ConfigValue>
    {
        [timeoutKey] = ConfigValue.Create(TimeSpan.FromSeconds(30)),
        [retriesKey] = ConfigValue.Create(3)
    }
);

// Define API-specific profile
var apiProfile = Profile.Create(
    new ApiScope("payment"),
    new Dictionary<ConfigKey, ConfigValue>
    {
        [timeoutKey] = ConfigValue.Create(TimeSpan.FromSeconds(60))
    }
);

// Resolve for payment API
var profiles = new[] { globalProfile, apiProfile };
var resolved = ProfileResolver.Resolve(profiles, new ApiScope("payment"));

// Access values
var timeout = resolved.Get(timeoutKey); // 60 seconds (API override)
var retries = resolved.Get(retriesKey); // 3 (from global)
```

### Multi-Dimensional Resolution

```csharp
// Define profiles for different dimensions
var globalProfile = Profile.Create(GlobalScope.Instance, new Dictionary<ConfigKey, ConfigValue>
{
    [timeoutKey] = ConfigValue.Create(TimeSpan.FromSeconds(30))
});

var envProfile = Profile.Create(new EnvironmentScope("staging"), new Dictionary<ConfigKey, ConfigValue>
{
    [timeoutKey] = ConfigValue.Create(TimeSpan.FromSeconds(45))
});

var apiProfile = Profile.Create(new ApiScope("payment"), new Dictionary<ConfigKey, ConfigValue>
{
    [timeoutKey] = ConfigValue.Create(TimeSpan.FromSeconds(60))
});

// Most specific scope wins
var compositeProfile = Profile.Create(
    new CompositeScope(new ApiScope("payment"), new EnvironmentScope("staging")),
    new Dictionary<ConfigKey, ConfigValue>
    {
        [timeoutKey] = ConfigValue.Create(TimeSpan.FromSeconds(90))
    }
);

// Resolve with multiple scopes
var resolved = ProfileResolver.Resolve(
    new[] { globalProfile, envProfile, apiProfile, compositeProfile },
    new[] { new ApiScope("payment"), new EnvironmentScope("staging") }
);

var timeout = resolved.Get(timeoutKey); // 90 seconds (composite wins)
```

## Built-in Scopes

| Scope | Precedence | Description |
|-------|-----------|-------------|
| **GlobalScope** | 0 | Applies to all contexts (lowest precedence) |
| **ApiScope** | 10 | API-specific configuration |
| **EnvironmentScope** | 15 | Environment-specific (prod, staging, dev) |
| **TagScope** | 20 | Tag-based configuration |
| **CompositeScope** | max+5 | Multi-dimensional combination |

## Custom Scopes

Implement `IScope` interface:

```csharp
public sealed record CustomRegionScope : IScope
{
    public string Region { get; }
    
    public CustomRegionScope(string region)
    {
        Region = region;
    }
    
    public string Id => Region;
    public string Type => "Region";
    public int Precedence => 12; // Between API and Environment
    public string Description => $"Region-specific config for {Region}";
    
    public bool Equals(IScope? other) =>
        other is CustomRegionScope r && r.Region == Region;
    
    public int CompareTo(IScope? other) =>
        other is null ? 1 : Precedence.CompareTo(other.Precedence);
    
    public override int GetHashCode() => HashCode.Combine(Type, Region);
}
```

## Conflict Detection

```csharp
// This will throw ConfigurationConflictException
var profile1 = Profile.Create(
    new ApiScope("payment"),
    new Dictionary<ConfigKey, ConfigValue>
    {
        [timeoutKey] = ConfigValue.Create(TimeSpan.FromSeconds(30))
    }
);

var profile2 = Profile.Create(
    new ApiScope("payment"),
    new Dictionary<ConfigKey, ConfigValue>
    {
        [timeoutKey] = ConfigValue.Create(TimeSpan.FromSeconds(60)) // Conflict!
    }
);

// Throws: Two profiles at same scope with different values
ProfileResolver.Resolve(new[] { profile1, profile2 }, new ApiScope("payment"));
```

## Architecture

```
┌──────────────────────────────┐
│    APPLICATION               │
│  ProfileService → UseCases   │
└──────────────┬───────────────┘
               ↓
┌──────────────────────────────┐
│    DOMAIN                    │
│  Scope → Profile →           │
│  ProfileResolver →           │
│  ResolvedProfile             │
└──────────────────────────────┘
```

**Zero Infrastructure Dependencies**
- No file I/O
- No database access
- No environment variables
- Pure, deterministic logic

## Configuration Types

Supported types: `String`, `Int`, `Duration`, `Double`, `Bool`

```csharp
ConfigValue.Create("text");                    // String
ConfigValue.Create(42);                        // Int
ConfigValue.Create(TimeSpan.FromMinutes(5));   // Duration
ConfigValue.Create(3.14);                      // Double
ConfigValue.Create(true);                      // Bool
```

## Audit Trail

Every resolution includes an audit trail:

```csharp
var resolved = ProfileResolver.Resolve(profiles, scope);

// See which scopes provided each value
var auditTrail = resolved.AuditTrail;
foreach (var (key, scopes) in auditTrail)
{
    Console.WriteLine($"{key.Name}: resolved from {string.Join(" → ", scopes.Select(s => s.Type))}");
}
```

## Testing

```bash
dotnet test tests/PerformanceEngine.Profile.Domain.Tests
```

## Dependencies

- .NET 8.0
- System.Collections.Immutable

## License

Internal use only

## See Also

- [Implementation Guide](IMPLEMENTATION_GUIDE.md)
- [Specification](../../specs/profile-domain/spec.md)
- [Tasks](../../specs/profile-domain/tasks.md)
