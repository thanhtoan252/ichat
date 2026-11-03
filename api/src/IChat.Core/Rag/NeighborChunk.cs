namespace IChat.Core.Rag;

public sealed class NeighborChunk
{
    public required Guid DocumentId { get; init; }

public required int ChunkIndex { get; init; }

public required string Content { get; init; }
}
