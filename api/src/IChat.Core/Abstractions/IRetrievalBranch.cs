namespace IChat.Core.Abstractions;

using IChat.Core.Rag;

/// <summary>One independent search branch. Adding a branch = one implementation plus one DI line.</summary>
public interface IRetrievalBranch
{
    /// <summary>The stage key returned to the Retrieval Lab; taken from RetrievalStageName.</summary>
    string StageName { get; }

    /// <summary>False when the mode does not select this branch, or when config has turned it off (TrigramCandidates = 0).</summary>
    bool IsEnabledFor(SearchMode mode);

    Task<SearchBranchResult> SearchAsync(string query, CancellationToken cancellationToken);
}
