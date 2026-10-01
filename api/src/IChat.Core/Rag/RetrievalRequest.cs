namespace IChat.Core.Rag;

using Microsoft.Extensions.AI;

public sealed class RetrievalRequest
{
    public required string Query { get; init; }

    public SearchMode Mode { get; init; } = SearchMode.Hybrid;

    public int? TopK { get; init; }

    public bool Rewrite { get; init; } = true;

    public IReadOnlyList<ChatMessage>? History { get; init; }

    public bool ApplyMmr { get; init; } = true;

    public bool ExpandNeighbors { get; init; } = true;

    public bool Rerank { get; init; } = true;
}
