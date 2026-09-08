namespace IChat.Infrastructure.Search.Branches;

using IChat.Core.Abstractions;
using IChat.Core.Rag;
using Microsoft.Extensions.Options;

public sealed class TrigramSearchBranch(IChunkSearch chunkSearch, IOptions<RagOptions> ragOptions) : IRetrievalBranch
{
    private readonly RetrievalOptions _retrieval = ragOptions.Value.Retrieval;

    public string StageName => RetrievalStageName.Trigram;

    // TrigramCandidates = 0 nghĩa là tắt nhánh 3.
    public bool IsEnabledFor(SearchMode mode)
    {
        return (mode is SearchMode.Hybrid or SearchMode.Trigram) && _retrieval.TrigramCandidates > 0;
    }

    public Task<SearchBranchResult> SearchAsync(string query, CancellationToken cancellationToken)
    {
        return chunkSearch.SearchTrigramAsync(query, _retrieval.TrigramCandidates, cancellationToken);
    }
}
