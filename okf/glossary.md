---
type: Glossary
title: Glossary
description: Ubiquitous language for the DevContextMcp domain — the core terms and how they relate.
tags: [glossary, terminology, domain]
timestamp: 2026-07-15T00:00:00Z
---

# Glossary

Core domain terms used across the bundle. See [Database Schema](database-schema.md)
for how most of these are persisted.

# Schema

| Term | Meaning |
|------|---------|
| **Source** | A configured feed the indexer reads from, with a unique `name` and an `environment` slug (`sources` table). |
| **Environment** | A slug (e.g. `public`, `prod`, `qa`) qualifying a source; it appears in library IDs and never cross-resolves. See [Version & Environment Resolution](version-resolution.md). |
| **Library** | A package within a source (`libraries` table); identified by a [library ID](mcp-surface.md) like `nuget:prod/Demo.Cities`. |
| **Library version** | One indexed version of a library (`library_versions`), with a `content_hash` for idempotency and listing/prerelease/deprecated flags. |
| **Artifact** | A stored file from a package version — README, XML docs, or text (`artifacts` table); addressable as a `nuget://…/artifact/{path}` resource. |
| **Document chunk** | A searchable, ordered slice of an artifact (`document_chunks` + `document_chunks_fts`) used for FTS5 documentation search. |
| **Symbol** | A public type or member extracted from assembly metadata (`symbols` table); addressable as a `nuget://…/symbol/{qualifiedName}` resource. See [Security & Safety Model](security-model.md). |
| **Index run** | One execution of the indexer against a source (`index_runs`); run history is retained and never deduplicated. |
| **Snapshot** | A projection of the last index run into the analytics DB (`index_snapshot_*`), served at `/api/context/last-run`. See [Analytics](analytics.md). |
| **Citation** | A `nuget://…` URI attached to a result that resolves to a read-only MCP resource. See [Resources & Citations](tools/resources.md). |
| **Evidence score** | A relevance score for a search result; results below `MinimumEvidenceScore` are excluded, which can yield `insufficient_evidence`. See [Retrieval Contracts](retrieval-contracts.md). |
| **Recommended version** | The version `list_versions` surfaces as preferred; selected per [Version & Environment Resolution](version-resolution.md). |
