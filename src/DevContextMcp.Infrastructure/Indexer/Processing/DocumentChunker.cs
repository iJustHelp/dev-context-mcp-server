using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using DevContextMcp.Indexer.Core.Infrastructure;
using DevContextMcp.Indexer.Core.Models;

namespace DevContextMcp.Infrastructure.Indexer.Processing;

/// <summary>
/// Splits documentation into searchable, size-bounded records while preserving
/// XML member names and natural text boundaries when possible.
/// </summary>
internal sealed partial class DocumentChunker(IContentHasher hasher)
{
    public IReadOnlyList<DocumentChunkRecord> Chunk(
        string path,
        string kind,
        string content,
        int maxCharacters,
        int minCharacters)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxCharacters);

        return kind.Equals("xml_documentation", StringComparison.Ordinal)
            ? ChunkXml(path, content, maxCharacters, minCharacters)
            : ChunkText(
                path: path,
                kind: kind,
                content: content,
                maxCharacters: maxCharacters,
                minCharacters: minCharacters);
    }

    private IReadOnlyList<DocumentChunkRecord> ChunkXml(
        string path,
        string content,
        int maxCharacters,
        int minCharacters)
    {
        try
        {
            var document = XDocument.Parse(content, LoadOptions.PreserveWhitespace);
            var members = document.Root?
                .Element("members")?
                .Elements("member")
                .ToArray() ?? [];

            if (members.Length == 0)
            {
                // Some XML documentation files are incomplete or use a nonstandard
                // shape. Index their raw text instead of silently dropping them.
                return ChunkText(
                    path: path,
                    kind: "xml_documentation",
                    content: content,
                    maxCharacters: maxCharacters,
                    minCharacters: minCharacters);
            }

            var chunks = new List<DocumentChunkRecord>();
            foreach (var member in members)
            {
                var memberName = member.Attribute("name")?.Value;
                var text = NormalizeWhitespace(string.Join(
                    Environment.NewLine,
                    member.Elements().Select(element =>
                        $"{element.Name.LocalName}: {NormalizeWhitespace(element.Value)}")));

                AddBoundedChunks(
                    chunks: chunks,
                    path: path,
                    kind: "xml_documentation",
                    memberName: memberName,
                    content: text,
                    maxCharacters: maxCharacters,
                    minCharacters: minCharacters);
            }

            return chunks;
        }
        catch (Exception exception) when (
            exception is InvalidOperationException or System.Xml.XmlException)
        {
            // Malformed XML can still contain useful documentation text.
            return ChunkText(
                path: path,
                kind: "xml_documentation",
                content: content,
                maxCharacters: maxCharacters,
                minCharacters: minCharacters);
        }
    }

    private IReadOnlyList<DocumentChunkRecord> ChunkText(
        string path,
        string kind,
        string content,
        int maxCharacters,
        int minCharacters)
    {
        var chunks = new List<DocumentChunkRecord>();
        var sections = SplitSections(content);

        foreach (var section in sections)
        {
            AddBoundedChunks(
                chunks: chunks,
                path: path,
                kind: kind,
                memberName: null,
                content: section,
                maxCharacters: maxCharacters,
                minCharacters: minCharacters);
        }

        return chunks;
    }

    private void AddBoundedChunks(
        List<DocumentChunkRecord> chunks,
        string path,
        string kind,
        string? memberName,
        string content,
        int maxCharacters,
        int minCharacters)
    {
        var remaining = content.Trim();
        while (remaining.Length > 0)
        {
            var length = Math.Min(maxCharacters, remaining.Length);
            if (length < remaining.Length)
            {
                // Prefer a nearby paragraph, sentence, or word boundary, but do
                // not create a very small chunk just to avoid a hard split.
                var boundary = remaining.LastIndexOfAny(
                    ['\n', '.', ' ', ';', ','],
                    length - 1,
                    length);
                if (boundary >= maxCharacters / 2)
                {
                    length = boundary + 1;
                }
            }

            var chunk = remaining[..length].Trim();
            remaining = remaining[length..].TrimStart();
            if (chunk.Length < minCharacters)
            {
                continue;
            }

            chunks.Add(new DocumentChunkRecord(
                Path: path,
                Kind: kind,
                MemberName: memberName,
                // The index is global to the document so ordering remains stable
                // when one section or XML member produces multiple chunks.
                Ordinal: chunks.Count,
                Content: chunk,
                ContentHash: hasher.Hash(Encoding.UTF8.GetBytes(chunk))));
        }
    }

    private static IReadOnlyList<string> SplitSections(string content)
    {
        var normalized = content.ReplaceLineEndings("\n").Trim();
        if (normalized.Length == 0)
        {
            return [];
        }

        var rawSections = normalized.Split(
            "\n\n",
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (rawSections.Length == 0)
        {
            return [normalized];
        }

        // Common README layout puts a blank line after headings. Keep the heading
        // and its body in one searchable chunk instead of indexing headings alone.
        var merged = new List<string>(rawSections.Length);
        for (var index = 0; index < rawSections.Length; index++)
        {
            var section = rawSections[index];
            while (IsMarkdownHeadingSection(section) && index + 1 < rawSections.Length)
            {
                index++;
                section = $"{section}\n\n{rawSections[index]}";
            }

            merged.Add(section);
        }

        return merged;
    }

    private static bool IsMarkdownHeadingSection(string section)
    {
        var lines = section.Split(
            '\n',
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        return lines.Length > 0
            && lines.All(line => MarkdownHeadingLine().IsMatch(line));
    }

    [GeneratedRegex(@"^#{1,6}\s+\S", RegexOptions.CultureInvariant)]
    private static partial Regex MarkdownHeadingLine();

    private static string NormalizeWhitespace(string value)
    {
        var builder = new StringBuilder(value.Length);
        var previousWasWhitespace = false;

        foreach (var character in value)
        {
            if (char.IsWhiteSpace(character))
            {
                if (!previousWasWhitespace)
                {
                    builder.Append(' ');
                }

                previousWasWhitespace = true;
            }
            else
            {
                builder.Append(character);
                previousWasWhitespace = false;
            }
        }

        return builder.ToString().Trim();
    }
}
