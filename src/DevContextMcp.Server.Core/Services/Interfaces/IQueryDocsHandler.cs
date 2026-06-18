using DevContextMcp.Server.Core.Contracts.QueryDocs;

namespace DevContextMcp.Server.Core.Services;

// Handles the query_docs tool request.
public interface IQueryDocsHandler
{
    Task<QueryDocsResponse> HandleAsync(QueryDocsRequest request, CancellationToken cancellationToken);
}
