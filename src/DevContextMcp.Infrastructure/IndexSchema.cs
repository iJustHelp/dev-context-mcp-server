namespace DevContextMcp.Infrastructure;

/// <summary>
/// Shared schema contract for the documentation index. The indexer stamps this value into
/// PRAGMA user_version; the read store refuses any database older than it. There are no
/// in-place migrations — bump this constant whenever the schema changes and rebuild the index.
/// </summary>
internal static class IndexSchema
{
    internal const int Version = 1;
}
