namespace IChat.Core.UnitTests.Rag;

using IChat.Core.Rag;
using FluentAssertions;
using NUnit.Framework;

[TestFixture]
public class NeighborExpansionTests
{
    private static readonly Guid Doc = Guid.Parse("00000000-0000-0000-0000-00000000d001");

    [Test]
    public void PlanNeighborKeys_FirstChunkOfDocument_HasNoNegativeIndex()
    {
        // Arrange
        var selected = Selected(0);

        // Act
        var keys = NeighborExpansion.PlanNeighborKeys([selected], before: 1, after: 1);

        // Assert
        keys.Should().Equal((Doc, 1));
    }

    [Test]
    public void PlanNeighborKeys_ExcludesChunksAlreadySelected()
    {
        // Arrange
        var selected = new[] { Selected(3), Selected(4) };

        // Act
        var keys = NeighborExpansion.PlanNeighborKeys(selected, before: 1, after: 1);

        // Assert
        keys.Should().Equal((Doc, 2), (Doc, 5));
    }

    [Test]
    public void PlanNeighborKeys_DeduplicatesSharedNeighbors()
    {
        // Arrange
        var selected = new[] { Selected(2), Selected(4) };

        // Act
        var keys = NeighborExpansion.PlanNeighborKeys(selected, before: 1, after: 1);

        // Assert
        keys.Should().Equal((Doc, 1), (Doc, 3), (Doc, 5));
        keys.Should().OnlyHaveUniqueItems();
    }

    [Test]
    public void Expand_LastChunkOfDocument_MissingNeighborIsSkipped()
    {
        // Arrange
        var selected = Selected(9);
        var neighbors = new[] { new NeighborChunk { DocumentId = Doc, ChunkIndex = 8, Content = "truoc" } };

        // Act
        var result = NeighborExpansion.Expand([selected], neighbors, before: 1, after: 1);

        // Assert
        var block = result.Should().ContainSingle().Subject;
        block.Text.Should().Contain("truoc").And.Contain("noi dung chunk 9");
        block.StartChunkIndex.Should().Be(8);
        block.EndChunkIndex.Should().Be(9);
    }

    [Test]
    public void Expand_TwoAdjacentSelectedChunks_MergeIntoOneBlockNotTwo()
    {
        // Arrange
        var selected = new[] { Selected(3, 0.9), Selected(4, 0.8) };
        var neighbors = new[]
        {
            new NeighborChunk { DocumentId = Doc, ChunkIndex = 2, Content = "truoc" },
            new NeighborChunk { DocumentId = Doc, ChunkIndex = 5, Content = "sau" }
        };

        // Act
        var result = NeighborExpansion.Expand(selected, neighbors, before: 1, after: 1);

        // Assert
        var block = result.Should().ContainSingle("two adjacent chunks must merge rather than duplicate").Subject;
        block.AnchorChunkIds.Should().HaveCount(2);
        block.StartChunkIndex.Should().Be(2);
        block.EndChunkIndex.Should().Be(5);
    }

    [Test]
    public void Expand_DistantSelectedChunks_StayAsSeparateBlocks()
    {
        // Arrange
        var selected = new[] { Selected(1), Selected(20) };

        // Act
        var result = NeighborExpansion.Expand(selected, [], before: 1, after: 1);

        // Assert
        result.Should().HaveCount(2);
    }

    [Test]
    public void Expand_NeighborsAreNotPromotedToAnchors()
    {
        // Arrange
        var anchor = Selected(5);
        var neighbors = new[]
        {
            new NeighborChunk { DocumentId = Doc, ChunkIndex = 4, Content = "truoc" },
            new NeighborChunk { DocumentId = Doc, ChunkIndex = 6, Content = "sau" }
        };

        // Act
        var result = NeighborExpansion.Expand([anchor], neighbors, 1, 1);

        // Assert
        result.Single().AnchorChunkIds.Should().Equal(anchor.ChunkId);
    }

    [Test]
    public void JoinTrimmingOverlap_RemovesDuplicatedOverlapRegion()
    {
        // Arrange
        var shared = new string('x', 40) + " doan van chung giua hai chunk ";
        var first = "phan dau cua chunk mot. " + shared;
        var second = shared + "phan cuoi cua chunk hai.";

        // Act
        var joined = NeighborExpansion.JoinTrimmingOverlap([first, second]);

        // Assert
        joined.Should().Contain("phan dau cua chunk mot");
        joined.Should().Contain("phan cuoi cua chunk hai");
        CountOccurrences(joined, shared.Trim()).Should().Be(1, "the overlapping part may appear only once");
    }

    [Test]
    public void JoinTrimmingOverlap_NoOverlap_KeepsBothParts()
    {
        // Arrange
        string[] parts = ["hoan toan khac nhau mot", "hoan toan khac biet hai"];

        // Act
        var joined = NeighborExpansion.JoinTrimmingOverlap(parts);

        // Assert
        joined.Should().Contain("mot").And.Contain("hai");
    }

    [Test]
    public void JoinTrimmingOverlap_EmptyInput_ReturnsEmpty()
    {
        // Arrange — nothing to join

        // Act
        var joined = NeighborExpansion.JoinTrimmingOverlap([]);

        // Assert
        joined.Should().BeEmpty();
    }

    private static ScoredChunk Selected(int index, double score = 1.0, string? content = null)
    {
        return new ScoredChunk
        {
            ChunkId = Guid.Parse($"00000000-0000-0000-0000-{index:D12}"),
            DocumentId = Doc,
            DocumentTitle = "Tài liệu",
            Content = content ?? $"noi dung chunk {index}",
            HeadingPath = "A > B",
            ChunkIndex = index,
            Score = score
        };
    }

    private static int CountOccurrences(string haystack, string needle)
    {
        var count = 0;
        var index = 0;
        while ((index = haystack.IndexOf(needle, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += needle.Length;
        }

        return count;
    }
}
