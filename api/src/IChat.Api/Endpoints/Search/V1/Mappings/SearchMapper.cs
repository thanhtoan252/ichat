namespace IChat.Api.Endpoints.Search.V1.Mappings;

using IChat.Api.Endpoints.Search.V1.DTOs;
using IChat.Core.Contracts.Search;
using IChat.Core.Rag;

public static class SearchMapper
{
    public static SearchChunksRequest ToServiceRequest(this SearchRequestDto dto) =>
        new()
        {
            Query = dto.Query,
            TopK = dto.TopK,
            Mode = (SearchMode)dto.Mode,
            Rewrite = dto.Rewrite,
            History = dto.History?.Select(turn => new SearchHistoryTurn
            {
                Role = turn.Role,
                Content = turn.Content
            }).ToArray(),
            ApplyMmr = dto.ApplyMmr,
            ExpandNeighbors = dto.ExpandNeighbors,
            Rerank = dto.Rerank
        };

    public static SearchResponse ToResponse(this SearchChunksResult result) =>
        new()
        {
            OriginalQuery = result.OriginalQuery,
            RewrittenQuery = result.RewrittenQuery,
            Stages = result.Stages.ToDictionary(stage => stage.Key, stage => stage.Value.ToResponse()),
            Degraded = result.Degraded,
            ElapsedMs = result.ElapsedMs
        };

    private static SearchStageResponse ToResponse(this SearchStageView stage) =>
        new()
        {
            Name = stage.Name,
            Count = stage.Count,
            ElapsedMs = stage.ElapsedMs,
            TsQuery = stage.TsQuery,
            Top = stage.Top.Select(ToResponse).ToArray()
        };

    private static SearchHitResponse ToResponse(this SearchHit hit) =>
        new()
        {
            ChunkId = hit.ChunkId,
            DocumentId = hit.DocumentId,
            HeadingPath = hit.HeadingPath,
            Snippet = hit.Snippet,
            ChunkIndex = hit.ChunkIndex,
            Score = hit.Score
        };
}
