# CLI Commands and Exit Codes

**Version**: 1.0  
**Created**: 2026-01-16  
**Specification**: specs/001-cli-interface/spec.md  
**Status**: Design

## Overview

This document defines the command-line interface (CLI) for the Performance & Reliability Testing Platform. The CLI provides deterministic, automation-friendly test execution with standardized exit codes and dual output formats (JSON and text).

## Design Principles

1. **Determinism** - Same input produces same output and exit code
2. **Thin Adapter** - CLI delegates all logic to application use cases
3. **Exit Code Semantics** - Clear, consistent exit codes for automation
4. **Dual Output** - JSON for machines, text for humans
5. **Fail-Fast** - Validate configuration before execution

## Command Structure

### Root Command

```
performance-engine [command] [options]
```

**Global Options:**
- `--config <path>` : Path to configuration file (required for most commands)
- `--format <json|text>` : Output format (default: text)
- `--verbose, -v` : Enable verbose output to stderr
- `--help, -h` : Show help information
- `--version` : Show version information

### Commands

#### 1. `run` - Execute Performance Tests

**Purpose**: Execute performance tests and report results.

**Usage:**
```bash
performance-engine run --config <path> [options]
```

**Options:**
- `--config <path>` : Configuration file path (required)
- `--profile <name>` : Test profile to execute (optional, uses default if not specified)
- `--format <json|text>` : Output format (default: text)
- `--output <path>` : Write results to file instead of stdout (optional)
- `--verbose, -v` : Verbose logging to stderr

**Behavior:**
1. Load and validate configuration (fail-fast on errors)
2. Execute tests according to configuration
3. Evaluate results against thresholds
4. Output results in specified format
5. Return appropriate exit code

**Exit Codes:**
- `0` : All tests passed, thresholds met
- `1` : Tests failed (thresholds violated)
- `2` : Inconclusive results (insufficient data, partial execution)
- `3` : Configuration error (invalid config, missing file)
- `4` : Validation error (pre-execution validation failed)
- `5` : Runtime error (system error during execution)
- `130` : Interrupted by SIGINT (Ctrl+C)
- `143` : Terminated by SIGTERM

**Examples:**
```bash
# Run with default profile, text output
performance-engine run --config test-config.yaml

# Run specific profile with JSON output
performance-engine run --config test-config.yaml --profile smoke-test --format json

# Run and save results to file
performance-engine run --config test-config.yaml --output results.json --format json
```

---

#### 2. `baseline save` - Save Performance Baseline

**Purpose**: Execute tests and save results as a new performance baseline.

**Usage:**
```bash
performance-engine baseline save --config <path> [options]
```

**Options:**
- `--config <path>` : Configuration file path (required)
- `--name <name>` : Baseline name/identifier (optional, auto-generated if not provided)
- `--profile <name>` : Test profile to execute (optional)
- `--format <json|text>` : Output format (default: text)
- `--verbose, -v` : Verbose logging

**Behavior:**
1. Execute tests (same as `run` command)
2. Persist test results as baseline with metadata
3. Output baseline ID and summary
4. Return appropriate exit code

**Exit Codes:**
- `0` : Baseline saved successfully
- `1` : Tests failed (baseline still saved with failure status)
- `2` : Inconclusive results (baseline saved with inconclusive status)
- `3` : Configuration error
- `4` : Validation error
- `5` : Runtime error (including baseline persistence failure)

**Examples:**
```bash
# Save baseline with auto-generated name
performance-engine baseline save --config test-config.yaml

# Save baseline with specific name
performance-engine baseline save --config test-config.yaml --name "release-1.0-baseline"

# Save and output JSON
performance-engine baseline save --config test-config.yaml --format json
```

---

#### 3. `baseline compare` - Compare Against Baseline

**Purpose**: Execute tests and compare results against an existing baseline.

**Usage:**
```bash
performance-engine baseline compare --config <path> --baseline <id|name> [options]
```

**Options:**
- `--config <path>` : Configuration file path (required)
- `--baseline <id|name>` : Baseline identifier or name to compare against (required)
- `--profile <name>` : Test profile to execute (optional)
- `--format <json|text>` : Output format (default: text)
- `--tolerance <percent>` : Regression tolerance percentage (optional, uses config default)
- `--verbose, -v` : Verbose logging

