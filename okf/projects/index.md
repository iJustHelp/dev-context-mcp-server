# Projects

The projects in the DevContextMcp repository — the eight .NET projects in
`DevContextMcp.slnx`, plus the `ui/` web app.

## Source

* [DevContextMcp.Server.Core](./server-core.md) - MCP wire contracts, retrieval models/abstractions, and retrieval services (application library).
* [DevContextMcp.Indexer.Core](./indexer-core.md) - source-neutral indexing models, ports, and IIndexCoordinator orchestration (domain library).
* [DevContextMcp.Infrastructure](./infrastructure.md) - SQLite/FTS5 retrieval, NuGet access, safe archive/symbol extraction, persistence.
* [DevContextMcp.Server](./server.md) - MCP executable and retrieval composition root; read-only over the index.
* [DevContextMcp.Indexer](./indexer.md) - one-shot indexing executable and sole index writer.

## Web

* [ui (Analytics Dashboard)](./ui.md) - Next.js dashboard for analytics and indexed-context (not part of the .slnx).

## Tests

* [Test projects](./tests.md) - unit and integration test projects and their coverage.
