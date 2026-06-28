using DevContextMcp.Infrastructure.Server;
using Microsoft.Data.Sqlite;

namespace DevContextMcp.UnitTests.Retrieval;

public sealed class SqliteNuGetReadStoreContextTests : IDisposable
{
    private readonly string _databasePath =
        Path.Combine(Path.GetTempPath(), $"context-index-{Guid.NewGuid():N}.db");
    private readonly SqliteNuGetReadStore _target = new();

    // Purpose: returns document and NuGet inventory without exposing indexed content bodies
    [Fact]
    public async Task GetIndexedContextAsync_WithSeededIndex_ReturnsInventory()
    {
        // arrange
        await SeedAsync();

        // act
        var actual = await _target.GetIndexedContextAsync(_databasePath, CancellationToken.None);

        // assert
        Assert.Equal(1, actual.Totals.SourceCount);
        Assert.Equal(1, actual.Totals.EnvironmentCount);
        Assert.Equal(1, actual.Totals.LibraryCount);
        Assert.Equal(1, actual.Totals.NuGetLibraryCount);
        Assert.Equal(2, actual.Totals.NuGetVersionCount);

        var nuget = Assert.Single(actual.Nugets);
        Assert.Equal("Demo.Cities", nuget.PackageId);
        Assert.Equal("qa", nuget.Environment);
        Assert.Equal("1.1.0", nuget.LatestVersion);
        Assert.Equal(["1.1.0", "1.0.0"], nuget.Versions);
        Assert.Equal(2, nuget.VersionCount);
        Assert.Equal(2, nuget.ArtifactCount);
        Assert.Equal(3, nuget.DocumentCount);
        Assert.Equal(3, nuget.SymbolCount);
    }

