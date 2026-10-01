namespace IChat.Core.Services;

using IChat.Core.Abstractions;
using IChat.Core.Common;
using IChat.Core.Contracts.Search;
using IChat.Core.Rag;
using Microsoft.Extensions.AI;

public sealed class SearchService(RetrievalPipeline pipeline) : ISearchService
{
    public async Task<Result<SearchChunksResult>> SearchAsync(SearchChunksRequest request, CancellationToken cancellationToken)
    {
        var history = (request.History ?? [])
            .Select(turn => new ChatMessage(
                turn.Role.Equals("assistant", StringComparison.OrdinalIgnoreCase) ? ChatRole.Assistant : ChatRole.User,
                turn.Content))
            .ToList();

        var retrievalRequest = new RetrievalRequest
        {
            Query = request.Query,
            Mode = request.Mode,
            TopK = request.TopK,
            Rewrite = request.Rewrite,
            History = history,
            ApplyMmr = request.ApplyMmr,
            ExpandNeighbors = request.ExpandNeighbors,
            Rerank = request.Rerank
        };

        var outcome = await pipeline.ExecuteAsync(retrievalRequest, cancellationToken);

        var stages = outcome.Stages.ToDictionary(
            stage => stage.Name,
            stage => new SearchStageView
            {
                Name = stage.Name,
                Count = stage.Count,
                ElapsedMs = stage.ElapsedMs,
                TsQuery = stage.TsQuery,
                Top = stage.Top.Select(candidate => new SearchHit
                {
                    ChunkId = candidate.ChunkId,
                    DocumentId = candidate.DocumentId,
                    HeadingPath = candidate.HeadingPath,
                    Snippet = candidate.Content,
                    ChunkIndex = candidate.ChunkIndex,
                    Score = candidate.Score
                }).ToList()
            });

        return Result.Success(new SearchChunksResult
        {
            OriginalQuery = outcome.OriginalQuery,
            RewrittenQuery = outcome.RewrittenQuery,
            Stages = stages,
            Degraded = outcome.Degraded,
            ElapsedMs = outcome.ElapsedMs
        });
    }
}
