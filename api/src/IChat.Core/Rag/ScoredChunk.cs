namespace IChat.Core.Rag;

public sealed class ScoredChunk
{
    public required Guid ChunkId { get; init; }

    public required Guid DocumentId { get; init; }

    public required string DocumentTitle { get; init; }

    public required string Content { get; init; }

    public string? HeadingPath { get; init; }

    public required int ChunkIndex { get; init; }

    public required double Score { get; init; }

    public float[]? Embedding { get; init; }

    /// <summary>A copy differing only in score — used when fusion or reranking overwrites it.</summary>
    public ScoredChunk WithScore(double score)
    {
        return new ScoredChunk
        {
            ChunkId = ChunkId,
            DocumentId = DocumentId,
            DocumentTitle = DocumentTitle,
            Content = Content,
            HeadingPath = HeadingPath,
            ChunkIndex = ChunkIndex,
            Score = score,
            Embedding = Embedding
        };
    }
}
