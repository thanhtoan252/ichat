namespace IChat.Api.Endpoints.Search.V1.DTOs;

public sealed class SearchStageResponse
{
    public required string Name { get; init; }

    public required int Count { get; init; }

    public required long ElapsedMs { get; init; }

    public string? TsQuery { get; init; }

    public required IReadOnlyList<SearchHitResponse> Top { get; init; }
}
