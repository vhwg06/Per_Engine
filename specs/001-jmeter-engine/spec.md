# Feature Specification: JMeter Execution Engine

**Feature Branch**: `001-jmeter-engine`  
**Created**: 2025-01-18  
**Status**: Draft  
**Input**: User description: "JMeter execution engine implementation as specialization of execution-engine contract"

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Basic JMeter Test Execution (Priority: P1)

A developer needs to execute a JMeter test plan (JMX file) and receive the raw output artifacts for further processing. The engine accepts a JMeter execution request containing the test plan path and configuration, invokes JMeter with the appropriate parameters, and returns the execution result with paths to generated artifacts (JTL files, logs).

**Why this priority**: This is the core capability of the engine - executing JMeter tests and capturing outputs. Without this, no other functionality is possible. This establishes the fundamental contract implementation.

**Independent Test**: Can be fully tested by providing a simple JMX test plan, invoking the execute method, and verifying that JMeter runs successfully and produces expected JTL and log files at specified locations.

**Acceptance Scenarios**:

1. **Given** a valid JMX test plan file and an execution request, **When** the engine executes the test, **Then** JMeter runs to completion and returns a result with status "SUCCESS" and paths to JTL output and log files
2. **Given** a JMX test plan with 100 virtual users and 1000 requests, **When** the engine executes the test, **Then** the JTL file contains exactly 1000 result entries and the log file captures JMeter's execution trace
3. **Given** an execution request with custom JMeter properties (thread count, ramp-up, duration), **When** the engine executes the test, **Then** JMeter runs with those exact parameters and the behavior is reflected in the output artifacts

---

### User Story 2 - JMeter Error Handling and Status Mapping (Priority: P2)

When JMeter encounters errors during execution (invalid JMX, missing resources, runtime failures), the engine must map JMeter's exit codes and error states to the execution-engine contract's standardized result states (SUCCESS, FAILURE, ERROR) so downstream components can handle failures appropriately.

**Why this priority**: Proper error handling is critical for reliability but can be implemented after basic execution works. It enables robust integration with the rest of the system.

**Independent Test**: Can be fully tested by providing various failure scenarios (malformed JMX, missing CSV files, invalid configuration) and verifying that each produces the correct ExecutionResult status and includes JMeter error messages in the result.

**Acceptance Scenarios**:

1. **Given** a malformed JMX file with XML syntax errors, **When** the engine attempts execution, **Then** returns ExecutionResult with status "ERROR" and error message indicating JMX parsing failure
2. **Given** a valid JMX that references a non-existent CSV data file, **When** the engine executes the test, **Then** JMeter fails with a clear error, the result status is "FAILURE", and the error details are captured in the result
3. **Given** JMeter exits with non-zero exit code during test execution, **When** the engine processes the exit code, **Then** the result status correctly reflects the failure type (FAILURE for test assertions, ERROR for JMeter crashes)

---

### User Story 3 - Reproducible Execution Environment (Priority: P3)

To ensure consistent test results across executions, the engine must provide deterministic JMeter invocation with controlled environment settings, working directory management, and isolation from system-level JMeter configurations.

**Why this priority**: Reproducibility is important for reliable performance testing but can be added after core execution and error handling work. It ensures tests run consistently regardless of where they execute.

**Independent Test**: Can be fully tested by executing the same test plan multiple times with identical parameters and verifying that JMeter is invoked with the same command-line arguments, environment variables, and produces outputs in predictable locations.

**Acceptance Scenarios**:

1. **Given** the same JMX file and execution request executed twice, **When** the engine runs both executions, **Then** both invocations use identical JMeter command-line parameters and produce artifacts with consistent naming conventions
2. **Given** an execution request specifying a working directory, **When** the engine executes the test, **Then** all output artifacts are written to that directory and JMeter runs with that directory as its working path
3. **Given** system-level JMeter configuration files in user home directory, **When** the engine executes a test, **Then** the execution is isolated and not affected by system-level settings unless explicitly specified in the request

