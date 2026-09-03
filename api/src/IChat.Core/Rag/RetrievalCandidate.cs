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
    /// Chỉ có nghĩa ở chặng nhánh. Sau fusion một chunk có thể đến từ nhiều nhánh cùng lúc
    /// nên một giá trị Source là vô nghĩa: các chặng fused/afterMmr/reranked/final để null.
    /// </summary>
    public RetrievalSource? Source { get; init; }

    public float[]? Embedding { get; init; }
}
