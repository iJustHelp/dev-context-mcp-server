using DevContextMcp.Server.Core.Contracts.GetSymbol;

namespace DevContextMcp.Server.Core.Retrieval.Services;

public interface IGetSymbolHandler
{
    Task<GetSymbolResponse> HandleAsync(GetSymbolRequest request, CancellationToken cancellationToken);
}
