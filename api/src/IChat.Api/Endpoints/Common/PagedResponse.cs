namespace IChat.Api.Endpoints.Common;

public sealed class PagedResponse<TItem>
{
    public required IReadOnlyList<TItem> Items { get; init; }

    public required int Offset { get; init; }

    public required int Limit { get; init; }

    public required int TotalCount { get; init; }

    public required bool HasMore { get; init; }
}
