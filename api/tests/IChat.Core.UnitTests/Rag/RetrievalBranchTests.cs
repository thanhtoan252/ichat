namespace IChat.Core.UnitTests.Rag;

using IChat.Core.Abstractions;
using IChat.Core.Rag;
using IChat.Infrastructure.Search.Branches;
using FluentAssertions;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

public class RetrievalBranchTests
{
    [Theory]
    [InlineData(SearchMode.Hybrid, true)]
    [InlineData(SearchMode.Vector, true)]
    [InlineData(SearchMode.FullText, false)]
    [InlineData(SearchMode.Trigram, false)]
    public void VectorBranch_IsEnabled_OnlyForHybridAndVector(SearchMode mode, bool expected)
    {
        VectorBranch(new RecordingChunkSearch()).IsEnabledFor(mode).Should().Be(expected);
    }

    [Theory]
    [InlineData(SearchMode.Hybrid, true)]
    [InlineData(SearchMode.FullText, true)]
    [InlineData(SearchMode.Vector, false)]
    [InlineData(SearchMode.Trigram, false)]
    public void FullTextBranch_IsEnabled_OnlyForHybridAndFullText(SearchMode mode, bool expected)
    {
        new FullTextSearchBranch(new RecordingChunkSearch(), Rag()).IsEnabledFor(mode).Should().Be(expected);
    }

    [Theory]
    [InlineData(SearchMode.Hybrid, true)]
    [InlineData(SearchMode.Trigram, true)]
    [InlineData(SearchMode.Vector, false)]
    [InlineData(SearchMode.FullText, false)]
    public void TrigramBranch_IsEnabled_OnlyForHybridAndTrigram_WhenCandidatesAreConfigured(SearchMode mode, bool expected)
    {
        var branch = new TrigramSearchBranch(new RecordingChunkSearch(), Rag(trigramCandidates: 20));

        branch.IsEnabledFor(mode).Should().Be(expected);
    }

    // TrigramCandidates = 0 là công tắc tắt nhánh 3 (mặc định của appsettings).
    [Theory]
    [InlineData(SearchMode.Hybrid)]
    [InlineData(SearchMode.Trigram)]
    public void TrigramBranch_IsDisabled_WhenTrigramCandidatesIsZero(SearchMode mode)
    {
        var branch = new TrigramSearchBranch(new RecordingChunkSearch(), Rag(trigramCandidates: 0));

        branch.IsEnabledFor(mode).Should().BeFalse();
    }

    [Fact]
    public void StageNames_MatchThePublishedContract()
    {
        var chunkSearch = new RecordingChunkSearch();

        VectorBranch(chunkSearch).StageName.Should().Be("vector");
        new FullTextSearchBranch(chunkSearch, Rag()).StageName.Should().Be("fulltext");
        new TrigramSearchBranch(chunkSearch, Rag()).StageName.Should().Be("trigram");
    }

    [Fact]
    public async Task VectorBranch_PassesConfiguredLimitsToTheStore()
    {
        var chunkSearch = new RecordingChunkSearch();

        await VectorBranch(chunkSearch).SearchAsync("cau hoi", CancellationToken.None);

        chunkSearch.VectorLimit.Should().Be(40);
        chunkSearch.VectorMinSimilarity.Should().Be(0.20);
    }

    // Embedding hỏng là bước phụ có thể degrade: nhánh tự báo Degraded thay vì ném lỗi ra
    // pipeline, để full-text và trigram vẫn trả lời được.
    [Fact]
    public async Task VectorBranch_EmbeddingFails_ReturnsDegradedWithNoCandidates()
    {
        var chunkSearch = new RecordingChunkSearch();
        var branch = VectorBranch(chunkSearch, new StubEmbeddingGenerator { ShouldFail = true });

        var result = await branch.SearchAsync("cau hoi", CancellationToken.None);

        result.Degraded.Should().BeTrue();
        result.Candidates.Should().BeEmpty();
        chunkSearch.VectorCalls.Should().Be(0, "the store must not be queried without an embedding");
    }

    [Fact]
    public async Task VectorBranch_EmbeddingSucceeds_IsNotDegraded()
    {
        var result = await VectorBranch(new RecordingChunkSearch()).SearchAsync("cau hoi", CancellationToken.None);

        result.Degraded.Should().BeFalse();
    }

    [Fact]
    public async Task TrigramBranch_PassesConfiguredLimitToTheStore()
    {
        var chunkSearch = new RecordingChunkSearch();
        var branch = new TrigramSearchBranch(chunkSearch, Rag(trigramCandidates: 15));

        await branch.SearchAsync("cau hoi", CancellationToken.None);

        chunkSearch.TrigramLimit.Should().Be(15);
    }

    private static VectorSearchBranch VectorBranch(IChunkSearch chunkSearch, StubEmbeddingGenerator? embeddings = null)
    {
        return new VectorSearchBranch(
            chunkSearch,
            embeddings ?? new StubEmbeddingGenerator(),
            Rag(),
            NullLogger<VectorSearchBranch>.Instance);
    }

    private static IOptions<RagOptions> Rag(int trigramCandidates = 0)
    {
        return Options.Create(new RagOptions
        {
            Retrieval = new RetrievalOptions { TrigramCandidates = trigramCandidates }
        });
    }

    private sealed class RecordingChunkSearch : IChunkSearch
    {
        public int VectorCalls { get; private set; }

        public int VectorLimit { get; private set; }

        public double VectorMinSimilarity { get; private set; }

        public int TrigramLimit { get; private set; }

        public Task<SearchBranchResult> SearchVectorAsync(float[] queryEmbedding, int limit, double minSimilarity, CancellationToken cancellationToken)
        {
            VectorCalls++;
            VectorLimit = limit;
            VectorMinSimilarity = minSimilarity;

            return Task.FromResult(Empty());
        }

        public Task<SearchBranchResult> SearchFullTextAsync(string query, int limit, double minRank, CancellationToken cancellationToken)
        {
            return Task.FromResult(Empty());
        }

        public Task<SearchBranchResult> SearchTrigramAsync(string query, int limit, CancellationToken cancellationToken)
        {
            TrigramLimit = limit;

            return Task.FromResult(Empty());
        }

        private static SearchBranchResult Empty()
        {
            return new SearchBranchResult { Candidates = [], ElapsedMs = 0 };
        }
    }

    private sealed class StubEmbeddingGenerator : IEmbeddingGenerator<string, Embedding<float>>
    {
        public bool ShouldFail { get; init; }

        public Task<GeneratedEmbeddings<Embedding<float>>> GenerateAsync(
            IEnumerable<string> values,
            EmbeddingGenerationOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            if (ShouldFail)
            {
                throw new InvalidOperationException("Stub embedding provider failing on purpose.");
            }

            var embeddings = values.Select(_ => new Embedding<float>(new float[] { 1f, 0f })).ToList();

            return Task.FromResult(new GeneratedEmbeddings<Embedding<float>>(embeddings));
        }

        public object? GetService(Type serviceType, object? serviceKey = null) => null;

        public void Dispose()
        {
        }
    }
}
