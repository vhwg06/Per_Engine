# Custom Scope Extension Guide

## Overview

The Profile Domain provides **extensibility without modification** through the `IScope` interface. You can create custom scope types to support domain-specific configuration dimensions without touching any core code.

This guide demonstrates:
- How to implement custom scopes
- How to choose appropriate precedence values
- How to integrate custom scopes with the resolver
- Complete working examples

---

## The IScope Interface

All scope types implement the `IScope` interface:

```csharp
public interface IScope : IEquatable<IScope>, IComparable<IScope>
{
    /// <summary>
    /// The type identifier for this scope (e.g., "global", "api", "environment")
    /// </summary>
    string Type { get; }
    
    /// <summary>
    /// The specific value for this scope instance (e.g., "prod", "payment", "critical")
    /// May be null for singleton scopes like GlobalScope
    /// </summary>
    string? Value { get; }
    
    /// <summary>
    /// Precedence determines resolution order. Higher precedence overrides lower precedence.
    /// </summary>
    int Precedence { get; }
    
    /// <summary>
    /// Determines if this scope matches a requested scope during resolution.
    /// </summary>
    /// <param name="requestedScope">The scope being resolved for</param>
    /// <returns>True if this scope applies to the requested scope</returns>
    bool Matches(IScope requestedScope);
}
```

---

## Implementation Requirements

### 1. Value-Based Equality

Scopes with the same type and value should be **equal**, regardless of reference identity:

```csharp
public override bool Equals(object? obj)
{
    return obj is MyCustomScope other &&
           Type == other.Type &&
           Value == other.Value &&
           Precedence == other.Precedence;
}

public bool Equals(IScope? other)
{
    return other is MyCustomScope scope &&
           Type == scope.Type &&
           Value == scope.Value &&
           Precedence == scope.Precedence;
}

public override int GetHashCode()
{
    return HashCode.Combine(Type, Value, Precedence);
}
```

### 2. Precedence-Based Comparison

Implement `IComparable<IScope>` to enable sorting by precedence:

```csharp
public int CompareTo(IScope? other)
{
    if (other == null) return 1;
    return Precedence.CompareTo(other.Precedence);
}
```

### 3. Matching Logic

The `Matches()` method determines when a profile applies. Common patterns:

**Exact Match**:
```csharp
public bool Matches(IScope requestedScope)
{
    // This scope only applies if types and values match exactly
    return requestedScope.Type == Type && requestedScope.Value == Value;
}
```

**Type Match**:
```csharp
public bool Matches(IScope requestedScope)
{
    // This scope applies to any requested scope of the same type
    return requestedScope.Type == Type;
}
```

**Universal Match** (like GlobalScope):
```csharp
public bool Matches(IScope requestedScope)
{
    // This scope applies to ALL requests
    return true;
}
```

**Composite Match**:
```csharp
public bool Matches(IScope requestedScope)
{
    // This scope applies if it matches any dimension of a composite scope
    if (requestedScope is CompositeScope composite)
    {
        return composite.Scopes.Any(s => this.Matches(s));
    }
    return requestedScope.Type == Type && requestedScope.Value == Value;
}
```

---

## Example: PaymentMethodScope

Let's create a custom scope for payment method-specific configuration (e.g., Visa, Mastercard, PayPal).

### Step 1: Implement IScope

