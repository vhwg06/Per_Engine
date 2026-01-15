# API Contracts: Core Domain Enrichment - Profile Domain

**Completed**: 2026-01-15  
**Scope**: Profile Domain enrichment interfaces and data contracts  
**Format**: C# interface specifications with state machine diagrams

---

## Enum: ProfileState

**Namespace**: `PerformanceEngine.Profile.Domain.Profiles`

### Definition

```csharp
public enum ProfileState
{
    /// <summary>
    /// Profile is new or being built; accepts override applications.
    /// Transition: Unresolved → Resolved (via Resolve) or Unresolved → Invalid (via Validate).
    /// </summary>
    Unresolved = 1,
    
    /// <summary>
    /// Profile has been resolved to final configuration; immutable.
    /// No more overrides accepted; ready for evaluation.
    /// </summary>
    Resolved = 2,
    
    /// <summary>
    /// Profile failed validation; cannot be used for evaluation.
    /// Must be created anew with fixes; no state transitions out of Invalid.
    /// </summary>
    Invalid = 3
}
```

### State Transitions

```
┌──────────────┐
│  Unresolved  │ ← New profile created here
└──────┬───────┘
       │
       ├─→ ApplyOverride(scope, key, value)
       │   └─→ remains Unresolved (state unchanged)
       │
       ├─→ Resolve(inputs)
       │   ├─→ Success → Resolved state (immutable from here)
       │   └─→ Error → remains Unresolved (re-try possible)
       │
       └─→ Validate()
           ├─→ Success → Resolved state (validation passed)
           └─→ Failure → Invalid state (no recovery)

┌──────────┐
│ Resolved │ ← Profile is final; ApplyOverride throws
└──────────┘

┌─────────┐
│ Invalid │ ← Validation failed; create new profile to recover
└─────────┘
```

---

## Entity: Profile (Extended)

**Namespace**: `PerformanceEngine.Profile.Domain.Profiles`

### Definition

```csharp
public sealed class Profile
{
    /// <summary>
    /// Unique profile identifier.
    /// </summary>
    public Guid Id { get; }
    
    /// <summary>
    /// Profile name (human-readable).
    /// </summary>
    public string Name { get; }
    
    /// <summary>
    /// NEW: Current profile state (Unresolved, Resolved, or Invalid).
    /// Controls which operations are allowed.
    /// </summary>
    public ProfileState State { get; private set; }
    
    /// <summary>
    /// Unresolved overrides (scope:key → value).
    /// Mutable while state is Unresolved; frozen when Resolved.
    /// </summary>
    internal IReadOnlyDictionary<string, ConfigurationValue> Overrides { get; }
    
    /// <summary>
    /// NEW: Post-resolution configuration (null until Resolved).
    /// Immutable snapshot of resolved configuration.
    /// </summary>
    public IReadOnlyDictionary<string, ConfigurationValue>? ResolvedConfiguration { get; private set; }
    
    /// <summary>
    /// NEW: Validation errors (null until Validate() called).
    /// Non-empty if validation failed.
    /// </summary>
    public IReadOnlyList<ValidationError>? ValidationErrors { get; private set; }
    
    public Profile(Guid id, string name)
    {
        Id = id;
        Name = name;
        State = ProfileState.Unresolved;
        Overrides = new Dictionary<string, ConfigurationValue>();
        ResolvedConfiguration = null;
        ValidationErrors = null;
    }
}
```

### Methods

#### ApplyOverride

```csharp
/// <summary>
/// Apply override to profile.
/// Throws if profile already Resolved or Invalid.
/// Scope examples: "global", "api:customer-api", "endpoint:GET:/v1/user"
/// </summary>
public void ApplyOverride(string scope, string key, ConfigurationValue value)
{
    if (State != ProfileState.Unresolved)
        throw new InvalidOperationException(
            $"Cannot apply overrides when profile is {State}. " +
            $"Create new profile or reset to Unresolved state.");
    
    ValidateScope(scope);
    ValidateKey(key);
    ValidateValue(value);
    
    var compositeKey = $"{scope}:{key}";
    ((Dictionary<string, ConfigurationValue>)Overrides)[compositeKey] = value;
}
```

