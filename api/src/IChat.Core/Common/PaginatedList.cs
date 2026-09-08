namespace IChat.Core.Common;

public sealed class PaginatedList<TItem>
{
    public required IReadOnlyList<TItem> Items { get; init; }

    public required int Offset { get; init; }

    public required int Limit { get; init; }

    public required int TotalCount { get; init; }

    /// <summary>Suy ra từ những gì đã trả, nên không nói dối khi trang cuối ngắn hơn limit.</summary>
    public bool HasMore => Offset + Items.Count < TotalCount;
}
