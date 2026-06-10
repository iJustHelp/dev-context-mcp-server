using DevContextMcp.Indexer.Cli;
using DevContextMcp.Indexer.Services;
using DevContextMcp.Infrastructure.Indexing.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DevContextMcp.IntegrationTests.Indexing;

public sealed class NuGetIndexingPipelineTests
{
    [Fact]
    public async Task LocalPackageIsIndexedIntoSqliteAndFtsIdempotently()
    {
        var root = Path.Combine(Path.GetTempPath(), $"mcp-doc-server-tests-{Guid.NewGuid():N}");
        var feed = Path.Combine(root, "feed");
        var databasePath = Path.Combine(root, "index", "docs.db");
        FixtureNuGetPackage.Create(feed);

        try
        {
            using var provider = CreateProvider(feed, databasePath);
            var coordinator = provider.GetRequiredService<IIndexCoordinator>();

            var first = Assert.Single(await coordinator.IndexAllAsync(CancellationToken.None));

            Assert.Equal("succeeded", first.Status);
            Assert.Equal(1, first.Discovered);
            Assert.Equal(1, first.Indexed);
            Assert.Equal(1, first.Changed);
            Assert.Equal(0, first.Unchanged);

            await using var connection = new SqliteConnection(
                $"Data Source={databasePath};Pooling=False");
            await connection.OpenAsync();
            Assert.Equal(1L, await ScalarAsync(connection, "SELECT COUNT(*) FROM library_versions;"));
            Assert.Equal(3L, await ScalarAsync(connection, "PRAGMA user_version;"));
            Assert.Equal(
                "test",
                await TextScalarAsync(connection, "SELECT environment FROM sources;"));
            Assert.True(await ScalarAsync(connection, "SELECT COUNT(*) FROM dependencies;") > 0);
            Assert.True(await ScalarAsync(connection, "SELECT COUNT(*) FROM target_frameworks;") > 0);
            Assert.True(await ScalarAsync(connection, "SELECT COUNT(*) FROM symbols;") > 0);
            Assert.True(await ScalarAsync(
                connection,
                "SELECT COUNT(*) FROM document_chunks_fts WHERE document_chunks_fts MATCH 'fixture';") > 0);

            var second = Assert.Single(await coordinator.IndexAllAsync(CancellationToken.None));
            Assert.Equal(0, second.Changed);
            Assert.Equal(1, second.Unchanged);
            Assert.Equal(1L, await ScalarAsync(connection, "SELECT COUNT(*) FROM library_versions;"));
            Assert.Equal(2L, await ScalarAsync(connection, "SELECT COUNT(*) FROM index_runs;"));

            FixtureNuGetPackage.ReplaceWithUnsafeArchive(feed);
            var failed = Assert.Single(await coordinator.IndexAllAsync(CancellationToken.None));

            Assert.Equal("failed", failed.Status);
            Assert.Single(failed.Errors);
            Assert.Equal(1L, await ScalarAsync(connection, "SELECT COUNT(*) FROM library_versions;"));
            Assert.True(await ScalarAsync(
                connection,
                "SELECT COUNT(*) FROM document_chunks_fts WHERE document_chunks_fts MATCH 'fixture';") > 0);
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [Fact]
    public async Task VersionTwoDatabaseMigratesEnvironmentWithoutChangingSourceId()
    {
        var root = Path.Combine(Path.GetTempPath(), $"mcp-doc-migration-{Guid.NewGuid():N}");
        var databasePath = Path.Combine(root, "docs.db");
        Directory.CreateDirectory(root);

        try
        {
            await using (var connection = new SqliteConnection(
                             $"Data Source={databasePath};Pooling=False"))
            {
                await connection.OpenAsync();
                await using var command = connection.CreateCommand();
                command.CommandText =
                    """
                    CREATE TABLE sources (
                        id TEXT PRIMARY KEY,
                        name TEXT NOT NULL,
                        service_index TEXT NOT NULL,
                        last_indexed_at TEXT NULL
                    );
                    INSERT INTO sources (id, name, service_index)
                    VALUES ('stable-source-id', 'qa-feed', 'https://packages.example/v3/index.json');
                    PRAGMA user_version = 2;
                    """;
                await command.ExecuteNonQueryAsync();
            }

            var store = new SqliteIndexStore();
            await store.InitializeAsync(databasePath, CancellationToken.None);

            await using var migrated = new SqliteConnection(
                $"Data Source={databasePath};Mode=ReadOnly;Pooling=False");
            await migrated.OpenAsync();
            Assert.Equal(3L, await ScalarAsync(migrated, "PRAGMA user_version;"));
            Assert.Equal(
                "stable-source-id",
                await TextScalarAsync(migrated, "SELECT id FROM sources;"));
            Assert.Equal(
                "qa-feed",
                await TextScalarAsync(migrated, "SELECT environment FROM sources;"));
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [Fact]
    public async Task ReindexingUpdatesEnvironmentWithoutChangingStableIds()
    {
        var root = Path.Combine(Path.GetTempPath(), $"mcp-doc-environment-{Guid.NewGuid():N}");
        var feed = Path.Combine(root, "feed");
        var databasePath = Path.Combine(root, "index", "docs.db");
        FixtureNuGetPackage.Create(feed);

        try
        {
            using (var provider = CreateProvider(feed, databasePath, "qa"))
            {
                await provider.GetRequiredService<IIndexCoordinator>()
                    .IndexAllAsync(CancellationToken.None);
            }

            string sourceId;
            string libraryId;
            string versionId;
            await using (var connection = new SqliteConnection(
                             $"Data Source={databasePath};Pooling=False"))
            {
                await connection.OpenAsync();
                sourceId = await TextScalarAsync(connection, "SELECT id FROM sources;");
                libraryId = await TextScalarAsync(connection, "SELECT id FROM libraries;");
                versionId = await TextScalarAsync(connection, "SELECT id FROM library_versions;");
            }

            using (var provider = CreateProvider(feed, databasePath, "production"))
            {
                await provider.GetRequiredService<IIndexCoordinator>()
                    .IndexAllAsync(CancellationToken.None);
            }

            await using var updated = new SqliteConnection(
                $"Data Source={databasePath};Mode=ReadOnly;Pooling=False");
            await updated.OpenAsync();
            Assert.Equal(sourceId, await TextScalarAsync(updated, "SELECT id FROM sources;"));
            Assert.Equal(libraryId, await TextScalarAsync(updated, "SELECT id FROM libraries;"));
            Assert.Equal(versionId, await TextScalarAsync(updated, "SELECT id FROM library_versions;"));
            Assert.Equal(
                "production",
                await TextScalarAsync(updated, "SELECT environment FROM sources;"));
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    private static ServiceProvider CreateProvider(
        string feed,
        string databasePath,
        string environment = "test")
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DevContextMcp:DatabasePath"] = databasePath,
                ["DevContextMcp:NuGetSources:0:Name"] = "fixture",
                ["DevContextMcp:NuGetSources:0:Environment"] = environment,
                ["DevContextMcp:NuGetSources:0:ServiceIndex"] = feed,
                ["DevContextMcp:NuGetSources:0:PackageIds:0"] = FixtureNuGetPackage.PackageId,
                ["DevContextMcp:NuGetSources:0:MaxVersionsPerPackage"] = "1",
                ["DevContextMcp:NuGetSources:0:MaxPackages"] = "10",
                ["DevContextMcp:Indexing:MaxCompressionRatio"] = "10000"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddIndexerCli(configuration);
        return services.BuildServiceProvider(validateScopes: true);
    }

    private static async Task<long> ScalarAsync(SqliteConnection connection, string sql)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        return Convert.ToInt64(await command.ExecuteScalarAsync());
    }

    private static async Task<string> TextScalarAsync(
        SqliteConnection connection,
        string sql)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        return Convert.ToString(await command.ExecuteScalarAsync())!;
    }
}
