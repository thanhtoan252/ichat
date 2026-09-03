namespace IChat.Core.Abstractions;

/// <summary>Chỉ tìm kiếm ứng viên. Consumer duy nhất là các <see cref="IRetrievalBranch"/>.</summary>
public interface IChunkSearch
{
    Task<SearchBranchResult> SearchVectorAsync(float[] queryEmbedding, int limit, double minSimilarity, CancellationToken cancellationToken);

    Task<SearchBranchResult> SearchFullTextAsync(string query, int limit, double minRank, CancellationToken cancellationToken);

    Task<SearchBranchResult> SearchTrigramAsync(string query, int limit, CancellationToken cancellationToken);
}