---

### Edge Cases

- What happens when JMeter process is killed mid-execution (SIGKILL, system crash)?
  - Engine should detect abnormal termination and return ERROR status with indication of incomplete execution
  
- What happens when JTL output file path is not writable (permissions, disk full)?
  - JMeter will fail to start or fail during execution; engine maps this to ERROR status with filesystem error details
  
- What happens when JMX file contains remote execution configuration (distributed testing)?
  - Engine executes JMeter as specified; remote execution is a JMeter concern, not engine's concern. Result includes all artifacts JMeter produces.
  
- What happens when test plan generates extremely large JTL files (multi-GB)?
  - Engine completes execution and returns file paths; handling large files is the responsibility of consumers (jtl-adapter). Engine may implement streaming or chunked artifact handling as enhancement.
  
- What happens when JMeter stdout/stderr exceed buffer limits?
  - Engine must capture all output to log files rather than memory buffers; result includes paths to complete log files.
  
- What happens when concurrent execution requests are made?
  - Each execution runs in isolated working directory with unique artifact paths; concurrent executions do not interfere with each other.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: JMeterEngine MUST implement the Engine interface defined by the execution-engine contract
- **FR-002**: JMeterEngine MUST accept JMeterExecutionRequest containing test plan path, output directory, and optional JMeter configuration parameters
- **FR-003**: JMeterEngine MUST invoke Apache JMeter command-line interface with parameters derived from the execution request
- **FR-004**: JMeterEngine MUST capture all JMeter output artifacts including JTL result files and execution logs
- **FR-005**: JMeterEngine MUST return JMeterExecutionResult containing execution status, artifact file paths, and any error messages
- **FR-006**: JMeterEngine MUST map JMeter exit codes to ExecutionResult status values (0 → SUCCESS, non-zero → FAILURE or ERROR based on exit code semantics)
- **FR-007**: JMeterEngine MUST preserve all JMeter command-line output (stdout/stderr) in log files referenced by the execution result
- **FR-008**: JMeterEngine MUST ensure each execution writes artifacts to isolated output directories to prevent conflicts
- **FR-009**: JMeterEngine MUST validate that the JMX test plan file exists and is readable before attempting execution
- **FR-010**: JMeterEngine MUST provide deterministic JMeter invocation with consistent command-line parameters for reproducible results
- **FR-011**: JMeterEngine MUST support configurable JMeter home directory and Java executable paths to support different JMeter installations
- **FR-012**: JMeterEngine MUST pass custom JMeter properties from the execution request to JMeter via -J flags or property files
- **FR-013**: JMeterEngine MUST capture JMeter process exit code and include it in the execution result for diagnostic purposes
- **FR-014**: JMeterEngine MUST handle JMeter process failures (crashes, kills) and return ERROR status with diagnostic information
- **FR-015**: JMeterEngine MUST NOT parse, transform, or aggregate JTL file contents - it only captures and references the raw artifact paths

### Key Entities

- **JMeterEngine**: Adapter implementing the execution-engine contract for Apache JMeter. Responsible for process invocation, artifact capture, and status mapping. Attributes: JMeter installation path, default properties, execution timeout.

- **JMeterExecutionRequest**: Specialization of ExecutionRequest containing JMeter-specific parameters. Attributes: JMX test plan path, output directory path, JMeter properties (map of key-value pairs), JMeter home directory (optional), additional JMeter CLI flags (optional).

- **JMeterExecutionResult**: Specialization of ExecutionResult containing JMeter-specific output. Attributes: execution status (SUCCESS/FAILURE/ERROR), JTL file path (primary result artifact), JMeter log file path, JMeter stdout/stderr log paths, JMeter exit code, start timestamp, end timestamp, error message (if applicable).

- **JMeterInvocation**: Internal representation of the JMeter command to execute. Attributes: JMeter executable path, command-line arguments array, working directory, environment variables, timeout duration.

