namespace IChat.Infrastructure.Search.Branches;

using System.Diagnostics;
using IChat.Core.Abstractions;
using IChat.Core.Rag;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

public sealed class VectorSearchBranch(
    IChunkSearch chunkSearch,
    IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator,
    IOptions<RagOptions> ragOptions,
    ILogger<VectorSearchBranch> logger) : IRetrievalBranch
{
    private readonly RetrievalOptions _retrieval = ragOptions.Value.Retrieval;

    public string StageName => RetrievalStageName.Vector;

    public bool IsEnabledFor(SearchMode mode)
    {
        return mode is SearchMode.Hybrid or SearchMode.Vector;
    }

    public async Task<SearchBranchResult> SearchAsync(string query, CancellationToken cancellationToken)
    {
        var embedding = await TryEmbedAsync(query, cancellationToken);

        if (embedding is null)
        {
            // Embedding hỏng là bước phụ có thể degrade: chạy tiếp bằng full-text
            // và trigram thay vì làm hỏng cả request.
            logger.LogWarning("The embedding provider failed; retrieval degrades to full-text and trigram.");

            return new SearchBranchResult { Candidates = [], ElapsedMs = 0, Degraded = true };
        }

        return await chunkSearch.SearchVectorAsync(
            embedding,
            _retrieval.VectorCandidates,
            _retrieval.VectorMinSimilarity,
            cancellationToken);
    }

    private async Task<float[]?> TryEmbedAsync(string query, CancellationToken cancellationToken)
    {
        try
        {
            var embeddings = await embeddingGenerator.GenerateAsync([query], cancellationToken: cancellationToken);

            return embeddings[0].Vector.ToArray();
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogWarning(exception, "Could not generate an embedding for the query.");

            return null;
        }
    }
}
