# Configuration Schema and Validation

**Version**: 1.0  
**Created**: 2026-01-16  
**Specification**: specs/001-config-format/spec.md  
**Status**: Design

## Overview

This document defines the configuration schema, validation rules, default values, and multi-source composition for the Performance & Reliability Testing Platform. The configuration system ensures explicit defaults, strict validation, and fail-fast behavior.

## Design Principles

1. **Explicit Defaults** - All defaults documented and visible
2. **Fail-Fast Validation** - Errors detected before execution
3. **Multi-Source Composition** - File, environment, CLI with clear precedence
4. **Schema Versioning** - Backward compatibility and migration support
5. **Deterministic Behavior** - Same configuration always produces same result

## Configuration Schema

### Root Structure

```yaml
# Configuration schema version (required)
schemaVersion: "1.0"

# Test execution parameters
testExecution:
  duration: "5m"                # Required: Test duration (e.g., "5m", "30s", "1h")
  virtualUsers: 100             # Required: Number of concurrent virtual users
  rampUpPeriod: "30s"          # Optional: Ramp-up duration (default: "0s")
  target: "https://api.example.com"  # Required: Target endpoint

# Request settings
requestSettings:
  timeout: "30s"               # Optional: Request timeout (default: "30s")
  retries: 3                   # Optional: Retry attempts (default: 3)
  retryDelay: "1s"            # Optional: Delay between retries (default: "1s")
  headers:                     # Optional: Custom headers
    User-Agent: "PerformanceEngine/1.0"
    Authorization: "${AUTH_TOKEN}"  # Environment variable substitution

# Rate limiting
rateLimits:
  requestsPerSecond: 1000      # Optional: Max requests per second (default: unlimited)
  concurrency: 50              # Optional: Max concurrent connections (default: 100)

# Performance thresholds
thresholds:
  - metric: "p95"              # Metric name (p50, p95, p99, errorRate, throughput)
    operator: "<="             # Comparison operator (<, <=, >, >=, ==)
    value: 300                 # Threshold value (units depend on metric)
    severity: "critical"       # Severity level (critical, warning, info)

# Named test profiles
profiles:
  smoke-test:
    testExecution:
      duration: "1m"
      virtualUsers: 10
    thresholds:
      - metric: "errorRate"
        operator: "<="
        value: 0.01

  load-test:
    testExecution:
      duration: "10m"
      virtualUsers: 500
    rateLimits:
      requestsPerSecond: 5000

  stress-test:
    testExecution:
      duration: "30m"
      virtualUsers: 2000
    rateLimits:
      concurrency: 200
```

## Schema Definition (C# Models)

### Domain Layer Models

**Location**: `src/PerformanceEngine.Domain.Shared/Configuration/`

