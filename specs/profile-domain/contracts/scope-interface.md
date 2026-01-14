# IScope Interface Contract

## Overview

The `IScope` interface defines the contract for all scope types in the Profile Domain. It represents a **context dimension** (e.g., API, Environment, Tag) that determines which configuration applies. All scopes must implement value-based equality, precedence comparison, and matching logic.

**Location**: `PerformanceEngine.Profile.Domain.Domain.Scopes.IScope`

---

## Interface Definition

```csharp
namespace PerformanceEngine.Profile.Domain.Domain.Scopes
{
    /// <summary>
    /// Represents a configuration scope (context dimension) that determines
    /// which configuration profiles apply during resolution.
    /// </summary>
    public interface IScope : IEquatable<IScope>, IComparable<IScope>
    {
        /// <summary>
        /// The type identifier for this scope dimension.
        /// Must be a non-empty, lowercase string.
        /// </summary>
        /// <example>
        /// "global", "api", "environment", "tag", "custom"
        /// </example>
        string Type { get; }
        
        /// <summary>
        /// The specific value for this scope instance.
        /// May be null for singleton scopes (e.g., GlobalScope).
        /// </summary>
        /// <example>
        /// "prod", "payment", "critical", "us-east"
        /// </example>
        string? Value { get; }
        
        /// <summary>
        /// Precedence determines resolution order. Higher values override lower values.
        /// Must be >= 0.
        /// </summary>
        /// <remarks>
        /// Standard precedence values:
        /// - GlobalScope: 0
        /// - ApiScope: 10
        /// - EnvironmentScope: 15
        /// - TagScope: 20 (default)
        /// - CompositeScope: max(scopeA, scopeB) + 5
        /// - Custom scopes: 25-100
        /// </remarks>
        int Precedence { get; }
        
        /// <summary>
        /// Determines if this scope applies to the requested scope during resolution.
        /// </summary>
        /// <param name="requestedScope">The scope being resolved for</param>
        /// <returns>
        /// True if this scope's configuration should be included in resolution.
        /// </returns>
        /// <remarks>
        /// Matching strategies:
        /// - GlobalScope: Always returns true (applies to all contexts)
        /// - ApiScope/EnvironmentScope: Type and Value must match exactly
        /// - CompositeScope: Any dimension must match
        /// - Custom scopes: Implementation-defined logic
        /// </remarks>
        bool Matches(IScope requestedScope);
    }
}
```

---

## Contract Requirements

### 1. Type Property

**Requirements**:
- MUST return a non-null, non-empty string
- SHOULD be lowercase for consistency
- MUST be immutable (same value across object lifetime)
- SHOULD be descriptive (e.g., "api", "environment", "tag")

**Valid Examples**:
```csharp
Type = "global";       // ✅
Type = "api";          // ✅
Type = "environment";  // ✅
Type = "payment-method"; // ✅
```

**Invalid Examples**:
```csharp
Type = null;         // ❌ Violates non-null requirement
Type = "";           // ❌ Violates non-empty requirement
Type = "API";        // ⚠️  Not lowercase (allowed but discouraged)
```

---

### 2. Value Property

**Requirements**:
- MAY be null for singleton scopes (e.g., GlobalScope)
- MUST be immutable
- SHOULD be normalized (e.g., lowercase, trimmed)
- SHOULD be human-readable for debugging

**Valid Examples**:
```csharp
Value = null;          // ✅ Valid for GlobalScope
Value = "prod";        // ✅ Environment value
Value = "payment";     // ✅ API identifier
Value = "critical";    // ✅ Tag value
```

**Invalid Examples**:
```csharp
Value = "";            // ⚠️  Empty string allowed but discouraged (prefer null)
```

---

### 3. Precedence Property

**Requirements**:
- MUST be >= 0
- MUST be immutable
- SHOULD follow standard precedence ranges (see interface definition)
- MUST be consistent: Same type/value = same precedence

**Standard Values**:
```csharp
GlobalScope: 0           // ✅ Lowest precedence (applies to all)
ApiScope: 10             // ✅ API-specific configuration
EnvironmentScope: 15     // ✅ Environment-specific configuration
TagScope: 20             // ✅ Tag-based configuration
CompositeScope: max+5    // ✅ Multi-dimensional configuration
Custom scopes: 25-100    // ✅ Domain-specific scopes
```

**Invalid Examples**:
```csharp
Precedence = -1;         // ❌ Violates >= 0 requirement
Precedence = varies;     // ❌ Violates immutability
```

---

### 4. Matches() Method

**Requirements**:
- MUST return a deterministic result (same inputs = same output)
- MUST handle `CompositeScope` (check if any dimension matches)
- MUST be side-effect free (no I/O, no state mutation)
- SHOULD return true for GlobalScope (applies universally)

**Common Implementations**:

**Exact Match** (ApiScope, EnvironmentScope, TagScope):
```csharp
public bool Matches(IScope requestedScope)
{
    if (requestedScope is CompositeScope composite)
    {
        return composite.Scopes.Any(s => Matches(s));
    }
    
    return requestedScope.Type == Type && requestedScope.Value == Value;
}
```

**Universal Match** (GlobalScope):
```csharp
public bool Matches(IScope requestedScope)
{
    return true; // Applies to all contexts
}
```

**Type Match** (for hierarchical scopes):
```csharp
public bool Matches(IScope requestedScope)
{
    if (requestedScope is CompositeScope composite)
    {
        return composite.Scopes.Any(s => Matches(s));
    }
    
    return requestedScope.Type == Type; // Match any value of this type
}
```

---

