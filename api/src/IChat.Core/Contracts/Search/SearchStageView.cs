namespace IChat.Core.Contracts.Search;

public sealed class SearchStageView
{
    public required string Name { get; init; }

    public required int Count { get; init; }

    public required long ElapsedMs { get; init; }

    public string? TsQuery { get; init; }

    public required IReadOnlyList<SearchHit> Top { get; init; }
}
