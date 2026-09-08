namespace IChat.Core.UnitTests.Rag;

using IChat.Core.Rag;
using FluentAssertions;
using NUnit.Framework;

[TestFixture]
public class ReciprocalRankFusionTests
{
    private static readonly Guid A = Guid.Parse("00000000-0000-0000-0000-0000000000a1");
    private static readonly Guid B = Guid.Parse("00000000-0000-0000-0000-0000000000b2");
    private static readonly Guid C = Guid.Parse("00000000-0000-0000-0000-0000000000c3");

    [Test]
    public void Fuse_NoLists_ReturnsEmpty()
    {
        // Arrange — not a single ranked list

        // Act
        var fused = ReciprocalRankFusion.Fuse([], 60, 10);

        // Assert
        fused.Should().BeEmpty();
    }

    [Test]
    public void Fuse_AllListsEmpty_ReturnsEmpty()
    {
        // Arrange
        IReadOnlyList<IReadOnlyList<Guid>> lists = [[], []];

        // Act
        var fused = ReciprocalRankFusion.Fuse(lists, 60, 10);

        // Assert
        fused.Should().BeEmpty();
    }

    [Test]
    public void Fuse_OneEmptyList_StillRanksTheOther()
    {
        // Arrange
        IReadOnlyList<IReadOnlyList<Guid>> lists = [[A, B], []];

        // Act
        var fused = ReciprocalRankFusion.Fuse(lists, 60, 10);

        // Assert
        fused.Select(item => item.Id).Should().Equal(A, B);
    }

    [Test]
    public void Fuse_ChunkPresentInBothLists_OutranksChunkInOneList()
    {
        // Arrange
        // B đứng hạng 2 ở cả hai danh sách, A đứng hạng 1 chỉ ở một danh sách.
        IReadOnlyList<IReadOnlyList<Guid>> lists = [[A, B], [C, B]];

        // Act
        var fused = ReciprocalRankFusion.Fuse(lists, 60, 10);

        // Assert
        fused[0].Id.Should().Be(B);
        fused.Select(item => item.Id).Should().Contain([A, C]);
    }

    [Test]
    public void Fuse_ScoreMatchesFormula()
    {
        // Arrange
        IReadOnlyList<IReadOnlyList<Guid>> lists = [[A, B]];

        // Act
        var fused = ReciprocalRankFusion.Fuse(lists, 60, 10);

        // Assert
        fused.Single(item => item.Id == A).Score.Should().BeApproximately(1.0 / 61, 1e-12);
        fused.Single(item => item.Id == B).Score.Should().BeApproximately(1.0 / 62, 1e-12);
    }

    [Test]
    public void Fuse_RespectsTopK()
    {
        // Arrange
        IReadOnlyList<IReadOnlyList<Guid>> lists = [[A, B, C]];

        // Act
        var fused = ReciprocalRankFusion.Fuse(lists, 60, 2);

        // Assert
        fused.Should().HaveCount(2);
        fused.Select(item => item.Id).Should().Equal(A, B);
    }

    [Test]
    public void Fuse_HigherRankScoresHigher()
    {
        // Arrange
        IReadOnlyList<IReadOnlyList<Guid>> lists = [[A, B, C]];

        // Act
        var fused = ReciprocalRankFusion.Fuse(lists, 60, 10);

        // Assert
        fused[0].Score.Should().BeGreaterThan(fused[1].Score);
        fused[1].Score.Should().BeGreaterThan(fused[2].Score);
    }
}
