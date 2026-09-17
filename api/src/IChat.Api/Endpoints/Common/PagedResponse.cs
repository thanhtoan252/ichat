namespace IChat.Api.Endpoints.Common;

/// <summary>
/// The paging envelope shared by every feature; it keeps the shape of PaginatedList and
/// echoes the offset/limit actually applied, so the client knows which window it got.
/// </summary>
public sealed class PagedResponse<TItem>
{
    public required IReadOnlyList<TItem> Items { get; init; }

    public required int Offset { get; init; }

    public required int Limit { get; init; }

    public required int TotalCount { get; init; }

    public required bool HasMore { get; init; }
}
