namespace IChat.Core.UnitTests.Rag;

using IChat.Core.Rag;
using FluentAssertions;
using NUnit.Framework;

[TestFixture]
public class CitationExtractorTests
{
    [Test]
    public void Extract_NoMarkers_ReturnsEmpty()
    {
        // Arrange
        const string answer = "Câu trả lời không có trích dẫn nào.";

        // Act
        var citations = CitationExtractor.Extract(answer, 5, out var invalid);

        // Assert
        citations.Should().BeEmpty();
        invalid.Should().BeEmpty();
    }

    [Test]
    public void Extract_MarkerOutOfRange_IsDropped()
    {
        // Arrange
        const string answer = "Theo tài liệu [1] và [99].";

        // Act
        var citations = CitationExtractor.Extract(answer, sourceCount: 3, out var invalid);

        // Assert
        citations.Select(citation => citation.MarkerIndex).Should().Equal(1);
        invalid.Should().Equal(99);
    }

    [Test]
    public void Extract_RepeatedMarker_RecordedOnce()
    {
        // Arrange
        const string answer = "[2] nói vậy, và [2] cũng xác nhận, xem thêm [2].";

        // Act
        var citations = CitationExtractor.Extract(answer, 3, out _);

        // Assert
        citations.Should().ContainSingle().Which.MarkerIndex.Should().Be(2);
    }

    [Test]
    public void Extract_MarkersInsideFencedCodeBlock_AreIgnored()
    {
        // Arrange
        const string answer = """
            Xem đoạn mã sau:

            ```csharp
            var arr = new int[10];
            var x = arr[1];
            var y = arr[2];
            ```

            Theo tài liệu [3].
            """;

        // Act
        var citations = CitationExtractor.Extract(answer, 5, out _);

        // Assert
        citations.Select(citation => citation.MarkerIndex).Should().Equal(3);
    }

    [Test]
    public void Extract_MarkersInsideInlineCode_AreIgnored()
    {
        // Arrange
        const string answer = "Dùng `list[1]` để lấy phần tử. Theo [2].";

        // Act
        var citations = CitationExtractor.Extract(answer, 5, out _);

        // Assert
        citations.Select(citation => citation.MarkerIndex).Should().Equal(2);
    }

    [Test]
    public void Extract_PreservesOrderOfFirstAppearance()
    {
        // Arrange
        const string answer = "[3] rồi [1] rồi [2].";

        // Act
        var citations = CitationExtractor.Extract(answer, 5, out _);

        // Assert
        citations.Select(citation => citation.MarkerIndex).Should().Equal(3, 1, 2);
    }

    [Test]
    public void Extract_SourceOrdinalIsZeroBased()
    {
        // Arrange
        const string answer = "[1]";

        // Act
        var citations = CitationExtractor.Extract(answer, 3, out _);

        // Assert
        citations.Single().SourceOrdinal.Should().Be(0);
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public void Extract_EmptyAnswer_ReturnsEmpty(string? answer)
    {
        // Arrange & Act
        var citations = CitationExtractor.Extract(answer, 5, out _);

        // Assert
        citations.Should().BeEmpty();
    }

    [Test]
    public void Extract_ZeroSources_ReturnsEmpty()
    {
        // Arrange
        const string answer = "[1]";

        // Act
        var citations = CitationExtractor.Extract(answer, 0, out _);

        // Assert
        citations.Should().BeEmpty();
    }

    [Test]
    public void Extract_MarkerZero_IsInvalid()
    {
        // Arrange
        const string answer = "[0]";

        // Act
        var citations = CitationExtractor.Extract(answer, 3, out var invalid);

        // Assert
        citations.Should().BeEmpty();
        invalid.Should().Equal(0);
    }
}