```csharp
using System;
using PerformanceEngine.Profile.Domain.Domain.Scopes;

namespace MyCompany.PaymentScopes
{
    /// <summary>
    /// Represents configuration specific to a payment method.
    /// </summary>
    public sealed class PaymentMethodScope : IScope, IEquatable<PaymentMethodScope>
    {
        private readonly string _paymentMethod;

        /// <summary>
        /// Creates a new PaymentMethodScope for the specified payment method.
        /// </summary>
        /// <param name="paymentMethod">The payment method identifier (e.g., "visa", "mastercard", "paypal")</param>
        /// <param name="precedence">The precedence value (default: 25 - higher than TagScope)</param>
        /// <exception cref="ArgumentException">If paymentMethod is null or whitespace</exception>
        public PaymentMethodScope(string paymentMethod, int precedence = 25)
        {
            if (string.IsNullOrWhiteSpace(paymentMethod))
                throw new ArgumentException("Payment method cannot be null or whitespace", nameof(paymentMethod));

            _paymentMethod = paymentMethod.ToLowerInvariant(); // Normalize
            Type = "payment-method";
            Value = _paymentMethod;
            Precedence = precedence;
        }

        public string Type { get; }
        public string Value { get; }
        public int Precedence { get; }

        /// <summary>
        /// Matches if the requested scope is also a PaymentMethodScope with the same payment method.
        /// </summary>
        public bool Matches(IScope requestedScope)
        {
            if (requestedScope is CompositeScope composite)
            {
                // Match if we're part of a composite scope
                return composite.Scopes.Any(s => Matches(s));
            }

            return requestedScope is PaymentMethodScope other &&
                   _paymentMethod == other._paymentMethod;
        }

        public bool Equals(IScope? other)
        {
            return other is PaymentMethodScope scope &&
                   Type == scope.Type &&
                   Value == scope.Value &&
                   Precedence == scope.Precedence;
        }

        public bool Equals(PaymentMethodScope? other)
        {
            return other is not null &&
                   _paymentMethod == other._paymentMethod &&
                   Precedence == other.Precedence;
        }

        public override bool Equals(object? obj)
        {
            return obj is PaymentMethodScope other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Type, Value, Precedence);
        }

        public int CompareTo(IScope? other)
        {
            if (other == null) return 1;
            return Precedence.CompareTo(other.Precedence);
        }

        public override string ToString()
        {
            return $"PaymentMethod:{_paymentMethod}";
        }

        public static bool operator ==(PaymentMethodScope? left, PaymentMethodScope? right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(PaymentMethodScope? left, PaymentMethodScope? right)
        {
            return !Equals(left, right);
        }
    }
}
```

### Step 2: Define Configuration Profiles

```csharp
using PerformanceEngine.Profile.Domain.Domain.Profiles;
using PerformanceEngine.Profile.Domain.Domain.Configs;
using System.Collections.Immutable;

// Global defaults
var globalProfile = new Profile(
    GlobalScope.Instance,
    ImmutableDictionary<ConfigKey, ConfigValue>.Empty
        .Add(new ConfigKey("transaction_timeout"), ConfigValue.Create("30s"))
        .Add(new ConfigKey("max_retries"), ConfigValue.Create(3))
        .Add(new ConfigKey("security_protocol"), ConfigValue.Create("TLS1.2"))
);

// Visa-specific configuration
var visaProfile = new Profile(
    new PaymentMethodScope("visa", precedence: 25),
    ImmutableDictionary<ConfigKey, ConfigValue>.Empty
        .Add(new ConfigKey("transaction_timeout"), ConfigValue.Create("45s"))
        .Add(new ConfigKey("requires_3ds"), ConfigValue.Create(true))
        .Add(new ConfigKey("fraud_check_level"), ConfigValue.Create("high"))
);

// Mastercard-specific configuration
var mastercardProfile = new Profile(
    new PaymentMethodScope("mastercard", precedence: 25),
    ImmutableDictionary<ConfigKey, ConfigValue>.Empty
        .Add(new ConfigKey("transaction_timeout"), ConfigValue.Create("40s"))
        .Add(new ConfigKey("requires_3ds"), ConfigValue.Create(true))
        .Add(new ConfigKey("fraud_check_level"), ConfigValue.Create("medium"))
);

// PayPal-specific configuration
var paypalProfile = new Profile(
    new PaymentMethodScope("paypal", precedence: 25),
    ImmutableDictionary<ConfigKey, ConfigValue>.Empty
        .Add(new ConfigKey("transaction_timeout"), ConfigValue.Create("60s"))
        .Add(new ConfigKey("requires_3ds"), ConfigValue.Create(false))
        .Add(new ConfigKey("oauth_enabled"), ConfigValue.Create(true))
);
```

