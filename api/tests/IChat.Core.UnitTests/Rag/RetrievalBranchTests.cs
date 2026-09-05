namespace IChat.Core.UnitTests.Rag;

using IChat.Core.Abstractions;
using IChat.Core.Rag;
using IChat.Infrastructure.Search.Branches;
using FluentAssertions;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;

[TestFixture]
public class RetrievalBranchTests
{
    private Mock<IChunkSearch> _chunkSearch = null!;
    private Mock<IEmbeddingGenerator<string, Embedding<float>>> _embeddings = null!;

    [SetUp]
    public void SetUp()
    {
        _chunkSearch = new Mock<IChunkSearch>();
        _chunkSearch
            .Setup(search => search.SearchVectorAsync(It.IsAny<float[]>(), It.IsAny<int>(), It.IsAny<double>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(EmptyResult);
        _chunkSearch
            .Setup(search => search.SearchFullTextAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<double>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(EmptyResult);
        _chunkSearch
            .Setup(search => search.SearchTrigramAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(EmptyResult);

        _embeddings = new Mock<IEmbeddingGenerator<string, Embedding<float>>>();
        _embeddings
            .Setup(generator => generator.GenerateAsync(
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<EmbeddingGenerationOptions?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((IEnumerable<string> values, EmbeddingGenerationOptions? _, CancellationToken _) =>
                new GeneratedEmbeddings<Embedding<float>>(values.Select(_ => new Embedding<float>(new float[] { 1f, 0f })).ToList()));
    }

    [TestCase(SearchMode.Hybrid, true)]
    [TestCase(SearchMode.Vector, true)]
    [TestCase(SearchMode.FullText, false)]
    [TestCase(SearchMode.Trigram, false)]
    public void VectorBranch_IsEnabled_OnlyForHybridAndVector(SearchMode mode, bool expected)
    {
        // Arrange
        var branch = VectorBranch();

        // Act
        var enabled = branch.IsEnabledFor(mode);

        // Assert
        enabled.Should().Be(expected);
    }

    [TestCase(SearchMode.Hybrid, true)]
    [TestCase(SearchMode.FullText, true)]
    [TestCase(SearchMode.Vector, false)]
    [TestCase(SearchMode.Trigram, false)]
    public void FullTextBranch_IsEnabled_OnlyForHybridAndFullText(SearchMode mode, bool expected)
    {
        // Arrange
        var branch = new FullTextSearchBranch(_chunkSearch.Object, Rag());

        // Act
        var enabled = branch.IsEnabledFor(mode);

        // Assert
        enabled.Should().Be(expected);
    }

    [TestCase(SearchMode.Hybrid, true)]
    [TestCase(SearchMode.Trigram, true)]
    [TestCase(SearchMode.Vector, false)]
    [TestCase(SearchMode.FullText, false)]
    public void TrigramBranch_IsEnabled_OnlyForHybridAndTrigram_WhenCandidatesAreConfigured(SearchMode mode, bool expected)
    {
        // Arrange
        var branch = new TrigramSearchBranch(_chunkSearch.Object, Rag(trigramCandidates: 20));

        // Act
        var enabled = branch.IsEnabledFor(mode);

        // Assert
        enabled.Should().Be(expected);
    }

    // TrigramCandidates = 0 là công tắc tắt nhánh 3 (mặc định của appsettings).
    [TestCase(SearchMode.Hybrid)]
    [TestCase(SearchMode.Trigram)]
    public void TrigramBranch_IsDisabled_WhenTrigramCandidatesIsZero(SearchMode mode)
    {
        // Arrange
        var branch = new TrigramSearchBranch(_chunkSearch.Object, Rag(trigramCandidates: 0));

        // Act
        var enabled = branch.IsEnabledFor(mode);

        // Assert
        enabled.Should().BeFalse();
    }

    [Test]
    public void StageNames_MatchThePublishedContract()
    {
        // Arrange
        var vector = VectorBranch();
        var fullText = new FullTextSearchBranch(_chunkSearch.Object, Rag());
        var trigram = new TrigramSearchBranch(_chunkSearch.Object, Rag());

        // Act & Assert
        vector.StageName.Should().Be("vector");
        fullText.StageName.Should().Be("fulltext");
        trigram.StageName.Should().Be("trigram");
    }

    [Test]
    public async Task VectorBranch_PassesConfiguredLimitsToTheStore()
    {
        // Arrange
        var branch = VectorBranch();

        // Act
        await branch.SearchAsync("cau hoi", CancellationToken.None);

        // Assert
        _chunkSearch.Verify(
            search => search.SearchVectorAsync(It.IsAny<float[]>(), 40, 0.20, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // Embedding hỏng là bước phụ có thể degrade: nhánh tự báo Degraded thay vì ném lỗi ra
    // pipeline, để full-text và trigram vẫn trả lời được.
    [Test]
    public async Task VectorBranch_EmbeddingFails_ReturnsDegradedWithNoCandidates()
    {
        // Arrange
        _embeddings
            .Setup(generator => generator.GenerateAsync(
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<EmbeddingGenerationOptions?>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Stub embedding provider failing on purpose."));
        var branch = VectorBranch();

        // Act
        var result = await branch.SearchAsync("cau hoi", CancellationToken.None);

        // Assert
        result.Degraded.Should().BeTrue();
        result.Candidates.Should().BeEmpty();
        _chunkSearch.Verify(
            search => search.SearchVectorAsync(It.IsAny<float[]>(), It.IsAny<int>(), It.IsAny<double>(), It.IsAny<CancellationToken>()),
            Times.Never,
            "the store must not be queried without an embedding");
    }

    [Test]
    public async Task VectorBranch_EmbeddingSucceeds_IsNotDegraded()
    {
        // Arrange
        var branch = VectorBranch();

        // Act
        var result = await branch.SearchAsync("cau hoi", CancellationToken.None);

        // Assert
        result.Degraded.Should().BeFalse();
    }

    [Test]
    public async Task TrigramBranch_PassesConfiguredLimitToTheStore()
    {
        // Arrange
        var branch = new TrigramSearchBranch(_chunkSearch.Object, Rag(trigramCandidates: 15));

        // Act
        await branch.SearchAsync("cau hoi", CancellationToken.None);

        // Assert
        _chunkSearch.Verify(
            search => search.SearchTrigramAsync(It.IsAny<string>(), 15, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    private static SearchBranchResult EmptyResult => new() { Candidates = [], ElapsedMs = 0 };

    private VectorSearchBranch VectorBranch()
    {
        return new VectorSearchBranch(
            _chunkSearch.Object,
            _embeddings.Object,
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
}