**Behavior:**
1. Load baseline by ID or name
2. Execute tests (same as `run` command)
3. Compare results against baseline with tolerance thresholds
4. Output comparison results with detected regressions
5. Return exit code based on comparison outcome

**Exit Codes:**
- `0` : No regressions detected (within tolerance)
- `1` : Regressions detected (performance degraded beyond tolerance)
- `2` : Inconclusive comparison (insufficient data, incompatible baseline)
- `3` : Configuration error (invalid config, baseline not found)
- `4` : Validation error
- `5` : Runtime error

**Examples:**
```bash
# Compare against baseline by ID
performance-engine baseline compare --config test-config.yaml --baseline baseline-123

# Compare with custom tolerance
performance-engine baseline compare --config test-config.yaml --baseline "release-1.0" --tolerance 10

# Compare and output JSON
performance-engine baseline compare --config test-config.yaml --baseline baseline-123 --format json
```

---

#### 4. `baseline list` - List Available Baselines

**Purpose**: List saved baselines with metadata.

**Usage:**
```bash
performance-engine baseline list [options]
```

**Options:**
- `--format <json|text>` : Output format (default: text)
- `--count <number>` : Maximum number of baselines to list (default: 10)
- `--verbose, -v` : Show detailed baseline information

**Behavior:**
1. Query repository for recent baselines
2. Output baseline list with metadata (ID, name, timestamp, status)
3. Return exit code 0 on success

**Exit Codes:**
- `0` : List retrieved successfully (even if empty)
- `5` : Runtime error (repository access failed)

**Examples:**
```bash
# List 10 most recent baselines
performance-engine baseline list

# List 50 baselines with JSON output
performance-engine baseline list --count 50 --format json
```

---

#### 5. `validate` - Validate Configuration

**Purpose**: Validate configuration file without executing tests.

**Usage:**
```bash
performance-engine validate --config <path> [options]
```

**Options:**
- `--config <path>` : Configuration file path (required)
- `--format <json|text>` : Output format (default: text)
- `--verbose, -v` : Show detailed validation messages

**Behavior:**
1. Load configuration from all sources (file, env, CLI)
2. Perform full validation (structural, type, range, logic, dependencies)
3. Output validation results (success or list of errors)
4. Return exit code based on validation outcome

**Exit Codes:**
- `0` : Configuration valid
- `3` : Configuration error (structural/syntax errors)
- `4` : Validation error (semantic/logical errors)

**Examples:**
```bash
# Validate configuration
performance-engine validate --config test-config.yaml

# Validate with JSON output
performance-engine validate --config test-config.yaml --format json
```

---

#### 6. `export` - Export Test Results

**Purpose**: Export test results to external formats for analysis.

**Usage:**
```bash
performance-engine export --input <path> --output <path> [options]
```

**Options:**
- `--input <path>` : Input results file (JSON format from previous run)
- `--output <path>` : Output file path
- `--format <format>` : Export format (csv, excel, html) (default: csv)

**Behavior:**
1. Load results from input file
2. Transform to requested export format
3. Write to output file
4. Return exit code based on outcome

**Exit Codes:**
- `0` : Export successful
- `3` : Input file not found or invalid
- `5` : Export failed (write error, format error)

**Examples:**
```bash
# Export to CSV
performance-engine export --input results.json --output results.csv

# Export to HTML report
performance-engine export --input results.json --output report.html --format html
```

---

## Exit Code Specification

### Standard Exit Codes

| Code | Name | Meaning | Use Case |
|------|------|---------|----------|
| 0 | SUCCESS | All operations succeeded, tests passed | CI/CD pipeline passes |
| 1 | FAIL | Tests failed (thresholds violated, regressions detected) | CI/CD pipeline fails |
| 2 | INCONCLUSIVE | Results ambiguous (insufficient data, partial results) | CI/CD pipeline retries or investigates |
| 3 | CONFIG_ERROR | Invalid configuration, missing files, parse errors | User fixes configuration |
| 4 | VALIDATION_ERROR | Validation failures before execution | User fixes validation issues |
| 5 | RUNTIME_ERROR | System errors during execution | User investigates system issues |
| 130 | INTERRUPTED | SIGINT received (Ctrl+C) | User cancelled operation |
| 143 | TERMINATED | SIGTERM received | Process manager terminated |

