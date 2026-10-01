namespace IChat.Core.Rag;

public sealed class PipelineOutcome
{
    public required string OriginalQuery { get; init; }

    public required string RewrittenQuery { get; init; }

    public required IReadOnlyList<ScoredChunk> Final { get; init; }

    public required IReadOnlyList<ExpandedContext> Contexts { get; init; }

    public required IReadOnlyList<RetrievalStage> Stages { get; init; }

    public required bool Degraded { get; init; }

    public required long ElapsedMs { get; init; }

    public required long RetrievalMs { get; init; }
}
