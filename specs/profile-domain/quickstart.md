# Profile Domain Quickstart

## Overview

The **Profile Domain** provides deterministic, context-aware configuration resolution. It allows you to define configuration profiles for different contexts (Global, API, Environment, Tags, Custom) and resolve them based on requested scopes with clear precedence rules.

This quickstart will get you:
- Resolving configuration in 5 minutes
- Understanding scope hierarchy
- Creating custom scopes
- Testing deterministic behavior

---

## Prerequisites

- **.NET 8.0 or higher**
- **PerformanceEngine.Profile.Domain** package/project reference
- Basic understanding of C# and dependency injection

---

## Installation

### Option 1: Add Project Reference

```bash
dotnet add reference ../src/PerformanceEngine.Profile.Domain/PerformanceEngine.Profile.Domain.csproj
```

### Option 2: Add Package Reference (when published)

```bash
dotnet add package PerformanceEngine.Profile.Domain --version 1.0.0
```

---

## Quick Start: Basic Resolution

### Step 1: Define Configuration Profiles

```csharp
using PerformanceEngine.Profile.Domain.Domain.Profiles;
using PerformanceEngine.Profile.Domain.Domain.Scopes;
using PerformanceEngine.Profile.Domain.Domain.Configs;
using System.Collections.Immutable;

// Global defaults (applies to all contexts)
var globalProfile = new Profile(
    scope: GlobalScope.Instance,
    configuration: ImmutableDictionary<ConfigKey, ConfigValue>.Empty
        .Add(new ConfigKey("timeout"), ConfigValue.Create("30s"))
        .Add(new ConfigKey("max_retries"), ConfigValue.Create(3))
        .Add(new ConfigKey("log_level"), ConfigValue.Create("info"))
);

// Production environment overrides
var prodProfile = new Profile(
    scope: new EnvironmentScope("prod"),
    configuration: ImmutableDictionary<ConfigKey, ConfigValue>.Empty
        .Add(new ConfigKey("timeout"), ConfigValue.Create("90s"))
        .Add(new ConfigKey("log_level"), ConfigValue.Create("warn"))
);

// Payment API-specific configuration
var paymentApiProfile = new Profile(
    scope: new ApiScope("payment"),
    configuration: ImmutableDictionary<ConfigKey, ConfigValue>.Empty
        .Add(new ConfigKey("timeout"), ConfigValue.Create("60s"))
        .Add(new ConfigKey("fraud_check"), ConfigValue.Create(true))
);
```

### Step 2: Resolve Configuration

```csharp
using PerformanceEngine.Profile.Domain.Domain.Profiles;

var profiles = new[] { globalProfile, prodProfile, paymentApiProfile };

// Resolve for production environment
var prodScope = new EnvironmentScope("prod");
var prodConfig = ProfileResolver.Resolve(profiles, prodScope);

Console.WriteLine(prodConfig.Configuration[new ConfigKey("timeout")]);    
// Output: 90s (from prod profile)

Console.WriteLine(prodConfig.Configuration[new ConfigKey("max_retries")]); 
// Output: 3 (from global profile)

Console.WriteLine(prodConfig.Configuration[new ConfigKey("log_level")]);   
// Output: warn (from prod profile)

// Resolve for payment API
var paymentScope = new ApiScope("payment");
var paymentConfig = ProfileResolver.Resolve(profiles, paymentScope);

Console.WriteLine(paymentConfig.Configuration[new ConfigKey("timeout")]);  
// Output: 60s (from payment API profile)

Console.WriteLine(paymentConfig.Configuration[new ConfigKey("fraud_check")]); 
// Output: true (from payment API profile)
```

### Step 3: Inspect Audit Trail

```csharp
// See which scopes contributed to each configuration key
foreach (var (key, scopes) in prodConfig.AuditTrail)
{
    Console.WriteLine($"{key.Name}: {string.Join(" → ", scopes)}");
}

// Output:
// timeout: GlobalScope → Environment:prod
// max_retries: GlobalScope
// log_level: GlobalScope → Environment:prod
```

---

## Multi-Dimensional Resolution

Combine multiple scope dimensions using `CompositeScope`:

```csharp
using PerformanceEngine.Profile.Domain.Domain.Scopes;

// Define a composite scope for "payment API in production"
var compositeScope = new CompositeScope(
    new ApiScope("payment"),
    new EnvironmentScope("prod")
);

// Create a profile for this specific combination
var prodPaymentProfile = new Profile(
    compositeScope,
    ImmutableDictionary<ConfigKey, ConfigValue>.Empty
        .Add(new ConfigKey("timeout"), ConfigValue.Create("120s"))
        .Add(new ConfigKey("rate_limit"), ConfigValue.Create(1000))
);

var profiles = new[] { globalProfile, prodProfile, paymentApiProfile, prodPaymentProfile };

// Resolve for prod + payment
var config = ProfileResolver.Resolve(profiles, compositeScope);

Console.WriteLine(config.Configuration[new ConfigKey("timeout")]);
// Output: 120s (most specific - composite scope wins)

Console.WriteLine(config.Configuration[new ConfigKey("rate_limit")]);
// Output: 1000 (from composite profile)

Console.WriteLine(config.Configuration[new ConfigKey("max_retries")]);
// Output: 3 (from global - not overridden)
```

**Precedence order** (highest wins):
1. `CompositeScope(payment, prod)` - precedence 20
2. `EnvironmentScope(prod)` - precedence 15
3. `ApiScope(payment)` - precedence 10
4. `GlobalScope` - precedence 0

---

## Application Layer Integration

Use `ProfileService` for a simplified API:

```csharp
using PerformanceEngine.Profile.Domain.Application.Services;

// Initialize service
var profileService = new ProfileService();

// Resolve configuration
var profiles = new[] { globalProfile, prodProfile };
var requestedScope = new EnvironmentScope("prod");

var resolvedProfile = profileService.Resolve(profiles, requestedScope);

// Access configuration
var timeout = resolvedProfile.Configuration[new ConfigKey("timeout")];
Console.WriteLine($"Timeout: {timeout.Value}");
```

---

## Creating Custom Scopes

Implement the `IScope` interface to create custom scope types:

```csharp
using System;
using PerformanceEngine.Profile.Domain.Domain.Scopes;

public sealed class RegionScope : IScope
{
    private readonly string _region;

    public RegionScope(string region)
    {
        if (string.IsNullOrWhiteSpace(region))
            throw new ArgumentException("Region cannot be null or whitespace", nameof(region));

        _region = region.ToLowerInvariant();
        Type = "region";
        Value = _region;
        Precedence = 12; // Between Api (10) and Environment (15)
    }

    public string Type { get; }
    public string Value { get; }
    public int Precedence { get; }

    public bool Matches(IScope requestedScope)
    {
        // Handle composite scopes
        if (requestedScope is CompositeScope composite)
        {
            return composite.Scopes.Any(s => Matches(s));
        }

        // Exact match by type and value
        return requestedScope is RegionScope other && _region == other._region;
    }

    public bool Equals(IScope? other)
    {
        return other is RegionScope scope &&
               Type == scope.Type &&
               Value == scope.Value &&
               Precedence == scope.Precedence;
    }

    public override bool Equals(object? obj) => Equals(obj as IScope);

    public override int GetHashCode() => HashCode.Combine(Type, Value, Precedence);

    public int CompareTo(IScope? other)
    {
        if (other == null) return 1;
        return Precedence.CompareTo(other.Precedence);
    }

    public override string ToString() => $"Region:{_region}";
}
```

### Using Custom Scopes

```csharp
// Define region-specific configuration
var usEastProfile = new Profile(
    new RegionScope("us-east"),
    ImmutableDictionary<ConfigKey, ConfigValue>.Empty
        .Add(new ConfigKey("cdn_url"), ConfigValue.Create("https://cdn-useast.example.com"))
        .Add(new ConfigKey("latency_target"), ConfigValue.Create("50ms"))
);

var euWestProfile = new Profile(
    new RegionScope("eu-west"),
    ImmutableDictionary<ConfigKey, ConfigValue>.Empty
        .Add(new ConfigKey("cdn_url"), ConfigValue.Create("https://cdn-euwest.example.com"))
        .Add(new ConfigKey("latency_target"), ConfigValue.Create("70ms"))
);

var profiles = new[] { globalProfile, usEastProfile, euWestProfile };

// Resolve for US East region
var usEastConfig = ProfileResolver.Resolve(profiles, new RegionScope("us-east"));
Console.WriteLine(usEastConfig.Configuration[new ConfigKey("cdn_url")]);
// Output: https://cdn-useast.example.com
```

**Learn more**: See [CUSTOM_SCOPES.md](../../docs/CUSTOM_SCOPES.md) for detailed implementation guide.

---

## Error Handling: Conflict Detection

