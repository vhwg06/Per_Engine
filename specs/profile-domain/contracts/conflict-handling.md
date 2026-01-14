# ConflictHandler Contract

## Overview

The `ConflictHandler` is a **stateless domain service** that detects and reports configuration conflicts. A conflict occurs when multiple profiles at the **exact same scope** define **different values** for the **same configuration key**.

**Key Responsibilities**:
- **Conflict Detection**: Identify ambiguous configuration
- **Fail-Fast**: Throw clear exceptions immediately
- **Error Reporting**: Provide actionable error messages

**Location**: `PerformanceEngine.Profile.Domain.Domain.Profiles.ConflictHandler`

---

## Class Definition

```csharp
namespace PerformanceEngine.Profile.Domain.Domain.Profiles
{
    /// <summary>
    /// Stateless domain service for detecting configuration conflicts.
    /// A conflict occurs when multiple profiles at the same scope define
    /// different values for the same configuration key.
    /// </summary>
    public static class ConflictHandler
    {
        /// <summary>
        /// Validates that there are no configuration conflicts in the provided profiles.
        /// </summary>
        /// <param name="profiles">Collection of profiles to validate</param>
        /// <exception cref="ArgumentNullException">If profiles is null</exception>
        /// <exception cref="ConfigurationConflictException">
        /// If conflicts are detected (multiple profiles at same scope with different values)
        /// </exception>
        public static void ValidateNoConflicts(IEnumerable<Profile> profiles);
    }
}
```

---

## Contract Requirements

### 1. Input Validation

**Requirements**:
- MUST throw `ArgumentNullException` if `profiles` is null
- SHOULD accept empty `profiles` collection (no conflicts by definition)
- MUST handle null profiles within the collection gracefully (skip or throw)

**Test Cases**:
```csharp
// ❌ Null profiles
ConflictHandler.ValidateNoConflicts(null);
// Throws: ArgumentNullException("profiles")

// ✅ Empty profiles (valid - no conflicts)
ConflictHandler.ValidateNoConflicts(Array.Empty<Profile>());
// Returns: No exception

// ✅ Single profile (valid - no conflicts)
ConflictHandler.ValidateNoConflicts(new[] { globalProfile });
// Returns: No exception
```

---

### 2. Conflict Detection Logic

**Conflict Definition**:
A conflict exists when:
1. Two or more profiles have **identical scopes** (same Type, Value, Precedence)
2. They define **different values** for the **same ConfigKey**

**Scope Equality**:
Two scopes are identical if:
```csharp
scopeA.Type == scopeB.Type &&
scopeA.Value == scopeB.Value &&
scopeA.Precedence == scopeB.Precedence
```

**Value Equality**:
Two configuration values are different if:
```csharp
!valueA.Equals(valueB)
```

---

### 3. Conflict Detection Algorithm

**Steps**:

1. **Group by Scope**:
   - For each profile, use its scope as a grouping key
   - Use value-based equality (not reference equality)

2. **For Each Scope Group**:
   - Merge all configuration dictionaries from profiles in this group
   - Track: `Dictionary<ConfigKey, List<ConfigValue>>`
   - If a key appears multiple times with different values → conflict

3. **Collect Conflicts**:
   - Build a list of conflict details:
     - ConfigKey that conflicts
     - Scope where conflict occurs
     - All conflicting values

4. **Throw Exception**:
   - If any conflicts found, throw `ConfigurationConflictException`
   - Include detailed conflict information in exception message

---

### 4. Exception Format

**ConfigurationConflictException Structure**:

