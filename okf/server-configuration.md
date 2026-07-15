---
type: Configuration
title: Server Configuration
description: appsettings.json sections that configure DevContextMcp.Server transport, retrieval behavior, analytics, and logging.
resource: src/DevContextMcp.Server/appsettings.json
tags: [configuration, server, host, mcp, appsettings]
timestamp: 2026-07-15T00:00:00Z
---

# Server Configuration

Configuration for [DevContextMcp.Server](/projects/server.md) comes from the
`DevContextMcp` section of `appsettings.json` (plus normal .NET configuration
overrides). The host opens the SQLite index read-only and never contacts NuGet
feeds; it must point at the same database the indexer writes.

The server selects **stdio** or **stateless Streamable HTTP** transport. The
checked-in development configuration uses HTTP via `McpUrl`.

# Schema

## `DevContextMcp` section

| Key | Type | Notes |
|-----|------|-------|
| `DatabasePath` | string | Path to the SQLite index database. Must be the same database the indexer writes to. |
| `McpUrl` | string | Full Streamable HTTP endpoint including path. Must be an unauthenticated loopback `http://` URL with a path and no query/fragment. Loopback, local development only. |
| `Retrieval` | object | Behavior for documentation and symbol queries. |
| `ToolLogging` | object | Diagnostic payload logging. |
| `Analytics` | object | Tool-usage analytics capture. |

### `Retrieval`

| Key | Type | Default | Notes |
|-----|------|---------|-------|
| `EnvironmentOrder` | string[] | `["qa","prod","public"]` | Ordered fallback for lookups with no environment; first is the default. |
| `DefaultMaxResults` | int | `8` | Default results returned by `query_docs`. |
| `MaxResults` | int | `25` | Maximum results allowed in any query response. |
| `MaxResponseBytes` | int | `102400` | Maximum total response size before truncation. |
| `QueryTimeout` | timespan | `00:00:05` | Maximum time for a single query operation. |
| `MinimumEvidenceScore` | double | `0.15` | Minimum relevance score (0.0–1.0) to include a result. |
| `AmbiguousSymbolLimit` | int | `10` | Maximum symbol candidates returned when a lookup is ambiguous. |

### `ToolLogging`

| Key | Type | Default | Notes |
|-----|------|---------|-------|
| `MaxPayloadBytes` | int | `32768` | Max request/response payload size included in logs; larger payloads are truncated. |

### `Analytics`

| Key | Type | Default | Notes |
|-----|------|---------|-------|
| `Enabled` | bool | `true` | Whether tool-invocation analytics are captured. |
| `DatabasePath` | string | — | Path to the host-owned analytics database. |
| `UserHeaderName` | string | `X-User-Name` | Request header used to attribute invocations to a user. |

## Logging

The server configures a `Serilog` block (Console + File sinks). Console writes
to standard error from `Verbose` so stdio transport keeps stdout clean for the
MCP protocol. Default level `Information`; `DevContextMcp.Server.Tools` raised to
`Debug`, `Microsoft` lowered to `Warning`. File sink rolls daily under
`logs/server-.log`, retaining 14 files with a 10 MB size limit.

# Examples

`appsettings.json` (`DevContextMcp` section, checked-in development configuration):

```json
"DevContextMcp": {
  "DatabasePath": "../../../../../database/docs.db",
  "McpUrl": "http://127.0.0.1:2222/mcp",
  "Retrieval": {
    "EnvironmentOrder": [ "qa", "prod", "public" ],
    "DefaultMaxResults": 8,
    "MaxResults": 25,
    "MaxResponseBytes": 102400,
    "QueryTimeout": "00:00:05",
    "MinimumEvidenceScore": 0.15,
    "AmbiguousSymbolLimit": 10
  },
  "ToolLogging": {
    "MaxPayloadBytes": 32768
  },
  "Analytics": {
    "Enabled": true,
    "DatabasePath": "../../../../../database/analytics.db",
    "UserHeaderName": "X-User-Name"
  }
}
```

See [MCP Surface](/mcp-surface.md) for the tools this server exposes,
[Indexer Configuration](/indexer-configuration.md) for the writer side, and
[Database Schema](/database-schema.md) for the databases it reads and writes.

# Citations

[1] [appsettings.json](../src/DevContextMcp.Server/appsettings.json)
[2] [Server configuration](../docs/server-configuration.md)
