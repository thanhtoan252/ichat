namespace IChat.Api.Endpoints.Search.V1.DTOs;

public sealed class SearchRequestDto
{
    public required string Query { get; init; }

    public int? TopK { get; init; }

    public SearchModeDto Mode { get; init; } = SearchModeDto.Hybrid;

    public bool Rewrite { get; init; } = true;

    public IReadOnlyList<SearchHistoryTurnDto>? History { get; init; }

    public bool ApplyMmr { get; init; } = true;

    public bool ExpandNeighbors { get; init; } = true;

    public bool Rerank { get; init; } = true;
}