The resolver detects conflicts when **multiple profiles at the same scope** define **different values** for the same key:

```csharp
// ❌ This will throw ConfigurationConflictException
var conflictingProfile1 = new Profile(
    new ApiScope("payment"),
    ImmutableDictionary<ConfigKey, ConfigValue>.Empty
        .Add(new ConfigKey("timeout"), ConfigValue.Create("30s"))
);

var conflictingProfile2 = new Profile(
    new ApiScope("payment"), // SAME SCOPE
    ImmutableDictionary<ConfigKey, ConfigValue>.Empty
        .Add(new ConfigKey("timeout"), ConfigValue.Create("60s")) // DIFFERENT VALUE
);

var profiles = new[] { conflictingProfile1, conflictingProfile2 };

try
{
    var result = ProfileResolver.Resolve(profiles, new ApiScope("payment"));
}
catch (ConfigurationConflictException ex)
{
    Console.WriteLine(ex.Message);
    // Output: Configuration conflicts detected: 1 conflict(s)
    //   - Key 'timeout' has conflicting values in scope Api:payment: 30s vs 60s
}
```

**Resolution**: Remove duplicate profiles or use different scopes.

---

## Testing Your Configuration

### Basic Determinism Test

```csharp
using Xunit;
using FluentAssertions;
using PerformanceEngine.Profile.Domain.Domain.Profiles;

public class ConfigurationTests
{
    [Fact]
    public void Configuration_ShouldBeDeterministic()
    {
        // Arrange
        var profiles = new[]
        {
            new Profile(GlobalScope.Instance, 
                ImmutableDictionary<ConfigKey, ConfigValue>.Empty
                    .Add(new ConfigKey("timeout"), ConfigValue.Create("30s"))
            ),
            new Profile(new EnvironmentScope("prod"),
                ImmutableDictionary<ConfigKey, ConfigValue>.Empty
                    .Add(new ConfigKey("timeout"), ConfigValue.Create("90s"))
            )
        };

        var scope = new EnvironmentScope("prod");

        // Act
        var result1 = ProfileResolver.Resolve(profiles, scope);
        var result2 = ProfileResolver.Resolve(profiles, scope);

        // Assert
        result1.Configuration.Should().BeEquivalentTo(result2.Configuration);
        result1.AuditTrail.Should().BeEquivalentTo(result2.AuditTrail);
    }

    [Fact]
    public void Configuration_ShouldBeOrderIndependent()
    {
        // Arrange
        var profile1 = new Profile(GlobalScope.Instance, 
            ImmutableDictionary<ConfigKey, ConfigValue>.Empty
                .Add(new ConfigKey("timeout"), ConfigValue.Create("30s"))
        );

        var profile2 = new Profile(new EnvironmentScope("prod"),
            ImmutableDictionary<ConfigKey, ConfigValue>.Empty
                .Add(new ConfigKey("timeout"), ConfigValue.Create("90s"))
        );

        var scope = new EnvironmentScope("prod");

        // Act
        var resultAB = ProfileResolver.Resolve(new[] { profile1, profile2 }, scope);
        var resultBA = ProfileResolver.Resolve(new[] { profile2, profile1 }, scope);

        // Assert
        resultAB.Configuration.Should().BeEquivalentTo(resultBA.Configuration);
    }
}
```

### Advanced: 1000+ Iteration Determinism

```csharp
using PerformanceEngine.Profile.Domain.Tests.Domain;

public class AdvancedDeterminismTests : DeterminismTestBase
{
    [Fact]
    public void Configuration_ShouldBeDeterministic_Over1000Runs()
    {
        // Arrange
        var profiles = new[]
        {
            new Profile(GlobalScope.Instance, 
                ImmutableDictionary<ConfigKey, ConfigValue>.Empty
                    .Add(new ConfigKey("timeout"), ConfigValue.Create("30s"))
            )
        };

        var scope = GlobalScope.Instance;

        // Act & Assert - Runs 1000 times, verifies byte-identical JSON
        AssertDeterministic(() => ProfileResolver.Resolve(profiles, scope));
    }
}
```

---

## Configuration Best Practices

### 1. Always Define a Global Profile

```csharp
// ✅ Good: Provides defaults
var globalProfile = new Profile(
    GlobalScope.Instance,
    ImmutableDictionary<ConfigKey, ConfigValue>.Empty
        .Add(new ConfigKey("timeout"), ConfigValue.Create("30s"))
        .Add(new ConfigKey("retries"), ConfigValue.Create(3))
);
```

