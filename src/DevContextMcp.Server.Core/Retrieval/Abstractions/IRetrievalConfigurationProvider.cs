using DevContextMcp.Server.Core.Retrieval.Models;

namespace DevContextMcp.Server.Core.Retrieval.Abstractions;

public interface IRetrievalConfigurationProvider
{
    RetrievalSettings GetSettings();
}
