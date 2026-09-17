namespace IChat.Core.Abstractions;

/// <summary>Candidate search only. Its only consumers are the <see cref="IRetrievalBranch"/> implementations.</summary>
public interface IChunkSearch
{
    Task<SearchBranchResult> SearchVectorAsync(float[] queryEmbedding, int limit, double minSimilarity, CancellationToken cancellationToken);

    Task<SearchBranchResult> SearchFullTextAsync(string query, int limit, double minRank, CancellationToken cancellationToken);

    Task<SearchBranchResult> SearchTrigramAsync(string query, int limit, CancellationToken cancellationToken);
}