### Exit Code Mapping Logic

**Application Layer (Result<T, Error>):**

```csharp
public enum ErrorType
{
    ConfigurationError,      // → Exit code 3
    ValidationError,         // → Exit code 4
    RuntimeError,           // → Exit code 5
    TestFailure,            // → Exit code 1
    InconclusiveResult,     // → Exit code 2
}

public record Error
{
    public required ErrorType Type { get; init; }
    public required string Message { get; init; }
    public string? Details { get; init; }
    public string? FieldPath { get; init; } // For validation errors
}

public record Result<T, TError>
{
    public T? Value { get; init; }
    public TError? Error { get; init; }
    public bool IsSuccess => Error == null;
    public bool IsFailure => !IsSuccess;
}
```

**Infrastructure Layer (Exit Code Mapper):**

```csharp
public static class ExitCodeMapper
{
    public static int MapToExitCode<T>(Result<T, Error> result)
    {
        if (result.IsSuccess)
        {
            return 0; // SUCCESS
        }

        return result.Error!.Type switch
        {
            ErrorType.ConfigurationError => 3,
            ErrorType.ValidationError => 4,
            ErrorType.RuntimeError => 5,
            ErrorType.TestFailure => 1,
            ErrorType.InconclusiveResult => 2,
            _ => 5 // Default to runtime error
        };
    }
    
    public static int MapSignalToExitCode(int signal)
    {
        return signal switch
        {
            2 => 130,   // SIGINT
            15 => 143,  // SIGTERM
            _ => 128 + signal // Standard Unix convention
        };
    }
}
```

### CI/CD Integration

**GitHub Actions Example:**

```yaml
- name: Run Performance Tests
  id: perf-test
  run: performance-engine run --config perf-test.yaml --format json --output results.json
  continue-on-error: true

- name: Check Results
  run: |
    EXIT_CODE=${{ steps.perf-test.outcome }}
    if [ $EXIT_CODE -eq 0 ]; then
      echo "✅ Tests passed"
    elif [ $EXIT_CODE -eq 1 ]; then
      echo "❌ Tests failed"
      exit 1
    elif [ $EXIT_CODE -eq 2 ]; then
      echo "⚠️ Inconclusive results - investigating"
      # Post to Slack, create issue, etc.
    elif [ $EXIT_CODE -ge 3 ]; then
      echo "❌ Configuration or runtime error"
      exit 1
    fi
```

## Output Formats

### JSON Output Format

**Success Response:**

```json
{
  "status": "success",
  "exitCode": 0,
  "timestamp": "2026-01-16T08:30:00Z",
  "command": "run",
  "config": {
    "file": "/path/to/config.yaml",
    "profile": "default",
    "schemaVersion": "1.0"
  },
  "results": {
    "totalTests": 5,
    "passed": 5,
    "failed": 0,
    "duration": "5m30s",
    "tests": [
      {
        "name": "api-latency",
        "status": "passed",
        "metrics": {
          "p50": 120,
          "p95": 250,
          "p99": 400
        },
        "thresholds": {
          "p95": {
            "limit": 300,
            "actual": 250,
            "passed": true
          }
        }
      }
    ]
  }
}
```

**Failure Response:**

```json
{
  "status": "fail",
  "exitCode": 1,
  "timestamp": "2026-01-16T08:30:00Z",
  "command": "run",
  "results": {
    "totalTests": 5,
    "passed": 3,
    "failed": 2,
    "violations": [
      {
        "test": "api-latency",
        "metric": "p95",
        "threshold": 300,
        "actual": 450,
        "exceeded": 150
      },
      {
        "test": "error-rate",
        "metric": "errorRate",
        "threshold": 0.01,
        "actual": 0.025,
        "exceeded": 0.015
      }
    ]
  }
}
```