### Step 3: Resolve Configuration

```csharp
using PerformanceEngine.Profile.Domain.Domain.Profiles;

var profiles = new[] { globalProfile, visaProfile, mastercardProfile, paypalProfile };

// Resolve for Visa
var visaScope = new PaymentMethodScope("visa");
var visaConfig = ProfileResolver.Resolve(profiles, visaScope);

Console.WriteLine(visaConfig.Configuration[new ConfigKey("transaction_timeout")]); 
// Output: 45s (from Visa profile)

Console.WriteLine(visaConfig.Configuration[new ConfigKey("max_retries")]); 
// Output: 3 (from global profile)

Console.WriteLine(visaConfig.Configuration[new ConfigKey("fraud_check_level")]); 
// Output: high (from Visa profile)

// Resolve for PayPal
var paypalScope = new PaymentMethodScope("paypal");
var paypalConfig = ProfileResolver.Resolve(profiles, paypalScope);

Console.WriteLine(paypalConfig.Configuration[new ConfigKey("transaction_timeout")]); 
// Output: 60s (from PayPal profile)

Console.WriteLine(paypalConfig.Configuration[new ConfigKey("requires_3ds")]); 
// Output: false (from PayPal profile)
```

---

## Multi-Dimensional Resolution

You can combine custom scopes with built-in scopes using `CompositeScope`:

```csharp
// Production + Visa combination
var prodVisaScope = new CompositeScope(
    new EnvironmentScope("prod"),
    new PaymentMethodScope("visa")
);

var prodVisaProfile = new Profile(
    prodVisaScope,
    ImmutableDictionary<ConfigKey, ConfigValue>.Empty
        .Add(new ConfigKey("transaction_timeout"), ConfigValue.Create("60s"))
        .Add(new ConfigKey("fraud_check_level"), ConfigValue.Create("maximum"))
);

var profiles = new[] { globalProfile, visaProfile, prodVisaProfile };

// Resolve for prod + Visa
var config = ProfileResolver.Resolve(profiles, prodVisaScope);

Console.WriteLine(config.Configuration[new ConfigKey("transaction_timeout")]); 
// Output: 60s (from prod+Visa composite - highest precedence)

Console.WriteLine(config.Configuration[new ConfigKey("fraud_check_level")]); 
// Output: maximum (from prod+Visa composite)
```

**Precedence Calculation**:
- `EnvironmentScope("prod")`: precedence 15
- `PaymentMethodScope("visa")`: precedence 25
- `CompositeScope(env, payment)`: precedence `max(15, 25) + 5 = 30`

---

## Choosing Precedence Values

When implementing custom scopes, choose precedence values that reflect the **specificity** of the configuration:

| Precedence Range | Use Case | Examples |
|------------------|----------|----------|
| **0** | Universal defaults | GlobalScope |
| **1-9** | Reserved for future built-in scopes | - |
| **10** | Service/API-level configuration | ApiScope |
| **15** | Environment-level configuration | EnvironmentScope |
| **20** | Tag-based configuration | TagScope |
| **25-50** | Custom domain-specific scopes | PaymentMethodScope, RegionScope, TenantScope |
| **51-99** | Highly specific custom scopes | UserScope, SessionScope |
| **100+** | Override/emergency configuration | MaintenanceScope, HotfixScope |

### Guidelines:

1. **Lower precedence = more general**: Global configuration should have the lowest precedence
2. **Higher precedence = more specific**: User-specific configuration should override environment configuration
3. **Leave gaps**: Use increments of 5 or 10 to allow inserting new scopes later
4. **Document rationale**: Explain why you chose a particular precedence value
5. **Avoid conflicts**: Don't use the same precedence as built-in scopes unless intentional

