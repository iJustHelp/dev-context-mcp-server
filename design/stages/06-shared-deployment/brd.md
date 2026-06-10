# Stage 5 BRD: Shared Deployment

## Purpose

Deploy the MCP documentation service as a secure shared internal service using
Streamable HTTP and source-aware authorization.

## Business Outcome

Authorized teams and coding agents can use one centrally managed, observable
documentation index without receiving evidence from sources they are not
permitted to access.

## Dependencies

- Stage 4 completed.
- Organization identity-provider requirements.
- Hosting platform and deployment standards.
- Defined source ownership and access policies.

## In Scope

- ASP.NET Core host with MCP Streamable HTTP transport.
- Stateless MCP operation where supported.
- Enterprise authentication.
- Tool and source-level authorization.
- Secure secret and credential integration.
- Deployment manifests and environment configuration.
- Production health, readiness, telemetry, and operational runbooks.
- Deployment and authorization tests.

## Functional Requirements

### FR-1: HTTP transport

The ASP.NET Core host must:

- Expose the MCP endpoint using Streamable HTTP.
- Use stateless mode unless a documented feature requires sessions.
- Keep health and readiness endpoints separate from the MCP endpoint.
- Apply request-size, timeout, and concurrency limits.
- Preserve stdio mode for local development.

### FR-2: Authentication

HTTP clients must authenticate through the organization's approved identity
provider.

The service must:

- Validate issuer, audience, signature, and token lifetime.
- Reject unauthenticated MCP requests.
- Avoid logging bearer tokens or sensitive claims.
- Expose only minimal authentication diagnostics.

### FR-3: Authorization

Authorization must apply to:

- MCP tool invocation.
- Library discovery.
- Search results.
- Symbol details.
- MCP resource retrieval.
- Administrative refresh operations.

Filtering must occur before restricted evidence is returned or used to compute
a response. A caller must not learn restricted package or service names through
counts, errors, suggestions, or timing-sensitive detail.

### FR-4: Source policy

Each configured source must have:

- A stable source ID.
- An owner.
- Allowed caller roles, groups, or claims.
- Indexing credentials separate from caller identity.
- An audit classification.

Default policy must deny access when no rule matches.

### FR-5: Secrets

Feed, repository, and database credentials must come from approved
secret-management or workload-identity mechanisms.

Secrets must not be stored in:

- Source control.
- Application settings checked into the repository.
- SQLite evidence records.
- Logs, traces, metrics, or MCP results.

### FR-6: Deployment

Provide deployment assets for the selected hosting platform, including:

- Service definition.
- Configuration and secret references.
- Persistent storage requirements.
- Health and readiness probes.
- Resource requests and limits.
- Network and egress restrictions.
- Upgrade and rollback procedure.

### FR-7: Auditability

Audit events must identify:

- Caller identity or approved pseudonymous identifier.
- Tool or administrative action.
- Source IDs involved.
- Result status.
- Time and correlation ID.

Audit logs must not contain full document bodies.

## Non-Functional Requirements

- Horizontal scaling must not corrupt the index or run duplicate uncontrolled
  refreshes.
- The selected storage and refresh-leader strategy must be documented.
- HTTP endpoints use TLS in deployed environments.
- Authorization checks are covered by deny-by-default tests.
- Deployment supports rolling upgrades and rollback.
- Service-level objectives and alert thresholds are documented.

## Deliverables

- ASP.NET Core Streamable HTTP host.
- Authentication configuration.
- Tool, resource, and source authorization policies.
- Secret-provider integration.
- Deployment manifests.
- Production dashboards and alerts.
- Audit logging.
- Security, deployment, and rollback runbooks.
- End-to-end shared-service tests.

## Acceptance Criteria

1. An authorized MCP client connects over Streamable HTTP and discovers tools.
2. An unauthenticated request is rejected.
3. A caller can retrieve evidence from an allowed source.
4. The same caller cannot discover or retrieve evidence from a denied source.
5. Administrative refresh operations require elevated authorization.
6. Stdio mode still works for local development.
7. Secrets do not appear in configuration artifacts, logs, telemetry, or MCP
   responses.
8. Health and readiness probes behave correctly during startup and refresh.
9. A deployment can be upgraded and rolled back using the documented process.
10. Production telemetry and audit events are available.
11. `dotnet build` and `dotnet test` succeed.

## Out of Scope

- Public internet access.
- Cross-organization federation.
- Billing or usage metering.
- A graphical administration portal.
- Semantic/vector search unless separately approved.

## Exit Gate

The product is ready for controlled internal adoption when security review,
deployment validation, authorization tests, and operational readiness checks
all pass.