**Error Response:**

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
        "field": "testExecution.duration",
        "value": "-5m",
        "constraint": "must be positive",
        "source": "config.yaml:12"
      },
      {
        "field": "rateLimits.requestsPerSecond",
        "value": "abc",
        "constraint": "must be a number",
        "source": "config.yaml:20"
      }
    ]
  }
}
```

### Text Output Format

**Success Output:**

```
✅ Performance Tests - PASSED

Configuration: /path/to/config.yaml (profile: default)
Executed: 2026-01-16 08:30:00 UTC
Duration: 5m30s

Results Summary:
  Total Tests: 5
  Passed: 5
  Failed: 0

Test Details:
  ✅ api-latency
     p50: 120ms  p95: 250ms  p99: 400ms
     Threshold: p95 < 300ms ✓

  ✅ throughput
     Requests/sec: 1500
     Threshold: > 1000 req/s ✓

  ✅ error-rate
     Error Rate: 0.5%
     Threshold: < 1% ✓

Exit Code: 0 (SUCCESS)
```

**Failure Output:**

```
❌ Performance Tests - FAILED

Configuration: /path/to/config.yaml (profile: load-test)
Executed: 2026-01-16 08:30:00 UTC
Duration: 10m15s

Results Summary:
  Total Tests: 5
  Passed: 3
  Failed: 2

Failed Tests:
  ❌ api-latency
     p95: 450ms (threshold: 300ms) - EXCEEDED by 150ms
     p99: 650ms (threshold: 500ms) - EXCEEDED by 150ms

  ❌ error-rate
     Error Rate: 2.5% (threshold: 1%) - EXCEEDED by 1.5%

Passed Tests:
  ✅ throughput
  ✅ response-time-p50
  ✅ concurrent-users

Exit Code: 1 (FAIL)
```

**Error Output:**

```
❌ Configuration Validation - FAILED

Configuration: /path/to/config.yaml
Validated: 2026-01-16 08:30:00 UTC

Validation Errors (2):

  1. Field: testExecution.duration
     Value: "-5m"
     Error: Duration must be positive
     Location: config.yaml:12

  2. Field: rateLimits.requestsPerSecond
     Value: "abc"
     Error: Must be a number (integer or float)
     Location: config.yaml:20

Fix these errors and try again.

Exit Code: 4 (VALIDATION_ERROR)
```

## Implementation Architecture

### CLI Layer Structure

```
src/PerformanceEngine.CLI/
├── Program.cs                      # Entry point, DI container
├── Commands/
│   ├── RunCommand.cs              # run command handler
│   ├── BaselineSaveCommand.cs     # baseline save handler
│   ├── BaselineCompareCommand.cs  # baseline compare handler
│   ├── BaselineListCommand.cs     # baseline list handler
│   ├── ValidateCommand.cs         # validate handler
│   └── ExportCommand.cs           # export handler
├── OutputFormatters/
│   ├── IOutputFormatter.cs        # Formatter interface
│   ├── JsonOutputFormatter.cs     # JSON implementation
│   ├── TextOutputFormatter.cs     # Text implementation
│   └── OutputFormatterFactory.cs  # Factory pattern
├── Models/
│   ├── CommandOptions.cs          # CLI option models
│   └── OutputModels.cs            # Output response models
└── Infrastructure/
    ├── ExitCodeMapper.cs          # Exit code mapping
    ├── SignalHandler.cs           # SIGINT/SIGTERM handling
    └── ConsoleWriter.cs           # Stdout/stderr abstraction
```

### Command Handler Pattern

```csharp
public interface ICommand
{
    Task<int> ExecuteAsync(CancellationToken cancellationToken);
}

public class RunCommand : ICommand
{
    private readonly IRunTestsUseCase _useCase;
    private readonly IOutputFormatter _formatter;
    private readonly ExitCodeMapper _exitCodeMapper;

    public RunCommand(
        IRunTestsUseCase useCase,
        IOutputFormatter formatter,
        ExitCodeMapper exitCodeMapper)
    {
        _useCase = useCase;
        _formatter = formatter;
        _exitCodeMapper = exitCodeMapper;
    }

