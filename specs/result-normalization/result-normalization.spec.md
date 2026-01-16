# Feature Specification: Result Normalization

**Feature Branch**: `001-result-normalization`  
**Created**: 2025-01-23  
**Status**: Draft  
**Input**: User description: "Normalize raw execution engine outputs into standardized domain metrics"

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Basic Engine Output Normalization (Priority: P1)

A performance test tool developer runs a JMeter test and needs to convert the raw JTL output file into standardized domain metrics for storage and analysis. The normalizer reads the JTL file, transforms each result record into domain metric format (response time, throughput, errors), and marks the data quality as COMPLETE when all fields map successfully.

**Why this priority**: This is the foundational capability that enables any downstream metric processing. Without basic normalization, no other features can function.

**Independent Test**: Can be fully tested by providing a valid JTL file as input and verifying the output contains correctly mapped domain metrics with all expected fields (timestamp, response time, success status, label) and COMPLETE quality marker.

**Acceptance Scenarios**:

1. **Given** a valid JMeter JTL output file with standard fields (timestamp, elapsed, label, success), **When** the normalizer processes the file, **Then** each record is transformed into a domain metric with all fields populated and quality marked as COMPLETE
2. **Given** a JTL file with 1000 test results, **When** the normalizer processes it, **Then** exactly 1000 domain metrics are produced with identical timestamps and response times
3. **Given** a JTL file processed twice with identical input, **When** comparing both normalized outputs, **Then** the outputs are identical (deterministic transformation)

---

### User Story 2 - Loss-Aware Mapping with Data Quality Tracking (Priority: P1)

A developer integrates a custom load testing engine that outputs incomplete data (missing error messages or missing request labels). The normalizer transforms available fields to domain metrics but marks the output as PARTIAL and reports which fields could not be mapped, allowing downstream systems to make informed decisions about metric completeness.

**Why this priority**: Data quality awareness is a core semantic requirement. Without tracking completeness, users cannot trust or properly interpret normalized metrics.

**Independent Test**: Can be tested by providing engine output with missing optional fields and verifying the normalizer produces metrics marked as PARTIAL with a clear report of unmappable fields.

**Acceptance Scenarios**:

1. **Given** engine output missing error message fields, **When** the normalizer processes it, **Then** domain metrics are created with available fields populated, quality marked as PARTIAL, and unmappable fields listed in the quality report
2. **Given** engine output with completely unrecognized fields, **When** the normalizer attempts to map it, **Then** it produces metrics with only mappable fields, marks them as PARTIAL or LOSSY, and reports all unrecognized fields
3. **Given** engine output where a required field cannot be inferred, **When** the normalizer processes it, **Then** it does NOT make assumptions or fill default values but marks the metric as PARTIAL and documents the missing required field

---

### User Story 3 - Multi-Format Engine Support (Priority: P2)

A test automation engineer uses multiple load testing tools (JMeter outputting JTL, Gatling outputting JSON, K6 outputting CSV). Each engine produces different formats and field names. The normalizer provides format-specific transformers that all output to the same standardized domain metric structure, enabling consistent downstream processing regardless of source engine.

**Why this priority**: Supporting multiple engines enables broader adoption and makes the system practical for real-world use cases where teams use different tools.

**Independent Test**: Can be tested by providing output files from three different engines (JMeter JTL, Gatling JSON, K6 CSV) and verifying all produce domain metrics with the same structure and field definitions.

**Acceptance Scenarios**:

1. **Given** JMeter JTL, Gatling JSON, and K6 CSV outputs from equivalent test runs, **When** each is normalized, **Then** all produce domain metrics with identical structure (same field names, same units, same timestamp format)
2. **Given** a Gatling JSON output file, **When** the normalizer processes it using the Gatling-specific transformer, **Then** Gatling-specific fields (like scenario names) are correctly mapped to domain metric fields (like labels)
3. **Given** outputs from two different engines with different timestamp formats (epoch milliseconds vs ISO8601), **When** normalized, **Then** both produce domain metrics with timestamps in the same standardized format

