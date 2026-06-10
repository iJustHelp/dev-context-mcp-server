using DevContextMcp.Server.Core.Contracts.QueryDocs;

namespace DevContextMcp.Server.Core.Retrieval.Services;

public interface IQueryDocsHandler
{
    Task<QueryDocsResponse> HandleAsync(QueryDocsRequest request, CancellationToken cancellationToken);
}
