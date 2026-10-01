namespace IChat.Core.UnitTests.Rag;

using IChat.Core.Rag;
using FluentAssertions;
using NUnit.Framework;

[TestFixture]
public class MaximalMarginalRelevanceTests
{
    [Test]
    public void Select_EmptyCandidates_ReturnsEmpty()
    {
        // Arrange — no candidates at all

        // Act
        var selected = MaximalMarginalRelevance.Select([], 0.7, 3, 8);

        // Assert
        selected.Should().BeEmpty();
    }

    [Test]
    public void Select_SingleCandidate_ReturnsIt()
    {
        // Arrange
        var only = Chunk("1", 0.9, [1f, 0f]);

        // Act
        var selected = MaximalMarginalRelevance.Select([only], 0.7, 3, 8);

        // Assert
        selected.Should().ContainSingle().Which.ChunkId.Should().Be(only.ChunkId);
    }

    [Test]
    public void Select_LambdaOne_DegeneratesToPureRelevanceRanking()
    {
        // Arrange
        var candidates = new[]
        {
            Chunk("1", 0.5, [1f, 0f]),
            Chunk("2", 0.9, [1f, 0f]),
            Chunk("3", 0.7, [1f, 0f])
        };

        // Act
        var selected = MaximalMarginalRelevance.Select(candidates, lambda: 1.0, maxChunksPerDocument: 10, finalTopK: 3);

        // Assert
        selected.Select(chunk => chunk.Score).Should().Equal(0.9, 0.7, 0.5);
    }

    [Test]
    public void Select_AllCandidatesIdentical_StillReturnsRequestedCount()
    {
        // Arrange
        var candidates = Enumerable.Range(1, 5)
            .Select(i => Chunk(i.ToString(), 0.8, [1f, 0f]))
            .ToList();

        // Act
        var selected = MaximalMarginalRelevance.Select(candidates, 0.7, maxChunksPerDocument: 10, finalTopK: 3);

        // Assert
        selected.Should().HaveCount(3);
        selected.Select(chunk => chunk.ChunkId).Should().OnlyHaveUniqueItems();
    }

    [Test]
    public void Select_PrefersDiverseCandidateOverNearDuplicate()
    {
        // Arrange
        var anchor = Chunk("1", 0.90, [1f, 0f]);
        var duplicate = Chunk("2", 0.89, [1f, 0f]);
        var diverse = Chunk("3", 0.70, [0f, 1f]);

        // Act
        var selected = MaximalMarginalRelevance.Select([anchor, duplicate, diverse], lambda: 0.5, maxChunksPerDocument: 10, finalTopK: 2);

        // Assert
        selected[0].ChunkId.Should().Be(anchor.ChunkId);
        selected[1].ChunkId.Should().Be(diverse.ChunkId, "MMR must penalise candidates that duplicate an already selected one");
    }

    [Test]
    public void Select_EnforcesMaxChunksPerDocument()
    {
        // Arrange
        var docOne = Guid.Parse("00000000-0000-0000-0000-00000000dddd");
        var docTwo = Guid.Parse("00000000-0000-0000-0000-00000000eeee");
        var candidates = new[]
        {
            Chunk("1", 0.99, [1f, 0f], docOne),
            Chunk("2", 0.98, [0f, 1f], docOne),
            Chunk("3", 0.97, [1f, 1f], docOne),
            Chunk("4", 0.10, [0f, 1f], docTwo)
        };

        // Act
        var selected = MaximalMarginalRelevance.Select(candidates, 1.0, maxChunksPerDocument: 2, finalTopK: 4);

        // Assert
        selected.Should().HaveCount(3);
        selected.Count(chunk => chunk.DocumentId == docOne).Should().Be(2);
        selected.Count(chunk => chunk.DocumentId == docTwo).Should().Be(1);
    }

    [Test]
    public void CosineSimilarity_HandlesNullAndMismatchedLengths()
    {
        // Arrange & Act
        var nullVector = MaximalMarginalRelevance.CosineSimilarity(null, [1f]);
        var mismatchedLength = MaximalMarginalRelevance.CosineSimilarity([1f, 0f], [1f]);
        var zeroVector = MaximalMarginalRelevance.CosineSimilarity([0f, 0f], [1f, 0f]);
        var identical = MaximalMarginalRelevance.CosineSimilarity([1f, 0f], [1f, 0f]);

        // Assert
        nullVector.Should().Be(0);
        mismatchedLength.Should().Be(0);
        zeroVector.Should().Be(0);
        identical.Should().BeApproximately(1.0, 1e-9);
    }

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
}