---

### User Story 4 - Detailed Unmappable Field Reporting (Priority: P2)

A developer integrating a new custom engine wants to understand which engine-specific fields are not being mapped to domain metrics. The normalizer provides a detailed report listing every unmappable field, its sample values, and frequency, helping the developer decide whether to extend the normalizer or accept data loss.

**Why this priority**: Transparency about data loss enables informed decisions and facilitates system extension for new engine types.

**Independent Test**: Can be tested by providing engine output with custom fields and verifying the normalizer produces a report listing all unmappable fields with their names and sample values.

**Acceptance Scenarios**:

1. **Given** engine output with 5 standard fields and 3 custom fields, **When** the normalizer processes it, **Then** the report lists the 3 unmappable custom fields with their names and at least one sample value each
2. **Given** engine output where an unmappable field appears in 100 out of 100 records, **When** the normalizer reports on it, **Then** the report indicates 100% occurrence frequency for that field
3. **Given** multiple files from the same engine processed over time, **When** reviewing unmappable field reports, **Then** developers can identify consistently unmapped fields that may warrant normalizer extension

---

### User Story 5 - Preserve Maximum Information (Priority: P3)

A power user wants to retain as much engine-specific information as possible even if it doesn't map to standard domain metric fields. The normalizer includes an extensible metadata section in domain metrics where unmapped but potentially useful fields are preserved as key-value pairs, enabling specialized downstream analysis without information loss.

**Why this priority**: Enables advanced use cases and future extensibility without requiring immediate normalizer updates for every engine-specific field.

**Independent Test**: Can be tested by providing engine output with custom metadata fields and verifying they appear in the domain metric's metadata section as key-value pairs.

**Acceptance Scenarios**:

1. **Given** engine output with custom fields like "threadGroup" and "hostname", **When** normalized, **Then** these fields appear in the domain metric metadata section with their original values preserved
2. **Given** engine output with nested JSON structures in custom fields, **When** normalized, **Then** the nested structures are preserved in metadata (potentially as serialized JSON strings) rather than discarded
3. **Given** domain metrics with preserved metadata, **When** downstream systems access them, **Then** they can read both standard fields and metadata without special knowledge of the source engine

---

### Edge Cases

- What happens when the engine output file is empty (zero test results)?
  - Normalizer produces zero domain metrics with a status report indicating empty input
- What happens when the engine output has malformed records (e.g., missing required fields like timestamp)?
  - Malformed records are skipped with errors logged; remaining valid records are normalized; report indicates number of skipped records
- How does the system handle extremely large files (gigabytes of test results)?
  - Normalizer processes records in streaming fashion without loading entire file into memory; may produce metrics incrementally
- What happens when engine output contains duplicate records (identical timestamps and labels)?
  - Duplicates are preserved as separate domain metrics (no deduplication logic); downstream systems decide how to handle duplicates
- How does the system handle timestamp formats it doesn't recognize?
  - If timestamp cannot be parsed, record is marked as PARTIAL/LOSSY and timestamp parsing error is reported; record is not discarded unless timestamp is deemed required
- What happens when response time values are negative or impossibly large?
  - Values are preserved as-is (no validation logic in normalizer); domain metrics include the raw values; downstream evaluation logic handles validation
- How does the system handle engine output with mixed formats in the same file?
  - If format is consistent per file, it's processed normally; if format changes mid-file, processing may fail with clear error; specification assumes one format per input file
- What happens when a field maps to multiple possible domain metric fields?
  - Normalizer uses deterministic mapping rules (same input always maps the same way); ambiguous mappings are resolved by predefined priority rules documented per engine
- How does the system handle encoding issues (non-UTF8 text)?
  - Encoding issues are treated as parsing failures; affected records may be skipped; report indicates encoding problems encountered
- What happens when the domain metric structure needs to evolve (new standard fields added)?
  - Normalizer can be extended to populate new fields; existing domain metrics from old versions remain valid; backward compatibility is maintained

