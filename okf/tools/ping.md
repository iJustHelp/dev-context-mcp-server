---
type: MCP Tool
title: ping
description: Minimal connectivity and configuration check that echoes the calling user from the X-User-Name header.
resource: src/DevContextMcp.Server/Tools/PingTool.cs
tags: [tool, diagnostics, connectivity]
timestamp: 2026-07-15T00:00:00Z
---

# ping

A minimal diagnostic tool to verify connectivity and configuration. It does not
touch the index.

# Schema

No inputs. Reads the `X-User-Name` request header (via `IHttpContextAccessor`)
and replies with a greeting.

**Output:** `PingResponse(Message, User)` — e.g. `Message = "Hi, alice!"`,
`User = "alice"`. When the header is absent, `User` falls back to `"unknown user"`.

# Examples

```text
# Request header: X-User-Name: alice
ping  →  { "message": "Hi, alice!", "user": "alice" }
```

The same `X-User-Name` header attributes tool calls in [Analytics](../analytics.md).

# Citations

[1] [PingTool.cs](../../src/DevContextMcp.Server/Tools/PingTool.cs)