### 2. Use Specific Scopes for Overrides

```csharp
// ✅ Good: Clear precedence hierarchy
var profiles = new[]
{
    globalProfile,                          // Precedence 0
    new Profile(new ApiScope("payment"), …),       // Precedence 10
    new Profile(new EnvironmentScope("prod"), …)   // Precedence 15
};
```

### 3. Avoid Same-Scope Conflicts

```csharp
// ❌ Bad: Same scope, different values = conflict
var profile1 = new Profile(new ApiScope("payment"), …);
var profile2 = new Profile(new ApiScope("payment"), …); // Conflict!

// ✅ Good: Use different scopes or tags
var profile1 = new Profile(new ApiScope("payment"), …);
var profile2 = new Profile(new CompositeScope(
    new ApiScope("payment"),
    new TagScope("critical")
), …);
```

### 4. Document Custom Precedence Choices

```csharp
// ✅ Good: Clear reasoning
public class RegionScope : IScope
{
    // Precedence: 12
    // Rationale: More specific than API (10), less specific than Environment (15)
    // Region-based routing should override API defaults but not environment settings
    public int Precedence => 12;
}
```

---

## Common Use Cases

### Use Case 1: Environment-Specific Configuration

```csharp
var devConfig = new Profile(new EnvironmentScope("dev"), …);
var stagingConfig = new Profile(new EnvironmentScope("staging"), …);
var prodConfig = new Profile(new EnvironmentScope("prod"), …);

var profiles = new[] { globalProfile, devConfig, stagingConfig, prodConfig };

// Resolve based on runtime environment
var currentEnv = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "dev";
var config = ProfileResolver.Resolve(profiles, new EnvironmentScope(currentEnv));
```

### Use Case 2: Feature Flags

```csharp
var betaFeatureProfile = new Profile(
    new TagScope("beta", precedence: 25),
    ImmutableDictionary<ConfigKey, ConfigValue>.Empty
        .Add(new ConfigKey("new_checkout_enabled"), ConfigValue.Create(true))
);

var profiles = new[] { globalProfile, betaFeatureProfile };

// Enable beta features for tagged users
var config = ProfileResolver.Resolve(profiles, new TagScope("beta"));
```

### Use Case 3: Multi-Tenant Configuration

```csharp
public class TenantScope : IScope
{
    public TenantScope(Guid tenantId)
    {
        Type = "tenant";
        Value = tenantId.ToString();
        Precedence = 30; // High precedence - tenant overrides everything
    }
    // ... implement IScope
}

var tenant1Profile = new Profile(new TenantScope(tenant1Id), …);
var tenant2Profile = new Profile(new TenantScope(tenant2Id), …);

// Resolve per-tenant configuration
var config = ProfileResolver.Resolve(profiles, new TenantScope(currentTenantId));
```

---

## Next Steps

1. **Read the Architecture Guide**: [Profile Domain README](../../src/PerformanceEngine.Profile.Domain/README.md)
2. **Learn About Precedence**: [SCOPE_HIERARCHY.md](../../docs/SCOPE_HIERARCHY.md)
3. **Create Custom Scopes**: [CUSTOM_SCOPES.md](../../docs/CUSTOM_SCOPES.md)
4. **Review Implementation Guide**: [IMPLEMENTATION_GUIDE.md](../../src/PerformanceEngine.Profile.Domain/IMPLEMENTATION_GUIDE.md)
5. **Explore Test Examples**: `tests/PerformanceEngine.Profile.Domain.Tests/`

---

## Troubleshooting

### Issue: "Configuration conflicts detected"

**Cause**: Multiple profiles at the same scope define different values for the same key.

**Solution**: Ensure each scope has at most one profile, or use different scopes.

### Issue: "Configuration key not found"

**Cause**: No profile (including Global) defines the requested key.

**Solution**: Add the key to your global profile as a default value.

### Issue: Wrong configuration value returned

**Cause**: Unexpected precedence ordering.

**Solution**: Check the audit trail to see which scope contributed the value:

```csharp
foreach (var (key, scopes) in resolvedProfile.AuditTrail)
{
    Console.WriteLine($"{key.Name}: {string.Join(" → ", scopes)}");
}
```

---

## Support

- **Issues**: File issues in the project repository
- **Documentation**: See `/docs` folder for detailed guides
- **Examples**: See `tests/` folder for comprehensive test examples
