namespace IChat.Core.UnitTests.Ingestion;

using IChat.Core.Abstractions;
using IChat.Core.Domain.Documents.Parsing;
using IChat.Core.Rag;
using IChat.Infrastructure.Ai;
using IChat.Infrastructure.Ingestion;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Xunit;

public class HeadingAwareChunkerTests
{
    private static HeadingAwareChunker Build(int targetTokens = 800, int overlapTokens = 120, int minTokens = 80, bool prepend = true)
    {
        var options = Options.Create(new RagOptions
        {
            Chunking = new ChunkingOptions
            {
                TargetTokens = targetTokens,
                OverlapTokens = overlapTokens,
                MinTokens = minTokens,
                PrependHeadingPath = prepend
            }
        });

        return new HeadingAwareChunker(options, new SimpleTokenEstimator());
    }

    private static DocumentBlock Heading(string text, int level, int order)
    {
        return new DocumentBlock
        {
            Kind = BlockKind.Heading,
            Text = text,
            HeadingLevel = level,
            Order = order
        };
    }

    private static DocumentBlock Paragraph(string text, int order)
    {
        return new DocumentBlock
        {
            Kind = BlockKind.Paragraph,
            Text = text,
            Order = order
        };
    }

    private static string LongText(int words) => string.Join(" ", Enumerable.Repeat("word", words));

    [Fact]
    public void Chunk_NeverMergesAcrossHeadingBoundary()
    {
        var blocks = new[]
        {
            Heading("Phan A", 1, 0),
            Paragraph("noi dung A", 1),
            Heading("Phan B", 1, 2),
            Paragraph("noi dung B", 3)
        };

        var chunks = Build(minTokens: 0).Chunk(blocks, "Tai lieu");

        chunks.Should().HaveCount(2);
        chunks[0].Content.Should().Contain("noi dung A").And.NotContain("noi dung B");
        chunks[1].Content.Should().Contain("noi dung B").And.NotContain("noi dung A");
    }

    [Fact]
    public void Chunk_BuildsMultiLevelHeadingPath()
    {
        var blocks = new[]
        {
            Heading("Cai dat", 1, 0),
            Heading("Cau hinh", 2, 1),
            Heading("Bien moi truong", 3, 2),
            Paragraph("noi dung sau cung", 3)
        };

        var chunks = Build(minTokens: 0).Chunk(blocks, "Tai lieu");

        chunks.Should().ContainSingle();
        chunks[0].HeadingPath.Should().Be("Cai dat > Cau hinh > Bien moi truong");
    }

    [Fact]
    public void Chunk_PopsHeadingStackWhenLevelGoesBackUp()
    {
        var blocks = new[]
        {
            Heading("A", 1, 0),
            Heading("A1", 2, 1),
            Paragraph("trong A1", 2),
            Heading("B", 1, 3),
            Paragraph("trong B", 4)
        };

        var chunks = Build(minTokens: 0).Chunk(blocks, "Tai lieu");

        chunks.Single(chunk => chunk.Content.Contains("trong A1")).HeadingPath.Should().Be("A > A1");
        chunks.Single(chunk => chunk.Content.Contains("trong B")).HeadingPath.Should().Be("B");
    }

    [Fact]
    public void Chunk_DocumentWithoutAnyHeading_StillProducesChunks()
    {
        var blocks = new[] { Paragraph("chi co doan van", 0), Paragraph("khong heading nao", 1) };

        var chunks = Build(minTokens: 0).Chunk(blocks, "Tai lieu");

        chunks.Should().NotBeEmpty();
        chunks.Should().OnlyContain(chunk => chunk.HeadingPath == null);
    }

    [Fact]
    public void Chunk_TableIsNeverSplit_EvenWhenLargerThanTarget()
    {
        var bigTable = "| a | b |\n| --- | --- |\n" + string.Join("\n", Enumerable.Range(0, 400).Select(i => $"| row{i} | val{i} |"));
        var blocks = new[]
        {
            Heading("Bang so lieu", 1, 0),
            new DocumentBlock
            {
                Kind = BlockKind.Table,
                Text = bigTable,
                Order = 1
            }
        };

        var chunks = Build(targetTokens: 100, minTokens: 0).Chunk(blocks, "Tai lieu");

        var tableChunks = chunks.Where(chunk => chunk.Content.Contains("row399")).ToList();
        tableChunks.Should().ContainSingle("a table is an atomic unit");
        tableChunks[0].Content.Should().Contain("| a | b |", "half a table without its column header is meaningless");
        tableChunks[0].Content.Should().Contain("row0");
    }

