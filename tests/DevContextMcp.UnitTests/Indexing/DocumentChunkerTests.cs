using DevContextMcp.Infrastructure.Indexer.Processing;

namespace DevContextMcp.UnitTests.Indexing;

public sealed class DocumentChunkerTests
{
    private readonly DocumentChunker _chunker = new(new Sha256ContentHasher());

    [Fact]
    public void XmlDocumentationIsChunkedPerMember()
    {
        const string xml =
            """
            <doc>
              <members>
                <member name="T:Fixture.Widget"><summary>A fixture widget.</summary></member>
                <member name="M:Fixture.Widget.Run"><summary>Runs the widget.</summary></member>
              </members>
            </doc>
            """;

        var chunks = _chunker.Chunk(
            path: "lib/net10.0/Fixture.xml",
            kind: "xml_documentation",
            content: xml,
            maxCharacters: 4000,
            minCharacters: 0);

        Assert.Equal(2, chunks.Count);
        Assert.Contains(chunks, chunk => chunk.MemberName == "T:Fixture.Widget");
        Assert.Contains(chunks, chunk => chunk.Content.Contains("fixture widget", StringComparison.Ordinal));
    }

    [Fact]
    public void MarkdownHeadingWithBlankLineStaysWithBody()
    {
        const string content =
            """
            ## Getting Started

            Install the package via NuGet and call Initialize() to begin using the library.
            """;

        var chunks = _chunker.Chunk(
            path: "README.md",
            kind: "readme",
            content: content,
            maxCharacters: 4000,
            minCharacters: 0);

        Assert.Contains(
            chunks,
            chunk => chunk.Content.Contains("Getting Started", StringComparison.Ordinal)
                && chunk.Content.Contains("Initialize", StringComparison.Ordinal));
        Assert.DoesNotContain(
            chunks,
            chunk => chunk.Content.Equals("## Getting Started", StringComparison.Ordinal));
    }

    [Fact]
    public void NestedMarkdownHeadingsStayWithFollowingBody()
    {
        const string content =
            """
            # Fixture Documentation

            ## Getting Started

            Install the package via NuGet and call Initialize().
            """;

        var chunks = _chunker.Chunk(
            path: "README.md",
            kind: "readme",
            content: content,
            maxCharacters: 4000,
            minCharacters: 0);

        Assert.Contains(
            chunks,
            chunk => chunk.Content.Contains("Getting Started", StringComparison.Ordinal)
                && chunk.Content.Contains("Initialize", StringComparison.Ordinal));
    }

    [Fact]
    public void TextChunksRespectMaximumLength()
    {
        var chunks = _chunker.Chunk(
            path: "README.md",
            kind: "readme",
            content: string.Join(' ', Enumerable.Repeat("documentation", 100)),
            maxCharacters: 100,
            minCharacters: 0);

        Assert.True(chunks.Count > 1);
        Assert.All(chunks, chunk => Assert.True(chunk.Content.Length <= 100));
    }
}
