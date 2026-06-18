namespace DevContextMcp.Server.Core.Models;

// Limits that bound retrieval requests: result counts, response size, timeout, and scoring.
public sealed record RetrievalLimits(
    int DefaultMaxResults,
    int MaxResults,
    int MaxResponseBytes,
    TimeSpan QueryTimeout,
    double MinimumEvidenceScore,
    int AmbiguousSymbolLimit);
