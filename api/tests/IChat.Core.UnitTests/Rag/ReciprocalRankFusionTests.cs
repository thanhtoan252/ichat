namespace IChat.Core.UnitTests.Rag;

using IChat.Core.Rag;
using FluentAssertions;
using Xunit;

public class ReciprocalRankFusionTests
{
    private static readonly Guid A = Guid.Parse("00000000-0000-0000-0000-0000000000a1");
    private static readonly Guid B = Guid.Parse("00000000-0000-0000-0000-0000000000b2");
    private static readonly Guid C = Guid.Parse("00000000-0000-0000-0000-0000000000c3");

    [Fact]
    public void Fuse_NoLists_ReturnsEmpty()
    {
        var fused = ReciprocalRankFusion.Fuse([], 60, 10);

        fused.Should().BeEmpty();
    }

    [Fact]
    public void Fuse_AllListsEmpty_ReturnsEmpty()
    {
        var fused = ReciprocalRankFusion.Fuse([[], []], 60, 10);

        fused.Should().BeEmpty();
    }

    [Fact]
    public void Fuse_OneEmptyList_StillRanksTheOther()
    {
        var fused = ReciprocalRankFusion.Fuse([[A, B], []], 60, 10);

        fused.Select(item => item.Id).Should().Equal(A, B);
    }

    [Fact]
    public void Fuse_ChunkPresentInBothLists_OutranksChunkInOneList()
    {
        // B đứng hạng 2 ở cả hai danh sách, A đứng hạng 1 chỉ ở một danh sách.
        var fused = ReciprocalRankFusion.Fuse([[A, B], [C, B]], 60, 10);

        fused[0].Id.Should().Be(B);
        fused.Select(item => item.Id).Should().Contain([A, C]);
    }

    [Fact]
    public void Fuse_ScoreMatchesFormula()
    {
        var fused = ReciprocalRankFusion.Fuse([[A, B]], 60, 10);

        fused.Single(item => item.Id == A).Score.Should().BeApproximately(1.0 / 61, 1e-12);
        fused.Single(item => item.Id == B).Score.Should().BeApproximately(1.0 / 62, 1e-12);
    }

    [Fact]
    public void Fuse_RespectsTopK()
    {
        var fused = ReciprocalRankFusion.Fuse([[A, B, C]], 60, 2);

        fused.Should().HaveCount(2);
        fused.Select(item => item.Id).Should().Equal(A, B);
    }

    [Fact]
    public void Fuse_HigherRankScoresHigher()
    {
        var fused = ReciprocalRankFusion.Fuse([[A, B, C]], 60, 10);

        fused[0].Score.Should().BeGreaterThan(fused[1].Score);
        fused[1].Score.Should().BeGreaterThan(fused[2].Score);
    }
}
