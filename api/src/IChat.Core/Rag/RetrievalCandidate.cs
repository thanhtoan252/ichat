namespace IChat.Core.Rag;

public sealed class RetrievalCandidate
{
    public required Guid ChunkId { get; init; }

    public required Guid DocumentId { get; init; }

    public required string Content { get; init; }

    public string? HeadingPath { get; init; }

    public required int ChunkIndex { get; init; }

    public required double Score { get; init; }

    /// <summary>
    /// Only meaningful at the branch stages. After fusion a chunk can come from several branches at once,
    /// so a single Source value would be meaningless: fused/afterMmr/reranked/final leave it null.
    /// </summary>
    public RetrievalSource? Source { get; init; }

    public float[]? Embedding { get; init; }
}