```csharp
namespace PerformanceEngine.Domain.Shared.Configuration;

/// <summary>
/// Root configuration model.
/// </summary>
public sealed record Configuration
{
    public required string SchemaVersion { get; init; }
    public required TestExecutionConfig TestExecution { get; init; }
    public RequestSettingsConfig? RequestSettings { get; init; }
    public RateLimitsConfig? RateLimits { get; init; }
    public IReadOnlyList<ThresholdConfig>? Thresholds { get; init; }
    public IReadOnlyDictionary<string, ProfileConfig>? Profiles { get; init; }

    /// <summary>
    /// Source attribution for traceability.
    /// Maps configuration path to source (e.g., "testExecution.duration" → "config.yaml:5")
    /// </summary>
    public IReadOnlyDictionary<string, ConfigSource>? SourceMap { get; init; }
}

/// <summary>
/// Test execution parameters.
/// </summary>
public sealed record TestExecutionConfig
{
    public required TimeSpan Duration { get; init; }
    public required int VirtualUsers { get; init; }
    public TimeSpan RampUpPeriod { get; init; } = TimeSpan.Zero;
    public required string Target { get; init; }
}

/// <summary>
/// Request-level settings.
/// </summary>
public sealed record RequestSettingsConfig
{
    public TimeSpan Timeout { get; init; } = TimeSpan.FromSeconds(30);
    public int Retries { get; init; } = 3;
    public TimeSpan RetryDelay { get; init; } = TimeSpan.FromSeconds(1);
    public IReadOnlyDictionary<string, string>? Headers { get; init; }
}

/// <summary>
/// Rate limiting configuration.
/// </summary>
public sealed record RateLimitsConfig
{
    public int? RequestsPerSecond { get; init; }  // null = unlimited
    public int Concurrency { get; init; } = 100;
}

/// <summary>
/// Performance threshold for pass/fail criteria.
/// </summary>
public sealed record ThresholdConfig
{
    public required string Metric { get; init; }  // p50, p95, p99, errorRate, throughput
    public required ComparisonOperator Operator { get; init; }
    public required double Value { get; init; }
    public SeverityLevel Severity { get; init; } = SeverityLevel.Critical;
}

public enum ComparisonOperator
{
    LessThan,           // <
    LessThanOrEqual,    // <=
    GreaterThan,        // >
    GreaterThanOrEqual, // >=
    Equal               // ==
}

public enum SeverityLevel
{
    Info,
    Warning,
    Critical
}

/// <summary>
/// Named test profile.
/// </summary>
public sealed record ProfileConfig
{
    public TestExecutionConfig? TestExecution { get; init; }
    public RequestSettingsConfig? RequestSettings { get; init; }
    public RateLimitsConfig? RateLimits { get; init; }
    public IReadOnlyList<ThresholdConfig>? Thresholds { get; init; }
}

/// <summary>
/// Configuration source attribution.
/// </summary>
public sealed record ConfigSource
{
    public required ConfigSourceType Type { get; init; }
    public required string Location { get; init; }  // File path + line or env var name
}

public enum ConfigSourceType
{
    Default,
    ConfigFile,
    Environment,
    CommandLine
}
```

## Validation Rules

### Application Layer Validator

**Location**: `src/PerformanceEngine.Application/Configuration/ConfigurationValidator.cs`

