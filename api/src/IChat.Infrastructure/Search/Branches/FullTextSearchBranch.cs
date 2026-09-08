namespace IChat.Infrastructure.Search.Branches;

using IChat.Core.Abstractions;
using IChat.Core.Rag;
using Microsoft.Extensions.Options;

public sealed class FullTextSearchBranch(IChunkSearch chunkSearch, IOptions<RagOptions> ragOptions) : IRetrievalBranch
{
    private readonly RetrievalOptions _retrieval = ragOptions.Value.Retrieval;

    public string StageName => RetrievalStageName.FullText;

    public bool IsEnabledFor(SearchMode mode)
    {
        return mode is SearchMode.Hybrid or SearchMode.FullText;
    }

    public Task<SearchBranchResult> SearchAsync(string query, CancellationToken cancellationToken)
    {
        return chunkSearch.SearchFullTextAsync(
            query,
            _retrieval.FullTextCandidates,
            _retrieval.FullTextMinRank,
            cancellationToken);
    }
}
