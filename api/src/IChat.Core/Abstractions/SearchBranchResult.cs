namespace IChat.Core.Abstractions;

using IChat.Core.Rag;

public sealed class SearchBranchResult
{
    public required IReadOnlyList<RetrievalCandidate> Candidates { get; init; }

    public required long ElapsedMs { get; init; }

    public string? TsQuery { get; init; }

    /// <summary>
    /// The branch reports for itself that it ran degraded (for instance the embedding call failed so
    /// it found nothing), so the pipeline never has to know what goes on inside a branch.
    /// </summary>
    public bool Degraded { get; init; }
}
