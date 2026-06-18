namespace DevContextMcp.Server.Core.Models;

// A resource document returned to the client, with its text and MIME type.
public sealed record ResourceDocumentRecord(string Text, string MimeType);
