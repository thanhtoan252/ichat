namespace IChat.Core.UnitTests.Parsing;

using IChat.Core.Abstractions;
using IChat.Core.Common;
using IChat.Infrastructure.Ingestion.Parsing;
using FluentAssertions;
using Xunit;

public class DocumentFormatResolverTests
{
    private static readonly byte[] ZipHeader = [0x50, 0x4B, 0x03, 0x04, 0x00, 0x00, 0x00, 0x00];

    private static readonly byte[] Ole2Header = [0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1];

    private static readonly byte[] PdfHeader = [0x25, 0x50, 0x44, 0x46, 0x2D, 0x31, 0x2E, 0x37];

    private static readonly byte[] TextHeader = [0x48, 0x65, 0x6C, 0x6C, 0x6F, 0x0A, 0x0A, 0x0A];

    // .doc phải bị chặn TRƯỚC TIÊN kèm hướng dẫn, không bao giờ để nổ thành 500 trong OpenXml.
    [Fact]
    public void Resolve_LegacyDocExtension_Returns415WithGuidance()
    {
        var result = Resolve("bao-cao.doc", "application/msword", Ole2Header);

        result.Error.Code.Should().Be(Error.UnsupportedMediaTypeCode);
        result.Error.Message.Should().Contain("save it as .docx");
    }

    [Fact]
    public void Resolve_Ole2MagicUnderADocxName_IsStillRejectedAsLegacyDoc()
    {
        // Đổi đuôi file không đổi được nội dung: magic OLE2 vẫn là Word 97-2003.
        var result = Resolve("bao-cao.docx", "application/octet-stream", Ole2Header);

        result.Error.Message.Should().Contain("Word 97-2003");
    }

    [Fact]
    public void Resolve_DocxExtensionWithoutZipHeader_Returns415()
    {
        var result = Resolve("gia-mao.docx", "application/octet-stream", TextHeader);

        result.Error.Code.Should().Be(Error.UnsupportedMediaTypeCode);
        result.Error.Message.Should().Be("The file has a .docx extension but its content is not an Office (zip) container.");
    }

    [Fact]
    public void Resolve_DocxExtensionWithZipHeader_ResolvesToDocx()
    {
        var result = Resolve("tai-lieu.docx", "application/octet-stream", ZipHeader);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(DocxDocumentParser.DocxContentType);
    }

    [Fact]
    public void Resolve_NoExtensionButPdfMagic_ResolvesToPdf()
    {
        var result = Resolve("khong-duoi-file", "application/octet-stream", PdfHeader);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(PdfDocumentParser.PdfContentType);
    }

    [Theory]
    [InlineData("ghi-chu.md", "text/markdown")]
    [InlineData("ghi-chu.markdown", "text/markdown")]
    [InlineData("ghi-chu.txt", "text/plain")]
    public void Resolve_KnownExtension_ResolvesToItsContentType(string fileName, string expected)
    {
        Resolve(fileName, "application/octet-stream", TextHeader).Value.Should().Be(expected);
    }

    [Fact]
    public void Resolve_UnknownExtension_Returns415ListingWhatIsAccepted()
    {
        var result = Resolve("bang-tinh.xlsx", "application/octet-stream", TextHeader);

        result.Error.Code.Should().Be(Error.UnsupportedMediaTypeCode);
        result.Error.Message.Should().Be("The '.xlsx' format is not supported. Only .docx, .pdf, .md and .txt are accepted.");
    }

    // Trình duyệt hay gửi sai content type, nên nó chỉ được dùng khi đuôi lẫn magic đều câm.
    [Fact]
    public void Resolve_UnknownExtension_FallsBackToTheDeclaredContentType()
    {
        var result = Resolve("khong-duoi-file", "text/markdown", TextHeader);

        result.Value.Should().Be(MarkdownDocumentParser.MarkdownContentType);
    }

    [Fact]
    public void Resolve_ReturnsTheParserOwningTheResolvedFormat()
    {
        var result = Resolver().Resolve("tai-lieu.pdf", "application/octet-stream", PdfHeader);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeOfType<PdfDocumentParser>();
    }

    private static Result<string> Resolve(string fileName, string declaredContentType, byte[] header)
    {
        return Resolver().ResolveContentType(fileName, declaredContentType, header);
    }

    private static DocumentParserResolver Resolver()
    {
        // Thứ tự parser khớp với DependencyInjection: nó quyết định danh sách đuôi file
        // trong thông điệp 415.
        IDocumentParser[] parsers =
        [
            new DocxDocumentParser(),
            new PdfDocumentParser(),
            new MarkdownDocumentParser(),
            new PlainTextDocumentParser()
        ];

        return new DocumentParserResolver(parsers, [new LegacyDocFormatDetector()]);
    }
}
