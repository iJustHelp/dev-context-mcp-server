# Stage 4 BRD: Hardening

## Purpose

Prepare the local MCP server for dependable day-to-day use by adding refresh,
failure recovery, limits, security controls, telemetry, and end-to-end agent
validation.

## Business Outcome

The service remains correct and diagnosable as indexed sources change, fail,
or contain malformed and adversarial content.

## Dependencies

- Stage 3 completed.
- Representative NuGet sources, including a generated API-client package.
- At least one supported coding-agent client for acceptance testing.

## In Scope

- Scheduled and manually triggered incremental refresh.
- Source-level failure recovery and retention.
- Resource, timeout, and response limits.
- Security validation for all indexed inputs.
- Structured logs, metrics, traces, health, and readiness.
- Performance and resilience testing.
- End-to-end acceptance scenarios with a coding agent.
- Operational documentation for local use.

## Functional Requirements

### FR-1: Incremental refresh

The server must:

- Refresh configured sources on a schedule.
- Support an operator-triggered refresh.
- Use content hashes and source metadata to skip unchanged artifacts.
- Serialize or coordinate overlapping refreshes safely.
- Record the status and outcome of every run.

Tool queries must remain available during refresh.

### FR-2: Failure recovery

A failed source refresh must:

- Preserve the previous successful index.
- Record actionable diagnostics.
- Avoid partially visible source updates.
- Retry according to a bounded policy for transient failures.
- Avoid infinite retry loops for permanent failures.

One failing source must not block unrelated sources.

### FR-3: Limits

Enforce configurable limits for:

- Package and document download size.
- Extracted archive size and entry count.
- Parse and source-fetch duration.
- Search result count.
- Tool response size.
- Concurrent indexing operations.

Limit failures must return clear diagnostics without leaking source content.

### FR-4: Security

Verify and test:

- Remote-host and local-root allowlists.
- ZIP path traversal prevention.
- Decompression-bomb defenses.
- Metadata-only assembly inspection.
- Secret and excluded-path filtering.
- Prompt-injection resistance at the retrieval boundary.

Indexed content that resembles instructions must remain labeled as evidence and
must not alter server behavior.

### FR-5: Observability

Provide:

- Structured logs with correlation identifiers.
- OpenTelemetry traces and metrics.
- Health and readiness checks.
- Index duration, failure, and record-count metrics.
- Tool latency, result count, and result-status metrics.
- Cache and upstream-fetch metrics.

Sensitive document bodies and credentials must not be logged.

### FR-6: Agent acceptance scenarios

Run scripted scenarios proving that an agent can:

- Resolve a package from a concept.
- Select or honor an exact version.
- Find a symbol and use its real signature.
- Resolve a generated API-client package and find a real client method.
- Cite the returned source.
- Decline to invent an unknown API.

Observed failure modes must become automated regression tests where practical.

## Non-Functional Requirements

- Tool availability is maintained during successful and failed refreshes.
- Performance targets from the main specification are measured in CI or a
  repeatable benchmark environment.
- Telemetry does not use source content as high-cardinality labels.
- Security tests use local fixtures and do not require external services.
- Recovery procedures are documented and repeatable.

## Deliverables

- Refresh scheduler and manual trigger.
- Atomic source-update and recovery behavior.
- Centralized limit and timeout policies.
- Security test suite.
- OpenTelemetry instrumentation.
- Health and readiness checks.
- Performance benchmark suite.
- Agent acceptance test report and regressions.
- Local operations documentation.

## Acceptance Criteria

1. Unchanged sources are skipped during incremental refresh.
2. Queries remain available while indexing runs.
3. A failed refresh preserves the previous successful data.
4. One failed source does not prevent other sources from updating.
5. Malicious archive and reference fixtures are rejected.
6. Configured size, duration, result, and response limits are enforced.
7. Health, readiness, metrics, and traces expose useful operational state.
8. No credentials or document bodies appear in logs.
9. All main agent scenarios pass, including the unknown-API scenario.
10. Performance targets are met on the agreed representative dataset.
11. `dotnet build` and `dotnet test` succeed.

## Out of Scope

- Multi-user HTTP access.
- Enterprise identity integration.
- Per-source caller authorization.
- Deployment manifests for a shared environment.
- Semantic/vector search.

## Exit Gate

Stage 5 may begin when the stdio service is stable under refresh, failure,
malformed input, and representative agent workloads.
