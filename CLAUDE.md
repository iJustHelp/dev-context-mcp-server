# CLAUDE.md

Guidance for Claude Code / coding agents in this repository.

See [`AGENTS.md`](AGENTS.md) for the full guide. In short:

- **Start with the OKF knowledge bundle: [`okf/index.md`](okf/index.md).** It maps
  the solution and cites the real source files. Read the actual source before
  editing; if a doc disagrees with the code, the code wins (then fix the doc).
- **Respect the hard constraints** (enforced by architecture tests): dependency
  direction across the projects, Indexer-writes / Server-reads-only, never execute
  package assemblies, environment + version isolation, idempotent re-indexing.
  Details in [`AGENTS.md`](AGENTS.md) and [`okf/architecture.md`](okf/architecture.md).
- **Build/test:** `dotnet build .\DevContextMcp.slnx` / `dotnet test .\DevContextMcp.slnx`.
  If a full build hits DLL copy locks (server exe or VS running), use
  `-p:BuildProjectReferences=false`.
- **Keep docs in sync:** when behavior changes, update the relevant `okf/*.md`,
  add an [`okf/log.md`](okf/log.md) entry, and list new docs in [`okf/index.md`](okf/index.md).
