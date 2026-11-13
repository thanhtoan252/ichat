namespace IChat.Core.UnitTests.Parsing;

using IChat.Core.Abstractions;
using IChat.Core.Common;
using IChat.Infrastructure.Ingestion.Parsing;
using FluentAssertions;
using NUnit.Framework;

[TestFixture]
public class DocumentFormatResolverTests
{
    private static readonly byte[] ZipHeader = [0x50, 0x4B, 0x03, 0x04, 0x00, 0x00, 0x00, 0x00];

    private static readonly byte[] Ole2Header = [0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1];

    private static readonly byte[] PdfHeader = [0x25, 0x50, 0x44, 0x46, 0x2D, 0x31, 0x2E, 0x37];

    private static readonly byte[] TextHeader = [0x48, 0x65, 0x6C, 0x6C, 0x6F, 0x0A, 0x0A, 0x0A];

    private DocumentParserResolver _resolver = null!;

    [SetUp]
    public void SetUp()
    {

        IDocumentParser[] parsers =
                [
                    new DocxDocumentParser(),
            new PdfDocumentParser(),
            new MarkdownDocumentParser(),
            new PlainTextDocumentParser()
                ];

        _resolver = new DocumentParserResolver(parsers, [new LegacyDocFormatDetector()]);
    }

    [Test]
    public void Resolve_LegacyDocExtension_Returns415WithGuidance()
    {

        const string fileName = "bao-cao.doc";

        var result = Resolve(fileName, "application/msword", Ole2Header);

        result.Error.Code.Should().Be(Error.UnsupportedMediaTypeCode);
        result.Error.Message.Should().Contain("save it as .docx");
    }

    [Test]
    public void Resolve_Ole2MagicUnderADocxName_IsStillRejectedAsLegacyDoc()
    {

        const string fileName = "bao-cao.docx";

        var result = Resolve(fileName, "application/octet-stream", Ole2Header);

        result.Error.Message.Should().Contain("Word 97-2003");
    }

    [Test]
    public void Resolve_DocxExtensionWithoutZipHeader_Returns415()
    {

        const string fileName = "gia-mao.docx";

        var result = Resolve(fileName, "application/octet-stream", TextHeader);

        result.Error.Code.Should().Be(Error.UnsupportedMediaTypeCode);
        result.Error.Message.Should().Be("The file has a .docx extension but its content is not an Office (zip) container.");
    }

    [Test]
    public void Resolve_DocxExtensionWithZipHeader_ResolvesToDocx()
    {

        const string fileName = "tai-lieu.docx";

        var result = Resolve(fileName, "application/octet-stream", ZipHeader);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(DocxDocumentParser.DocxContentType);
    }

    [Test]
    public void Resolve_NoExtensionButPdfMagic_ResolvesToPdf()
    {

        const string fileName = "khong-duoi-file";

        var result = Resolve(fileName, "application/octet-stream", PdfHeader);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(PdfDocumentParser.PdfContentType);
    }

    [TestCase("ghi-chu.md", "text/markdown")]
    [TestCase("ghi-chu.markdown", "text/markdown")]
    [TestCase("ghi-chu.txt", "text/plain")]
    public void Resolve_KnownExtension_ResolvesToItsContentType(string fileName, string expected)
    {

        var result = Resolve(fileName, "application/octet-stream", TextHeader);

        result.Value.Should().Be(expected);
    }

    [Test]
    public void Resolve_UnknownExtension_Returns415ListingWhatIsAccepted()
    {

        const string fileName = "bang-tinh.xlsx";

        var result = Resolve(fileName, "application/octet-stream", TextHeader);

        result.Error.Code.Should().Be(Error.UnsupportedMediaTypeCode);
        result.Error.Message.Should().Be("The '.xlsx' format is not supported. Only .docx, .pdf, .md and .txt are accepted.");
    }

    [Test]
    public void Resolve_UnknownExtension_FallsBackToTheDeclaredContentType()
    {

        const string fileName = "khong-duoi-file";

        var result = Resolve(fileName, "text/markdown", TextHeader);

        result.Value.Should().Be(MarkdownDocumentParser.MarkdownContentType);
    }

    [Test]
    public void Resolve_ReturnsTheParserOwningTheResolvedFormat()
    {

        const string fileName = "tai-lieu.pdf";

        var result = _resolver.Resolve(fileName, "application/octet-stream", PdfHeader);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeOfType<PdfDocumentParser>();
    }

    private Result<string> Resolve(string fileName, string declaredContentType, byte[] header)
    {
        return _resolver.ResolveContentType(fileName, declaredContentType, header);
    }
}
