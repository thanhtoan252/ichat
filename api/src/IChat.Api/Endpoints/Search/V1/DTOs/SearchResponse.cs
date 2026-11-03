namespace IChat.Api.Endpoints.Search.V1.DTOs;

public sealed class SearchResponse
{
    public required string OriginalQuery { get; init; }

public required string RewrittenQuery { get; init; }

public required IReadOnlyDictionary<string, SearchStageResponse> Stages { get; init; }

public required bool Degraded { get; init; }

public required long ElapsedMs { get; init; }
}
