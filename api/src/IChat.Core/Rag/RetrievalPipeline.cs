namespace IChat.Core.Rag;

using System.Diagnostics;
using IChat.Core.Abstractions;
using Microsoft.Extensions.Options;

public sealed class RetrievalPipeline(
    IEnumerable<IRetrievalBranch> branches,
    IChunkLoader chunkLoader,
    IQueryRewriter queryRewriter,
    IReranker reranker,
    IOptions<RagOptions> ragOptions)
{
    /// <summary>Span riêng cho từng chặng retrieval, để nhìn được chunk rơi rụng ở đâu.</summary>
    public static ActivitySource ActivitySource { get; } = new("IChat.Rag");

    private static readonly SearchBranchResult EmptyBranch = new() { Candidates = [], ElapsedMs = 0 };

    private readonly RagOptions _rag = ragOptions.Value;

    // Thứ tự của IEnumerable<T> là thứ tự đăng ký DI, và đó chính là thứ tự stage mà
    // Retrieval Lab đang đọc: materialise một lần để không phụ thuộc vào lazy enumeration.
    private readonly IReadOnlyList<IRetrievalBranch> _branches = [.. branches];

    public async Task<PipelineOutcome> ExecuteAsync(RetrievalRequest request, CancellationToken cancellationToken)
    {
        using var pipelineActivity = ActivitySource.StartActivity("rag.retrieve");
        pipelineActivity?.SetTag("rag.mode", request.Mode.ToString());

        var totalStopwatch = Stopwatch.StartNew();
        var stages = new StageLog();

        var rewrittenQuery = await RewriteAsync(request, cancellationToken);

        var retrievalStopwatch = Stopwatch.StartNew();

        // Toàn bộ retrieval chạy trên câu hỏi ĐÃ VIẾT LẠI, không phải câu gốc.
        List<IReadOnlyList<Guid>> branchResults;
        bool degraded;

        using (var branchActivity = ActivitySource.StartActivity("rag.branches"))
        {
            (branchResults, degraded) = await RunBranchesAsync(request, rewrittenQuery, stages, cancellationToken);
            branchActivity?.SetTag("rag.branches.enabled", string.Join(",", EnabledBranches(request.Mode).Select(branch => branch.StageName)));
        }

        var fusedChunks = await FuseAndLoadAsync(branchResults, cancellationToken);
        stages.Add(RetrievalStageName.Fused, fusedChunks, retrievalStopwatch);

        var selected = SelectDiverse(request, fusedChunks);
        stages.Add(RetrievalStageName.AfterMmr, selected, retrievalStopwatch);

        if (request.Rerank && _rag.Reranking.Mode != RerankMode.None)
        {
            selected = [.. await reranker.RerankAsync(rewrittenQuery, selected, cancellationToken)];
            stages.Add(RetrievalStageName.Reranked, selected, retrievalStopwatch);
        }

        var contexts = await ExpandNeighborsAsync(request, selected, cancellationToken);

        pipelineActivity?.SetTag("rag.degraded", degraded);
        pipelineActivity?.SetTag("rag.final_count", selected.Count);

        stages.Add(RetrievalStageName.Final, selected, retrievalStopwatch);
        retrievalStopwatch.Stop();

        return new PipelineOutcome
        {
            OriginalQuery = request.Query,
            RewrittenQuery = rewrittenQuery,
            Final = selected,
            Contexts = contexts,
            Stages = stages.Stages,
            Degraded = degraded,
            ElapsedMs = totalStopwatch.ElapsedMilliseconds,
            RetrievalMs = retrievalStopwatch.ElapsedMilliseconds
        };
    }

    private async Task<string> RewriteAsync(RetrievalRequest request, CancellationToken cancellationToken)
    {
        using var rewriteActivity = ActivitySource.StartActivity("rag.rewrite");

        var rewrittenQuery = request.Rewrite
            ? await queryRewriter.RewriteAsync(request.Query, request.History ?? [], cancellationToken)
            : request.Query;

        rewriteActivity?.SetTag("rag.rewritten", rewrittenQuery != request.Query);

        return rewrittenQuery;
    }

    /// <summary>
    /// Trộn các nhánh bằng RRF rồi nạp lại nội dung chunk theo đúng thứ tự đã trộn.
    /// Chunk không nạp được (đã bị xoá giữa chừng) bị loại bỏ chứ không làm hỏng request.
    /// </summary>
    private async Task<List<ScoredChunk>> FuseAndLoadAsync(
        List<IReadOnlyList<Guid>> branches,
        CancellationToken cancellationToken)
    {
        var fused = ReciprocalRankFusion.Fuse(branches, _rag.Retrieval.RrfK, _rag.Retrieval.FusedTopK);

        var fusedIds = fused.Select(item => item.Id).ToList();
        var loaded = await chunkLoader.LoadChunksAsync(fusedIds, cancellationToken);
        var scoreById = fused.ToDictionary(item => item.Id, item => item.Score);

        return fusedIds
            .Select(id => loaded.FirstOrDefault(chunk => chunk.ChunkId == id))
            .Where(chunk => chunk is not null)
            .Select(chunk => chunk!.WithScore(scoreById[chunk.ChunkId]))
            .ToList();
    }

    private List<ScoredChunk> SelectDiverse(RetrievalRequest request, List<ScoredChunk> fusedChunks)
    {
        var finalTopK = request.TopK ?? _rag.Diversity.FinalTopK;

        if (!request.ApplyMmr)
        {
            return fusedChunks.Take(finalTopK).ToList();
        }

        return [.. MaximalMarginalRelevance.Select(
            fusedChunks,
            _rag.Diversity.MmrLambda,
            _rag.Diversity.MaxChunksPerDocument,
            finalTopK)];
    }

    /// <summary>
    /// Chạy song song các nhánh mà mode hiện tại bật, nhưng ghi stage cho MỌI nhánh đã đăng ký:
    /// Retrieval Lab đọc kết quả theo tên chặng, và một chặng biến mất khác hẳn một chặng rỗng.
    /// </summary>
    private async Task<(List<IReadOnlyList<Guid>> Branches, bool Degraded)> RunBranchesAsync(
        RetrievalRequest request,
        string rewrittenQuery,
        StageLog stages,
        CancellationToken cancellationToken)
    {
        var running = new Dictionary<IRetrievalBranch, Task<SearchBranchResult>>();

        foreach (var branch in EnabledBranches(request.Mode))
        {
            running[branch] = branch.SearchAsync(rewrittenQuery, cancellationToken);
        }

        await Task.WhenAll(running.Values);

        var branches = new List<IReadOnlyList<Guid>>();
        var degraded = false;

        foreach (var branch in _branches)
        {
            var result = running.TryGetValue(branch, out var task) ? await task : EmptyBranch;

            degraded |= result.Degraded;
            stages.AddBranch(branch.StageName, result);

            if (result.Candidates.Count > 0)
            {
                branches.Add(result.Candidates.Select(candidate => candidate.ChunkId).ToList());
            }
        }

        return (branches, degraded);
    }

    private IEnumerable<IRetrievalBranch> EnabledBranches(SearchMode mode)
    {
        return _branches.Where(branch => branch.IsEnabledFor(mode));
    }

    private async Task<IReadOnlyList<ExpandedContext>> ExpandNeighborsAsync(
        RetrievalRequest request,
        IReadOnlyList<ScoredChunk> selected,
        CancellationToken cancellationToken)
    {
        using var expandActivity = ActivitySource.StartActivity("rag.expand_neighbors");

        var contexts = await ExpandAsync(request, selected, cancellationToken);
        expandActivity?.SetTag("rag.context_blocks", contexts.Count);

        return contexts;
    }

    private async Task<IReadOnlyList<ExpandedContext>> ExpandAsync(
        RetrievalRequest request,
        IReadOnlyList<ScoredChunk> selected,
        CancellationToken cancellationToken)
    {
        if (selected.Count == 0)
        {
            return [];
        }

        if (!request.ExpandNeighbors || !_rag.NeighborExpansion.Enabled)
        {
            return NeighborExpansion.Expand(selected, [], 0, 0);
        }

        var keys = NeighborExpansion.PlanNeighborKeys(selected, _rag.NeighborExpansion.Before, _rag.NeighborExpansion.After);
        var neighbors = await chunkLoader.LoadNeighborsAsync(keys, cancellationToken);

        return NeighborExpansion.Expand(selected, neighbors, _rag.NeighborExpansion.Before, _rag.NeighborExpansion.After);
    }

    private static IReadOnlyList<RetrievalCandidate> ToCandidates(IReadOnlyList<ScoredChunk> chunks)
    {
        return chunks
            .Select(chunk => new RetrievalCandidate
            {
                ChunkId = chunk.ChunkId,
                DocumentId = chunk.DocumentId,
                Content = TextSnippet.From(chunk.Content),
                HeadingPath = chunk.HeadingPath,
                ChunkIndex = chunk.ChunkIndex,
                Score = chunk.Score
            })
            .ToList();
    }

    /// <summary>
    /// Nhật ký từng chặng. Endpoint search trả nguyên nó ra ngoài, vì phần lớn thời gian
    /// debug RAG là nhìn xem chunk rơi rụng ở chặng nào chứ không phải sửa prompt.
    /// </summary>
    private sealed class StageLog
    {
        private readonly List<RetrievalStage> _stages = [];

        public IReadOnlyList<RetrievalStage> Stages => _stages;

        public void Add(string name, IReadOnlyList<ScoredChunk> chunks, Stopwatch stopwatch)
        {
            _stages.Add(new RetrievalStage
            {
                Name = name,
                Count = chunks.Count,
                ElapsedMs = stopwatch.ElapsedMilliseconds,
                Top = ToCandidates(chunks)
            });
        }

        public void AddBranch(string name, SearchBranchResult branch)
        {
            _stages.Add(new RetrievalStage
            {
                Name = name,
                Count = branch.Candidates.Count,
                ElapsedMs = branch.ElapsedMs,
                Top = branch.Candidates,
                TsQuery = branch.TsQuery
            });
        }
    }
}