---

## Advanced: Runtime Scope Registration

If you need to discover custom scopes at runtime, use the `ScopeRegistry`:

```csharp
using PerformanceEngine.Profile.Domain.Domain.Scopes;

// Register custom scope factory
ScopeRegistry.RegisterScope("payment-method", () => new PaymentMethodScope("default"));

// Check if registered
bool isRegistered = ScopeRegistry.IsRegistered("payment-method");

// Get scope by type
IScope? scope = ScopeRegistry.GetScopeByType("payment-method");

// List all registered types
var allTypes = ScopeRegistry.GetRegisteredTypes();

// Unregister (for testing)
ScopeRegistry.UnregisterScope("payment-method");
```

**Use Cases**:
- Plugin systems where scopes are loaded from external assemblies
- Dynamic configuration where scope types are defined at runtime
- Testing scenarios where you need to inject mock scope implementations

---

## Testing Custom Scopes

### Unit Tests

```csharp
using Xunit;
using FluentAssertions;

public class PaymentMethodScopeTests
{
    [Fact]
    public void PaymentMethodScope_ShouldHaveCorrectPrecedence()
    {
        var scope = new PaymentMethodScope("visa");
        scope.Precedence.Should().Be(25);
    }

    [Fact]
    public void PaymentMethodScope_ShouldMatchIdenticalScope()
    {
        var scope1 = new PaymentMethodScope("visa");
        var scope2 = new PaymentMethodScope("visa");
        
        scope1.Matches(scope2).Should().BeTrue();
    }

    [Fact]
    public void PaymentMethodScope_ShouldNotMatchDifferentPaymentMethod()
    {
        var visaScope = new PaymentMethodScope("visa");
        var mastercardScope = new PaymentMethodScope("mastercard");
        
        visaScope.Matches(mastercardScope).Should().BeFalse();
    }

    [Fact]
    public void PaymentMethodScope_ShouldBeEqualByValue()
    {
        var scope1 = new PaymentMethodScope("visa");
        var scope2 = new PaymentMethodScope("visa");
        
        scope1.Should().Be(scope2);
        scope1.GetHashCode().Should().Be(scope2.GetHashCode());
    }

    [Fact]
    public void PaymentMethodScope_ShouldWorkWithCompositeScope()
    {
        var paymentScope = new PaymentMethodScope("visa");
        var envScope = new EnvironmentScope("prod");
        var compositeScope = new CompositeScope(paymentScope, envScope);
        
        paymentScope.Matches(compositeScope).Should().BeTrue();
    }
}
```

### Integration Tests

```csharp
using Xunit;
using FluentAssertions;
using PerformanceEngine.Profile.Domain.Domain.Profiles;
using PerformanceEngine.Profile.Domain.Domain.Configs;
using System.Collections.Immutable;

public class PaymentMethodIntegrationTests
{
    [Fact]
    public void ProfileResolver_ShouldResolvePaymentMethodConfiguration()
    {
        // Arrange
        var globalProfile = new Profile(
            GlobalScope.Instance,
            ImmutableDictionary<ConfigKey, ConfigValue>.Empty
                .Add(new ConfigKey("timeout"), ConfigValue.Create("30s"))
        );

        var visaProfile = new Profile(
            new PaymentMethodScope("visa"),
            ImmutableDictionary<ConfigKey, ConfigValue>.Empty
                .Add(new ConfigKey("timeout"), ConfigValue.Create("45s"))
        );

        var profiles = new[] { globalProfile, visaProfile };
        var requestedScope = new PaymentMethodScope("visa");

        // Act
        var result = ProfileResolver.Resolve(profiles, requestedScope);

        // Assert
        result.Configuration[new ConfigKey("timeout")].Value.Should().Be("45s");
        result.AuditTrail[new ConfigKey("timeout")].Should().HaveCount(2);
    }

    [Fact]
    public void ProfileResolver_ShouldResolveDeterministically_WithPaymentMethodScopes()
    {
        // Arrange
        var profiles = new[]
        {
            new Profile(GlobalScope.Instance, 
                ImmutableDictionary<ConfigKey, ConfigValue>.Empty
                    .Add(new ConfigKey("timeout"), ConfigValue.Create("30s"))
            ),
            new Profile(new PaymentMethodScope("visa"),
                ImmutableDictionary<ConfigKey, ConfigValue>.Empty
                    .Add(new ConfigKey("timeout"), ConfigValue.Create("45s"))
            )
        };

        var requestedScope = new PaymentMethodScope("visa");

        // Act
        var result1 = ProfileResolver.Resolve(profiles, requestedScope);
        var result2 = ProfileResolver.Resolve(profiles, requestedScope);

        // Assert
        result1.Configuration.Should().BeEquivalentTo(result2.Configuration);
    }
}
```