```csharp
public class ConfigurationConflictException : Exception
{
    /// <summary>
    /// Collection of detected conflicts.
    /// </summary>
    public IReadOnlyList<ConfigConflict> Conflicts { get; }
    
    /// <summary>
    /// Creates a new ConfigurationConflictException.
    /// </summary>
    /// <param name="conflicts">List of detected conflicts</param>
    public ConfigurationConflictException(IReadOnlyList<ConfigConflict> conflicts)
        : base(BuildMessage(conflicts))
    {
        Conflicts = conflicts;
    }
    
    private static string BuildMessage(IReadOnlyList<ConfigConflict> conflicts)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Configuration conflicts detected: {conflicts.Count} conflict(s)");
        
        foreach (var conflict in conflicts)
        {
            var values = string.Join(" vs ", conflict.ConflictingValues.Select(v => v.Value));
            sb.AppendLine($"  - Key '{conflict.Key.Name}' has conflicting values in scope {conflict.Scope}: {values}");
        }
        
        return sb.ToString();
    }
}

public sealed class ConfigConflict
{
    /// <summary>
    /// The configuration key that has conflicting values.
    /// </summary>
    public ConfigKey Key { get; }
    
    /// <summary>
    /// The scope where the conflict occurs.
    /// </summary>
    public IScope Scope { get; }
    
    /// <summary>
    /// All conflicting values for this key in this scope.
    /// </summary>
    public IReadOnlyList<ConfigValue> ConflictingValues { get; }
    
    public ConfigConflict(ConfigKey key, IScope scope, IReadOnlyList<ConfigValue> conflictingValues)
    {
        Key = key;
        Scope = scope;
        ConflictingValues = conflictingValues;
    }
}
```

**Example Error Message**:
```
Configuration conflicts detected: 2 conflict(s)
  - Key 'timeout' has conflicting values in scope Api:payment: 30s vs 60s
  - Key 'retries' has conflicting values in scope Environment:prod: 3 vs 5
```

---

### 5. Conflict vs. No Conflict Examples

**❌ Conflict** (same scope, different values):
```csharp
var profile1 = new Profile(
    new ApiScope("payment"),
    ImmutableDictionary<ConfigKey, ConfigValue>.Empty
        .Add(new ConfigKey("timeout"), ConfigValue.Create("30s"))
);

var profile2 = new Profile(
    new ApiScope("payment"), // SAME SCOPE (Type, Value, Precedence all equal)
    ImmutableDictionary<ConfigKey, ConfigValue>.Empty
        .Add(new ConfigKey("timeout"), ConfigValue.Create("60s")) // DIFFERENT VALUE
);

ConflictHandler.ValidateNoConflicts(new[] { profile1, profile2 });
// ❌ Throws: ConfigurationConflictException
// Message: Key 'timeout' has conflicting values in scope Api:payment: 30s vs 60s
```

**✅ No Conflict** (different scopes):
```csharp
var globalProfile = new Profile(
    GlobalScope.Instance, // Precedence 0
    ImmutableDictionary<ConfigKey, ConfigValue>.Empty
        .Add(new ConfigKey("timeout"), ConfigValue.Create("30s"))
);

var apiProfile = new Profile(
    new ApiScope("payment"), // Precedence 10 (DIFFERENT SCOPE)
    ImmutableDictionary<ConfigKey, ConfigValue>.Empty
        .Add(new ConfigKey("timeout"), ConfigValue.Create("60s"))
);

ConflictHandler.ValidateNoConflicts(new[] { globalProfile, apiProfile });
// ✅ No exception - different scopes, precedence resolves ambiguity
```

**✅ No Conflict** (same scope, same value):
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

ConflictHandler.ValidateNoConflicts(new[] { profile1, profile2 });
// ✅ No exception - identical values are not a conflict
```

**✅ No Conflict** (different keys):
```csharp
var profile1 = new Profile(
    new ApiScope("payment"),
    ImmutableDictionary<ConfigKey, ConfigValue>.Empty
        .Add(new ConfigKey("timeout"), ConfigValue.Create("30s"))
);

var profile2 = new Profile(
    new ApiScope("payment"), // SAME SCOPE
    ImmutableDictionary<ConfigKey, ConfigValue>.Empty
        .Add(new ConfigKey("retries"), ConfigValue.Create(3)) // DIFFERENT KEY
);

ConflictHandler.ValidateNoConflicts(new[] { profile1, profile2 });
// ✅ No exception - different keys, no overlap
```

**❌ Conflict** (custom scope, same precedence):
```csharp
var tag1 = new TagScope("critical", precedence: 20);
var tag2 = new TagScope("critical", precedence: 20); // SAME TYPE, VALUE, PRECEDENCE

var profile1 = new Profile(
    tag1,
    ImmutableDictionary<ConfigKey, ConfigValue>.Empty
        .Add(new ConfigKey("max_retries"), ConfigValue.Create(10))
);

var profile2 = new Profile(
    tag2, // SAME SCOPE
    ImmutableDictionary<ConfigKey, ConfigValue>.Empty
        .Add(new ConfigKey("max_retries"), ConfigValue.Create(5)) // DIFFERENT VALUE
);

