using System.ComponentModel;
using DevContextMcp.Server.Core.Services;
using DevContextMcp.Server.Core.Contracts.QueryDocs;
using ModelContextProtocol.Server;

namespace DevContextMcp.Server.Tools;

/// <summary>
/// MCP tool that exposes query_docs, delegating to the handler.
/// </summary>
[McpServerToolType]
internal sealed class QueryDocsTool(
    IQueryDocsHandler handler,
    ToolInvocationLogger invocationLogger)
{
    [McpServerTool(
        Name = "query_docs",
        UseStructuredContent = true,
        OutputSchemaType = typeof(QueryDocsResponse))]
    [Description("Finds indexed documentation and examples for one internal library.")]
    public Task<QueryDocsResponse> QueryDocsAsync(
        [Description("Stable library identifier returned by resolve_library.")] string libraryId,
        [Description("A focused topic or question. Short, topical queries (1–3 words near the document's subject) retrieve best; broaden first, then narrow.")] string question,
        [Description("Exact package or client version.")] string? version = null,
        [Description("Target framework used by the calling project, such as net10.0.")] string? targetFramework = null,
        [Description("Maximum number of documentation fragments and symbols to return.")] int maxResults = 8,
        [Description("Package version referenced by the calling project.")] string? projectVersion = null,
        CancellationToken cancellationToken = default)
    {
        var request = new QueryDocsRequest(
            LibraryId: libraryId,
            Question: question,
            Version: version,
            TargetFramework: targetFramework,
            MaxResults: maxResults,
            ProjectVersion: projectVersion);

        return invocationLogger.InvokeAsync(
            toolName: "query_docs",
            request: request,
            invoke: token => handler.HandleAsync(request, token),
            cancellationToken: cancellationToken);
    }
}