---

## Common Patterns

### Pattern 1: Hierarchical Scopes

```csharp
public class RegionScope : IScope
{
    private readonly string _region;

    public RegionScope(string region, int precedence = 12)
    {
        _region = region;
        Type = "region";
        Value = region;
        Precedence = precedence;
    }

    // Region precedence between Api and Environment
    // Allows: Global < Api < Region < Environment < Tag
}
```

### Pattern 2: Tenant-Based Scopes

```csharp
public class TenantScope : IScope
{
    private readonly Guid _tenantId;

    public TenantScope(Guid tenantId, int precedence = 30)
    {
        _tenantId = tenantId;
        Type = "tenant";
        Value = tenantId.ToString();
        Precedence = precedence;
    }

    // High precedence - tenant configuration overrides everything except composite
}
```

### Pattern 3: Feature Flag Scopes

```csharp
public class FeatureFlagScope : IScope
{
    private readonly string _featureName;
    private readonly bool _enabled;

    public FeatureFlagScope(string featureName, bool enabled, int precedence = 40)
    {
        _featureName = featureName;
        _enabled = enabled;
        Type = "feature-flag";
        Value = $"{featureName}:{enabled}";
        Precedence = precedence;
    }

    public bool Matches(IScope requestedScope)
    {
        return requestedScope is FeatureFlagScope other &&
               _featureName == other._featureName &&
               _enabled == other._enabled;
    }
}
```

---

## Best Practices

### 1. **Immutable Scopes**

Make all scope fields `readonly` to ensure immutability:

```csharp
private readonly string _value;
```

### 2. **Value Normalization**

Normalize values in the constructor to avoid case-sensitivity issues:

```csharp
_paymentMethod = paymentMethod.ToLowerInvariant();
```

### 3. **Descriptive ToString()**

Implement `ToString()` for debugging and logging:

```csharp
public override string ToString()
{
    return $"PaymentMethod:{_paymentMethod}";
}
```

### 4. **Validation in Constructor**

Fail fast with clear error messages:

```csharp
if (string.IsNullOrWhiteSpace(paymentMethod))
    throw new ArgumentException("Payment method cannot be null or whitespace", nameof(paymentMethod));
```

### 5. **Composite Scope Support**

Always handle `CompositeScope` in `Matches()`:

```csharp
if (requestedScope is CompositeScope composite)
{
    return composite.Scopes.Any(s => Matches(s));
}
```

---

## See Also

- [SCOPE_HIERARCHY.md](SCOPE_HIERARCHY.md) - Precedence rules and resolution algorithm
- [Profile Domain README](../src/PerformanceEngine.Profile.Domain/README.md) - Architecture overview
- [Implementation Guide](../src/PerformanceEngine.Profile.Domain/IMPLEMENTATION_GUIDE.md) - Step-by-step examples
