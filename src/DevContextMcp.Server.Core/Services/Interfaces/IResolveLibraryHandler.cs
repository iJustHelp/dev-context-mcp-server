using DevContextMcp.Server.Core.Contracts.ResolveLibrary;

namespace DevContextMcp.Server.Core.Services;

/// <summary>
/// Handles the resolve_library tool request.
/// </summary>
public interface IResolveLibraryHandler
{
    Task<ResolveLibraryResponse> HandleAsync(ResolveLibraryRequest request, CancellationToken cancellationToken);
}
