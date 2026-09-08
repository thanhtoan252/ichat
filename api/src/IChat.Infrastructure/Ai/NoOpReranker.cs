namespace IChat.Infrastructure.Ai;

using IChat.Core.Abstractions;
using IChat.Core.Rag;

/// <summary>
/// Luôn tồn tại một implementation no-op để pipeline ở Core không phải rẽ nhánh `if`
/// theo Reranking.Mode.
/// </summary>
public sealed class NoOpReranker : IReranker
{
    public Task<IReadOnlyList<ScoredChunk>> RerankAsync(string query, IReadOnlyList<ScoredChunk> candidates, CancellationToken cancellationToken)
    {
        return Task.FromResult(candidates);
    }
}
