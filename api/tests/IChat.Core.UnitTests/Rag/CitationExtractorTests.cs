namespace IChat.Core.UnitTests.Rag;

using IChat.Core.Rag;
using FluentAssertions;
using Xunit;

public class CitationExtractorTests
{
    [Fact]
    public void Extract_NoMarkers_ReturnsEmpty()
    {
        var citations = CitationExtractor.Extract("Câu trả lời không có trích dẫn nào.", 5, out var invalid);

        citations.Should().BeEmpty();
        invalid.Should().BeEmpty();
    }

    [Fact]
    public void Extract_MarkerOutOfRange_IsDropped()
    {
        var citations = CitationExtractor.Extract("Theo tài liệu [1] và [99].", sourceCount: 3, out var invalid);

        citations.Select(citation => citation.MarkerIndex).Should().Equal(1);
        invalid.Should().Equal(99);
    }

    [Fact]
    public void Extract_RepeatedMarker_RecordedOnce()
    {
        var citations = CitationExtractor.Extract("[2] nói vậy, và [2] cũng xác nhận, xem thêm [2].", 3, out _);

        citations.Should().ContainSingle().Which.MarkerIndex.Should().Be(2);
    }

    [Fact]
    public void Extract_MarkersInsideFencedCodeBlock_AreIgnored()
    {
        var answer = """
            Xem đoạn mã sau:

            ```csharp
            var arr = new int[10];
            var x = arr[1];
            var y = arr[2];
            ```

            Theo tài liệu [3].
            """;

        var citations = CitationExtractor.Extract(answer, 5, out _);

        citations.Select(citation => citation.MarkerIndex).Should().Equal(3);
    }

    [Fact]
    public void Extract_MarkersInsideInlineCode_AreIgnored()
    {
        var citations = CitationExtractor.Extract("Dùng `list[1]` để lấy phần tử. Theo [2].", 5, out _);

        citations.Select(citation => citation.MarkerIndex).Should().Equal(2);
    }

    [Fact]
    public void Extract_PreservesOrderOfFirstAppearance()
    {
        var citations = CitationExtractor.Extract("[3] rồi [1] rồi [2].", 5, out _);

        citations.Select(citation => citation.MarkerIndex).Should().Equal(3, 1, 2);
    }

    [Fact]
    public void Extract_SourceOrdinalIsZeroBased()
    {
        var citations = CitationExtractor.Extract("[1]", 3, out _);

        citations.Single().SourceOrdinal.Should().Be(0);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Extract_EmptyAnswer_ReturnsEmpty(string? answer)
    {
        CitationExtractor.Extract(answer, 5, out _).Should().BeEmpty();
    }

    [Fact]
    public void Extract_ZeroSources_ReturnsEmpty()
    {
        CitationExtractor.Extract("[1]", 0, out _).Should().BeEmpty();
    }

    [Fact]
    public void Extract_MarkerZero_IsInvalid()
    {
        var citations = CitationExtractor.Extract("[0]", 3, out var invalid);

        citations.Should().BeEmpty();
        invalid.Should().Equal(0);
    }
}
