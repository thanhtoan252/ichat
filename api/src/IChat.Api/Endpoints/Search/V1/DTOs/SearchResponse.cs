namespace IChat.Api.Endpoints.Search.V1.DTOs;

/// <summary>
/// Returning PER-STAGE results and the tsquery that was built is the whole point: most of the time
/// spent debugging RAG goes into seeing which stage dropped the chunks, not into editing prompts.
/// </summary>
public sealed class SearchResponse
{
    public required string OriginalQuery { get; init; }

    public required string RewrittenQuery { get; init; }

    public required IReadOnlyDictionary<string, SearchStageResponse> Stages { get; init; }

    public required bool Degraded { get; init; }

    public required long ElapsedMs { get; init; }
}