- **ArtifactPaths**: Collection of file paths for all artifacts produced by JMeter execution. Attributes: JTL result file, jmeter.log file, stdout capture file, stderr capture file, any additional logs JMeter generates.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: JMeterEngine successfully executes a sample JMX test plan and produces a valid JTL file containing all request/response results
- **SC-002**: JMeterEngine correctly maps all documented JMeter exit codes (0, 1, 2, 3) to appropriate ExecutionResult status values with 100% accuracy
- **SC-003**: Execution artifacts (JTL, logs) from 10 concurrent test executions are isolated with zero file conflicts or overwrites
- **SC-004**: JMeterEngine captures complete JMeter output with zero data loss for test executions generating up to 10MB of log output
- **SC-005**: Same JMX test plan executed 5 times with identical parameters produces identical JMeter command-line invocations (verifiable via process monitoring)
- **SC-006**: JMeterEngine handles JMeter failures (invalid JMX, missing resources, JMeter crashes) and returns ERROR status with diagnostic messages in 100% of failure scenarios
- **SC-007**: Execution of a 5-minute JMeter test completes within 5 minutes plus 10 seconds overhead for process startup and artifact collection
- **SC-008**: All JMeter artifacts referenced in JMeterExecutionResult exist on filesystem and are readable after execution completes

## Architectural Context

### Relationship to Execution-Engine Contract

JMeterEngine is a concrete adapter that implements the generic execution-engine port interface. This follows the hexagonal/ports-and-adapters architecture pattern where:

- **execution-engine** defines the abstract contract (Engine interface, ExecutionRequest, ExecutionResult)
- **jmeter-engine** provides the JMeter-specific implementation of that contract
- Consumers depend on the execution-engine port, not directly on jmeter-engine
- JMeterEngine translates between JMeter's command-line interface and the generic execution contract

### Integration Points

**Upstream Dependencies (what JMeterEngine depends on)**:
- **execution-engine contract**: Defines the Engine interface that JMeterEngine implements
- **Apache JMeter**: External tool that JMeterEngine invokes via command-line

**Downstream Consumers (what depends on JMeterEngine)**:
- **jtl-adapter**: Consumes the JTL file paths from JMeterExecutionResult to parse and normalize results
- **result-normalization**: Uses execution status and artifacts to produce normalized performance metrics
- **evaluation-domain**: Receives execution results to determine pass/fail based on thresholds
- **testplan-generation**: Provides the JMX test plans that JMeterEngine executes

### Scope Boundaries

**In Scope (JMeterEngine responsibilities)**:
- Process invocation: Starting JMeter with correct command-line arguments
- Artifact capture: Recording paths to all output files JMeter produces
- Status mapping: Translating JMeter exit codes to ExecutionResult states
- Error propagation: Capturing and reporting JMeter errors
- Execution isolation: Ensuring concurrent executions don't conflict

**Out of Scope (handled by other components)**:
- JTL parsing and interpretation → jtl-adapter's responsibility
- Metric calculation and aggregation → result-normalization's responsibility
- Performance evaluation and thresholds → evaluation-domain's responsibility
- JMX test plan generation → testplan-generation's responsibility
- Test plan validation beyond file existence → JMeter's responsibility
- Result storage and persistence → persistence layer's responsibility

### Assumptions

- Apache JMeter is installed and accessible on the system where JMeterEngine runs
- JMeter version supports command-line execution with standard flags (-n, -t, -l, -j, -J)
- Filesystem has sufficient space for JTL output files (engine does not enforce disk space quotas)
- JMX test plans are well-formed XML files (validation happens at JMeter level, not engine level)
- Execution timeout is configurable but defaults to "unlimited" - long-running tests are expected and supported
- Standard JMeter exit code conventions: 0=success, 1=test errors, 2=invalid parameters, 3=other errors
- Working directory has write permissions for creating output artifacts
- JMeter is configured for non-GUI execution mode (this is a command-line adapter, not a GUI wrapper)
