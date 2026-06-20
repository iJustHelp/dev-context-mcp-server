using DevContextMcp.Indexer;
using DevContextMcp.Indexer.Core.Services;
using DevContextMcp.IntegrationTests.Indexing;
using DevContextMcp.Server;
using DevContextMcp.Server.Core.Contracts.Common;
using DevContextMcp.Server.Core.Contracts.ListVersions;
using DevContextMcp.Server.Core.Contracts.QueryDocs;
using DevContextMcp.Server.Core.Contracts.ResolveLibrary;
using DevContextMcp.Server.Core.Services;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DevContextMcp.IntegrationTests.Retrieval;

public sealed class EnvironmentAwareRetrievalTests
{
    [Fact]
    public async Task SamePackageCanBeSelectedByEnvironmentAndVersion()
    {
        var root = Path.Combine(
            Path.GetTempPath(),
            $"mcp-doc-environment-retrieval-{Guid.NewGuid():N}");
        var qa = Path.Combine(root, "qa");
        var production = Path.Combine(root, "production");
        var databasePath = Path.Combine(root, "index", "docs.db");

        FixtureNuGetPackage.Create(
            qa,
            "2.0.0",
            "# QA\n\nQA version 2.0.0.");
        FixtureNuGetPackage.Create(
            qa,
            "2.1.0",
            "# QA\n\nQA recommended version 2.1.0.");
        FixtureNuGetPackage.Create(
            production,
            "1.0.0",
            "# Production\n\nProduction version 1.0.0.");

        try
        {
            using var provider = CreateProvider(qa, production, databasePath);
            var result = await provider.GetRequiredService<IIndexCoordinator>()
                .IndexAllAsync(CancellationToken.None);
            var summaries = result.Summaries;
            Assert.Equal(2, summaries.Count);
            Assert.All(summaries, summary => Assert.Equal("succeeded", summary.Status));
            var indexedLibrary = Assert.Single(result.IndexedLibraries);
            Assert.Equal(FixtureNuGetPackage.PackageId, indexedLibrary.PackageId);
            Assert.Equal(["production", "qa"], indexedLibrary.Environments
                .Select(environment => environment.Environment));
            Assert.Equal(
                ["1.0.0"],
                indexedLibrary.Environments[0].Versions);
            Assert.Equal(
                ["2.1.0", "2.0.0"],
                indexedLibrary.Environments[1].Versions);
            Assert.Equal(
                [
                    ("productionNuget", "production"),
                    ("qaNuget", "qa")
                ],
                await ReadSourcesAsync(databasePath));

            var resolver = provider.GetRequiredService<IResolveLibraryHandler>();
            var all = await resolver.HandleAsync(
                new ResolveLibraryRequest(FixtureNuGetPackage.PackageId),
                CancellationToken.None);
            Assert.Equal(ToolResultStatus.Ok, all.Status);
            Assert.Equal(2, all.Data!.Matches.Count);

            var qaMatch = Assert.Single(all.Data.Matches, match =>
                string.Equals(
                    match.Environment,
                    "qa",
                    StringComparison.OrdinalIgnoreCase));
            Assert.Equal($"nuget:qa/{FixtureNuGetPackage.PackageId}", qaMatch.LibraryId);
            Assert.Equal("qaNuget", qaMatch.SourceId);

            var productionMatch = Assert.Single(all.Data.Matches, match =>
                string.Equals(
                    match.Environment,
                    "production",
                    StringComparison.OrdinalIgnoreCase));
            Assert.Equal("productionNuget", productionMatch.SourceId);

            var filtered = await resolver.HandleAsync(
                new ResolveLibraryRequest(
                    FixtureNuGetPackage.PackageId,
                    Environment: "QA"),
                CancellationToken.None);
            Assert.Equal(ToolResultStatus.Ok, filtered.Status);
            Assert.Equal("qa", Assert.Single(filtered.Data!.Matches).Environment);

            var versionsHandler = provider.GetRequiredService<IListVersionsHandler>();
            var legacy = await versionsHandler.HandleAsync(
                new ListVersionsRequest($"nuget:{FixtureNuGetPackage.PackageId}"),
                CancellationToken.None);
            Assert.Equal("production", legacy.ResolvedContext!.Environment);
            Assert.Equal("productionNuget", legacy.ResolvedContext.SourceId);

            var qaVersions = await versionsHandler.HandleAsync(
                new ListVersionsRequest($"nuget:qa/{FixtureNuGetPackage.PackageId}"),
                CancellationToken.None);
            Assert.Equal("qa", qaVersions.ResolvedContext!.Environment);
            Assert.Equal("qaNuget", qaVersions.ResolvedContext.SourceId);
            Assert.Equal("2.1.0", qaVersions.Data!.RecommendedVersion);

            var docsHandler = provider.GetRequiredService<IQueryDocsHandler>();
            var qaDocs = await docsHandler.HandleAsync(
                new QueryDocsRequest(
                    $"nuget:qa/{FixtureNuGetPackage.PackageId}",
                    "QA",
                    Version: "2.0.0"),
                CancellationToken.None);
            Assert.Equal(ToolResultStatus.Ok, qaDocs.Status);
            Assert.Equal("qaNuget", qaDocs.ResolvedContext!.SourceId);
            Assert.Contains(qaDocs.Evidence, item =>
                item.Text.Contains("QA version", StringComparison.Ordinal));
            Assert.All(qaDocs.Citations, citation =>
                Assert.StartsWith("nuget://qaNuget/", citation.Uri, StringComparison.Ordinal));

            var isolated = await docsHandler.HandleAsync(
                new QueryDocsRequest(
                    $"nuget:qa/{FixtureNuGetPackage.PackageId}",
                    "Production",
                    Version: "1.0.0"),
                CancellationToken.None);
            Assert.Equal(ToolResultStatus.NotFound, isolated.Status);
            Assert.Equal("version_not_found", Assert.Single(isolated.Errors).Code);

            var missingEnvironment = await versionsHandler.HandleAsync(
                new ListVersionsRequest(
                    $"nuget:staging/{FixtureNuGetPackage.PackageId}"),
                CancellationToken.None);
            Assert.Equal(ToolResultStatus.NotFound, missingEnvironment.Status);
            Assert.Equal(
                "environment_not_found",
                Assert.Single(missingEnvironment.Errors).Code);
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    private static ServiceProvider CreateProvider(
        string qa,
        string production,
        string databasePath)
    {
        var values = new Dictionary<string, string?>
        {
            ["DevContextMcp:DatabasePath"] = databasePath,
            ["DevContextMcp:Retrieval:EnvironmentOrder:0"] = "production",
            ["DevContextMcp:Retrieval:EnvironmentOrder:1"] = "qa",
            ["DevContextMcp:Indexing:MaxCompressionRatio"] = "10000"
        };
        var root = Directory.GetParent(qa)!.FullName;
        values["DevContextMcp:IndexerSource:NugetsPath"] =
            FixtureNuGetConfiguration.CreatePackageFolder(
                root,
                new FixtureNuGetConfiguration.PackagePolicy(
                    "qa",
                    FixtureNuGetPackage.PackageId,
                    Versions: "2.1.0,2.0.0"),
                new FixtureNuGetConfiguration.PackagePolicy(
                    "production",
                    FixtureNuGetPackage.PackageId));
        AddSource(
            values: values,
            index: 0,
            name: "qaNuget",
            environment: "qa",
            serviceIndex: qa);
        AddSource(
            values: values,
            index: 1,
            name: "productionNuget",
            environment: "production",
            serviceIndex: production);

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDevContextMcpCore(configuration);
        services.AddIndexerCli(configuration);
        return services.BuildServiceProvider(validateScopes: true);
    }

    private static void AddSource(
        IDictionary<string, string?> values,
        int index,
        string name,
        string environment,
        string serviceIndex)
    {
        var prefix = $"DevContextMcp:NugetPackages:{index}";
        values[$"{prefix}:Name"] = name;
        values[$"{prefix}:Environment"] = environment;
        values[$"{prefix}:ServiceIndex"] = serviceIndex;
        values[$"{prefix}:MaxPackages"] = "10";
    }

    private static async Task<IReadOnlyList<(string Name, string Environment)>>
        ReadSourcesAsync(string databasePath)
    {
        await using var connection = new SqliteConnection(
            $"Data Source={databasePath};Mode=ReadOnly");
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText =
            "SELECT name, environment FROM sources WHERE kind = 'nuget' ORDER BY environment;";
        await using var reader = await command.ExecuteReaderAsync();
        var sources = new List<(string Name, string Environment)>();
        while (await reader.ReadAsync())
        {
            sources.Add((reader.GetString(0), reader.GetString(1)));
        }

        return sources;
    }
}