#### Resolve

```csharp
/// <summary>
/// Resolve profile deterministically to final configuration.
/// Transitions state to Resolved on success.
/// Determinism: identical inputs (any order) → identical resolved config.
/// Time: O(n log n) where n = number of overrides.
/// </summary>
public void Resolve(IReadOnlyDictionary<string, ConfigurationValue>? inputs = null)
{
    if (State != ProfileState.Unresolved)
        throw new InvalidOperationException(
            $"Cannot resolve profile in {State} state. " +
            $"Only Unresolved profiles can be resolved.");
    
    var resolver = new ProfileResolver();
    ResolvedConfiguration = resolver.Resolve(Overrides, inputs ?? new Dictionary<string, ConfigurationValue>());
    State = ProfileState.Resolved;
}
```

#### Get

```csharp
/// <summary>
/// Retrieve resolved configuration value.
/// Throws if profile not Resolved.
/// </summary>
public ConfigurationValue Get(string key)
{
    if (State != ProfileState.Resolved)
        throw new InvalidOperationException(
            $"Profile must be Resolved (current state: {State}) before reading values.");
    
    if (!ResolvedConfiguration!.TryGetValue(key, out var value))
        throw new KeyNotFoundException(
            $"Configuration key '{key}' not found in resolved profile.");
    
    return value;
}
```

#### Validate

```csharp
/// <summary>
/// Validate profile against rules.
/// Transitions state to Invalid if validation fails.
/// Returns ValidationResult with all errors collected.
/// </summary>
public ValidationResult Validate(IProfileValidator validator)
{
    var result = validator.Validate(this);
    
    if (!result.IsValid)
    {
        State = ProfileState.Invalid;
        ValidationErrors = result.Errors;
    }
    
    return result;
}
```

---

## Service: ProfileResolver

**Namespace**: `PerformanceEngine.Profile.Domain.Profiles`

### Definition

```csharp
public sealed class ProfileResolver
{
    /// <summary>
    /// Resolve profile deterministically.
    /// 
    /// Algorithm:
    /// 1. Start with input defaults (if provided)
    /// 2. Collect all overrides from all scopes
    /// 3. Sort by (scope priority, key) for deterministic order
    /// 4. Apply in priority order: global < api < endpoint
    /// 5. Return resolved configuration (ordered dict)
    /// 
    /// Determinism Guarantee:
    /// - Order-independent: {A, B, C} = {C, A, B} = {B, C, A}
    /// - Deterministic: same inputs → byte-identical output
    /// - Time: O(n log n) where n = overrides
    /// </summary>
    public IReadOnlyDictionary<string, ConfigurationValue> Resolve(
        IReadOnlyDictionary<string, ConfigurationValue> overrides,
        IReadOnlyDictionary<string, ConfigurationValue> inputs)
    {
        var resolved = new Dictionary<string, ConfigurationValue>(inputs ?? new Dictionary<string, ConfigurationValue>());
        
        // Sort overrides deterministically
        var sortedOverrides = overrides
            .OrderBy(kv => GetScopePriority(ExtractScope(kv.Key)))
            .ThenBy(kv => ExtractKey(kv.Key), StringComparer.Ordinal)
            .ToList();
        
        // Apply overrides in deterministic order
        foreach (var (compositeKey, value) in sortedOverrides)
        {
            var key = ExtractKey(compositeKey);
            resolved[key] = value;  // Override with priority
        }
        
        // Return immutable, sorted result for determinism
        return new ReadOnlyDictionary<string, ConfigurationValue>(
            resolved.OrderBy(kv => kv.Key, StringComparer.Ordinal)
                    .ToDictionary(kv => kv.Key, kv => kv.Value));
    }
    
    private int GetScopePriority(string scope)
    {
        return scope switch
        {
            "global" => 1,
            "api" => 2,
            "endpoint" => 3,
            _ => 0  // Unknown scope (apply first, can be overridden)
        };
    }
    
    private string ExtractScope(string compositeKey) =>
        compositeKey.Contains(':') ? compositeKey.Substring(0, compositeKey.IndexOf(':')) : compositeKey;
    
    private string ExtractKey(string compositeKey) =>
        compositeKey.Contains(':') ? compositeKey.Substring(compositeKey.IndexOf(':') + 1) : compositeKey;
}
```

