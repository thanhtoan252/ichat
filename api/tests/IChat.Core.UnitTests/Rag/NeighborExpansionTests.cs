namespace IChat.Core.UnitTests.Rag;

using IChat.Core.Rag;
using FluentAssertions;
using Xunit;

public class NeighborExpansionTests
{
    private static readonly Guid Doc = Guid.Parse("00000000-0000-0000-0000-00000000d001");

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

    [Fact]
    public void PlanNeighborKeys_FirstChunkOfDocument_HasNoNegativeIndex()
    {
        var keys = NeighborExpansion.PlanNeighborKeys([Selected(0)], before: 1, after: 1);

        keys.Should().Equal((Doc, 1));
    }

    [Fact]
    public void PlanNeighborKeys_ExcludesChunksAlreadySelected()
    {
        var keys = NeighborExpansion.PlanNeighborKeys([Selected(3), Selected(4)], before: 1, after: 1);

        keys.Should().Equal((Doc, 2), (Doc, 5));
    }

    [Fact]
    public void PlanNeighborKeys_DeduplicatesSharedNeighbors()
    {
        var keys = NeighborExpansion.PlanNeighborKeys([Selected(2), Selected(4)], before: 1, after: 1);

        keys.Should().Equal((Doc, 1), (Doc, 3), (Doc, 5));
        keys.Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void Expand_LastChunkOfDocument_MissingNeighborIsSkipped()
    {
        var result = NeighborExpansion.Expand([Selected(9)], [new NeighborChunk { DocumentId = Doc, ChunkIndex = 8, Content = "truoc" }], before: 1, after: 1);

        var block = result.Should().ContainSingle().Subject;
        block.Text.Should().Contain("truoc").And.Contain("noi dung chunk 9");
        block.StartChunkIndex.Should().Be(8);
        block.EndChunkIndex.Should().Be(9);
    }

    [Fact]
    public void Expand_TwoAdjacentSelectedChunks_MergeIntoOneBlockNotTwo()
    {
        var result = NeighborExpansion.Expand(
            [Selected(3, 0.9), Selected(4, 0.8)],
            [new NeighborChunk { DocumentId = Doc, ChunkIndex = 2, Content = "truoc" }, new NeighborChunk { DocumentId = Doc, ChunkIndex = 5, Content = "sau" }],
            before: 1,
            after: 1);

        var block = result.Should().ContainSingle("two adjacent chunks must merge rather than duplicate").Subject;
        block.AnchorChunkIds.Should().HaveCount(2);
        block.StartChunkIndex.Should().Be(2);
        block.EndChunkIndex.Should().Be(5);
    }

    [Fact]
    public void Expand_DistantSelectedChunks_StayAsSeparateBlocks()
    {
        var result = NeighborExpansion.Expand([Selected(1), Selected(20)], [], before: 1, after: 1);

        result.Should().HaveCount(2);
    }

    [Fact]
    public void Expand_NeighborsAreNotPromotedToAnchors()
    {
        var anchor = Selected(5);

        var result = NeighborExpansion.Expand([anchor], [new NeighborChunk { DocumentId = Doc, ChunkIndex = 4, Content = "truoc" }, new NeighborChunk { DocumentId = Doc, ChunkIndex = 6, Content = "sau" }], 1, 1);

        result.Single().AnchorChunkIds.Should().Equal(anchor.ChunkId);
    }

    [Fact]
    public void JoinTrimmingOverlap_RemovesDuplicatedOverlapRegion()
    {
        var shared = new string('x', 40) + " doan van chung giua hai chunk ";
        var first = "phan dau cua chunk mot. " + shared;
        var second = shared + "phan cuoi cua chunk hai.";

        var joined = NeighborExpansion.JoinTrimmingOverlap([first, second]);

        joined.Should().Contain("phan dau cua chunk mot");
        joined.Should().Contain("phan cuoi cua chunk hai");
        CountOccurrences(joined, shared.Trim()).Should().Be(1, "the overlapping part may appear only once");
    }

    [Fact]
    public void JoinTrimmingOverlap_NoOverlap_KeepsBothParts()
    {
        var joined = NeighborExpansion.JoinTrimmingOverlap(["hoan toan khac nhau mot", "hoan toan khac biet hai"]);

        joined.Should().Contain("mot").And.Contain("hai");
    }

    [Fact]
    public void JoinTrimmingOverlap_EmptyInput_ReturnsEmpty()
    {
        NeighborExpansion.JoinTrimmingOverlap([]).Should().BeEmpty();
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
