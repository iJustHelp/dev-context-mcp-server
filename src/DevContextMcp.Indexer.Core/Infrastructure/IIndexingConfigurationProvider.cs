using DevContextMcp.Indexer.Core.Models;

namespace DevContextMcp.Indexer.Core.Infrastructure;

/// <summary>
/// Supplies the resolved indexing settings for the current run.
/// </summary>
public interface IIndexingConfigurationProvider
{
    IndexingSettings GetSettings();
}
