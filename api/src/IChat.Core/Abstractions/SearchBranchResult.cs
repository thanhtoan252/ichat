namespace IChat.Core.Abstractions;

using IChat.Core.Rag;

public sealed class SearchBranchResult
{
    public required IReadOnlyList<RetrievalCandidate> Candidates { get; init; }

    public required long ElapsedMs { get; init; }

    public string? TsQuery { get; init; }

    public bool Degraded { get; init; }
}
