namespace DevContextMcp.Server.Tools;

/// <summary>
/// Expected public MCP tool surface.
/// </summary>
public sealed class ToolRegistrationCatalog
{
    public static readonly IReadOnlyList<string> ExpectedNames =
    [
        "resolve_library",
        "query_docs",
        "get_symbol",
        "list_versions"
    ];

    public IReadOnlyList<string> Names => ExpectedNames;
}
