namespace IChat.Infrastructure.Ai;

using IChat.Core.Abstractions;
using IChat.Core.Rag;

public sealed class NoOpReranker : IReranker
{
    public Task<IReadOnlyList<ScoredChunk>> RerankAsync(string query, IReadOnlyList<ScoredChunk> candidates, CancellationToken cancellationToken)
    {
        return Task.FromResult(candidates);
    }
}
