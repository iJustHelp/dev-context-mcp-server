---
type: Reference
title: Retrieval Contracts & Outcome Model
description: The common tool-response envelope, machine-readable status values, and the error/warning codes tools emit.
resource: src/DevContextMcp.Server.Core/Contracts/Common/ToolResponse.cs
tags: [contracts, envelope, status, errors, warnings]
timestamp: 2026-07-15T00:00:00Z
---

# Retrieval Contracts & Outcome Model

Every MCP documentation tool returns a common envelope with a machine-readable
status, so agents can branch on outcome without parsing prose. Tool-level errors
are returned as data on the envelope rather than as protocol errors. See
[MCP Tools](/tools/index.md) for the per-tool payloads and [MCP Surface](/mcp-surface.md)
for the tool list.

# Schema

## Envelope — `ToolResponse<TData>`

| Field | Type | Notes |
|-------|------|-------|
| `status` | `ToolResultStatus` | Machine-readable outcome (default `not_ready`). |
| `data` | `TData?` | Tool-specific payload; may be null when none is available. |
| `resolvedContext` | `ResolvedContext?` | Source/version context searched (libraryId, environment, version, version-selection reason). |
| `evidence` | `EvidenceItem[]?` | Optional ranked evidence; omitted on normal successful retrieval (use ordered `data` items + their `citationUri`). |
| `citations` | `Citation[]?` | Optional deduplicated citations; omitted on normal success. |
| `warnings` | `ToolWarning[]` | Non-fatal warnings (default empty). |
| `errors` | `ToolError[]` | Tool-level errors returned as data (default empty). |

Each tool derives a concrete response, e.g. `ResolveLibraryResponse : ToolResponse<ResolveLibraryResult>`.

## Status — `ToolResultStatus` (JSON string enum)

| Value | Meaning |
|-------|---------|
| `not_ready` | The tool contract exists but the capability is planned for a later stage. |
| `ok` | The tool found evidence and returned a normal result. |
| `not_found` | The requested package, version, operation, or symbol was not found. |
| `insufficient_evidence` | The index held too little reliable evidence to answer. |
| `error` | The tool failed before it could return a normal response. |

## Common error / warning codes

Produced by `RetrievalHandlerSupport` and the per-tool handlers:

- `not_found` codes: `invalid_library_id`, `environment_not_found`,
  `library_not_found`, `version_not_found` / `stable_version_not_found`.
- `insufficient_evidence`: nothing cleared `MinimumEvidenceScore`, or a query timeout.
- `error`: `index_unavailable` (mapped from `IndexUnavailableException`).
- Warnings: `response_truncated` (from `ResponseBudget` enforcing max count/bytes),
  `recommended_version_not_indexed` (see [Version Resolution](/version-resolution.md)).

# Citations

[1] [ToolResponse.cs](../src/DevContextMcp.Server.Core/Contracts/Common/ToolResponse.cs)
[2] [ToolResultStatus.cs](../src/DevContextMcp.Server.Core/Contracts/Common/ToolResultStatus.cs)
[3] [RetrievalHandlerSupport.cs](../src/DevContextMcp.Server.Core/Services/RetrievalHandlerSupport.cs)
