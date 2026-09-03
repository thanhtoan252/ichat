namespace IChat.Core.Rag;

/// <summary>Một chunk lân cận dùng để làm giàu context. Không bao giờ trở thành citation.</summary>
public sealed class NeighborChunk
{
    public required Guid DocumentId { get; init; }

    public required int ChunkIndex { get; init; }

    public required string Content { get; init; }
}
