using System.Globalization;
using DevContextMcp.Indexer.Core.Infrastructure;
using DevContextMcp.Indexer.Core.Models;
using Microsoft.Data.Sqlite;
using NuGet.Versioning;

namespace DevContextMcp.Infrastructure.Indexer.Persistence;

/// <summary>
/// Write helpers that insert and delete packages, versions, runs and search rows.
/// </summary>
internal sealed partial class SqliteIndexStore
{
    private const int MaxStoredNuGetVersionsPerMajor = 2;

    private static async Task InsertPackageAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        string sourceId,
        string libraryId,
        string versionId,
        PackageIndexData package,
        CancellationToken cancellationToken)
    {
        await ExecuteAsync(
            connection: connection,
            transaction: transaction,
            sql: """
            INSERT INTO library_versions (
                id, library_id, version, content_hash, title, description, summary,
                authors, tags, project_url, repository_url, is_listed, is_prerelease,
                is_deprecated, published_at, indexed_at)
            VALUES (
                $id, $libraryId, $version, $contentHash, $title, $description, $summary,
                $authors, $tags, $projectUrl, $repositoryUrl, $isListed, $isPrerelease,
                $isDeprecated, $publishedAt, $indexedAt);
            """,
            parameters: [
                ("$id", versionId),
                ("$libraryId", libraryId),
                ("$version", package.Version),
                ("$contentHash", package.ContentHash),
                ("$title", package.Title),
                ("$description", package.Description),
                ("$summary", package.Summary),
                ("$authors", package.Authors),
                ("$tags", package.Tags),
                ("$projectUrl", package.ProjectUrl),
                ("$repositoryUrl", package.RepositoryUrl),
                ("$isListed", package.IsListed ? 1 : 0),
                ("$isPrerelease", package.IsPrerelease ? 1 : 0),
                ("$isDeprecated", package.IsDeprecated ? 1 : 0),
                ("$publishedAt", package.PublishedAt?.ToString("O")),
                ("$indexedAt", DateTimeOffset.UtcNow.ToString("O"))
            ],
            cancellationToken: cancellationToken);

        var artifactIds = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var artifact in package.Artifacts)
        {
            var artifactId = StableId(versionId, artifact.Path);
            artifactIds[artifact.Path] = artifactId;
            await ExecuteAsync(
                connection: connection,
                transaction: transaction,
                sql: """
                INSERT INTO artifacts (
                    id, library_version_id, path, kind, content_hash, size, content)
                VALUES (
                    $id, $versionId, $path, $kind, $contentHash, $size, $content);
                """,
                parameters: [
                    ("$id", artifactId),
                    ("$versionId", versionId),
                    ("$path", artifact.Path),
                    ("$kind", artifact.Kind),
                    ("$contentHash", artifact.ContentHash),
                    ("$size", artifact.Size),
                    ("$content", artifact.Content)
                ],
                cancellationToken: cancellationToken);
        }

        foreach (var document in package.Documents)
        {
            var documentId = StableId(
                versionId,
                document.Path,
                document.MemberName ?? string.Empty,
                document.Ordinal.ToString(CultureInfo.InvariantCulture));
            artifactIds.TryGetValue(document.Path, out var artifactId);

            await ExecuteAsync(
                connection: connection,
                transaction: transaction,
                sql: """
                INSERT INTO document_chunks (
                    id, library_version_id, artifact_id, path, kind, member_name,
                    ordinal, content, content_hash)
                VALUES (
                    $id, $versionId, $artifactId, $path, $kind, $memberName,
                    $ordinal, $content, $contentHash);
                """,
                parameters: [
                    ("$id", documentId),
                    ("$versionId", versionId),
                    ("$artifactId", artifactId),
                    ("$path", document.Path),
                    ("$kind", document.Kind),
                    ("$memberName", document.MemberName),
                    ("$ordinal", document.Ordinal),
                    ("$content", document.Content),
                    ("$contentHash", document.ContentHash)
                ],
                cancellationToken: cancellationToken);

            await ExecuteAsync(
                connection: connection,
                transaction: transaction,
                sql: """
                INSERT INTO document_chunks_fts (
                    document_chunk_id, package_id, version, path, member_name, content)
                VALUES (
                    $documentId, $packageId, $version, $path, $memberName, $content);
                """,
                parameters: [
                    ("$documentId", documentId),
                    ("$packageId", package.PackageId),
                    ("$version", package.Version),
                    ("$path", document.Path),
                    ("$memberName", document.MemberName),
                    ("$content", document.Content)
                ],
                cancellationToken: cancellationToken);
        }

        foreach (var symbol in package.Symbols)
        {
            var symbolId = StableId(
                versionId,
                symbol.AssemblyPath,
                symbol.Kind,
                symbol.FullyQualifiedName,
                symbol.Signature);
            await ExecuteAsync(
                connection: connection,
                transaction: transaction,
                sql: """
                INSERT INTO symbols (
                    id, library_version_id, namespace, fully_qualified_name, kind,
                    signature, containing_type, assembly_path, target_framework,
                    xml_documentation_member)
                VALUES (
                    $id, $versionId, $namespace, $fullyQualifiedName, $kind,
                    $signature, $containingType, $assemblyPath, $targetFramework,
                    $xmlDocumentationMember);
                """,
                parameters: [
                    ("$id", symbolId),
                    ("$versionId", versionId),
                    ("$namespace", symbol.Namespace),
                    ("$fullyQualifiedName", symbol.FullyQualifiedName),
                    ("$kind", symbol.Kind),
                    ("$signature", symbol.Signature),
                    ("$containingType", symbol.ContainingType),
                    ("$assemblyPath", symbol.AssemblyPath),
                    ("$targetFramework", symbol.TargetFramework),
                    ("$xmlDocumentationMember", symbol.XmlDocumentationMember)
                ],
                cancellationToken: cancellationToken);
        }

        foreach (var dependency in package.Dependencies)
        {
            var dependencyId = StableId(
                versionId,
                dependency.PackageId,
                dependency.VersionRange,
                dependency.TargetFramework ?? string.Empty);
            await ExecuteAsync(
                connection: connection,
                transaction: transaction,
                sql: """
                INSERT INTO dependencies (
                    id, library_version_id, package_id, version_range, target_framework)
                VALUES ($id, $versionId, $packageId, $versionRange, $targetFramework);
                """,
                parameters: [
                    ("$id", dependencyId),
                    ("$versionId", versionId),
                    ("$packageId", dependency.PackageId),
                    ("$versionRange", dependency.VersionRange),
                    ("$targetFramework", dependency.TargetFramework)
                ],
                cancellationToken: cancellationToken);
        }

        foreach (var framework in package.TargetFrameworks)
        {
            await ExecuteAsync(
                connection: connection,
                transaction: transaction,
                sql: """
                INSERT INTO target_frameworks (library_version_id, framework)
                VALUES ($versionId, $framework);
                """,
                parameters: [("$versionId", versionId), ("$framework", framework.Framework)],
                cancellationToken: cancellationToken);
        }
    }

    private static async Task<IReadOnlyList<PackageIdentityKey>> DeleteRemovedPackagesAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        string sourceId,
        IReadOnlyList<string> keepPackageIds,
        CancellationToken cancellationToken)
    {
        var keep = keepPackageIds
            .Select(packageId => packageId.Trim().ToLowerInvariant())
            .ToHashSet(StringComparer.Ordinal);

        var deleted = new List<PackageIdentityKey>();

        var libraries = await GetSourceLibrariesAsync(
            connection: connection,
            transaction: transaction,
            sourceId: sourceId,
            cancellationToken: cancellationToken);

        foreach (var library in libraries)
        {
            if (keep.Contains(library.NormalizedPackageId))
            {
                continue;
            }

            var versions = await GetLibraryVersionsAsync(
                connection: connection,
                transaction: transaction,
                libraryId: library.LibraryId,
                cancellationToken: cancellationToken);
            foreach (var version in versions)
            {
                await DeleteVersionAsync(
                    connection: connection,
                    transaction: transaction,
                    versionId: version.VersionId,
                    cancellationToken: cancellationToken);
                deleted.Add(new PackageIdentityKey(library.PackageId, version.Version));
            }

            await ExecuteAsync(
                connection: connection,
                transaction: transaction,
                sql: "DELETE FROM libraries_fts WHERE library_id = $libraryId;",
                parameters: [("$libraryId", library.LibraryId)],
                cancellationToken: cancellationToken);
            await ExecuteAsync(
                connection: connection,
                transaction: transaction,
                sql: "DELETE FROM libraries WHERE id = $libraryId;",
                parameters: [("$libraryId", library.LibraryId)],
                cancellationToken: cancellationToken);
        }

        return deleted;
    }

    private static async Task<IReadOnlyList<(string LibraryId, string PackageId, string NormalizedPackageId)>>
        GetSourceLibrariesAsync(
            SqliteConnection connection,
            SqliteTransaction transaction,
            string sourceId,
            CancellationToken cancellationToken)
    {
        var libraries = new List<(string LibraryId, string PackageId, string NormalizedPackageId)>();
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText =
            """
            SELECT id, package_id, normalized_package_id
            FROM libraries
            WHERE source_id = $sourceId;
            """;
        command.Parameters.AddWithValue("$sourceId", sourceId);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            libraries.Add((reader.GetString(0), reader.GetString(1), reader.GetString(2)));
        }

        return libraries;
    }

    private static async Task<IReadOnlyList<PackageIdentityKey>> PruneStoredPackageVersionsAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        string sourceId,
        IReadOnlyList<string> packageIds,
        CancellationToken cancellationToken)
    {
        var deleted = new List<PackageIdentityKey>();

        foreach (var packageId in packageIds)
        {
            var library = await GetLibraryAsync(
                connection: connection,
                transaction: transaction,
                sourceId: sourceId,
                packageId: packageId,
                cancellationToken: cancellationToken);
            if (library is null)
            {
                continue;
            }

            var versions = await GetLibraryVersionsAsync(
                connection: connection,
                transaction: transaction,
                libraryId: library.Value.LibraryId,
                cancellationToken: cancellationToken);
            var pruned = versions
                .GroupBy(version => NuGetVersion.Parse(version.Version).Major)
                .SelectMany(majorGroup => majorGroup
                    .OrderByDescending(version => NuGetVersion.Parse(version.Version), VersionComparer.VersionRelease)
                    .Skip(MaxStoredNuGetVersionsPerMajor))
                .ToArray();

            foreach (var version in pruned)
            {
                await DeleteVersionAsync(
                    connection: connection,
                    transaction: transaction,
                    versionId: version.VersionId,
                    cancellationToken: cancellationToken);
                deleted.Add(new PackageIdentityKey(library.Value.PackageId, version.Version));
            }
        }

        return deleted;
    }

    private static async Task<(string LibraryId, string PackageId)?> GetLibraryAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        string sourceId,
        string packageId,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText =
            """
            SELECT id, package_id
            FROM libraries
            WHERE source_id = $sourceId
                AND normalized_package_id = $packageId;
            """;
        command.Parameters.AddWithValue("$sourceId", sourceId);
        command.Parameters.AddWithValue(
            "$packageId",
            packageId.Trim().ToLowerInvariant());

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken)
            ? (reader.GetString(0), reader.GetString(1))
            : null;
    }

    private static async Task<IReadOnlyList<(string VersionId, string Version)>>
        GetLibraryVersionsAsync(
            SqliteConnection connection,
            SqliteTransaction transaction,
            string libraryId,
            CancellationToken cancellationToken)
    {
        var versions = new List<(string VersionId, string Version)>();
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText =
            """
            SELECT id, version
            FROM library_versions
            WHERE library_id = $libraryId;
            """;
        command.Parameters.AddWithValue("$libraryId", libraryId);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            versions.Add((reader.GetString(0), reader.GetString(1)));
        }

        return versions;
    }

    private static async Task DeleteVersionAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        string versionId,
        CancellationToken cancellationToken)
    {
        await ExecuteAsync(
            connection: connection,
            transaction: transaction,
            sql: """
            DELETE FROM document_chunks_fts
            WHERE document_chunk_id IN (
                SELECT id FROM document_chunks WHERE library_version_id = $versionId
            );
            """,
            parameters: [("$versionId", versionId)],
            cancellationToken: cancellationToken);
        await ExecuteAsync(
            connection: connection,
            transaction: transaction,
            sql: "DELETE FROM library_versions WHERE id = $versionId;",
            parameters: [("$versionId", versionId)],
            cancellationToken: cancellationToken);
    }

    private static async Task<string?> GetContentHashAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        string versionId,
        CancellationToken cancellationToken)
    {
        var value = await ExecuteScalarAsync(
            connection: connection,
            transaction: transaction,
            sql: "SELECT content_hash FROM library_versions WHERE id = $versionId;",
            cancellationToken: cancellationToken,
            parameters: [("$versionId", versionId)]);
        return value is null or DBNull ? null : Convert.ToString(value, CultureInfo.InvariantCulture);
    }

    private static Task UpsertSourceAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        string sourceId,
        IndexSourceDefinition source,
        CancellationToken cancellationToken) =>
        UpsertSourceAsync(
            connection: connection,
            transaction: transaction,
            sourceId: sourceId,
            name: source.Name,
            environment: source.Environment,
            serviceIndex: source.ServiceIndex,
            kind: "nuget",
            cancellationToken: cancellationToken);

    private static Task UpsertSourceAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        string sourceId,
        string name,
        string environment,
        string serviceIndex,
        string kind,
        CancellationToken cancellationToken) =>
        ExecuteAsync(
            connection: connection,
            transaction: transaction,
            sql: """
            INSERT INTO sources (id, name, environment, service_index, kind)
            VALUES ($id, $name, $environment, $serviceIndex, $kind)
            ON CONFLICT(id) DO UPDATE SET
                name = excluded.name,
                environment = excluded.environment,
                service_index = excluded.service_index,
                kind = excluded.kind;
            """,
            parameters: [
                ("$id", sourceId),
                ("$name", name),
                ("$environment", environment),
                ("$serviceIndex", serviceIndex),
                ("$kind", kind)
            ],
            cancellationToken: cancellationToken);

    private static Task UpsertLibraryAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        string libraryId,
        string sourceId,
        string packageId,
        CancellationToken cancellationToken) =>
        UpsertLibraryAsync(
            connection: connection,
            transaction: transaction,
            libraryId: libraryId,
            sourceId: sourceId,
            packageId: packageId,
            kind: "nuget",
            displayName: packageId,
            cancellationToken: cancellationToken);

    private static Task UpsertLibraryAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        string libraryId,
        string sourceId,
        string packageId,
        string kind,
        string displayName,
        CancellationToken cancellationToken) =>
        ExecuteAsync(
            connection: connection,
            transaction: transaction,
            sql: """
            INSERT INTO libraries (
                id, source_id, package_id, normalized_package_id, kind, display_name)
            VALUES (
                $id, $sourceId, $packageId, $normalizedPackageId, $kind, $displayName)
            ON CONFLICT(id) DO UPDATE SET
                package_id = excluded.package_id,
                kind = excluded.kind,
                display_name = excluded.display_name;
            """,
            parameters: [
                ("$id", libraryId),
                ("$sourceId", sourceId),
                ("$packageId", packageId),
                ("$normalizedPackageId", packageId.Trim().ToLowerInvariant()),
                ("$kind", kind),
                ("$displayName", displayName)
            ],
            cancellationToken: cancellationToken);

    private static async Task InsertRunAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        string runId,
        string sourceId,
        string status,
        DateTimeOffset startedAt,
        DateTimeOffset completedAt,
        int indexed,
        int changed,
        int unchanged,
        IReadOnlyList<IndexRunError> errors,
        CancellationToken cancellationToken)
    {
        await ExecuteAsync(
            connection: connection,
            transaction: transaction,
            sql: """
            INSERT INTO index_runs (
                id, source_id, status, started_at, completed_at, duration_ms,
                indexed_count, changed_count, unchanged_count, error_count)
            VALUES (
                $id, $sourceId, $status, $startedAt, $completedAt, $durationMs,
                $indexed, $changed, $unchanged, $errorCount);
            """,
            parameters: [
                ("$id", runId),
                ("$sourceId", sourceId),
                ("$status", status),
                ("$startedAt", startedAt.ToString("O")),
                ("$completedAt", completedAt.ToString("O")),
                ("$durationMs", (long)(completedAt - startedAt).TotalMilliseconds),
                ("$indexed", indexed),
                ("$changed", changed),
                ("$unchanged", unchanged),
                ("$errorCount", errors.Count)
            ],
            cancellationToken: cancellationToken);

        for (var index = 0; index < errors.Count; index++)
        {
            var error = errors[index];
            await ExecuteAsync(
                connection: connection,
                transaction: transaction,
                sql: """
                INSERT INTO index_run_errors (
                    id, index_run_id, code, message, package_id, version)
                VALUES ($id, $runId, $code, $message, $packageId, $version);
                """,
                parameters: [
                    ("$id", StableId(runId, index.ToString(CultureInfo.InvariantCulture))),
                    ("$runId", runId),
                    ("$code", error.Code),
                    ("$message", error.Message),
                    ("$packageId", error.PackageId),
                    ("$version", error.Version)
                ],
                cancellationToken: cancellationToken);
        }
    }

    private static async Task RefreshSourceLibrarySearchAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        string sourceId,
        CancellationToken cancellationToken)
    {
        await ExecuteAsync(
            connection: connection,
            transaction: transaction,
            sql: """
            DELETE FROM libraries_fts
            WHERE library_id IN (SELECT id FROM libraries WHERE source_id = $sourceId);
            """,
            parameters: [("$sourceId", sourceId)],
            cancellationToken: cancellationToken);
        await InsertLibrarySearchRowsAsync(
            connection: connection,
            transaction: transaction,
            whereClause: "WHERE l.source_id = $sourceId",
            parameters: [("$sourceId", sourceId)],
            cancellationToken: cancellationToken);
    }

    private static async Task InsertLibrarySearchRowsAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        string whereClause,
        IReadOnlyList<(string Name, object? Value)> parameters,
        CancellationToken cancellationToken)
    {
        await ExecuteAsync(
            connection: connection,
            transaction: transaction,
            sql: $$"""
            INSERT INTO libraries_fts (
                library_id, source_name, package_id, title, description, summary, tags, document_text)
            SELECT
                l.id,
                s.name,
                l.package_id,
                COALESCE(lv.title, ''),
                COALESCE(lv.description, ''),
                COALESCE(lv.summary, ''),
                COALESCE(lv.tags, ''),
                COALESCE((
                    SELECT substr(group_concat(dc.content, ' '), 1, 50000)
                    FROM document_chunks dc
                    WHERE dc.library_version_id = lv.id
                ), '')
            FROM libraries l
            INNER JOIN sources s ON s.id = l.source_id
            LEFT JOIN library_versions lv ON lv.id = (
                SELECT candidate.id
                FROM library_versions candidate
                WHERE candidate.library_id = l.id
                ORDER BY
                    candidate.is_listed DESC,
                    candidate.is_prerelease ASC,
                    COALESCE(candidate.published_at, candidate.indexed_at) DESC,
                    candidate.version DESC
                LIMIT 1
            )
            {{whereClause}};
            """,
            parameters: parameters,
            cancellationToken: cancellationToken);
    }
}
