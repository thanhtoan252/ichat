namespace IChat.Core.Contracts.Search;

using IChat.Core.Rag;

public sealed class SearchChunksRequest
{
    public required string Query { get; init; }

    public int? TopK { get; init; }

    public SearchMode Mode { get; init; } = SearchMode.Hybrid;

    public bool Rewrite { get; init; } = true;

    public IReadOnlyList<SearchHistoryTurn>? History { get; init; }

    public bool ApplyMmr { get; init; } = true;

    public bool ExpandNeighbors { get; init; } = true;

    public bool Rerank { get; init; } = true;
}
