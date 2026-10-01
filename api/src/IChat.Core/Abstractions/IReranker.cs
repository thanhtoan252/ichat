namespace IChat.Core.Abstractions;

using IChat.Core.Rag;

/// <summary>
/// There is always a no-op implementation so the pipeline never needs an if branch in Core.
/// Note: RRF is fusion, NOT reranking.
/// </summary>
public interface IReranker
{
    Task<IReadOnlyList<ScoredChunk>> RerankAsync(string query, IReadOnlyList<ScoredChunk> candidates, CancellationToken cancellationToken);
}