## Requirements *(mandatory)*

### Functional Requirements

#### Core Normalization Contract

- **FR-001**: System MUST accept raw engine output in multiple formats (JTL, JSON, CSV) as input
- **FR-002**: System MUST transform engine-specific output into standardized domain metric structure
- **FR-003**: System MUST ensure deterministic transformation (identical input produces identical output every time)
- **FR-004**: System MUST track data quality for each normalized metric (COMPLETE, PARTIAL, LOSSY)
- **FR-005**: System MUST mark metrics as PARTIAL when one or more optional fields cannot be mapped
- **FR-006**: System MUST mark metrics as LOSSY when required data is missing or unmappable
- **FR-007**: System MUST NOT make assumptions about missing data (no default values for unmappable fields)
- **FR-008**: System MUST preserve as much information as possible from source engine output

#### Field Mapping and Transformation

- **FR-009**: System MUST map engine-specific timestamp formats to a standardized timestamp format
- **FR-010**: System MUST map engine-specific response time fields to standardized response time metric (in milliseconds)
- **FR-011**: System MUST map engine-specific success/failure indicators to standardized success boolean
- **FR-012**: System MUST map engine-specific request labels/names to standardized label field
- **FR-013**: System MUST map engine-specific error messages to standardized error field when available
- **FR-014**: System MUST map engine-specific throughput indicators when available
- **FR-015**: System MUST preserve unmappable fields in a metadata section of the domain metric
- **FR-016**: System MUST use consistent field names and units across all normalized outputs regardless of source engine

#### Data Quality Reporting

- **FR-017**: System MUST generate a report listing all unmappable fields encountered during normalization
- **FR-018**: System MUST include sample values for each unmappable field in the report
- **FR-019**: System MUST track frequency/occurrence count of unmappable fields
- **FR-020**: System MUST report number of successfully normalized records
- **FR-021**: System MUST report number of skipped/failed records with reasons
- **FR-022**: System MUST report overall data quality assessment (percentage of COMPLETE vs PARTIAL vs LOSSY metrics)

#### Engine Format Support

- **FR-023**: System MUST provide format-specific normalizers for JMeter JTL output
- **FR-024**: System MUST provide format-specific normalizers for Gatling JSON output
- **FR-025**: System MUST provide format-specific normalizers for K6 CSV output
- **FR-026**: System MUST support extensibility for adding new engine format normalizers
- **FR-027**: System MUST detect or be told which engine format is being processed
- **FR-028**: System MUST fail gracefully with clear error messages when format detection fails or format is unsupported

#### Processing Behavior

- **FR-029**: System MUST process records in streaming fashion for memory efficiency with large files
- **FR-030**: System MUST skip malformed records and continue processing remaining records
- **FR-031**: System MUST log errors for skipped records with sufficient detail for debugging
- **FR-032**: System MUST handle empty input (zero records) without errors
- **FR-033**: System MUST preserve record order from source engine output when processing sequentially
- **FR-034**: System MUST produce output progressively (not requiring entire file to be processed before outputting first metrics)

### Key Entities

- **RawEngineOutput**: Represents unstructured or semi-structured output from any performance testing engine. Contains engine-specific fields, formats, and conventions. May be in XML (JTL), JSON, CSV, or other formats. Key attributes: format type, source engine identifier, record count, raw content or record stream.

- **DomainMetric**: Standardized representation of a single performance test result. Contains fields common across all engines: timestamp (ISO8601 or epoch milliseconds), response time (milliseconds), success status (boolean), label (string identifier for the request type), error message (string, optional), throughput indicators (optional), and metadata (key-value map for preserving unmapped fields). Includes data quality marker (COMPLETE, PARTIAL, LOSSY).

- **Normalizer**: Component responsible for transforming engine-specific output to domain metric format. Engine-specific implementations exist for each supported format (JMeterNormalizer, GatlingNormalizer, K6Normalizer). Encapsulates mapping rules, field transformations, and format parsing logic.