### Determinism Example

```csharp
var overrides = new Dictionary<string, ConfigurationValue>
{
    { "endpoint:timeout", new ConfigurationValue(120) },
    { "global:timeout", new ConfigurationValue(30) },
    { "api:timeout", new ConfigurationValue(60) }
};

var resolver = new ProfileResolver();

// Order 1: endpoint, global, api
var result1 = resolver.Resolve(
    overrides.Where(kv => new[] { "endpoint:timeout", "global:timeout", "api:timeout" }.Contains(kv.Key)).ToDictionary(kv => kv.Key, kv => kv.Value),
    null);

// Order 2: global, api, endpoint (different input order)
var overrides2 = new Dictionary<string, ConfigurationValue>
{
    { "global:timeout", new ConfigurationValue(30) },
    { "api:timeout", new ConfigurationValue(60) },
    { "endpoint:timeout", new ConfigurationValue(120) }
};

var result2 = resolver.Resolve(overrides2, null);

// result1 == result2 (deterministic, order-independent)
// Both resolve to: { "timeout": 120 } (endpoint scope wins)
```

---

## Port: IProfileValidator

**Namespace**: `PerformanceEngine.Profile.Domain.Validation`

### Definition

```csharp
public interface IProfileValidator
{
    /// <summary>
    /// Validate profile against rules.
    /// Returns ValidationResult with all errors collected at once.
    /// Pure function: same profile → same result every time (deterministic).
    /// </summary>
    ValidationResult Validate(Profile profile);
    
    /// <summary>
    /// Get validation rules applicable to profile type.
    /// </summary>
    IReadOnlyList<ValidationRule> GetApplicableRules(string profileType);
}
```

### Contract

**Implementations** must:
1. Validate all rules and collect all errors before returning
2. Check for circular dependencies (acyclic graph validation)
3. Verify required configuration keys present
4. Validate value types match schema
5. Return deterministic ValidationResult (same profile → same errors every time)
6. Be pure functions (no side effects, no state mutations)

### Validation Rules Checked

| Rule | Description | Error Code |
|------|-------------|-----------|
| **No Circular Dependencies** | Override dependencies form acyclic directed graph | `CIRCULAR_DEPENDENCY` |
| **Required Keys Present** | All mandatory config keys have values | `MISSING_REQUIRED_KEY` |
| **Type Correctness** | Values match schema (int, string, bool, etc.) | `TYPE_MISMATCH` |
| **Scope Validity** | All referenced scopes are recognized | `INVALID_SCOPE` |
| **Range Constraints** | Numeric values within acceptable bounds | `CONSTRAINT_VIOLATION` |
| **No Null Defaults** | Required fields cannot have null values | `NULL_REQUIRED_FIELD` |

---

## Value Object: ValidationResult

**Namespace**: `PerformanceEngine.Profile.Domain.Validation`

### Definition

```csharp
public sealed class ValidationResult : ValueObject
{
    /// <summary>
    /// True if validation passed (no errors); false if any errors exist.
    /// </summary>
    public bool IsValid { get; }
    
    /// <summary>
    /// All validation errors collected (empty if IsValid == true).
    /// </summary>
    public IReadOnlyList<ValidationError> Errors { get; }
    
    public static ValidationResult Success() =>
        new ValidationResult(isValid: true, errors: ImmutableList<ValidationError>.Empty);
    
    public static ValidationResult Failure(IReadOnlyList<ValidationError> errors) =>
        new ValidationResult(isValid: errors?.Count > 0 ? false : true, errors: errors ?? ImmutableList<ValidationError>.Empty);
    
    private ValidationResult(bool isValid, IReadOnlyList<ValidationError> errors)
    {
        IsValid = isValid;
        Errors = errors ?? ImmutableList<ValidationError>.Empty;
    }
    
    /// <summary>
    /// Filter errors by category (e.g., "CIRCULAR_DEPENDENCY").
    /// </summary>
    public IReadOnlyList<ValidationError> ErrorsByCategory(string category) =>
        Errors.Where(e => e.Category == category).ToList().AsReadOnly();
    
    /// <summary>
    /// Filter errors by scope (e.g., "api:customer-api").
    /// </summary>
    public IReadOnlyList<ValidationError> ErrorsByScope(string scope) =>
        Errors.Where(e => e.Scope == scope).ToList().AsReadOnly();
    
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return IsValid;
        yield return string.Join(",", Errors.OrderBy(e => e.Code).Select(e => e.Code));
    }
}
```

