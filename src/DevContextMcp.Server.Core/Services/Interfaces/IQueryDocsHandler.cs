using DevContextMcp.Server.Core.Contracts.QueryDocs;

namespace DevContextMcp.Server.Core.Services;

/// <summary>
/// Handles the query_docs tool request.
/// </summary>
public interface IQueryDocsHandler
{
    Task<QueryDocsResponse> HandleAsync(QueryDocsRequest request, CancellationToken cancellationToken);
}
