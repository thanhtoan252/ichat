namespace IChat.Core.UnitTests.Parsing;

using IChat.Core.Domain.Documents.Parsing;
using IChat.Infrastructure.Ingestion.Parsing;
using FluentAssertions;
using Xunit;

public class PdfDocumentParserTests
{
    private static readonly Lazy<ParsedDocument> Parsed = new(() =>
    {
        var path = Path.Combine(AppContext.BaseDirectory, "fixtures", "sample.pdf");
        using var stream = File.OpenRead(path);

        return new PdfDocumentParser().ParseAsync(stream, CancellationToken.None).GetAwaiter().GetResult();
    });

    private static IReadOnlyList<DocumentBlock> Blocks => Parsed.Value.Blocks;

    [Fact]
    public void Parse_ExtractsText()
    {
        string.Join(" ", Blocks.Select(block => block.Text))
            .Should().Contain("Timeout mac dinh la 120 giay");
    }

    [Fact]
    public void Parse_InfersHeadingsFromLargerFontSize()
    {
        var headings = Blocks.Where(block => block.Kind == BlockKind.Heading).Select(block => block.Text).ToList();

        headings.Should().Contain("Cai dat he thong");
        headings.Should().Contain("Cau hinh nang cao");
    }

    [Fact]
    public void Parse_BodyTextIsNotTreatedAsHeading()
    {
        Blocks.Where(block => block.Kind == BlockKind.Heading)
            .Should().NotContain(block => block.Text.Contains("Doan van mo dau"));
    }

    [Fact]
    public void Parse_PdfBlocksCarryPageNumber()
    {
        // page CHỈ có ý nghĩa với PDF; docx phải để null.
        Blocks.Should().OnlyContain(block => block.Metadata != null && block.Metadata.ContainsKey("page"));
        Blocks[0].Metadata!["page"].Should().Be("1");
    }

    [Fact]
    public void Parse_ReportsPdfFormat()
    {
        Parsed.Value.Metadata["format"].Should().Be("pdf");
    }
}