### JSON Serialization Contract

```json
{
  "isValid": false,
  "errors": [
    {
      "code": "CIRCULAR_DEPENDENCY",
      "message": "Profile has circular override: timeout → max_retries → timeout",
      "category": "CIRCULAR_DEPENDENCY",
      "scope": "api:payment-api",
      "path": ["timeout", "max_retries", "timeout"]
    },
    {
      "code": "MISSING_REQUIRED_KEY",
      "message": "Configuration key 'api_key' is required",
      "category": "MISSING_REQUIRED_KEY",
      "scope": "endpoint:POST:/v1/payments",
      "path": null
    }
  ]
}
```

---

## Value Object: ValidationError

**Namespace**: `PerformanceEngine.Profile.Domain.Validation`

### Definition

```csharp
public sealed class ValidationError : ValueObject
{
    /// <summary>
    /// Error code (machine-readable, e.g., "CIRCULAR_DEPENDENCY").
    /// </summary>
    public string Code { get; }
    
    /// <summary>
    /// Human-readable error message.
    /// Must be sufficient for remediation without further investigation.
    /// </summary>
    public string Message { get; }
    
    /// <summary>
    /// Error category (e.g., "CIRCULAR_DEPENDENCY", "MISSING_KEY", "TYPE_MISMATCH").
    /// Enables filtering and application-level categorization.
    /// </summary>
    public string Category { get; }
    
    /// <summary>
    /// Scope where error occurred (e.g., "api:customer-api").
    /// Null if error is global (not scope-specific).
    /// </summary>
    public string? Scope { get; }
    
    /// <summary>
    /// Path of involved keys (for circular dependencies, type mismatches, etc.).
    /// Example: ["override_a", "override_b", "override_a"] for circular path.
    /// </summary>
    public IReadOnlyList<string>? Path { get; }
    
    public ValidationError(
        string code,
        string message,
        string category,
        string? scope = null,
        IReadOnlyList<string>? path = null)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Code required", nameof(code));
        
        if (string.IsNullOrWhiteSpace(message))
            throw new ArgumentException("Message required", nameof(message));
        
        Code = code;
        Message = message;
        Category = category;
        Scope = scope;
        Path = path ?? ImmutableList<string>.Empty;
    }
    
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Code;
        yield return Message;
        yield return Category;
        yield return Scope;
        yield return string.Join(",", Path ?? new List<string>());
    }
}
```

---

## Contract Examples

### Example 1: Profile Creation and Resolution

```csharp
// Create profile
var profile = new Profile(id: Guid.NewGuid(), name: "API-Config");
Assert.Equal(ProfileState.Unresolved, profile.State);

// Apply overrides
profile.ApplyOverride("global", "timeout", new ConfigurationValue(30));
profile.ApplyOverride("api", "timeout", new ConfigurationValue(60));
profile.ApplyOverride("endpoint", "timeout", new ConfigurationValue(120));

// Resolve (deterministic)
profile.Resolve();
Assert.Equal(ProfileState.Resolved, profile.State);
Assert.Equal(120, profile.Get("timeout").AsInt());

// Attempt to modify after resolution (should throw)
Assert.Throws<InvalidOperationException>(() =>
    profile.ApplyOverride("global", "timeout", new ConfigurationValue(90)));
```

### Example 2: Validation Failure

```csharp
var profile = new Profile(id: Guid.NewGuid(), name: "Bad-Config");
profile.ApplyOverride("global", "timeout", new ConfigurationValue(-10));  // Invalid value

var validator = new ProfileValidator();
var result = profile.Validate(validator);

Assert.False(result.IsValid);
Assert.Single(result.Errors);
Assert.Equal("CONSTRAINT_VIOLATION", result.Errors[0].Code);
Assert.Equal(ProfileState.Invalid, profile.State);
```