```csharp
namespace PerformanceEngine.Application.Configuration;

/// <summary>
/// Multi-phase configuration validator following fail-fast principle.
/// </summary>
public class ConfigurationValidator : IConfigurationValidator
{
    public ValidationResult Validate(Configuration config)
    {
        var errors = new List<ValidationError>();

        // Phase 1: Structural validation (schema version, required fields)
        errors.AddRange(ValidateStructure(config));
        if (errors.Any()) return ValidationResult.Failure(errors);

        // Phase 2: Type validation (already enforced by strong typing in C#)

        // Phase 3: Range validation (numeric constraints)
        errors.AddRange(ValidateRanges(config));

        // Phase 4: Logical consistency (cross-field validation)
        errors.AddRange(ValidateLogic(config));

        // Phase 5: Dependency validation (required field combinations)
        errors.AddRange(ValidateDependencies(config));

        return errors.Any() 
            ? ValidationResult.Failure(errors) 
            : ValidationResult.Success();
    }

    private IEnumerable<ValidationError> ValidateStructure(Configuration config)
    {
        // FR-031: Schema version required
        if (string.IsNullOrWhiteSpace(config.SchemaVersion))
        {
            yield return new ValidationError
            {
                FieldPath = "schemaVersion",
                ErrorCode = "REQUIRED_FIELD",
                Message = "Schema version is required",
                Severity = ErrorSeverity.Error
            };
        }

        // FR-033: Schema version must be supported
        if (!IsSupportedSchemaVersion(config.SchemaVersion))
        {
            yield return new ValidationError
            {
                FieldPath = "schemaVersion",
                ErrorCode = "UNSUPPORTED_VERSION",
                Message = $"Schema version '{config.SchemaVersion}' is not supported. Supported versions: 1.0",
                Severity = ErrorSeverity.Error
            };
        }

        // Required fields
        if (config.TestExecution == null)
        {
            yield return new ValidationError
            {
                FieldPath = "testExecution",
                ErrorCode = "REQUIRED_FIELD",
                Message = "Test execution configuration is required",
                Severity = ErrorSeverity.Error
            };
        }
    }

    private IEnumerable<ValidationError> ValidateRanges(Configuration config)
    {
        // FR-008: Duration must be positive
        if (config.TestExecution.Duration <= TimeSpan.Zero)
        {
            yield return new ValidationError
            {
                FieldPath = "testExecution.duration",
                Value = config.TestExecution.Duration.ToString(),
                ErrorCode = "INVALID_RANGE",
                Message = "Test duration must be positive",
                Constraint = "> 0",
                Severity = ErrorSeverity.Error,
                Source = GetSource(config, "testExecution.duration")
            };
        }

        // FR-008: Virtual users must be positive
        if (config.TestExecution.VirtualUsers <= 0)
        {
            yield return new ValidationError
            {
                FieldPath = "testExecution.virtualUsers",
                Value = config.TestExecution.VirtualUsers.ToString(),
                ErrorCode = "INVALID_RANGE",
                Message = "Virtual users must be positive",
                Constraint = "> 0",
                Severity = ErrorSeverity.Error,
                Source = GetSource(config, "testExecution.virtualUsers")
            };
        }

        // Ramp-up period must be non-negative
        if (config.TestExecution.RampUpPeriod < TimeSpan.Zero)
        {
            yield return new ValidationError
            {
                FieldPath = "testExecution.rampUpPeriod",
                Value = config.TestExecution.RampUpPeriod.ToString(),
                ErrorCode = "INVALID_RANGE",
                Message = "Ramp-up period must be non-negative",
                Constraint = ">= 0",
                Severity = ErrorSeverity.Error,
                Source = GetSource(config, "testExecution.rampUpPeriod")
            };
        }

        // Request timeout must be positive
        if (config.RequestSettings?.Timeout <= TimeSpan.Zero)
        {
            yield return new ValidationError
            {
                FieldPath = "requestSettings.timeout",
                Value = config.RequestSettings.Timeout.ToString(),
                ErrorCode = "INVALID_RANGE",
                Message = "Request timeout must be positive",
                Constraint = "> 0",
                Severity = ErrorSeverity.Error
            };
        }

        // Retries must be non-negative
        if (config.RequestSettings?.Retries < 0)
        {
            yield return new ValidationError
            {
                FieldPath = "requestSettings.retries",
                Value = config.RequestSettings.Retries.ToString(),
                ErrorCode = "INVALID_RANGE",
                Message = "Retries must be non-negative",
                Constraint = ">= 0",
                Severity = ErrorSeverity.Error
            };
        }
    }

    private IEnumerable<ValidationError> ValidateLogic(Configuration config)
    {
        // FR-011: Logical consistency checks

        // Ramp-up period cannot exceed test duration
        if (config.TestExecution.RampUpPeriod > config.TestExecution.Duration)
        {
            yield return new ValidationError
            {
                FieldPath = "testExecution.rampUpPeriod",
                ErrorCode = "LOGICAL_INCONSISTENCY",
                Message = "Ramp-up period cannot exceed test duration",
                Constraint = $"<= {config.TestExecution.Duration}",
                Severity = ErrorSeverity.Error
            };
        }

        // Validate threshold values
        if (config.Thresholds != null)
        {
            foreach (var threshold in config.Thresholds)
            {
                // Error rate must be between 0 and 1
                if (threshold.Metric.Equals("errorRate", StringComparison.OrdinalIgnoreCase))
                {
                    if (threshold.Value < 0 || threshold.Value > 1)
                    {
                        yield return new ValidationError
                        {
                            FieldPath = "thresholds[errorRate].value",
                            Value = threshold.Value.ToString(),
                            ErrorCode = "INVALID_RANGE",
                            Message = "Error rate threshold must be between 0 and 1 (0% to 100%)",
                            Constraint = "0 <= value <= 1",
                            Severity = ErrorSeverity.Error
                        };
                    }
                }

                // Throughput must be positive
                if (threshold.Metric.Equals("throughput", StringComparison.OrdinalIgnoreCase))
                {
                    if (threshold.Value <= 0)
                    {
                        yield return new ValidationError
                        {
                            FieldPath = "thresholds[throughput].value",
                            Value = threshold.Value.ToString(),
                            ErrorCode = "INVALID_RANGE",
                            Message = "Throughput threshold must be positive",
                            Constraint = "> 0",
                            Severity = ErrorSeverity.Error
                        };
                    }
                }
            }
        }

        // Validate target URL format
        if (!Uri.TryCreate(config.TestExecution.Target, UriKind.Absolute, out var uri))
        {
            yield return new ValidationError
            {
                FieldPath = "testExecution.target",
                Value = config.TestExecution.Target,
                ErrorCode = "INVALID_FORMAT",
                Message = "Target must be a valid absolute URL",
                Constraint = "http:// or https:// URL",
                Severity = ErrorSeverity.Error
            };
        }
    }

    private IEnumerable<ValidationError> ValidateDependencies(Configuration config)
    {
        // FR-012: Cross-field dependency validation
        
        // If custom headers include Authorization, warn about security
        if (config.RequestSettings?.Headers?.ContainsKey("Authorization") == true)
        {
            var authValue = config.RequestSettings.Headers["Authorization"];
            if (!authValue.StartsWith("$"))  // Not an environment variable
            {
                yield return new ValidationError
                {
                    FieldPath = "requestSettings.headers.Authorization",
                    ErrorCode = "SECURITY_WARNING",
                    Message = "Authorization header contains literal value. Consider using environment variable (${VAR_NAME})",
                    Severity = ErrorSeverity.Warning
                };
            }
        }
    }

    private bool IsSupportedSchemaVersion(string version)
    {
        // FR-035: Support at least two major versions
        var supportedVersions = new[] { "1.0", "1.1" };
        return supportedVersions.Contains(version);
    }

    private string? GetSource(Configuration config, string fieldPath)
    {
        // FR-028: Report source of configuration values
        return config.SourceMap?.TryGetValue(fieldPath, out var source) == true 
            ? source.Location 
            : null;
    }
}

/// <summary>
/// Validation error with detailed context.
/// </summary>
public sealed record ValidationError
{
    public required string FieldPath { get; init; }
    public string? Value { get; init; }
    public required string ErrorCode { get; init; }
    public required string Message { get; init; }
    public string? Constraint { get; init; }
    public string? Source { get; init; }  // File:line or env var name
    public ErrorSeverity Severity { get; init; } = ErrorSeverity.Error;
}

public enum ErrorSeverity
{
    Info,
    Warning,
    Error
}

/// <summary>
/// Validation result.
/// </summary>
public sealed record ValidationResult
{
    public bool IsValid { get; init; }
    public IReadOnlyList<ValidationError> Errors { get; init; } = Array.Empty<ValidationError>();

    public static ValidationResult Success() => new() { IsValid = true };
    public static ValidationResult Failure(IEnumerable<ValidationError> errors) => 
        new() { IsValid = false, Errors = errors.ToList() };
}
```

