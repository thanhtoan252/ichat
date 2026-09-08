namespace IChat.Core.UnitTests.Rag;

using IChat.Core.Abstractions;
using IChat.Core.Rag;
using FluentAssertions;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;

/// <summary>
/// Thứ tự và tên chặng là contract đã công bố cho Retrieval Lab: nó đọc kết quả theo tên
/// chặng, nên một chặng đổi tên, biến mất hay đổi chỗ đều làm hỏng màn hình debug.
/// </summary>
[TestFixture]
public class RetrievalStageOrderTests
{
    private static readonly Guid VectorChunk = Guid.Parse("00000000-0000-0000-0000-0000000000a1");
    private static readonly Guid FullTextChunk = Guid.Parse("00000000-0000-0000-0000-0000000000b2");

    private static readonly string[] PublishedStageOrder = ["vector", "fulltext", "trigram", "fused", "afterMmr", "final"];

    [Test]
    public async Task Execute_EmitsStagesInThePublishedOrder()
    {
        // Arrange
        var pipeline = Pipeline();

        // Act
        var outcome = await pipeline.ExecuteAsync(Request(SearchMode.Hybrid), CancellationToken.None);

        // Assert
        outcome.Stages.Select(stage => stage.Name).Should().Equal(PublishedStageOrder);
    }

    // Nhánh bị tắt vẫn phải ghi một chặng RỖNG: Retrieval Lab phân biệt "chặng chạy mà không
    // ra gì" với "chặng không tồn tại", và integration test đang chốt điều này.
    [Test]
    public async Task Execute_DisabledBranch_StillEmitsAnEmptyStage()
    {
        // Arrange
        var pipeline = Pipeline(trigramEnabled: false);

        // Act
        var outcome = await pipeline.ExecuteAsync(Request(SearchMode.Hybrid), CancellationToken.None);

        // Assert
        outcome.Stages.Select(stage => stage.Name).Should().Equal(PublishedStageOrder);
        outcome.Stages.Single(stage => stage.Name == "trigram").Count.Should().Be(0);
    }

    [Test]
    public async Task Execute_ModeSelectsOneBranch_StillEmitsEveryBranchStage()
    {
        // Arrange
        var pipeline = Pipeline();

        // Act
        var outcome = await pipeline.ExecuteAsync(Request(SearchMode.Vector), CancellationToken.None);

        // Assert
        outcome.Stages.Select(stage => stage.Name).Should().Contain(["vector", "fulltext", "trigram"]);
        outcome.Stages.Single(stage => stage.Name == "fulltext").Count.Should().Be(0);
    }

    [Test]
    public async Task Execute_BranchReportsDegraded_MarksTheWholeOutcomeDegraded()
    {
        // Arrange
        var pipeline = Pipeline(vectorDegraded: true);

        // Act
        var outcome = await pipeline.ExecuteAsync(Request(SearchMode.Hybrid), CancellationToken.None);

        // Assert
        outcome.Degraded.Should().BeTrue();
    }

    [Test]
    public async Task Execute_CandidatesAfterFusion_HaveNoSingleSource()
    {
        // Arrange
        var pipeline = Pipeline();

        // Act
        var outcome = await pipeline.ExecuteAsync(Request(SearchMode.Hybrid), CancellationToken.None);

        // Assert
        // Sau fusion một chunk có thể đến từ nhiều nhánh, nên gán một Source là nói dối.
        outcome.Stages.Single(stage => stage.Name == "fused").Top.Should().OnlyContain(candidate => candidate.Source == null);
        outcome.Stages.Single(stage => stage.Name == "vector").Top.Should().OnlyContain(candidate => candidate.Source == RetrievalSource.Vector);
        outcome.Stages.Single(stage => stage.Name == "fulltext").Top.Should().OnlyContain(candidate => candidate.Source == RetrievalSource.FullText);
    }

    private static RetrievalPipeline Pipeline(bool trigramEnabled = true, bool vectorDegraded = false)
    {
        IRetrievalBranch[] branches =
        [
            Branch(RetrievalStageName.Vector, [SearchMode.Hybrid, SearchMode.Vector], [Candidate(VectorChunk, RetrievalSource.Vector)], vectorDegraded),
            Branch(RetrievalStageName.FullText, [SearchMode.Hybrid, SearchMode.FullText], [Candidate(FullTextChunk, RetrievalSource.FullText)]),
            Branch(RetrievalStageName.Trigram, trigramEnabled ? [SearchMode.Hybrid, SearchMode.Trigram] : [], [])
        ];

        return new RetrievalPipeline(
            branches,
            ChunkLoader(VectorChunk, FullTextChunk),
            PassThroughRewriter(),
            PassThroughReranker(),
            Options.Create(new RagOptions()));
    }

    private static IRetrievalBranch Branch(
        string stageName,
        IReadOnlyList<SearchMode> enabledModes,
        IReadOnlyList<RetrievalCandidate> candidates,
        bool degraded = false)
    {
        var branch = new Mock<IRetrievalBranch>();
        branch.SetupGet(b => b.StageName).Returns(stageName);
        branch.Setup(b => b.IsEnabledFor(It.IsAny<SearchMode>())).Returns((SearchMode mode) => enabledModes.Contains(mode));
        branch
            .Setup(b => b.SearchAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SearchBranchResult
            {
                Candidates = candidates,
                ElapsedMs = 1,
                Degraded = degraded
            });

        return branch.Object;
    }

    private static IChunkLoader ChunkLoader(params Guid[] known)
    {
        var loader = new Mock<IChunkLoader>();
        loader
            .Setup(l => l.LoadChunksAsync(It.IsAny<IReadOnlyList<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<Guid> chunkIds, CancellationToken _) =>
            {
                IReadOnlyList<ScoredChunk> chunks =
                [
                    .. chunkIds.Where(known.Contains).Select(id => new ScoredChunk
                    {
                        ChunkId = id,
                        DocumentId = id,
                        DocumentTitle = "Tai lieu",
                        Content = "noi dung",
                        ChunkIndex = 0,
                        Score = 0
                    })
                ];

                return chunks;
            });
        loader
            .Setup(l => l.LoadNeighborsAsync(It.IsAny<IReadOnlyList<(Guid DocumentId, int ChunkIndex)>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        return loader.Object;
    }

    private static IQueryRewriter PassThroughRewriter()
    {
        var rewriter = new Mock<IQueryRewriter>();
        rewriter
            .Setup(r => r.RewriteAsync(It.IsAny<string>(), It.IsAny<IReadOnlyList<ChatMessage>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string originalQuestion, IReadOnlyList<ChatMessage> _, CancellationToken _) => originalQuestion);

        return rewriter.Object;
    }

    private static IReranker PassThroughReranker()
    {
        var reranker = new Mock<IReranker>();
        reranker
            .Setup(r => r.RerankAsync(It.IsAny<string>(), It.IsAny<IReadOnlyList<ScoredChunk>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string _, IReadOnlyList<ScoredChunk> candidates, CancellationToken _) => candidates);

        return reranker.Object;
    }

    private static RetrievalRequest Request(SearchMode mode)
    {
        return new RetrievalRequest { Query = "cau hoi", Mode = mode, Rewrite = false, Rerank = false };
    }

    private static RetrievalCandidate Candidate(Guid id, RetrievalSource source)
    {
        return new RetrievalCandidate
        {
            ChunkId = id,
            DocumentId = id,
            Content = "noi dung",
            ChunkIndex = 0,
            Score = 1,
            Source = source
        };
    }
}