### Example 3: Deterministic Resolution

```csharp
// Order 1: global, api, endpoint
var profile1 = new Profile(Guid.NewGuid(), "Profile");
profile1.ApplyOverride("global", "timeout", new ConfigurationValue(30));
profile1.ApplyOverride("api", "timeout", new ConfigurationValue(60));
profile1.ApplyOverride("endpoint", "timeout", new ConfigurationValue(120));
profile1.Resolve();
var result1 = profile1.Get("timeout").AsInt();

// Order 2: endpoint, global, api (different order)
var profile2 = new Profile(Guid.NewGuid(), "Profile");
profile2.ApplyOverride("endpoint", "timeout", new ConfigurationValue(120));
profile2.ApplyOverride("global", "timeout", new ConfigurationValue(30));
profile2.ApplyOverride("api", "timeout", new ConfigurationValue(60));
profile2.Resolve();
var result2 = profile2.Get("timeout").AsInt();

Assert.Equal(result1, result2);  // Both 120 (deterministic, order-independent)
```

---

## Backward Compatibility

### Migration Path

**Before Enrichment**:
```csharp
public class Profile
{
    public Guid Id { get; }
    public string Name { get; }
    public IReadOnlyDictionary<string, ConfigurationValue> Configuration { get; }
}
```

**After Enrichment**:
```csharp
public class Profile
{
    public Guid Id { get; }
    public string Name { get; }
    public ProfileState State { get; }
    public IReadOnlyDictionary<string, ConfigurationValue> Overrides { get; }
    public IReadOnlyDictionary<string, ConfigurationValue>? ResolvedConfiguration { get; }
    public IReadOnlyList<ValidationError>? ValidationErrors { get; }
    
    public void ApplyOverride(string scope, string key, ConfigurationValue value) { }
    public void Resolve(IReadOnlyDictionary<string, ConfigurationValue>? inputs) { }
    public ConfigurationValue Get(string key) { }
    public ValidationResult Validate(IProfileValidator validator) { }
}
```

**Compatibility Note**: Old code using `Configuration` property can migrate via adapter:
```csharp
// Migration method
public IReadOnlyDictionary<string, ConfigurationValue> GetConfiguration()
{
    if (State != ProfileState.Resolved)
        throw new InvalidOperationException("Profile must be resolved first.");
    
    return ResolvedConfiguration!;
}
```

---

## Contract Verification Tests

### Test: Deterministic Resolution (Order-Independent)

```csharp
[Fact]
public void ProfileResolver_Resolution_IsDeterministic_AndOrderIndependent()
{
    var permutations = GeneratePermutations(overrides);
    var results = new List<IReadOnlyDictionary<string, ConfigurationValue>>();
    
    var resolver = new ProfileResolver();
    
    foreach (var permutation in permutations)
    {
        var result = resolver.Resolve(permutation, inputs);
        results.Add(result);
    }
    
    // All permutations should produce identical result
    var first = results[0];
    Assert.All(results, result => Assert.Equal(first, result));
}
```

### Test: Immutability After Resolution

```csharp
[Fact]
public void Profile_Immutable_AfterResolution()
{
    var profile = new Profile(Guid.NewGuid(), "Config");
    profile.ApplyOverride("global", "timeout", new ConfigurationValue(30));
    profile.Resolve();
    
    // Should throw when attempting to modify
    Assert.Throws<InvalidOperationException>(() =>
        profile.ApplyOverride("api", "timeout", new ConfigurationValue(60)));
}
```

### Test: Circular Dependency Detection

```csharp
[Fact]
public void ProfileValidator_DetectsCircularDependencies()
{
    var profile = new Profile(Guid.NewGuid(), "Config");
    // Create circular dependency: A → B → C → A
    
    var validator = new ProfileValidator();
    var result = profile.Validate(validator);
    
    Assert.False(result.IsValid);
    Assert.Single(result.Errors.Where(e => e.Code == "CIRCULAR_DEPENDENCY"));
}
```

---

**Status**: ✅ Profile Domain Contract Complete