## Configuration Source Composition

### Multi-Source Loading Strategy

**Precedence Order** (FR-023):
1. **CLI Arguments** (highest precedence)
2. **Environment Variables**
3. **Configuration File**
4. **Default Values** (lowest precedence)

**Location**: `src/PerformanceEngine.Application/Configuration/ConfigurationComposer.cs`

```csharp
namespace PerformanceEngine.Application.Configuration;

/// <summary>
/// Composes configuration from multiple sources following precedence rules.
/// </summary>
public class ConfigurationComposer : IConfigurationComposer
{
    private readonly IConfigurationFileLoader _fileLoader;
    private readonly IEnvironmentVariableReader _envReader;
    private readonly IDefaultValuesRegistry _defaultsRegistry;

    public async Task<Configuration> ComposeAsync(
        string? configFilePath,
        IDictionary<string, string>? cliArguments,
        CancellationToken cancellationToken = default)
    {
        // Layer 1: Start with defaults
        var config = _defaultsRegistry.GetDefaults();
        var sourceMap = new Dictionary<string, ConfigSource>();
        TrackSources(config, ConfigSourceType.Default, "defaults", sourceMap);

        // Layer 2: Apply configuration file
        if (!string.IsNullOrWhiteSpace(configFilePath))
        {
            var fileConfig = await _fileLoader.LoadAsync(configFilePath, cancellationToken);
            config = MergeConfigurations(config, fileConfig, sourceMap, ConfigSourceType.ConfigFile, configFilePath);
        }

        // Layer 3: Apply environment variables
        var envConfig = _envReader.ReadEnvironmentVariables();
        config = MergeConfigurations(config, envConfig, sourceMap, ConfigSourceType.Environment, "environment");

        // Layer 4: Apply CLI arguments (highest precedence)
        if (cliArguments != null && cliArguments.Any())
        {
            var cliConfig = MapCliArgumentsToConfig(cliArguments);
            config = MergeConfigurations(config, cliConfig, sourceMap, ConfigSourceType.CommandLine, "CLI");
        }

        // Attach source map for traceability (FR-024, SC-005)
        return config with { SourceMap = sourceMap };
    }

    private Configuration MergeConfigurations(
        Configuration baseConfig,
        Configuration overrideConfig,
        Dictionary<string, ConfigSource> sourceMap,
        ConfigSourceType sourceType,
        string sourceLocation)
    {
        // Merge test execution
        var testExecution = baseConfig.TestExecution;
        if (overrideConfig.TestExecution != null)
        {
            testExecution = MergeTestExecution(
                baseConfig.TestExecution, 
                overrideConfig.TestExecution,
                sourceMap,
                sourceType,
                sourceLocation
            );
        }

        // Merge request settings
        var requestSettings = baseConfig.RequestSettings;
        if (overrideConfig.RequestSettings != null)
        {
            requestSettings = MergeRequestSettings(
                baseConfig.RequestSettings,
                overrideConfig.RequestSettings,
                sourceMap,
                sourceType,
                sourceLocation
            );
        }

        // Similar merging for other sections...

        return baseConfig with
        {
            TestExecution = testExecution,
            RequestSettings = requestSettings,
            // ...
        };
    }

    private TestExecutionConfig MergeTestExecution(
        TestExecutionConfig baseConfig,
        TestExecutionConfig overrideConfig,
        Dictionary<string, ConfigSource> sourceMap,
        ConfigSourceType sourceType,
        string sourceLocation)
    {
        var merged = baseConfig;

        if (overrideConfig.Duration != default)
        {
            merged = merged with { Duration = overrideConfig.Duration };
            sourceMap["testExecution.duration"] = new ConfigSource
            {
                Type = sourceType,
                Location = sourceLocation
            };
        }

        if (overrideConfig.VirtualUsers > 0)
        {
            merged = merged with { VirtualUsers = overrideConfig.VirtualUsers };
            sourceMap["testExecution.virtualUsers"] = new ConfigSource
            {
                Type = sourceType,
                Location = sourceLocation
            };
        }

        // Similar for other fields...

        return merged;
    }
}
```

