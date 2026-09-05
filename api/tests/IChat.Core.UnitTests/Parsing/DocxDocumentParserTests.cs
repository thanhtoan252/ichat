namespace IChat.Core.UnitTests.Parsing;

using IChat.Core.Domain.Documents.Parsing;
using IChat.Infrastructure.Ingestion.Parsing;
using FluentAssertions;
using NUnit.Framework;

[TestFixture]
public class DocxDocumentParserTests
{
    private IReadOnlyList<DocumentBlock> _blocks = null!;
    private string _allText = null!;

    // Parse một lần cho cả fixture: mọi test dưới đây chỉ đọc, không sửa kết quả parse.
    [OneTimeSetUp]
    public async Task ParseTheFixtureOnce()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "fixtures", "sample-traps.docx");
        await using var stream = File.OpenRead(path);

        var parsed = await new DocxDocumentParser().ParseAsync(stream, CancellationToken.None);

        _blocks = parsed.Blocks;
        _allText = string.Join("\n", _blocks.Select(block => block.Text));
    }

    [Test]
    public void Parse_RecognisesHeadingLevelFromParagraphStyle()
    {
        // Arrange & Act — parsing happened in OneTimeSetUp

        // Assert
        _blocks.Should().Contain(block => block.Kind == BlockKind.Heading && block.Text == "Cai dat" && block.HeadingLevel == 1);
    }

    [Test]
    public void Parse_RecognisesHeadingFromOutlineLevel_WhenStyleNameIsCustom()
    {
        // Arrange & Act — parsing happened in OneTimeSetUp

        // Assert
        // "TieuDeChuong" không khớp regex ^Heading[1-9]$ — chỉ nhận ra được qua w:outlineLvl.
        _blocks.Should().Contain(
            block => block.Kind == BlockKind.Heading && block.Text == "Cau hinh he thong" && block.HeadingLevel == 1,
            "enterprise templates often use custom style names, so it must fall back to w:outlineLvl");
    }

    [Test]
    public void Parse_TrackedChanges_DeletedContentNeverReachesOutput()
    {
        // Arrange & Act — parsing happened in OneTimeSetUp

        // Assert
        _allText.Should().NotContain(
            "KHONG_DUOC_XUAT_HIEN_TRONG_INDEX",
            "content inside w:del has been deleted; indexing it would make ichat cite struck-out clauses");
    }

    [Test]
    public void Parse_TrackedChanges_InsertedContentIsKept()
    {
        // Arrange & Act — parsing happened in OneTimeSetUp

        // Assert
        _allText.Should().Contain("VAN_CON_HIEU_LUC");
    }

    [Test]
    public void Parse_FragmentedRunsAreMergedIntoOneBlock()
    {
        // Arrange & Act — parsing happened in OneTimeSetUp

        // Assert
        _blocks.Should().Contain(
            block => block.Text == "Day la mot cau bi Word cat thanh nhieu run lien nhau.",
            "join every w:t within a paragraph, otherwise the chunker receives a few characters of noise");
    }

    [Test]
    public void Parse_TableBecomesSingleAtomicBlock()
    {
        // Arrange & Act
        var tables = _blocks.Where(block => block.Kind == BlockKind.Table).ToList();

        // Assert
        tables.Should().ContainSingle("a table is an atomic unit and is never split");
        tables[0].Text.Should().Contain("| Ten | Gia tri | Ghi chu |").And.Contain("| --- | --- | --- |");
    }

    [Test]
    public void Parse_MergedCellKeepsColumnCountConsistent()
    {
        // Arrange
        var table = _blocks.Single(block => block.Kind == BlockKind.Table);
        var rows = table.Text.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        // Act
        var columnCounts = rows.Select(row => row.Count(character => character == '|')).Distinct().ToList();

        // Assert
        columnCounts.Should().ContainSingle("merged cells must be padded so every row has the same column count");
    }

    [Test]
    public void Parse_TextBoxContentIsNotMissed()
    {
        // Arrange & Act — parsing happened in OneTimeSetUp

        // Assert
        _allText.Should().Contain(
            "NOI_DUNG_TRONG_TEXTBOX",
            "w:txbxContent sits outside the main paragraph flow, so it needs its own traversal");
    }

    [Test]
    public void Parse_TextBoxContentIsNotDuplicated()
    {
        // Arrange & Act
        var occurrences = _blocks.Count(block => block.Text.Contains("NOI_DUNG_TRONG_TEXTBOX", StringComparison.Ordinal));

        // Assert
        occurrences.Should().Be(1);
    }

    [Test]
    public void Parse_HeaderAndFooterAreExcluded()
    {
        // Arrange & Act — parsing happened in OneTimeSetUp

        // Assert
        _allText.Should().NotContain("HEADER_CONG_TY_KHONG_DUOC_VAO_INDEX");
        _allText.Should().NotContain("FOOTER_TRANG_KHONG_DUOC_VAO_INDEX");
    }

    [Test]
    public void Parse_AutomaticTableOfContentsIsExcluded()
    {
        // Arrange & Act — parsing happened in OneTimeSetUp

        // Assert
        _allText.Should().NotContain(
            "MUC_LUC_TU_DONG_KHONG_DUOC_VAO_INDEX",
            "an auto-generated table of contents produces a chunk that matches every query about section names");
    }

    [Test]
    public void Parse_HyperlinkKeepsDisplayText()
    {
        // Arrange & Act — parsing happened in OneTimeSetUp

        // Assert
        _allText.Should().Contain("Trang tai lieu chinh thuc");
    }

    [Test]
    public void Parse_ImageAltTextIsCaptured()
    {
        // Arrange & Act — parsing happened in OneTimeSetUp

        // Assert
        _allText.Should().Contain("SO_DO_KIEN_TRUC_ALT_TEXT");
    }

    [Test]
    public void Parse_ListItemsAreFlattenedWithMarker()
    {
        // Arrange & Act
        var listItems = _blocks.Where(block => block.Kind == BlockKind.ListItem).ToList();

        // Assert
        listItems.Should().HaveCount(2);
        listItems.Should().OnlyContain(block => block.Text.Contains("Muc danh sach"));
    }

    [Test]
    public void Parse_BlockOrderIsSequential()
    {
        // Arrange & Act
        var orders = _blocks.Select(block => block.Order);

        // Assert
        orders.Should().BeInAscendingOrder();
    }

    [Test]
    public void Parse_LegacyDocBinary_ThrowsRatherThanProducingGarbage()
    {
        // Arrange
        var path = Path.Combine(AppContext.BaseDirectory, "fixtures", "sample-legacy.doc");
        using var stream = File.OpenRead(path);
        var parser = new DocxDocumentParser();

        // Act
        // Resolver phải chặn .doc trước khi tới đây; test này chốt rằng nếu lọt tới
        // parser thì nó vẫn ném lỗi rõ ràng chứ không trả nội dung rác.
        var act = () => parser.ParseAsync(stream, CancellationToken.None).GetAwaiter().GetResult();

        // Assert
        act.Should().Throw<Exception>();
    }
}
