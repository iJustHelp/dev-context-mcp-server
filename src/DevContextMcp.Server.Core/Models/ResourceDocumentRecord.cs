namespace DevContextMcp.Server.Core.Models;

/// <summary>
/// A resource document returned to the client, with its text and MIME type.
/// </summary>
public sealed record ResourceDocumentRecord(string Text, string MimeType);