- **DataQualityMarker**: Enumeration indicating completeness of normalized metric. Values: COMPLETE (all expected fields successfully mapped), PARTIAL (some optional fields missing or unmappable), LOSSY (required fields missing or degraded). Attached to each domain metric.

- **NormalizationReport**: Summary of normalization process. Contains: total records processed, successful transformations count, skipped records count, data quality distribution (counts of COMPLETE/PARTIAL/LOSSY), list of unmappable fields with sample values and frequencies, errors encountered with details.

- **UnmappableField**: Represents a field from engine output that could not be mapped to domain metric structure. Attributes: field name, sample values (up to N examples), occurrence count, occurrence frequency (percentage).

- **FieldMapping**: Defines transformation rule from engine-specific field to domain metric field. Attributes: source field name, target field name, transformation function (e.g., timestamp parsing, unit conversion), required vs optional flag.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Normalizer correctly transforms 100% of valid records from supported engine formats (JMeter JTL, Gatling JSON, K6 CSV) into domain metrics
- **SC-002**: Identical input files processed multiple times produce byte-for-byte identical normalized output (deterministic transformation)
- **SC-003**: Data quality markers correctly reflect completeness: 95% or more of metrics from well-formed engine output are marked as COMPLETE
- **SC-004**: Processing time for normalizing engine output is linear with record count (O(n) complexity) and handles files with 1 million+ records
- **SC-005**: Memory usage remains constant regardless of input file size (streaming processing works correctly)
- **SC-006**: Normalization reports include 100% of unmappable fields with accurate sample values and frequencies
- **SC-007**: System correctly handles edge cases (empty files, malformed records, missing fields) without crashes or data corruption
- **SC-008**: Less than 1% of valid records are skipped due to normalization errors for well-formed engine output
- **SC-009**: Developers can add support for a new engine format by implementing a single normalizer interface without modifying core normalization logic
- **SC-010**: Normalized domain metrics from different engines can be processed by downstream systems without engine-specific logic (format unification successful)
- **SC-011**: No information loss: 100% of mappable fields are successfully transformed, and 100% of unmappable fields are preserved in metadata or reported
- **SC-012**: Processing completes within 5 seconds per 10,000 records on standard hardware

## Assumptions

- Input files are available in their entirety before normalization begins (no real-time streaming from live test execution)
- Each input file contains output from a single engine type (no mixed formats within one file)
- Engine output formats follow documented or commonly observed structures (JMeter JTL with standard fields, Gatling JSON with standard schema, K6 CSV with standard columns)
- Timestamp precision of milliseconds is sufficient for all use cases
- Response time measurements from engines are in milliseconds or can be converted to milliseconds
- Files are encoded in UTF-8 or a detectable encoding
- The domain metric structure is sufficient to represent core performance metrics across all supported engines
- Downstream systems can handle domain metrics with PARTIAL or LOSSY quality markers appropriately
- Processing performance requirements (5 seconds per 10,000 records) are based on typical hardware (modern CPU, adequate RAM, SSD storage)
- Normalizers will be implemented for one engine at a time based on priority (JMeter first as most common)

## Scope Boundaries

### In Scope

- Parsing and transforming engine-specific output formats (JTL, JSON, CSV)
- Mapping engine fields to standardized domain metric structure
- Tracking and reporting data quality and completeness
- Preserving unmappable information in metadata
- Generating normalization reports with unmappable fields
- Supporting multiple engine types through extensible architecture
- Deterministic and loss-aware transformation semantics
- Memory-efficient streaming processing for large files
- Error handling for malformed records

### Out of Scope

