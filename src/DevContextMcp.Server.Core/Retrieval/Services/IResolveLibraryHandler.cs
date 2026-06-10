using DevContextMcp.Server.Core.Contracts.ResolveLibrary;

namespace DevContextMcp.Server.Core.Retrieval.Services;

public interface IResolveLibraryHandler
{
    Task<ResolveLibraryResponse> HandleAsync(ResolveLibraryRequest request, CancellationToken cancellationToken);
}