### Environment Variable Mapping

**Convention**: `PERF_ENGINE_<SECTION>_<FIELD>`

Examples:
- `PERF_ENGINE_TEST_EXECUTION_DURATION=10m` → `testExecution.duration`
- `PERF_ENGINE_RATE_LIMITS_REQUESTS_PER_SECOND=5000` → `rateLimits.requestsPerSecond`
- `PERF_ENGINE_REQUEST_SETTINGS_TIMEOUT=60s` → `requestSettings.timeout`

```csharp
public class EnvironmentVariableReader : IEnvironmentVariableReader
{
    private const string Prefix = "PERF_ENGINE_";

    public Configuration ReadEnvironmentVariables()
    {
        var envVars = Environment.GetEnvironmentVariables();
        var config = new Configuration { SchemaVersion = "1.0" };

        foreach (DictionaryEntry entry in envVars)
        {
            var key = entry.Key.ToString();
            if (!key.StartsWith(Prefix)) continue;

            var path = key.Substring(Prefix.Length)
                .Replace("__", ".")  // Double underscore for nested paths
                .ToLowerInvariant();

            var value = entry.Value?.ToString();
            if (string.IsNullOrEmpty(value)) continue;

            // Map to configuration property
            ApplyEnvironmentVariable(config, path, value);
        }

        return config;
    }
}
```

## Default Values Registry

**Location**: `src/PerformanceEngine.Application/Configuration/DefaultValuesRegistry.cs`

