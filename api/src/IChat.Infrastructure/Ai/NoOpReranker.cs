namespace IChat.Infrastructure.Ai;

using IChat.Core.Abstractions;
using IChat.Core.Rag;

/// <summary>
/// A no-op implementation always exists so the pipeline in Core never needs an `if` on Reranking.Mode.
/// </summary>
public sealed class NoOpReranker : IReranker
{
    public Task<IReadOnlyList<ScoredChunk>> RerankAsync(string query, IReadOnlyList<ScoredChunk> candidates, CancellationToken cancellationToken)
    {
        return Task.FromResult(candidates);
    }
}