    [Fact]
    public void Chunk_TableKeepsContainsTableMetadata()
    {
        var blocks = new[]
        {
            new DocumentBlock
            {
                Kind = BlockKind.Table,
                Text = "| a |\n| --- |\n| 1 |",
                Order = 0
            }
        };

        var chunks = Build(minTokens: 0).Chunk(blocks, "Tai lieu");

        chunks[0].Metadata["containsTable"].Should().Be(true);
    }

    [Fact]
    public void Chunk_MergesChunksBelowMinTokens()
    {
        var blocks = new[]
        {
            Heading("Phan", 1, 0),
            Paragraph("ngan mot", 1),
            Paragraph("ngan hai", 2),
            Paragraph("ngan ba", 3)
        };

        var chunks = Build(targetTokens: 8, minTokens: 500).Chunk(blocks, "Tai lieu");

        chunks.Should().ContainSingle("every piece is below MinTokens, so they must be merged");
    }

    [Fact]
    public void Chunk_SplitsSectionLongerThanTargetTokens()
    {
        var blocks = new[] { Heading("Phan dai", 1, 0), Paragraph(LongText(3000), 1) };

        var chunks = Build(targetTokens: 200, overlapTokens: 20, minTokens: 0).Chunk(blocks, "Tai lieu");

        chunks.Count.Should().BeGreaterThan(1);
        chunks.Should().OnlyContain(chunk => chunk.HeadingPath == "Phan dai", "splitting inside a section must not lose the heading path");
    }

    [Fact]
    public void Chunk_ConsecutiveChunksOverlap()
    {
        var blocks = new[] { Paragraph(LongText(2000), 0) };

        var chunks = Build(targetTokens: 200, overlapTokens: 60, minTokens: 0).Chunk(blocks, "Tai lieu");

        chunks.Count.Should().BeGreaterThan(1);

        var firstTail = chunks[0].Content[^50..];
        chunks[1].Content.Should().Contain(firstTail[..20], "adjacent chunks must overlap so an answer straddling the boundary is not lost");
    }

    [Fact]
    public void Chunk_EmbeddedTextCarriesTitleAndHeadingPath()
    {
        var blocks = new[] { Heading("Cai dat", 1, 0), Heading("Cau hinh", 2, 1), Paragraph("noi dung", 2) };

        var chunks = Build(minTokens: 0).Chunk(blocks, "So tay ky thuat");

        chunks[0].EmbeddedText.Should().StartWith("So tay ky thuat > Cai dat > Cau hinh");
        chunks[0].EmbeddedText.Should().Contain("noi dung");
        chunks[0].Content.Should().Be("noi dung", "content stays verbatim for display and carries no heading prefix");
    }

    [Fact]
    public void Chunk_PrependHeadingPathDisabled_EmbeddedTextEqualsContent()
    {
        var blocks = new[] { Heading("Cai dat", 1, 0), Paragraph("noi dung", 1) };

        var chunks = Build(minTokens: 0, prepend: false).Chunk(blocks, "So tay");

        chunks[0].EmbeddedText.Should().Be(chunks[0].Content);
    }

    [Fact]
    public void Chunk_IndexesAreSequentialFromZero()
    {
        var blocks = new[]
        {
            Heading("A", 1, 0), Paragraph(LongText(100), 1),
            Heading("B", 1, 2), Paragraph(LongText(100), 3)
        };

        var chunks = Build(minTokens: 0).Chunk(blocks, "Tai lieu");

        chunks.Select(chunk => chunk.Index).Should().Equal(Enumerable.Range(0, chunks.Count));
    }

    [Fact]
    public void Chunk_DocxBlocksNeverGetFabricatedPageNumber()
    {
        var blocks = new[] { Heading("A", 1, 0), Paragraph("noi dung docx", 1) };

        var chunks = Build(minTokens: 0).Chunk(blocks, "Tai lieu");

        chunks[0].Metadata["page"].Should().BeNull("DOCX has no fixed pages until it is rendered");
        chunks[0].Metadata["startBlockIndex"].Should().Be(1);
    }

    [Fact]
    public void Chunk_PdfBlockKeepsPageNumber()
    {
        var blocks = new[]
        {
            new DocumentBlock
            {
                Kind = BlockKind.Paragraph,
                Text = "noi dung pdf",
                Order = 0,
                Metadata = new Dictionary<string, string> { ["page"] = "7" }
            }
        };

        var chunks = Build(minTokens: 0).Chunk(blocks, "Tai lieu");

        chunks[0].Metadata["page"].Should().Be(7);
    }

    [Fact]
    public void Chunk_EmptyInput_ReturnsEmpty()
    {
        Build().Chunk([], "Tai lieu").Should().BeEmpty();
    }
}