ConflictHandler.ValidateNoConflicts(new[] { profile1, profile2 });
// ❌ Throws: ConfigurationConflictException
// Message: Key 'max_retries' has conflicting values in scope Tag:critical: 10 vs 5
```

---

## Usage Examples

### Example 1: Pre-Resolution Validation

```csharp
using PerformanceEngine.Profile.Domain.Domain.Profiles;

public ResolvedProfile SafeResolve(IEnumerable<Profile> profiles, IScope requestedScope)
{
    // Validate before resolution (fail-fast)
    ConflictHandler.ValidateNoConflicts(profiles);
    
    // If no exception, proceed with resolution
    return ProfileResolver.Resolve(profiles, requestedScope);
}
```

### Example 2: Handling Conflicts Gracefully

```csharp
public ResolvedProfile? TryResolve(IEnumerable<Profile> profiles, IScope requestedScope, out string? error)
{
    try
    {
        ConflictHandler.ValidateNoConflicts(profiles);
        error = null;
        return ProfileResolver.Resolve(profiles, requestedScope);
    }
    catch (ConfigurationConflictException ex)
    {
        error = ex.Message;
        return null;
    }
}
```

### Example 3: Detailed Conflict Reporting

```csharp
public void AnalyzeConflicts(IEnumerable<Profile> profiles)
{
    try
    {
        ConflictHandler.ValidateNoConflicts(profiles);
        Console.WriteLine("✅ No conflicts detected.");
    }
    catch (ConfigurationConflictException ex)
    {
        Console.WriteLine($"❌ {ex.Conflicts.Count} conflict(s) detected:\n");
        
        foreach (var conflict in ex.Conflicts)
        {
            Console.WriteLine($"Scope: {conflict.Scope}");
            Console.WriteLine($"Key:   {conflict.Key.Name}");
            Console.WriteLine($"Values:");
            
            foreach (var value in conflict.ConflictingValues)
            {
                Console.WriteLine($"  - {value.Value} ({value.Type})");
            }
            
            Console.WriteLine();
        }
    }
}
```

**Output**:
```
❌ 2 conflict(s) detected:

Scope: Api:payment
Key:   timeout
Values:
  - 30s (string)
  - 60s (string)

Scope: Environment:prod
Key:   retries
Values:
  - 3 (integer)
  - 5 (integer)
```

### Example 4: Testing Conflict Detection

```csharp
using Xunit;
using FluentAssertions;

public class ConflictHandlerTests
{
    [Fact]
    public void ValidateNoConflicts_ShouldThrow_WhenSameScopeHasDifferentValues()
    {
        // Arrange
        var scope = new ApiScope("payment");
        
        var profile1 = new Profile(
            scope,
            ImmutableDictionary<ConfigKey, ConfigValue>.Empty
                .Add(new ConfigKey("timeout"), ConfigValue.Create("30s"))
        );
        
        var profile2 = new Profile(
            scope,
            ImmutableDictionary<ConfigKey, ConfigValue>.Empty
                .Add(new ConfigKey("timeout"), ConfigValue.Create("60s"))
        );
        
        var profiles = new[] { profile1, profile2 };
        
        // Act & Assert
        var exception = Assert.Throws<ConfigurationConflictException>(() =>
            ConflictHandler.ValidateNoConflicts(profiles)
        );
        
        exception.Conflicts.Should().HaveCount(1);
        exception.Conflicts[0].Key.Name.Should().Be("timeout");
        exception.Conflicts[0].Scope.Should().Be(scope);
        exception.Conflicts[0].ConflictingValues.Should().HaveCount(2);
    }
    
