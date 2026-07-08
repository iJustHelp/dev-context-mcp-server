using System.Globalization;
using DevContextMcp.Indexer.Core.Infrastructure;
using DevContextMcp.Indexer.Core.Models;
using NuGet.Versioning;

namespace DevContextMcp.Infrastructure.Indexer.Persistence;

/// <summary>
/// SQLite-backed index store that creates the schema and publishes packages and documentation,
/// computing per-run change sets against the previously indexed content.
/// </summary>
/// <remarks>
/// This class is split across several files for readability:
/// <list type="bullet">
/// <item><description><c>SqliteIndexStore.cs</c> — the <see cref="IIndexStore"/> operations.</description></item>
/// <item><description><c>SqliteIndexStore.Writes.cs</c> — package/version/run write helpers.</description></item>
/// <item><description><c>SqliteIndexStore.Commands.cs</c> — low-level SQLite command and id helpers.</description></item>
/// <item><description><c>SqliteIndexStore.Schema.cs</c> — the schema DDL.</description></item>
/// </list>
/// </remarks>
internal sealed partial class SqliteIndexStore : IIndexStore
{
    private sealed record IndexedLibraryRow(
        string PackageId,
        string Environment,
        string Version);

    public async Task<IReadOnlyList<IndexedLibrary>> GetIndexedLibrariesAsync(
        string databasePath,
        CancellationToken cancellationToken)
    {
        var resolvedPath = ResolveDatabasePath(databasePath);
        await using var connection = CreateConnection(resolvedPath);
        await connection.OpenAsync(cancellationToken);

        var rows = new List<IndexedLibraryRow>();
        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT l.package_id, s.environment, lv.version
            FROM library_versions lv
            INNER JOIN libraries l ON l.id = lv.library_id
            INNER JOIN sources s ON s.id = l.source_id
            WHERE l.kind = 'nuget';
            """;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            rows.Add(new IndexedLibraryRow(
                reader.GetString(0),
                reader.GetString(1),
                reader.GetString(2)));
        }

        return rows
            .GroupBy(row => row.PackageId, StringComparer.OrdinalIgnoreCase)
            .Select(packageGroup => new IndexedLibrary(
                SelectStoredCasing(packageGroup.Select(row => row.PackageId)),
                packageGroup
                    .GroupBy(row => row.Environment, StringComparer.OrdinalIgnoreCase)
                    .Select(environmentGroup => new IndexedLibraryEnvironment(
                        SelectStoredCasing(environmentGroup.Select(row => row.Environment)),
                        environmentGroup
                            .Select(row => row.Version)
                            .Distinct(StringComparer.OrdinalIgnoreCase)
                            .OrderByDescending(version => NuGetVersion.Parse(version))
                            .ThenBy(version => version, StringComparer.Ordinal)
                            .ToArray()))
                    .OrderBy(environment => environment.Environment, StringComparer.OrdinalIgnoreCase)
                    .ThenBy(environment => environment.Environment, StringComparer.Ordinal)
                    .ToArray()))
            .OrderBy(library => library.PackageId, StringComparer.OrdinalIgnoreCase)
            .ThenBy(library => library.PackageId, StringComparer.Ordinal)
            .ToArray();
    }

    public async Task InitializeAsync(
        string databasePath,
        CancellationToken cancellationToken)
    {
        var resolvedPath = ResolveDatabasePath(databasePath);
        Directory.CreateDirectory(Path.GetDirectoryName(resolvedPath)!);

        await using var connection = CreateConnection(resolvedPath);
        await connection.OpenAsync(cancellationToken);
        await ExecuteNonQueryAsync(
            connection: connection,
            transaction: null,
            sql: "PRAGMA foreign_keys = ON;",
            cancellationToken: cancellationToken);
        await ExecuteNonQueryAsync(
            connection: connection,
            transaction: null,
            sql: "PRAGMA journal_mode = WAL;",
            cancellationToken: cancellationToken);

        var version = Convert.ToInt32(
            await ExecuteScalarAsync(
                connection: connection,
                transaction: null,
                sql: "PRAGMA user_version;",
                cancellationToken: cancellationToken),
            CultureInfo.InvariantCulture);

        if (version > IndexSchema.Version)
        {
            throw new InvalidOperationException(
                $"Database schema version {version} is newer than supported version {IndexSchema.Version}.");
        }

        if (version == 0)
        {
            await using var transaction = connection.BeginTransaction();
            await ExecuteNonQueryAsync(
                connection: connection,
                transaction: transaction,
                sql: SchemaSql,
                cancellationToken: cancellationToken);
            await ExecuteNonQueryAsync(
                connection: connection,
                transaction: transaction,
                sql: $"PRAGMA user_version = {IndexSchema.Version};",
                cancellationToken: cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
    }

    public async Task<IndexPublishResult> PublishSourceAsync(
        string databasePath,
        IndexSourceDefinition source,
        DateTimeOffset startedAt,
        IReadOnlyList<PackageIndexData> packages,
        IReadOnlyList<IndexRunError> errors,
        bool pruneRemovedPackages,
        CancellationToken cancellationToken)
    {
        var resolvedPath = ResolveDatabasePath(databasePath);
        await using var connection = CreateConnection(resolvedPath);
        await connection.OpenAsync(cancellationToken);
        await ExecuteNonQueryAsync(
            connection: connection,
            transaction: null,
            sql: "PRAGMA foreign_keys = ON;",
            cancellationToken: cancellationToken);
        await using var transaction = connection.BeginTransaction();

        var sourceId = StableId(source.Name, source.ServiceIndex);
        await UpsertSourceAsync(
            connection: connection,
            transaction: transaction,
            sourceId: sourceId,
            source: source,
            cancellationToken: cancellationToken);

        var added = new List<PackageIdentityKey>();
        var updated = new List<PackageIdentityKey>();
        var unchanged = 0;
        foreach (var package in packages)
        {
            var identity = new PackageIdentityKey(package.PackageId, package.Version);
            var libraryId = StableId(sourceId, identity.NormalizedPackageId);
            var versionId = identity.ToStableId(sourceId);
            var existingHash = await GetContentHashAsync(
                connection: connection,
                transaction: transaction,
                versionId: versionId,
                cancellationToken: cancellationToken);

            if (string.Equals(existingHash, package.ContentHash, StringComparison.Ordinal))
            {
                unchanged++;
                continue;
            }

            if (existingHash is null)
            {
                added.Add(identity);
            }
            else
            {
                updated.Add(identity);
            }

            await DeleteVersionAsync(
                connection: connection,
                transaction: transaction,
                versionId: versionId,
                cancellationToken: cancellationToken);
            await UpsertLibraryAsync(
                connection: connection,
                transaction: transaction,
                libraryId: libraryId,
                sourceId: sourceId,
                packageId: package.PackageId,
                cancellationToken: cancellationToken);
            await InsertPackageAsync(
                connection: connection,
                transaction: transaction,
                sourceId: sourceId,
                libraryId: libraryId,
                versionId: versionId,
                package: package,
                cancellationToken: cancellationToken);
        }

        var deleted = new List<PackageIdentityKey>();
        if (pruneRemovedPackages)
        {
            deleted.AddRange(await DeleteRemovedPackagesAsync(
                connection: connection,
                transaction: transaction,
                sourceId: sourceId,
                keepPackageIds: source.Packages
                    .Select(package => package.PackageId)
                    .ToArray(),
                cancellationToken: cancellationToken));
        }

        deleted.AddRange(await PruneStoredPackageVersionsAsync(
            connection: connection,
            transaction: transaction,
            sourceId: sourceId,
            packageIds: packages
                .Select(package => package.PackageId)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray(),
            cancellationToken: cancellationToken));

        await RefreshSourceLibrarySearchAsync(
            connection: connection,
            transaction: transaction,
            sourceId: sourceId,
            cancellationToken: cancellationToken);

        var completedAt = DateTimeOffset.UtcNow;
        var changed = added.Count + updated.Count;
        var status = packages.Count == 0 && errors.Count > 0
            ? "failed"
            : errors.Count > 0 ? "partial_success" : "succeeded";
        var runId = StableId(
            sourceId,
            startedAt.ToString("O", CultureInfo.InvariantCulture),
            Guid.NewGuid().ToString("N"));

        await InsertRunAsync(
            connection: connection,
            transaction: transaction,
            runId: runId,
            sourceId: sourceId,
            status: status,
            startedAt: startedAt,
            completedAt: completedAt,
            indexed: packages.Count,
            changed: changed,
            unchanged: unchanged,
            errors: errors,
            cancellationToken: cancellationToken);

        await ExecuteAsync(
            connection: connection,
            transaction: transaction,
            sql: """
            UPDATE sources
            SET last_indexed_at = $lastIndexedAt
            WHERE id = $sourceId;
            """,
            parameters: [("$lastIndexedAt", completedAt.ToString("O")), ("$sourceId", sourceId)],
            cancellationToken: cancellationToken);

        await transaction.CommitAsync(cancellationToken);
        return new IndexPublishResult(
            Changed: changed,
            Unchanged: unchanged,
            Added: SortIdentities(added),
            Updated: SortIdentities(updated),
            Deleted: SortIdentities(deleted));
    }

    private static IReadOnlyList<PackageIdentityKey> SortIdentities(
        IEnumerable<PackageIdentityKey> identities) =>
        identities
            .OrderBy(identity => identity.PackageId, StringComparer.OrdinalIgnoreCase)
            .ThenBy(identity => identity.PackageId, StringComparer.Ordinal)
            .ThenBy(identity => identity.Version, StringComparer.OrdinalIgnoreCase)
            .ThenBy(identity => identity.Version, StringComparer.Ordinal)
            .ToArray();

    private static string SelectStoredCasing(IEnumerable<string> values) =>
        values
            .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
            .ThenBy(value => value, StringComparer.Ordinal)
            .First();
}
