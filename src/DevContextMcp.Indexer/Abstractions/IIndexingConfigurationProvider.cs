using DevContextMcp.Indexer.Models;

namespace DevContextMcp.Indexer.Abstractions;

public interface IIndexingConfigurationProvider
{
    IndexingSettings GetSettings();
}
