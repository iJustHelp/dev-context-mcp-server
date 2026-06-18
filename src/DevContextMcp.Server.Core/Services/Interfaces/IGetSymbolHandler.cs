using DevContextMcp.Server.Core.Contracts.GetSymbol;

namespace DevContextMcp.Server.Core.Services;

/// <summary>
/// Handles the get_symbol tool request.
/// </summary>
public interface IGetSymbolHandler
{
    Task<GetSymbolResponse> HandleAsync(GetSymbolRequest request, CancellationToken cancellationToken);
}
