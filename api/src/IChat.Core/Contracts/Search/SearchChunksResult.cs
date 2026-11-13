namespace IChat.Core.Contracts.Search;

public sealed class SearchChunksResult
{
    public required string OriginalQuery { get; init; }

    public required string RewrittenQuery { get; init; }

    public required IReadOnlyDictionary<string, SearchStageView> Stages { get; init; }

    public required bool Degraded { get; init; }

    public required long ElapsedMs { get; init; }
}
