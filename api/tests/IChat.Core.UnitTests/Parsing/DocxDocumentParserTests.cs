namespace IChat.Core.UnitTests.Parsing;

using IChat.Core.Domain.Documents.Parsing;
using IChat.Infrastructure.Ingestion.Parsing;
using FluentAssertions;
using Xunit;

public class DocxDocumentParserTests
{
    private static readonly Lazy<ParsedDocument> Parsed = new(() =>
    {
        var path = Path.Combine(AppContext.BaseDirectory, "fixtures", "sample-traps.docx");
        using var stream = File.OpenRead(path);

        return new DocxDocumentParser().ParseAsync(stream, CancellationToken.None).GetAwaiter().GetResult();
    });

    private static IReadOnlyList<DocumentBlock> Blocks => Parsed.Value.Blocks;

    private static string AllText => string.Join("\n", Blocks.Select(block => block.Text));

    [Fact]
    public void Parse_RecognisesHeadingLevelFromParagraphStyle()
    {
        Blocks.Should().Contain(block => block.Kind == BlockKind.Heading && block.Text == "Cai dat" && block.HeadingLevel == 1);
    }

    [Fact]
    public void Parse_RecognisesHeadingFromOutlineLevel_WhenStyleNameIsCustom()
    {
        // "TieuDeChuong" không khớp regex ^Heading[1-9]$ — chỉ nhận ra được qua w:outlineLvl.
        Blocks.Should().Contain(
            block => block.Kind == BlockKind.Heading && block.Text == "Cau hinh he thong" && block.HeadingLevel == 1,
            "enterprise templates often use custom style names, so it must fall back to w:outlineLvl");
    }

    [Fact]
    public void Parse_TrackedChanges_DeletedContentNeverReachesOutput()
    {
        AllText.Should().NotContain(
            "KHONG_DUOC_XUAT_HIEN_TRONG_INDEX",
            "content inside w:del has been deleted; indexing it would make ichat cite struck-out clauses");
    }

    [Fact]
    public void Parse_TrackedChanges_InsertedContentIsKept()
    {
        AllText.Should().Contain("VAN_CON_HIEU_LUC");
    }

    [Fact]
    public void Parse_FragmentedRunsAreMergedIntoOneBlock()
    {
        Blocks.Should().Contain(
            block => block.Text == "Day la mot cau bi Word cat thanh nhieu run lien nhau.",
            "join every w:t within a paragraph, otherwise the chunker receives a few characters of noise");
    }

    [Fact]
    public void Parse_TableBecomesSingleAtomicBlock()
    {
        var tables = Blocks.Where(block => block.Kind == BlockKind.Table).ToList();

        tables.Should().ContainSingle("a table is an atomic unit and is never split");
        tables[0].Text.Should().Contain("| Ten | Gia tri | Ghi chu |").And.Contain("| --- | --- | --- |");
    }

    [Fact]
    public void Parse_MergedCellKeepsColumnCountConsistent()
    {
        var table = Blocks.Single(block => block.Kind == BlockKind.Table);
        var rows = table.Text.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        var columnCounts = rows.Select(row => row.Count(character => character == '|')).Distinct().ToList();

        columnCounts.Should().ContainSingle("merged cells must be padded so every row has the same column count");
    }

    [Fact]
    public void Parse_TextBoxContentIsNotMissed()
    {
        AllText.Should().Contain(
            "NOI_DUNG_TRONG_TEXTBOX",
            "w:txbxContent sits outside the main paragraph flow, so it needs its own traversal");
    }

    [Fact]
    public void Parse_TextBoxContentIsNotDuplicated()
    {
        var occurrences = Blocks.Count(block => block.Text.Contains("NOI_DUNG_TRONG_TEXTBOX", StringComparison.Ordinal));

        occurrences.Should().Be(1);
    }

    [Fact]
    public void Parse_HeaderAndFooterAreExcluded()
    {
        AllText.Should().NotContain("HEADER_CONG_TY_KHONG_DUOC_VAO_INDEX");
        AllText.Should().NotContain("FOOTER_TRANG_KHONG_DUOC_VAO_INDEX");
    }

    [Fact]
    public void Parse_AutomaticTableOfContentsIsExcluded()
    {
        AllText.Should().NotContain(
            "MUC_LUC_TU_DONG_KHONG_DUOC_VAO_INDEX",
            "an auto-generated table of contents produces a chunk that matches every query about section names");
    }

    [Fact]
    public void Parse_HyperlinkKeepsDisplayText()
    {
        AllText.Should().Contain("Trang tai lieu chinh thuc");
    }

    [Fact]
    public void Parse_ImageAltTextIsCaptured()
    {
        AllText.Should().Contain("SO_DO_KIEN_TRUC_ALT_TEXT");
    }

    [Fact]
    public void Parse_ListItemsAreFlattenedWithMarker()
    {
        var listItems = Blocks.Where(block => block.Kind == BlockKind.ListItem).ToList();

        listItems.Should().HaveCount(2);
        listItems.Should().OnlyContain(block => block.Text.Contains("Muc danh sach"));
    }

    [Fact]
    public void Parse_BlockOrderIsSequential()
    {
        Blocks.Select(block => block.Order).Should().BeInAscendingOrder();
    }

    [Fact]
    public void Parse_LegacyDocBinary_ThrowsRatherThanProducingGarbage()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "fixtures", "sample-legacy.doc");
        using var stream = File.OpenRead(path);
        var parser = new DocxDocumentParser();

        // Resolver phải chặn .doc trước khi tới đây; test này chốt rằng nếu lọt tới
        // parser thì nó vẫn ném lỗi rõ ràng chứ không trả nội dung rác.
        var act = () => parser.ParseAsync(stream, CancellationToken.None).GetAwaiter().GetResult();

        act.Should().Throw<Exception>();
    }
}
