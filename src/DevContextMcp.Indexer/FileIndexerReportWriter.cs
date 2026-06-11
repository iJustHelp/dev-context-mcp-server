using System.Text;
using DevContextMcp.Indexer.Configuration;
using Microsoft.Extensions.Options;

namespace DevContextMcp.Indexer;

internal sealed class FileIndexerReportWriter(IOptions<IndexerOptions> options) :
    IIndexerReportWriter
{
    public async Task WriteAsync(
        string report,
        CancellationToken cancellationToken)
    {
        var path = Path.GetFullPath(
            options.Value.ReportPath,
            AppContext.BaseDirectory);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        await File.AppendAllTextAsync(
            path,
            report + Environment.NewLine + Environment.NewLine,
            Encoding.UTF8,
            cancellationToken);
    }
}
