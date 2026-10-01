namespace IChat.Core.Abstractions;

using IChat.Core.Rag;

/// <summary>Loads chunk content for already-known ids. Its only consumer is RetrievalPipeline.</summary>
public interface IChunkLoader
{
    Task<IReadOnlyList<ScoredChunk>> LoadChunksAsync(IReadOnlyList<Guid> chunkIds, CancellationToken cancellationToken);

    Task<IReadOnlyList<NeighborChunk>> LoadNeighborsAsync(IReadOnlyList<(Guid DocumentId, int ChunkIndex)> keys, CancellationToken cancellationToken);
}
