namespace IChat.Core.Rag;

public sealed class RetrievalStage
{
    public required string Name { get; init; }

    public required int Count { get; init; }

    public required long ElapsedMs { get; init; }

    public required IReadOnlyList<RetrievalCandidate> Top { get; init; }

    public string? TsQuery { get; init; }
}
