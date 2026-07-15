---
okf_version: "0.1"
---

# DevContextMcp Knowledge Bundle

An OKF knowledge bundle describing the DevContextMcp solution: a .NET 10 Model
Context Protocol server that gives coding agents grounded, version-aware access
to internal NuGet packages.

## Overview

* [Solution Overview](/solution.md) - what DevContextMcp is, why it exists, and its writer/reader process split.
* [Technology Stack](/tech-stack.md) - language, SDK, storage, protocol, transports, and key packages.
* [Architecture & Dependency Rules](/architecture.md) - project roles, the enforced dependency graph, and DI composition.
* [Data Flows](/data-flows.md) - how the Indexer writes and how the Server reads, with isolation guarantees.
* [Database Schema](/database-schema.md) - SQLite/FTS5 schema for the documentation index and analytics databases.
* [Glossary](/glossary.md) - ubiquitous language for the domain.
* [Operations](/operations.md) - build/test/run commands, coding conventions, and reference docs.

## MCP surface

* [MCP Surface](/mcp-surface.md) - the tools, library-ID format, citation URIs, and version selection.
* [MCP Tools](/tools/index.md) - per-tool concept docs and the `nuget://` resource templates.
* [Retrieval Contracts & Outcome Model](/retrieval-contracts.md) - the response envelope, status values, and error/warning codes.
* [Version & Environment Resolution](/version-resolution.md) - how a library ID resolves to one indexed version.

## Subsystems

* [HTTP API (non-MCP)](/http-api.md) - the `/api/context` and `/api/analytics/*` REST endpoints.
* [Analytics Subsystem](/analytics.md) - capture pipeline, store, and index-run snapshot lifecycle.
* [Dashboard UI](/dashboard-ui.md) - the Next.js analytics/context dashboard.
* [Security & Safety Model](/security-model.md) - untrusted-package extraction limits and metadata-only symbol reading.

## Configuration & standards

* [Indexer Configuration](/indexer-configuration.md) - appsettings.json sections and per-package NuGet policy files.
* [Server Configuration](/server-configuration.md) - host transport, retrieval behavior, analytics, and logging settings.
* [Coding Standards (Agent Skills)](/coding-standards.md) - the repo-authored architecture/naming/testing skills.
* [Testing Strategy](/testing-strategy.md) - unit and integration test categories.
* [Demo Apps & Data](/demo.md) - sample NuGet apps and prebuilt feeds/manifests.

## Projects

* [Projects index](/projects/index.md) - the eight projects in the solution.
