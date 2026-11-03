namespace IChat.Core.Abstractions;

using IChat.Core.Rag;

public interface IReranker
{
    Task<IReadOnlyList<ScoredChunk>> RerankAsync(string query, IReadOnlyList<ScoredChunk> candidates, CancellationToken cancellationToken);
}