- Implementation details of parsing libraries (CSV parser, XML parser, JSON parser selection)
- Aggregation or statistical analysis of metrics (belongs in metrics-domain)
- Evaluation of metrics against thresholds or SLOs (belongs in evaluation-domain)
- Storage or persistence of normalized metrics (belongs in repository-port)
- Real-time streaming of live test results (this feature handles batch/file-based processing)
- Data validation beyond format parsing (e.g., checking if response times are realistic)
- Deduplication of records (preserves all records as-is)
- Custom field mapping configuration by end users (uses predefined mappings per engine)
- Compression or decompression of input files (assumes files are already decompressed)
- Multi-threaded or parallel processing of records (sequential streaming is sufficient)
- Network I/O for fetching remote result files (assumes local file access)
- Authentication or authorization for accessing result files
- Versioning of domain metric schema (assumes single current version)

## Dependencies

- Access to engine output files (JTL, JSON, CSV) in readable format
- Knowledge of engine-specific output formats and field semantics
- Downstream systems must be able to consume domain metrics with quality markers
- Standard parsing capabilities for XML, JSON, and CSV formats (provided by language ecosystem)

## Risks and Mitigations

**Risk**: Engine output format changes over time (JMeter updates JTL schema)  
**Mitigation**: Version detection in normalizers; support multiple versions of same engine format; clear error messages when unsupported version detected

**Risk**: Unmappable fields contain critical information that shouldn't be lost  
**Mitigation**: Preserve all unmappable fields in metadata section; generate detailed reports; allow downstream systems to access metadata

**Risk**: Large files cause memory issues despite streaming approach  
**Mitigation**: Process records one at a time without buffering; limit metadata sample sizes; test with multi-gigabyte files

**Risk**: Ambiguous field mappings lead to incorrect transformations  
**Mitigation**: Document mapping rules clearly; use deterministic priority rules; include extensive test coverage with known inputs/outputs

**Risk**: New engines have completely different metric concepts that don't map to domain structure  
**Mitigation**: Domain metric structure includes flexible metadata; can be extended if truly new concepts emerge; prioritize common metrics first

## Standard Domain Metric Structure

The following defines the standardized domain metric structure that all normalizers produce:

### Core Fields (Common to All Engines)

- **timestamp**: ISO8601 format or epoch milliseconds (standardized to one format)
- **responseTime**: Duration in milliseconds (decimal allowed for sub-millisecond precision)
- **success**: Boolean indicating whether request succeeded
- **label**: String identifier for request type/endpoint/transaction
- **errorMessage**: Optional string containing error details if success=false
- **statusCode**: Optional HTTP status code or engine-specific status indicator

### Extended Fields (Engine-Dependent Availability)

- **threadName**: Identifier for virtual user/thread that executed request
- **dataSize**: Request or response payload size in bytes
- **latency**: Time to first byte in milliseconds
- **connectTime**: Connection establishment time in milliseconds
- **sentBytes**: Number of bytes sent in request
- **receivedBytes**: Number of bytes received in response

### Metadata Section

- **metadata**: Key-value map containing:
  - Unmappable engine-specific fields preserved as strings
  - Engine identifier (e.g., "jmeter-5.4", "gatling-3.8")
  - Original field names for mapped fields (for traceability)
  - Any additional context-specific information

### Quality Indicator

- **dataQuality**: Enumeration with values:
  - **COMPLETE**: All expected fields successfully mapped
  - **PARTIAL**: Some optional fields missing or unmappable
  - **LOSSY**: Required fields missing or transformation degraded

## Normalization Contract

The normalizer adheres to the following contract:

1. **Input**: RawEngineOutput (format-specific file or stream)
2. **Output**: Stream of DomainMetric objects + NormalizationReport
3. **Semantics**:
   - Deterministic: Same input always produces same output
   - Loss-aware: Track and report when data cannot be mapped
   - Preserving: Unmappable data stored in metadata when possible
   - No assumptions: Never fill missing data with defaults
   - Progressive: Output metrics as soon as they're normalized (don't wait for entire file)
4. **Error Handling**:
   - Malformed records are skipped with errors logged
   - Parsing errors reported in NormalizationReport
   - Processing continues despite individual record failures
   - Empty input produces empty output (not an error)
5. **Performance**:
   - O(n) time complexity with record count
   - O(1) memory complexity (constant memory regardless of file size)
   - Suitable for files with millions of records

