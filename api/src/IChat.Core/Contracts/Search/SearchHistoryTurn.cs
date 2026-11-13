namespace IChat.Core.Contracts.Search;

public sealed class SearchHistoryTurn
{
    public required string Role { get; init; }

    public required string Content { get; init; }
}