    [Fact]
    public void ValidateNoConflicts_ShouldNotThrow_WhenDifferentScopes()
    {
        // Arrange
        var profile1 = new Profile(
            GlobalScope.Instance,
            ImmutableDictionary<ConfigKey, ConfigValue>.Empty
                .Add(new ConfigKey("timeout"), ConfigValue.Create("30s"))
        );
        
        var profile2 = new Profile(
            new ApiScope("payment"),
            ImmutableDictionary<ConfigKey, ConfigValue>.Empty
                .Add(new ConfigKey("timeout"), ConfigValue.Create("60s"))
        );
        
        var profiles = new[] { profile1, profile2 };
        
        // Act & Assert
        var exception = Record.Exception(() =>
            ConflictHandler.ValidateNoConflicts(profiles)
        );
        
        exception.Should().BeNull();
    }
}
```

---

## Performance Characteristics

**Time Complexity**:
- **Grouping by Scope**: O(P) where P = number of profiles
- **Conflict Detection**: O(P × K) where K = average keys per profile
- **Overall**: O(P × K)

**Space Complexity**:
- **Scope Groups**: O(P)
- **Value Tracking**: O(P × K)
- **Overall**: O(P × K)

**Optimization Opportunities**:
- Short-circuit on first conflict (optional)
- Use HashSet for value comparison (O(1) lookup)
- Parallel conflict detection for large profile sets

---

## Design Rationale

### Why Fail-Fast?

**Correctness Over Convenience**:
- Ambiguous configuration is a **bug**, not a feature
- Better to catch conflicts early (at resolution time) than late (at runtime)
- Forces developers to resolve conflicts intentionally

**Clear Error Messages**:
- Exception includes all conflict details
- Users can immediately identify and fix the issue
- No silent failures or undefined behavior

**Alternative Approaches (Not Chosen)**:
1. **Last-Write-Wins**: Silently use the last profile's value
   - ❌ Hidden bugs, non-deterministic if profile order changes
2. **First-Write-Wins**: Use the first profile's value
   - ❌ Hidden bugs, dependent on profile input order
3. **Random Selection**: Pick a random value
   - ❌ Non-deterministic, impossible to debug

### Why Separate from ProfileResolver?

**Single Responsibility**:
- `ConflictHandler`: Detects conflicts
- `ProfileResolver`: Merges configuration

**Reusability**:
- Can validate profiles before resolution
- Can use in CI/CD to validate configuration files
- Can integrate with linting tools

**Testability**:
- Easier to test conflict detection logic in isolation
- Clearer test boundaries

---

## Integration with ProfileResolver

The `ProfileResolver` internally calls `ConflictHandler.ValidateNoConflicts()` before merging configuration:

```csharp
public static ResolvedProfile Resolve(IEnumerable<Profile> profiles, IScope requestedScope)
{
    if (profiles == null) throw new ArgumentNullException(nameof(profiles));
    if (requestedScope == null) throw new ArgumentNullException(nameof(requestedScope));
    
    // Step 1: Detect conflicts (fail-fast)
    ConflictHandler.ValidateNoConflicts(profiles);
    
    // Step 2: Filter applicable profiles
    var applicableProfiles = profiles.Where(p => p.Scope.Matches(requestedScope)).ToList();
    
    // Step 3: Sort by precedence
    applicableProfiles.Sort((a, b) => a.Scope.CompareTo(b.Scope));
    
    // Step 4: Merge configuration
    // ... (merging logic)
    
    return new ResolvedProfile(requestedScope, configuration, auditTrail);
}
```

**Timing**: Conflict detection happens **before** filtering and merging, ensuring clean input.

---

## Testing Requirements

### Unit Tests

1. **Input Validation**:
   - Null profiles throws ArgumentNullException
   - Empty profiles does not throw

2. **No Conflict Cases**:
   - Single profile does not throw
   - Different scopes do not throw
   - Same scope, same values do not throw
   - Different keys do not throw

3. **Conflict Cases**:
   - Same scope, different values throws
   - Multiple conflicts detected and reported
   - Exception contains correct conflict details

4. **Edge Cases**:
   - Empty configuration in profiles
   - Null values in ConfigValue
   - CompositeScope conflicts

### Integration Tests

1. **With ProfileResolver**:
   - Resolver calls ConflictHandler automatically
   - Conflicts detected before resolution

2. **Performance**:
   - 1000 profiles, 1000 keys validates in < 50ms

---

## See Also

- [scope-interface.md](scope-interface.md) - IScope contract
- [resolver-interface.md](resolver-interface.md) - ProfileResolver contract
- [resolved-profile.md](resolved-profile.md) - ResolvedProfile contract
- [SCOPE_HIERARCHY.md](../../../docs/SCOPE_HIERARCHY.md) - Precedence rules
- [Profile Domain README](../../../src/PerformanceEngine.Profile.Domain/README.md) - Architecture overview
