namespace IChat.Core.Abstractions;

using IChat.Core.Rag;

public interface IRetrievalBranch
{

string StageName { get; }

bool IsEnabledFor(SearchMode mode);

Task<SearchBranchResult> SearchAsync(string query, CancellationToken cancellationToken);
}
