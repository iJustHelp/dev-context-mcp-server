using DevContextMcp.Server.Core.Contracts.GetSymbol;

namespace DevContextMcp.Server.Core.Services;

// Handles the get_symbol tool request.
public interface IGetSymbolHandler
{
    Task<GetSymbolResponse> HandleAsync(GetSymbolRequest request, CancellationToken cancellationToken);
}