    public async Task<int> ExecuteAsync(CancellationToken cancellationToken)
    {
        // 1. Invoke application use case
        var result = await _useCase.ExecuteAsync(
            new RunTestsRequest { ConfigPath = _options.ConfigPath },
            cancellationToken
        );

        // 2. Format output
        var output = _formatter.Format(result);
        Console.WriteLine(output);

        // 3. Map to exit code
        return _exitCodeMapper.MapToExitCode(result);
    }
}
```

### Dependency Injection Setup

```csharp
// Program.cs
public class Program
{
    public static async Task<int> Main(string[] args)
    {
        // Setup DI container
        var services = new ServiceCollection();
        ConfigureServices(services);

        var provider = services.BuildServiceProvider();

        // Parse command
        var parser = provider.GetRequiredService<ICommandParser>();
        var command = parser.Parse(args);

        // Setup signal handling
        var signalHandler = provider.GetRequiredService<SignalHandler>();
        signalHandler.Register();

        // Execute command
        try
        {
            return await command.ExecuteAsync(CancellationToken.None);
        }
        catch (OperationCanceledException)
        {
            return signalHandler.GetExitCode();
        }
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        // Application use cases
        services.AddScoped<IRunTestsUseCase, RunTestsUseCase>();
        services.AddScoped<ISaveBaselineUseCase, SaveBaselineUseCase>();
        services.AddScoped<ICompareBaselineUseCase, CompareBaselineUseCase>();
        services.AddScoped<IValidateConfigUseCase, ValidateConfigUseCase>();

        // CLI infrastructure
        services.AddSingleton<ICommandParser, CommandParser>();
        services.AddSingleton<ExitCodeMapper>();
        services.AddSingleton<SignalHandler>();
        services.AddTransient<IOutputFormatterFactory, OutputFormatterFactory>();

        // Repositories (from infrastructure layer)
        services.AddScoped<IRepository<Baseline, BaselineId>, RedisBaselineRepository>();
        services.AddScoped<IAuditLog, RedisAuditLog>();
        
        // Configuration
        services.AddSingleton<IConfigurationValidator, ConfigurationValidator>();
    }
}
```

## Determinism Guarantees

### FR-023: Identical Output for Identical Input

**Guaranteed by:**
1. Configuration validation is deterministic (same config → same validation result)
2. Test execution uses fixed random seeds where applicable
3. Results are sorted consistently (alphabetically by test name)
4. Timestamps formatted with fixed timezone (UTC)
5. Numeric formatting uses fixed precision (e.g., 2 decimal places)
6. JSON output uses deterministic key ordering

**Implementation:**

```csharp
public class DeterministicOutputFormatter
{
    public string FormatResults(TestResults results)
    {
        // Sort tests alphabetically for consistent ordering
        var sortedTests = results.Tests.OrderBy(t => t.Name);

        // Format with fixed precision
        var json = JsonSerializer.Serialize(sortedTests, new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            DefaultIgnoreCondition = JsonIgnoreCondition.Never,
            // Deterministic serialization
            IncludeFields = false,
            NumberHandling = JsonNumberHandling.Strict
        });

        return json;
    }
}
```

## Testing Strategy

### Unit Tests

- Test command parsing and argument validation
- Test exit code mapping logic
- Test output formatters (JSON schema validation, text formatting)
- Test signal handling

### Integration Tests

- End-to-end CLI execution with real configuration files
- Test all commands with various input scenarios
- Verify exit codes match expected outcomes
- Verify JSON output is valid and parseable
- Verify text output is readable

### Acceptance Tests (from Specification)

- Execute all acceptance scenarios from cli-interface spec
- Verify deterministic behavior (FR-023 to FR-026)
- Verify error handling (FR-027 to FR-032)
- Verify success criteria (SC-001 to SC-010)

## References

- **Specification**: `/specs/001-cli-interface/spec.md`
- **Constitution**: `/.specify/memory/constitution.md`
- **Base Plan**: `/docs/architecture/base-architecture-plan.md`
- **Repository Contracts**: `/docs/architecture/repository-contracts.md`
