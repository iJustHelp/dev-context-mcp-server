---
type: Reference
title: Testing Strategy
description: The categories of unit and integration tests and representative files for each.
resource: tests
tags: [testing, xunit, integration, architecture-tests]
timestamp: 2026-07-15T00:00:00Z
---

# Testing Strategy

Two xUnit projects cover the solution: fast isolated unit tests and end-to-end
integration tests (some launching the real executables as child processes). This
expands the summary in [Projects: Tests](projects/tests.md).

# Schema

## Unit tests — `tests/DevContextMcp.UnitTests/`

| Category | Representative files |
|----------|----------------------|
| Architecture rules | `Architecture/ProjectDependencyTests.cs` (dependency direction), `Architecture/IndexingRegistrationTests.cs` (DI boundaries). |
| Indexing | `Indexing/ArchiveSafetyValidatorTests.cs`, `MetadataSymbolExtractorTests.cs`, `DocumentChunkerTests.cs`, `IndexCoordinatorTests.cs`, `IndexRunReportTests.cs`, `NuGetPackageSourceClientVersionSelectionTests.cs`. |
| Retrieval | `Retrieval/FtsQueryBuilderTests.cs`, `LibraryIdTests.cs`, `VersionResolverTests.cs`, `GetSymbolHandlerTests.cs`, `SqliteNuGetReadStoreContextTests.cs`. |
| Configuration | `Configuration/DatabasePathResolutionTests.cs`, `DevContextMcpOptionsValidatorTests.cs`, `IndexerOptionsValidatorTests.cs`. |
| Contracts | `Contracts/ToolContractSerializationTests.cs`. |
| Analytics | `Analytics/SqliteAnalyticsStoreTests.cs`, `AnalyticsUserResolverTests.cs`, `ToolInvocationLoggerCaptureTests.cs`. |

## Integration tests — `tests/DevContextMcp.IntegrationTests/`

| Category | Representative files |
|----------|----------------------|
| Child-process | `Indexing/IndexerProcessTests.cs` (launches the indexer as a real OS process; asserts exit behavior). |
| Idempotency | `Indexing/NuGetIndexingPipelineTests.cs` (`LocalPackageIsIndexedIntoSqliteAndFtsIdempotently` — second run reports 0 changed / 1 unchanged). |
| Indexing pipeline | `Indexing/NuGetIndexingPipelineTests.cs` + fixtures `FixtureNuGetPackage.cs`, `FixtureNuGetConfiguration.cs`. |
| MCP protocol | `Mcp/HttpProtocolTests.cs`, `ToolDiscoveryTests.cs`, `MissingIndexInvocationTests.cs`, `McpTestServer.cs`. |
| Retrieval | `Retrieval/NuGetRetrievalPipelineTests.cs`, `EnvironmentAwareRetrievalTests.cs`, `QueryDocsSimulatedCallsTests.cs`. |
| Startup / config | `Startup/StartupDiagnosticsTests.cs`, `InvalidConfigurationTests.cs`. |
| Context & analytics | `Context/ContextEndpointsTests.cs`, `Analytics/`. |

Architecture tests enforce the dependency graph in [Architecture & Dependency Rules](architecture.md);
idempotency tests back the guarantees in [Data Flows](data-flows.md); safety
tests back the [Security & Safety Model](security-model.md).

# Citations

[1] [tests/DevContextMcp.UnitTests](../tests/DevContextMcp.UnitTests)
[2] [tests/DevContextMcp.IntegrationTests](../tests/DevContextMcp.IntegrationTests)
[3] [Test plan](../design/test-plan.md)
