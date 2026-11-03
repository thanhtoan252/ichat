namespace IChat.Core.Abstractions;

using IChat.Core.Rag;

public interface IChunkLoader
{
    Task<IReadOnlyList<ScoredChunk>> LoadChunksAsync(IReadOnlyList<Guid> chunkIds, CancellationToken cancellationToken);

Task<IReadOnlyList<NeighborChunk>> LoadNeighborsAsync(IReadOnlyList<(Guid DocumentId, int ChunkIndex)> keys, CancellationToken cancellationToken);
}
