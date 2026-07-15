# MCP Tools

The MCP tools exposed by [DevContextMcp.Server](/projects/server.md). Each is a
thin wrapper that builds a typed request and delegates to a Core handler; all
return the common envelope described in [Retrieval Contracts](/retrieval-contracts.md).
Tool names are pinned in `ToolRegistrationCatalog.cs`.

## Tools

* [resolve_library](./resolve_library.md) - find indexed NuGet packages by name or concept.
* [list_versions](./list_versions.md) - list indexed versions and the recommended version.
* [query_docs](./query_docs.md) - find version-scoped documentation and examples.
* [get_symbol](./get_symbol.md) - describe a public type or member.
* [ping](./ping.md) - connectivity/configuration check.

## Resources

* [Resources & citations](./resources.md) - the `nuget://` resource templates and how citation URIs are built.
