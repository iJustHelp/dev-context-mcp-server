namespace DevContextMcp.Server.Core.Models;

/// <summary>
/// Limits that bound retrieval requests: result counts, response size, timeout, and scoring.
/// </summary>
public sealed record RetrievalLimits(
    int DefaultMaxResults,
    int MaxResults,
    int MaxResponseBytes,
    TimeSpan QueryTimeout,
    double MinimumEvidenceScore,
    int AmbiguousSymbolLimit);