```csharp
namespace PerformanceEngine.Application.Configuration;

/// <summary>
/// Centralized registry of all default configuration values.
/// </summary>
/// <remarks>
/// FR-015: System MUST define explicit default values for all optional fields
/// FR-016: System MUST document all defaults in single canonical location
/// FR-019: System MUST NOT use implicit or undocumented defaults
/// </remarks>
public class DefaultValuesRegistry : IDefaultValuesRegistry
{
    public Configuration GetDefaults()
    {
        return new Configuration
        {
            SchemaVersion = "1.0",
            
            TestExecution = new TestExecutionConfig
            {
                Duration = TimeSpan.FromMinutes(5),
                VirtualUsers = 10,
                RampUpPeriod = TimeSpan.Zero,
                Target = string.Empty  // Required, no default
            },

            RequestSettings = new RequestSettingsConfig
            {
                Timeout = TimeSpan.FromSeconds(30),
                Retries = 3,
                RetryDelay = TimeSpan.FromSeconds(1),
                Headers = null
            },

            RateLimits = new RateLimitsConfig
            {
                RequestsPerSecond = null,  // Unlimited by default
                Concurrency = 100
            },

            Thresholds = Array.Empty<ThresholdConfig>(),
            Profiles = new Dictionary<string, ProfileConfig>()
        };
    }

    public IDictionary<string, string> GetDefaultsDocumentation()
    {
        return new Dictionary<string, string>
        {
            ["testExecution.duration"] = "5m - Test duration",
            ["testExecution.virtualUsers"] = "10 - Number of concurrent virtual users",
            ["testExecution.rampUpPeriod"] = "0s - Ramp-up period",
            ["requestSettings.timeout"] = "30s - Request timeout",
            ["requestSettings.retries"] = "3 - Number of retry attempts",
            ["requestSettings.retryDelay"] = "1s - Delay between retries",
            ["rateLimits.requestsPerSecond"] = "unlimited - Max requests per second",
            ["rateLimits.concurrency"] = "100 - Max concurrent connections",
            ["thresholds"] = "[] - No thresholds by default",
            ["profiles"] = "{} - No profiles by default"
        };
    }
}
```

## Schema Versioning

### Version Support Matrix

| Schema Version | Status | Supported From | Deprecated In | Removed In |
|----------------|--------|----------------|---------------|------------|
| 1.0 | Current | v1.0.0 | - | - |
| 1.1 | Planned | v1.1.0 | - | - |
| 2.0 | Future | TBD | - | - |

**Backward Compatibility Rules** (FR-035):
- System MUST support at least two major schema versions
- Deprecated versions receive warnings but still function
- Unsupported versions are rejected with clear error messages

```csharp
public class SchemaVersionValidator
{
    private static readonly Dictionary<string, VersionStatus> _supportedVersions = new()
    {
        ["1.0"] = VersionStatus.Current,
        ["1.1"] = VersionStatus.Current,
        ["0.9"] = VersionStatus.Deprecated
    };

    public ValidationResult ValidateSchemaVersion(string version)
    {
        if (!_supportedVersions.TryGetValue(version, out var status))
        {
            return ValidationResult.Failure(new[]
            {
                new ValidationError
                {
                    FieldPath = "schemaVersion",
                    ErrorCode = "UNSUPPORTED_VERSION",
                    Message = $"Schema version '{version}' is not supported. Supported versions: {string.Join(", ", _supportedVersions.Keys)}",
                    Severity = ErrorSeverity.Error
                }
            });
        }

        if (status == VersionStatus.Deprecated)
        {
            return ValidationResult.Failure(new[]
            {
                new ValidationError
                {
                    FieldPath = "schemaVersion",
                    ErrorCode = "DEPRECATED_VERSION",
                    Message = $"Schema version '{version}' is deprecated and will be removed in a future release. Please upgrade to version 1.0 or later.",
                    Severity = ErrorSeverity.Warning
                }
            });
        }

        return ValidationResult.Success();
    }
}

public enum VersionStatus
{
    Current,
    Deprecated,
    Removed
}
```

## Example Configuration Files

### Minimal Configuration

```yaml
schemaVersion: "1.0"

testExecution:
  duration: "5m"
  virtualUsers: 100
  target: "https://api.example.com/test"
```

### Complete Configuration

