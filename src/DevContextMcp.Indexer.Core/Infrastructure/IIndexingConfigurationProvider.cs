using DevContextMcp.Indexer.Core.Models;

namespace DevContextMcp.Indexer.Core.Infrastructure;

// Supplies the resolved indexing settings for the current run.
public interface IIndexingConfigurationProvider
{
    IndexingSettings GetSettings();
}
