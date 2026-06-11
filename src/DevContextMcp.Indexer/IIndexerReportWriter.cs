namespace DevContextMcp.Indexer;

internal interface IIndexerReportWriter
{
    Task WriteAsync(string report, CancellationToken cancellationToken);
}
