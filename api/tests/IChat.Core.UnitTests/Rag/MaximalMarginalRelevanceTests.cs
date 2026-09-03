namespace IChat.Core.UnitTests.Rag;

using IChat.Core.Rag;
using FluentAssertions;
using Xunit;

public class MaximalMarginalRelevanceTests
{
    private static ScoredChunk Chunk(string id, double score, float[] embedding, Guid? documentId = null)
    {
        return new ScoredChunk
        {
            ChunkId = Guid.Parse($"00000000-0000-0000-0000-{id.PadLeft(12, '0')}"),
            DocumentId = documentId ?? Guid.Parse("00000000-0000-0000-0000-00000000d001"),
            DocumentTitle = "Doc",
            Content = $"content {id}",
            ChunkIndex = 0,
            Score = score,
            Embedding = embedding
        };
    }

    [Fact]
    public void Select_EmptyCandidates_ReturnsEmpty()
    {
        MaximalMarginalRelevance.Select([], 0.7, 3, 8).Should().BeEmpty();
    }

    [Fact]
    public void Select_SingleCandidate_ReturnsIt()
    {
        var only = Chunk("1", 0.9, [1f, 0f]);

        var selected = MaximalMarginalRelevance.Select([only], 0.7, 3, 8);

        selected.Should().ContainSingle().Which.ChunkId.Should().Be(only.ChunkId);
    }

    [Fact]
    public void Select_LambdaOne_DegeneratesToPureRelevanceRanking()
    {
        var candidates = new[]
        {
            Chunk("1", 0.5, [1f, 0f]),
            Chunk("2", 0.9, [1f, 0f]),
            Chunk("3", 0.7, [1f, 0f])
        };

        var selected = MaximalMarginalRelevance.Select(candidates, lambda: 1.0, maxChunksPerDocument: 10, finalTopK: 3);

        selected.Select(chunk => chunk.Score).Should().Equal(0.9, 0.7, 0.5);
    }

    [Fact]
    public void Select_AllCandidatesIdentical_StillReturnsRequestedCount()
    {
        var candidates = Enumerable.Range(1, 5)
            .Select(i => Chunk(i.ToString(), 0.8, [1f, 0f]))
            .ToList();

        var selected = MaximalMarginalRelevance.Select(candidates, 0.7, maxChunksPerDocument: 10, finalTopK: 3);

        selected.Should().HaveCount(3);
        selected.Select(chunk => chunk.ChunkId).Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void Select_PrefersDiverseCandidateOverNearDuplicate()
    {
        var anchor = Chunk("1", 0.90, [1f, 0f]);
        var duplicate = Chunk("2", 0.89, [1f, 0f]);
        var diverse = Chunk("3", 0.70, [0f, 1f]);

        var selected = MaximalMarginalRelevance.Select([anchor, duplicate, diverse], lambda: 0.5, maxChunksPerDocument: 10, finalTopK: 2);

        selected[0].ChunkId.Should().Be(anchor.ChunkId);
        selected[1].ChunkId.Should().Be(diverse.ChunkId, "MMR must penalise candidates that duplicate an already selected one");
    }

    [Fact]
    public void Select_EnforcesMaxChunksPerDocument()
    {
        var docOne = Guid.Parse("00000000-0000-0000-0000-00000000dddd");
        var docTwo = Guid.Parse("00000000-0000-0000-0000-00000000eeee");

        var candidates = new[]
        {
            Chunk("1", 0.99, [1f, 0f], docOne),
            Chunk("2", 0.98, [0f, 1f], docOne),
            Chunk("3", 0.97, [1f, 1f], docOne),
            Chunk("4", 0.10, [0f, 1f], docTwo)
        };

        var selected = MaximalMarginalRelevance.Select(candidates, 1.0, maxChunksPerDocument: 2, finalTopK: 4);

        selected.Should().HaveCount(3);
        selected.Count(chunk => chunk.DocumentId == docOne).Should().Be(2);
        selected.Count(chunk => chunk.DocumentId == docTwo).Should().Be(1);
    }

    [Fact]
    public void CosineSimilarity_HandlesNullAndMismatchedLengths()
    {
        MaximalMarginalRelevance.CosineSimilarity(null, [1f]).Should().Be(0);
        MaximalMarginalRelevance.CosineSimilarity([1f, 0f], [1f]).Should().Be(0);
        MaximalMarginalRelevance.CosineSimilarity([0f, 0f], [1f, 0f]).Should().Be(0);
        MaximalMarginalRelevance.CosineSimilarity([1f, 0f], [1f, 0f]).Should().BeApproximately(1.0, 1e-9);
    }
}
