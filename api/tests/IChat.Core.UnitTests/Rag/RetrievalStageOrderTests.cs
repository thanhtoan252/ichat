namespace IChat.Core.UnitTests.Rag;

using IChat.Core.Abstractions;
using IChat.Core.Rag;
using FluentAssertions;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using Xunit;

/// <summary>
/// Thứ tự và tên chặng là contract đã công bố cho Retrieval Lab: nó đọc kết quả theo tên
/// chặng, nên một chặng đổi tên, biến mất hay đổi chỗ đều làm hỏng màn hình debug.
/// </summary>
public class RetrievalStageOrderTests
{
    [Fact]
    public async Task Execute_EmitsStagesInThePublishedOrder()
    {
        var outcome = await Pipeline().ExecuteAsync(Request(SearchMode.Hybrid), CancellationToken.None);

        outcome.Stages.Select(stage => stage.Name)
            .Should().Equal("vector", "fulltext", "trigram", "fused", "afterMmr", "final");
    }

    // Nhánh bị tắt vẫn phải ghi một chặng RỖNG: Retrieval Lab phân biệt "chặng chạy mà không
    // ra gì" với "chặng không tồn tại", và integration test đang chốt điều này.
    [Fact]
    public async Task Execute_DisabledBranch_StillEmitsAnEmptyStage()
    {
        var pipeline = Pipeline(trigramEnabled: false);

        var outcome = await pipeline.ExecuteAsync(Request(SearchMode.Hybrid), CancellationToken.None);

        outcome.Stages.Select(stage => stage.Name)
            .Should().Equal("vector", "fulltext", "trigram", "fused", "afterMmr", "final");
        outcome.Stages.Single(stage => stage.Name == "trigram").Count.Should().Be(0);
    }

    [Fact]
    public async Task Execute_ModeSelectsOneBranch_StillEmitsEveryBranchStage()
    {
        var outcome = await Pipeline().ExecuteAsync(Request(SearchMode.Vector), CancellationToken.None);

        outcome.Stages.Select(stage => stage.Name).Should().Contain(["vector", "fulltext", "trigram"]);
        outcome.Stages.Single(stage => stage.Name == "fulltext").Count.Should().Be(0);
    }

    [Fact]
    public async Task Execute_BranchReportsDegraded_MarksTheWholeOutcomeDegraded()
    {
        var pipeline = Pipeline(vectorDegraded: true);

        var outcome = await pipeline.ExecuteAsync(Request(SearchMode.Hybrid), CancellationToken.None);

        outcome.Degraded.Should().BeTrue();
    }

    [Fact]
    public async Task Execute_CandidatesAfterFusion_HaveNoSingleSource()
    {
        // Sau fusion một chunk có thể đến từ nhiều nhánh, nên gán một Source là nói dối.
        var outcome = await Pipeline().ExecuteAsync(Request(SearchMode.Hybrid), CancellationToken.None);

        outcome.Stages.Single(stage => stage.Name == "fused").Top.Should().OnlyContain(candidate => candidate.Source == null);
        outcome.Stages.Single(stage => stage.Name == "vector").Top.Should().OnlyContain(candidate => candidate.Source == RetrievalSource.Vector);
        outcome.Stages.Single(stage => stage.Name == "fulltext").Top.Should().OnlyContain(candidate => candidate.Source == RetrievalSource.FullText);
    }

    private static RetrievalPipeline Pipeline(bool trigramEnabled = true, bool vectorDegraded = false)
    {
        var vector = Guid.Parse("00000000-0000-0000-0000-0000000000a1");
        var fullText = Guid.Parse("00000000-0000-0000-0000-0000000000b2");

        IRetrievalBranch[] branches =
        [
            new StubBranch(RetrievalStageName.Vector, [SearchMode.Hybrid, SearchMode.Vector], [Candidate(vector, RetrievalSource.Vector)], vectorDegraded),
            new StubBranch(RetrievalStageName.FullText, [SearchMode.Hybrid, SearchMode.FullText], [Candidate(fullText, RetrievalSource.FullText)]),
            new StubBranch(RetrievalStageName.Trigram, trigramEnabled ? [SearchMode.Hybrid, SearchMode.Trigram] : [], [])
        ];

        return new RetrievalPipeline(
            branches,
            new StubChunkLoader([vector, fullText]),
            new PassThroughRewriter(),
            new PassThroughReranker(),
            Options.Create(new RagOptions()));
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

    private sealed class StubBranch(
        string stageName,
        IReadOnlyList<SearchMode> enabledModes,
        IReadOnlyList<RetrievalCandidate> candidates,
        bool degraded = false) : IRetrievalBranch
    {
        public string StageName => stageName;

        public bool IsEnabledFor(SearchMode mode) => enabledModes.Contains(mode);

        public Task<SearchBranchResult> SearchAsync(string query, CancellationToken cancellationToken)
        {
            return Task.FromResult(new SearchBranchResult
            {
                Candidates = candidates,
                ElapsedMs = 1,
                Degraded = degraded
            });
        }
    }

    private sealed class StubChunkLoader(IReadOnlyList<Guid> known) : IChunkLoader
    {
        public Task<IReadOnlyList<ScoredChunk>> LoadChunksAsync(IReadOnlyList<Guid> chunkIds, CancellationToken cancellationToken)
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

            return Task.FromResult(chunks);
        }

        public Task<IReadOnlyList<NeighborChunk>> LoadNeighborsAsync(
            IReadOnlyList<(Guid DocumentId, int ChunkIndex)> keys,
            CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyList<NeighborChunk>>([]);
        }
    }

    private sealed class PassThroughRewriter : IQueryRewriter
    {
        public Task<string> RewriteAsync(string originalQuestion, IReadOnlyList<ChatMessage> history, CancellationToken cancellationToken)
        {
            return Task.FromResult(originalQuestion);
        }
    }

    private sealed class PassThroughReranker : IReranker
    {
        public Task<IReadOnlyList<ScoredChunk>> RerankAsync(string query, IReadOnlyList<ScoredChunk> candidates, CancellationToken cancellationToken)
        {
            return Task.FromResult(candidates);
        }
    }
}
