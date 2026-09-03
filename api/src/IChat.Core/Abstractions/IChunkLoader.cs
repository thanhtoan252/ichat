namespace IChat.Core.Abstractions;

using IChat.Core.Rag;

/// <summary>Chỉ nạp nội dung chunk đã biết id. Consumer duy nhất là RetrievalPipeline.</summary>
public interface IChunkLoader
{
    Task<IReadOnlyList<ScoredChunk>> LoadChunksAsync(IReadOnlyList<Guid> chunkIds, CancellationToken cancellationToken);

    Task<IReadOnlyList<NeighborChunk>> LoadNeighborsAsync(IReadOnlyList<(Guid DocumentId, int ChunkIndex)> keys, CancellationToken cancellationToken);
}
