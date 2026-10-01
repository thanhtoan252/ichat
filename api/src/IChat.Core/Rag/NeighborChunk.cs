namespace IChat.Core.Rag;

/// <summary>A neighbouring chunk used to enrich the context. It never becomes a citation.</summary>
public sealed class NeighborChunk
{
    public required Guid DocumentId { get; init; }

    public required int ChunkIndex { get; init; }

    public required string Content { get; init; }
}
