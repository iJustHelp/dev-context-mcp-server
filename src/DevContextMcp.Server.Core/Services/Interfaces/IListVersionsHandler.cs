using DevContextMcp.Server.Core.Contracts.ListVersions;

namespace DevContextMcp.Server.Core.Services;

// Handles the list_versions tool request.
public interface IListVersionsHandler
{
    Task<ListVersionsResponse> HandleAsync(ListVersionsRequest request, CancellationToken cancellationToken);
}
