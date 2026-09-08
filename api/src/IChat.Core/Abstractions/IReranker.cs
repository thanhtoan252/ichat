namespace IChat.Core.Abstractions;

using IChat.Core.Rag;

/// <summary>
/// Luôn có implementation no-op để pipeline không phải rẽ nhánh if ở tầng Core.
/// Lưu ý: RRF là fusion, KHÔNG phải rerank.
/// </summary>
public interface IReranker
{
    Task<IReadOnlyList<ScoredChunk>> RerankAsync(string query, IReadOnlyList<ScoredChunk> candidates, CancellationToken cancellationToken);
}
