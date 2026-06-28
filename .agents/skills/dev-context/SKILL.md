---
name: dev-context
description: Research internal NuGet packages, unfamiliar .NET APIs, public symbols, and implementation examples through the dev_context MCP server. Use before implementing or reviewing code that depends on internal packages or uncertain .NET APIs. Do not use for unrelated repository work whose APIs are already established locally.
---

# DevContext

Use the live `dev_context` MCP server as the source of truth for indexed NuGet
packages. Inspect project files first when target framework or referenced
package versions affect the query.

Do not skip resolution because a likely package ID or API name is remembered.
Do not infer that an API from another version exists in the selected version.
Do not invent APIs when results are `not_found`, `insufficient_evidence`, or
`not_ready`. State uncertainty and inspect the local repository for additional
evidence.

## Workflow

1. Call `resolve_library` with the package name, client name, type name, or
   implementation concept.
2. Call `list_versions` and select a version compatible with the current
   project. Prefer the project's referenced version when known.
3. Use `query_docs` for implementation guidance, examples, warnings, and usage
   patterns. Keep questions short and topical (about 1–3 words). Long natural-
   language questions match fewer indexed fragments.
4. Pass `projectVersion` from the calling project when known so retrieval stays
   on the referenced package version.
5. Use `get_symbol` only for a specific public type or member.
6. Preserve `citationUri` from `data.fragments`, `data.symbols`, and
   `data.symbol`, and mention important warnings, missing documentation, or
   insufficient evidence. Results are ordered best-first; prefer the first 1–2
   fragments for narrow questions. Code samples appear in `data.fragments` and
   `data.symbols`, not `data.examples`.