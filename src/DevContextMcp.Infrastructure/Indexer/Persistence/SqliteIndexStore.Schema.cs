namespace DevContextMcp.Infrastructure.Indexer.Persistence;

/// <summary>
/// The DDL applied when the index database is first created. There are no in-place migrations —
/// see <see cref="IndexSchema"/> for how the version is stamped and validated.
/// </summary>
internal sealed partial class SqliteIndexStore
{
    private const string SchemaSql =
        """
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
            display_name TEXT NULL,
            UNIQUE(source_id, normalized_package_id)
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
            indexed_at TEXT NOT NULL,
            UNIQUE(library_id, version)
        );

        CREATE TABLE artifacts (
            id TEXT PRIMARY KEY,
            library_version_id TEXT NOT NULL REFERENCES library_versions(id) ON DELETE CASCADE,
            path TEXT NOT NULL,
            kind TEXT NOT NULL,
            content_hash TEXT NOT NULL,
            size INTEGER NOT NULL,
            content TEXT NULL,
            UNIQUE(library_version_id, path)
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
            content_hash TEXT NOT NULL,
            UNIQUE(library_version_id, path, member_name, ordinal)
        );

        CREATE VIRTUAL TABLE document_chunks_fts USING fts5(
            document_chunk_id UNINDEXED,
            package_id,
            version UNINDEXED,
            path,
            member_name,
            content,
            tokenize = 'unicode61'
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

        CREATE INDEX ix_symbols_fully_qualified_name
            ON symbols(fully_qualified_name);

        CREATE TABLE dependencies (
            id TEXT PRIMARY KEY,
            library_version_id TEXT NOT NULL REFERENCES library_versions(id) ON DELETE CASCADE,
            package_id TEXT NOT NULL,
            version_range TEXT NOT NULL,
            target_framework TEXT NULL
        );

        CREATE TABLE target_frameworks (
            library_version_id TEXT NOT NULL REFERENCES library_versions(id) ON DELETE CASCADE,
            framework TEXT NOT NULL,
            PRIMARY KEY(library_version_id, framework)
        );

        CREATE TABLE index_runs (
            id TEXT PRIMARY KEY,
            source_id TEXT NOT NULL REFERENCES sources(id) ON DELETE CASCADE,
            status TEXT NOT NULL,
            started_at TEXT NOT NULL,
            completed_at TEXT NOT NULL,
            duration_ms INTEGER NOT NULL,
            indexed_count INTEGER NOT NULL,
            changed_count INTEGER NOT NULL,
            unchanged_count INTEGER NOT NULL,
            error_count INTEGER NOT NULL
        );

        CREATE TABLE index_run_errors (
            id TEXT PRIMARY KEY,
            index_run_id TEXT NOT NULL REFERENCES index_runs(id) ON DELETE CASCADE,
            code TEXT NOT NULL,
            message TEXT NOT NULL,
            package_id TEXT NULL,
            version TEXT NULL
        );

        CREATE INDEX ix_libraries_normalized_package_id
            ON libraries(normalized_package_id);

        CREATE INDEX ix_library_versions_selection
            ON library_versions(library_id, is_listed, is_prerelease, version);

        CREATE INDEX ix_document_chunks_lookup
            ON document_chunks(library_version_id, kind, member_name);

        CREATE INDEX ix_symbols_lookup
            ON symbols(library_version_id, fully_qualified_name, target_framework);

        CREATE INDEX ix_symbols_containing_type
            ON symbols(library_version_id, containing_type);

        CREATE VIRTUAL TABLE libraries_fts USING fts5(
            library_id UNINDEXED,
            source_name UNINDEXED,
            package_id,
            title,
            description,
            summary,
            tags,
            document_text,
            tokenize = 'unicode61'
        );
        """;
}