```yaml
schemaVersion: "1.0"

testExecution:
  duration: "10m"
  virtualUsers: 500
  rampUpPeriod: "2m"
  target: "https://api.example.com/test"

requestSettings:
  timeout: "60s"
  retries: 5
  retryDelay: "2s"
  headers:
    User-Agent: "PerformanceEngine/1.0"
    Authorization: "${AUTH_TOKEN}"
    X-Custom-Header: "test-value"

rateLimits:
  requestsPerSecond: 5000
  concurrency: 200

thresholds:
  - metric: "p95"
    operator: "<="
    value: 300
    severity: "critical"
  
  - metric: "p99"
    operator: "<="
    value: 500
    severity: "warning"
  
  - metric: "errorRate"
    operator: "<="
    value: 0.01
    severity: "critical"

profiles:
  smoke-test:
    testExecution:
      duration: "1m"
      virtualUsers: 10
    thresholds:
      - metric: "errorRate"
        operator: "<="
        value: 0.05
  
  load-test:
    testExecution:
      duration: "15m"
      virtualUsers: 1000
    rateLimits:
      requestsPerSecond: 10000
    thresholds:
      - metric: "p95"
        operator: "<="
        value: 250
```

### Configuration with Environment Variables

```yaml
schemaVersion: "1.0"

testExecution:
  duration: "${PERF_TEST_DURATION:-10m}"  # Default to 10m if not set
  virtualUsers: "${PERF_VIRTUAL_USERS:-100}"
  target: "${PERF_TARGET_URL}"  # Required from environment

requestSettings:
  headers:
    Authorization: "Bearer ${API_TOKEN}"  # Must be set in environment
```

## Error Reporting Examples

### Validation Error Output (Text Format)

```
❌ Configuration Validation Failed

Configuration File: /path/to/config.yaml
Validated: 2026-01-16 08:30:00 UTC

Errors (3):

  1. [INVALID_RANGE] testExecution.duration
     Value: "-5m"
     Error: Test duration must be positive
     Constraint: > 0
     Source: config.yaml:5

  2. [INVALID_FORMAT] testExecution.target
     Value: "not-a-url"
     Error: Target must be a valid absolute URL
     Constraint: http:// or https:// URL
     Source: config.yaml:7

  3. [LOGICAL_INCONSISTENCY] testExecution.rampUpPeriod
     Value: "15m"
     Error: Ramp-up period cannot exceed test duration
     Constraint: <= 10m
     Source: config.yaml:6

Fix these errors and try again.
```

### Validation Error Output (JSON Format)

```json
{
  "status": "error",
  "exitCode": 4,
  "timestamp": "2026-01-16T08:30:00Z",
  "command": "validate",
  "error": {
    "type": "ValidationError",
    "message": "Configuration validation failed",
    "errors": [
      {
        "fieldPath": "testExecution.duration",
        "value": "-5m",
        "errorCode": "INVALID_RANGE",
        "message": "Test duration must be positive",
        "constraint": "> 0",
        "severity": "error",
        "source": "config.yaml:5"
      },
      {
        "fieldPath": "testExecution.target",
        "value": "not-a-url",
        "errorCode": "INVALID_FORMAT",
        "message": "Target must be a valid absolute URL",
        "constraint": "http:// or https:// URL",
        "severity": "error",
        "source": "config.yaml:7"
      },
      {
        "fieldPath": "testExecution.rampUpPeriod",
        "value": "15m",
        "errorCode": "LOGICAL_INCONSISTENCY",
        "message": "Ramp-up period cannot exceed test duration",
        "constraint": "<= 10m",
        "severity": "error",
        "source": "config.yaml:6"
      }
    ]
  }
}
```

## Testing Strategy

### Unit Tests

- Test each validation rule in isolation
- Test default values registry completeness
- Test configuration merging logic
- Test environment variable parsing
- Test schema version validation

### Integration Tests

- Test multi-source composition (file + env + CLI)
- Test precedence resolution
- Test source attribution tracking
- Test validation with real configuration files
- Test error message clarity

### Acceptance Tests (from Specification)

- Verify all FR requirements from config-format spec
- Test all acceptance scenarios from spec
- Verify success criteria (SC-001 to SC-008)

## References

- **Specification**: `/specs/001-config-format/spec.md`
- **Constitution**: `/.specify/memory/constitution.md`
- **Base Plan**: `/docs/architecture/base-architecture-plan.md`
- **CLI Commands**: `/docs/architecture/cli-commands.md`
