namespace DevContextMcp.Indexer.Core.Models;

// Safety limits applied while downloading and extracting packages and documents.
public sealed record PackageProcessingLimits(
    long MaxPackageBytes,
    long MaxDocumentBytes,
    int MaxArchiveEntries,
    long MaxExtractedBytes,
    double MaxCompressionRatio,
    int MaxDocumentChars,
    TimeSpan PackageDownloadTimeout);
