namespace DevContextMcp.Indexer.Core.Models;

/// <summary>
/// Safety limits applied while downloading and extracting packages and documents.
/// </summary>
public sealed record PackageProcessingLimits(
    long MaxPackageBytes,
    long MaxDocumentBytes,
    int MaxArchiveEntries,
    long MaxExtractedBytes,
    double MaxCompressionRatio,
    int MaxDocumentChars,
    TimeSpan PackageDownloadTimeout);
