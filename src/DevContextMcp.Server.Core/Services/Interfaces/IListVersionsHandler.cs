using DevContextMcp.Server.Core.Contracts.ListVersions;

namespace DevContextMcp.Server.Core.Services;

/// <summary>
/// Handles the list_versions tool request.
/// </summary>
public interface IListVersionsHandler
{
    Task<ListVersionsResponse> HandleAsync(ListVersionsRequest request, CancellationToken cancellationToken);
}
