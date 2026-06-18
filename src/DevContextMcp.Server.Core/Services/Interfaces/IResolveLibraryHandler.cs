using DevContextMcp.Server.Core.Contracts.ResolveLibrary;

namespace DevContextMcp.Server.Core.Services;

// Handles the resolve_library tool request.
public interface IResolveLibraryHandler
{
    Task<ResolveLibraryResponse> HandleAsync(ResolveLibraryRequest request, CancellationToken cancellationToken);
}