    private async Task SeedAsync()
    {
        await using var connection = new SqliteConnection(new SqliteConnectionStringBuilder
        {
            DataSource = _databasePath,
            Mode = SqliteOpenMode.ReadWriteCreate,
            Pooling = false,
            ForeignKeys = true
        }.ToString());
        await connection.OpenAsync();

        await ExecuteAsync(
            connection,
            """
            PRAGMA user_version = 1;
            CREATE TABLE sources (
                id TEXT PRIMARY KEY,
                name TEXT NOT NULL,
                environment TEXT NOT NULL,
                service_index TEXT NOT NULL,
                kind TEXT NOT NULL DEFAULT 'nuget',
                last_indexed_at TEXT NULL
            );
            CREATE TABLE libraries (
                id TEXT PRIMARY KEY,
                source_id TEXT NOT NULL REFERENCES sources(id) ON DELETE CASCADE,
                package_id TEXT NOT NULL,
                normalized_package_id TEXT NOT NULL,
                kind TEXT NOT NULL DEFAULT 'nuget',
                display_name TEXT NULL
            );
            CREATE TABLE library_versions (
                id TEXT PRIMARY KEY,
                library_id TEXT NOT NULL REFERENCES libraries(id) ON DELETE CASCADE,
                version TEXT NOT NULL,
                content_hash TEXT NOT NULL,
                title TEXT NULL,
                description TEXT NULL,
                summary TEXT NULL,
                authors TEXT NULL,
                tags TEXT NULL,
                project_url TEXT NULL,
                repository_url TEXT NULL,
                is_listed INTEGER NOT NULL,
                is_prerelease INTEGER NOT NULL,
                is_deprecated INTEGER NOT NULL,
                published_at TEXT NULL,
                indexed_at TEXT NOT NULL
            );
            CREATE TABLE artifacts (
                id TEXT PRIMARY KEY,
                library_version_id TEXT NOT NULL REFERENCES library_versions(id) ON DELETE CASCADE,
                path TEXT NOT NULL,
                kind TEXT NOT NULL,
                content_hash TEXT NOT NULL,
                size INTEGER NOT NULL,
                content TEXT NULL
            );
            CREATE TABLE document_chunks (
                id TEXT PRIMARY KEY,
                library_version_id TEXT NOT NULL REFERENCES library_versions(id) ON DELETE CASCADE,
                artifact_id TEXT NULL REFERENCES artifacts(id) ON DELETE SET NULL,
                path TEXT NOT NULL,
                kind TEXT NOT NULL,
                member_name TEXT NULL,
                ordinal INTEGER NOT NULL,
                content TEXT NOT NULL,
                content_hash TEXT NOT NULL
            );
            CREATE TABLE symbols (
                id TEXT PRIMARY KEY,
                library_version_id TEXT NOT NULL REFERENCES library_versions(id) ON DELETE CASCADE,
                namespace TEXT NOT NULL,
                fully_qualified_name TEXT NOT NULL,
                kind TEXT NOT NULL,
                signature TEXT NOT NULL,
                containing_type TEXT NULL,
                assembly_path TEXT NOT NULL,
                target_framework TEXT NULL,
                xml_documentation_member TEXT NULL
            );
            INSERT INTO sources (id, name, environment, service_index, kind, last_indexed_at)
            VALUES
                ('nuget-source', 'Demo Feed', 'qa', 'file://demo', 'nuget', '2026-06-19T11:00:00.0000000+00:00');
            INSERT INTO libraries (id, source_id, package_id, normalized_package_id, kind, display_name)
            VALUES
                ('nuget-library', 'nuget-source', 'Demo.Cities', 'demo.cities', 'nuget', NULL);
            INSERT INTO library_versions (
                id, library_id, version, content_hash, is_listed, is_prerelease,
                is_deprecated, indexed_at)
            VALUES
                ('nuget-version-100', 'nuget-library', '1.0.0', 'hash-100', 1, 0, 0, '2026-06-19T10:00:00.0000000+00:00'),
                ('nuget-version-110', 'nuget-library', '1.1.0', 'hash-110', 1, 0, 0, '2026-06-19T11:00:00.0000000+00:00');
            INSERT INTO artifacts (id, library_version_id, path, kind, content_hash, size, content)
            VALUES
                ('artifact-100', 'nuget-version-100', 'readme.md', 'readme', 'a', 10, 'content'),
                ('artifact-110', 'nuget-version-110', 'lib/net8.0/Demo.Cities.xml', 'xml_documentation', 'b', 20, 'content');
            INSERT INTO document_chunks (id, library_version_id, artifact_id, path, kind, member_name, ordinal, content, content_hash)
            VALUES
                ('doc-100', 'nuget-version-100', 'artifact-100', 'readme.md', 'readme', NULL, 0, 'body', 'd'),
                ('doc-110-a', 'nuget-version-110', 'artifact-110', 'lib/net8.0/Demo.Cities.xml', 'xml_documentation', 'M:Demo.Cities.CityService.Get', 0, 'body', 'e'),
                ('doc-110-b', 'nuget-version-110', 'artifact-110', 'lib/net8.0/Demo.Cities.xml', 'xml_documentation', 'M:Demo.Cities.CityService.Find', 1, 'body', 'f');
            INSERT INTO symbols (
                id, library_version_id, namespace, fully_qualified_name, kind,
                signature, assembly_path, target_framework)
            VALUES
                ('sym-100', 'nuget-version-100', 'Demo.Cities', 'Demo.Cities.CityService', 'class', 'class CityService', 'Demo.Cities.dll', 'net8.0'),
                ('sym-110-a', 'nuget-version-110', 'Demo.Cities', 'Demo.Cities.CityService.Get', 'method', 'string Get()', 'Demo.Cities.dll', 'net8.0'),
                ('sym-110-b', 'nuget-version-110', 'Demo.Cities', 'Demo.Cities.CityService.Find', 'method', 'string Find()', 'Demo.Cities.dll', 'net8.0');
            """);
    }

    private static async Task ExecuteAsync(SqliteConnection connection, string sql)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync();
    }

    public void Dispose()
    {
        if (File.Exists(_databasePath))
        {
            File.Delete(_databasePath);
        }
    }
}