### 5. Equality (IEquatable<IScope>)

**Requirements**:
- MUST implement value-based equality (not reference equality)
- MUST be consistent with `GetHashCode()`
- MUST compare `Type`, `Value`, and `Precedence`
- MUST be symmetric: `a.Equals(b)` ⇔ `b.Equals(a)`

**Canonical Implementation**:
```csharp
public bool Equals(IScope? other)
{
    return other is MyScope scope &&
           Type == scope.Type &&
           Value == scope.Value &&
           Precedence == scope.Precedence;
}

public override bool Equals(object? obj)
{
    return obj is IScope scope && Equals(scope);
}

public override int GetHashCode()
{
    return HashCode.Combine(Type, Value, Precedence);
}
```

**Test Cases**:
```csharp
var scope1 = new ApiScope("payment");
var scope2 = new ApiScope("payment");
var scope3 = new ApiScope("checkout");

scope1.Equals(scope2); // ✅ True (same type, value, precedence)
scope1.Equals(scope3); // ✅ False (different value)
scope1.GetHashCode() == scope2.GetHashCode(); // ✅ True
```

---

### 6. Comparison (IComparable<IScope>)

**Requirements**:
- MUST compare by `Precedence` property
- MUST return: 
  - Negative if this.Precedence < other.Precedence
  - Zero if this.Precedence == other.Precedence
  - Positive if this.Precedence > other.Precedence
- MUST handle null (return 1 if other is null)

**Canonical Implementation**:
```csharp
public int CompareTo(IScope? other)
{
    if (other == null) return 1;
    return Precedence.CompareTo(other.Precedence);
}
```

**Test Cases**:
```csharp
var global = GlobalScope.Instance;      // Precedence 0
var api = new ApiScope("payment");      // Precedence 10
var env = new EnvironmentScope("prod"); // Precedence 15

global.CompareTo(api);  // ✅ Returns < 0 (0 < 10)
env.CompareTo(api);     // ✅ Returns > 0 (15 > 10)
api.CompareTo(api);     // ✅ Returns 0 (10 == 10)
```

---

## Usage Examples

### Example 1: Simple Scope

```csharp
public sealed class ApiScope : IScope
{
    private readonly string _apiName;

    public ApiScope(string apiName)
    {
        if (string.IsNullOrWhiteSpace(apiName))
            throw new ArgumentException("API name cannot be null or whitespace", nameof(apiName));

        _apiName = apiName.ToLowerInvariant();
        Type = "api";
        Value = _apiName;
        Precedence = 10;
    }

    public string Type { get; }
    public string Value { get; }
    public int Precedence { get; }

    public bool Matches(IScope requestedScope)
    {
        if (requestedScope is CompositeScope composite)
        {
            return composite.Scopes.Any(s => Matches(s));
        }

        return requestedScope is ApiScope other && _apiName == other._apiName;
    }

    public bool Equals(IScope? other)
    {
        return other is ApiScope scope &&
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

    public override string ToString() => $"Api:{_apiName}";
}
```

### Example 2: Singleton Scope (GlobalScope)

```csharp
public sealed class GlobalScope : IScope
{
    public static readonly GlobalScope Instance = new GlobalScope();

    private GlobalScope() { }

    public string Type => "global";
    public string? Value => null;
    public int Precedence => 0;

    public bool Matches(IScope requestedScope) => true; // Applies universally

    public bool Equals(IScope? other) => other is GlobalScope;

    public override bool Equals(object? obj) => obj is GlobalScope;

    public override int GetHashCode() => Type.GetHashCode();

    public int CompareTo(IScope? other)
    {
        if (other == null) return 1;
        return Precedence.CompareTo(other.Precedence);
    }

    public override string ToString() => "Global";
}
```

---

## Design Rationale

### Why IScope?

1. **Extensibility**: Open for custom scopes without modifying core code
2. **Type Safety**: Compile-time enforcement of scope contract
3. **Determinism**: Value-based equality ensures reproducible resolution
4. **Sorting**: IComparable enables precedence-based ordering
5. **Matching**: Flexible logic for partial/exact/composite matching

### Why Immutable?

- **Thread Safety**: No synchronization needed
- **Determinism**: Scopes never change after creation
- **Caching**: Safe to use as dictionary keys
- **Testing**: Easier to reason about and test

### Why Value-Based Equality?

- **Scope Identity**: Two scopes with same type/value/precedence are logically identical
- **Dictionary Keys**: Enable deduplication and lookup
- **Testing**: Simplify assertion writing

---

## Validation Checklist

When implementing IScope:

- [ ] Type is non-null, non-empty, immutable
- [ ] Value is immutable (may be null for singletons)
- [ ] Precedence is >= 0 and immutable
- [ ] Matches() handles CompositeScope
- [ ] Matches() is deterministic and side-effect free
- [ ] Equals() implements value-based equality
- [ ] GetHashCode() is consistent with Equals()
- [ ] CompareTo() compares by Precedence
- [ ] CompareTo() handles null
- [ ] ToString() provides human-readable representation
- [ ] Constructor validates inputs
- [ ] All properties are readonly
- [ ] Thread-safe (no mutable state)

---

## See Also

- [SCOPE_HIERARCHY.md](../../../docs/SCOPE_HIERARCHY.md) - Precedence rules and resolution
- [CUSTOM_SCOPES.md](../../../docs/CUSTOM_SCOPES.md) - How to implement custom scopes
- [resolver-interface.md](resolver-interface.md) - ProfileResolver contract
- [Profile Domain README](../../../src/PerformanceEngine.Profile.Domain/README.md) - Architecture overview
