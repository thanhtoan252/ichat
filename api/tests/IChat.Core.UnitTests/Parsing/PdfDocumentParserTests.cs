namespace IChat.Core.UnitTests.Parsing;

using IChat.Core.Domain.Documents.Parsing;
using IChat.Infrastructure.Ingestion.Parsing;
using FluentAssertions;
using NUnit.Framework;

[TestFixture]
public class PdfDocumentParserTests
{
    private ParsedDocument _parsed = null!;
    private IReadOnlyList<DocumentBlock> _blocks = null!;

    // Parse một lần cho cả fixture: mọi test dưới đây chỉ đọc, không sửa kết quả parse.
    [OneTimeSetUp]
    public async Task ParseTheFixtureOnce()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "fixtures", "sample.pdf");
        await using var stream = File.OpenRead(path);

        _parsed = await new PdfDocumentParser().ParseAsync(stream, CancellationToken.None);
        _blocks = _parsed.Blocks;
    }

    [Test]
    public void Parse_ExtractsText()
    {
        // Arrange & Act
        var allText = string.Join(" ", _blocks.Select(block => block.Text));

        // Assert
        allText.Should().Contain("Timeout mac dinh la 120 giay");
    }

    [Test]
    public void Parse_InfersHeadingsFromLargerFontSize()
    {
        // Arrange & Act
        var headings = _blocks.Where(block => block.Kind == BlockKind.Heading).Select(block => block.Text).ToList();

        // Assert
        headings.Should().Contain("Cai dat he thong");
        headings.Should().Contain("Cau hinh nang cao");
    }

    [Test]
    public void Parse_BodyTextIsNotTreatedAsHeading()
    {
        // Arrange & Act
        var headings = _blocks.Where(block => block.Kind == BlockKind.Heading);

        // Assert
        headings.Should().NotContain(block => block.Text.Contains("Doan van mo dau"));
    }

    [Test]
    public void Parse_PdfBlocksCarryPageNumber()
    {
        // Arrange & Act — parsing happened in OneTimeSetUp

        // Assert
        // page CHỈ có ý nghĩa với PDF; docx phải để null.
        _blocks.Should().OnlyContain(block => block.Metadata != null && block.Metadata.ContainsKey("page"));
        _blocks[0].Metadata!["page"].Should().Be("1");
    }

    [Test]
    public void Parse_ReportsPdfFormat()
    {
        // Arrange & Act — parsing happened in OneTimeSetUp

        // Assert
        _parsed.Metadata["format"].Should().Be("pdf");
    }
}
