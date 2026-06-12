using System.Text;
using DevContextMcp.Indexer.Core.Infrastructure;
using DevContextMcp.Indexer.Core.Models;

namespace DevContextMcp.Infrastructure.Indexer.Processing;

internal sealed class DocumentationSourceReader(
    IDocumentChunker chunker,
    IContentHasher hasher) : IDocumentationSourceReader
{
    private static readonly UTF8Encoding StrictUtf8 = new(
        encoderShouldEmitUTF8Identifier: false,
        throwOnInvalidBytes: true);

    public async Task<DocumentationIndexData> ReadAsync(
        DocumentationSourceDefinition source,
        PackageProcessingLimits limits,
        CancellationToken cancellationToken)
    {
        var root = Path.GetFullPath(source.RootPath);
        if (!Directory.Exists(root))
        {
            throw new DirectoryNotFoundException(
                $"Documentation root '{root}' does not exist.");
        }
        if (File.GetAttributes(root).HasFlag(FileAttributes.ReparsePoint))
        {
            throw new InvalidDataException(
                $"Documentation root '{root}' must not be a link or reparse point.");
        }

        var artifacts = new List<ArtifactRecord>();
        var documents = new List<DocumentChunkRecord>();
        foreach (var file in EnumerateFiles(root)
                     .Where(path => source.Extensions.Contains(Path.GetExtension(path)))
                     .Order(StringComparer.Ordinal))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var bytes = await File.ReadAllBytesAsync(file, cancellationToken);
            if (bytes.LongLength > limits.MaxDocumentBytes)
            {
                throw new InvalidDataException(
                    $"Documentation file '{file}' exceeds the maximum size.");
            }

            var relativePath = NormalizeRelativePath(root, file);
            var content = StrictUtf8.GetString(bytes).TrimStart('\uFEFF');
            var contentHash = hasher.Hash(bytes);
            artifacts.Add(new(
                relativePath,
                "company_document",
                contentHash,
                bytes.LongLength,
                content));
            documents.AddRange(chunker.Chunk(
                relativePath,
                "company_document",
                content,
                limits.MaxDocumentChars));
        }

        var snapshot = string.Join(
            '\n',
            artifacts.Select(artifact => $"{artifact.Path}\n{artifact.ContentHash}"));
        return new(
            hasher.Hash(Encoding.UTF8.GetBytes(snapshot)),
            artifacts,
            documents);
    }

    private static IEnumerable<string> EnumerateFiles(string root)
    {
        var pending = new Stack<string>();
        pending.Push(root);

        while (pending.Count > 0)
        {
            var directory = pending.Pop();
            foreach (var entry in new DirectoryInfo(directory).EnumerateFileSystemInfos())
            {
                if (IsHiddenOrReparsePoint(entry))
                {
                    continue;
                }

                if (entry is DirectoryInfo child)
                {
                    pending.Push(child.FullName);
                }
                else if (entry is FileInfo file)
                {
                    yield return file.FullName;
                }
            }
        }
    }

    private static bool IsHiddenOrReparsePoint(FileSystemInfo entry) =>
        entry.Name.StartsWith(".", StringComparison.Ordinal)
        || entry.Attributes.HasFlag(FileAttributes.Hidden)
        || entry.Attributes.HasFlag(FileAttributes.ReparsePoint);

    private static string NormalizeRelativePath(string root, string file)
    {
        var relative = Path.GetRelativePath(root, Path.GetFullPath(file))
            .Replace('\\', '/');
        if (relative.Length == 0
            || relative.Equals("..", StringComparison.Ordinal)
            || relative.StartsWith("../", StringComparison.Ordinal))
        {
            throw new InvalidDataException(
                $"Documentation file '{file}' is outside the configured root.");
        }

        return relative;
    }
}
