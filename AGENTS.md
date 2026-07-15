# Agent Guide — DevContextMcp

A .NET 10 Model Context Protocol server that gives coding agents grounded,
version-aware access to internal NuGet packages (indexes package docs + public
symbols into SQLite/FTS5; serves them read-only over MCP).

## Start here: the OKF knowledge bundle

Before writing or reviewing code, read [`okf/index.md`](okf/index.md) — a
knowledge bundle describing this solution. It is organized as Overview, MCP
surface, Subsystems, Configuration & standards, and Projects. Each doc cites the
real source files, so use it as a fast index into the code, then read the actual
source before editing. If a doc disagrees with the code, **the code wins** (and
please fix the doc).

Especially useful:
- [`okf/architecture.md`](okf/architecture.md) — project roles + the enforced dependency graph.
- [`okf/data-flows.md`](okf/data-flows.md) — indexing (write) vs retrieval (read) paths.
- [`okf/tools/index.md`](okf/tools/index.md) + [`okf/retrieval-contracts.md`](okf/retrieval-contracts.md) — MCP tool inputs/outputs and the response/outcome model.
- [`okf/database-schema.md`](okf/database-schema.md), [`okf/security-model.md`](okf/security-model.md), [`okf/glossary.md`](okf/glossary.md).

## Hard constraints (violating these causes real bugs)

- **Dependency direction is enforced by architecture tests.** `Server.Core` and
  `Indexer.Core` have no project references; `Infrastructure` → both cores;
  `Server` (host) → Server.Core + Infrastructure; `Indexer` (CLI) → Indexer.Core
  + Infrastructure. Keep MCP contracts in `Server.Core`; put external/IO concerns
  in `Infrastructure`.
- **The Indexer is the sole writer; the Server is read-only.** Never make the host
  write to the index or contact NuGet feeds.
- **Never load or execute package assemblies.** Symbols are read via metadata
  APIs only (`PEReader`/`MetadataReader`). Treat package archives as untrusted.
- **Environment + version isolation.** `nuget:{env}/{package}` ids never cross
  environments; evidence is never combined across versions.
- **Idempotency.** Content hashes/deterministic ids keep re-indexing safe; don't
  break that when touching persistence.

## Build / test / run

```powershell
dotnet build .\DevContextMcp.slnx
dotnet test  .\DevContextMcp.slnx
dotnet run --project .\src\DevContextMcp.Indexer\DevContextMcp.Indexer.csproj   # build the index
dotnet run --project .\src\DevContextMcp.Server\DevContextMcp.Server.csproj     # serve MCP (default http://127.0.0.1:2222/mcp)
```

- **Build-lock workaround:** full builds can fail with DLL copy locks when the
  server exe or Visual Studio is running. Use `-p:BuildProjectReferences=false`.
- Company C#/test/architecture conventions live as agent skills under
  `.claude/skills/` — see [`okf/coding-standards.md`](okf/coding-standards.md).

## When you change things

- Keep the OKF bundle in sync when behavior changes: update the relevant
  `okf/*.md`, add a dated entry to [`okf/log.md`](okf/log.md), and register any
  new doc in [`okf/index.md`](okf/index.md).
- Prefer pointer/constraint content over transcribing code (schema/signature
  tables drift fastest).
