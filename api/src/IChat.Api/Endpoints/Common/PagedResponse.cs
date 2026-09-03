namespace IChat.Api.Endpoints.Common;

/// <summary>Bao phân trang dùng chung cho mọi feature; giữ đúng shape của PaginatedList.</summary>
public sealed class PagedResponse<TItem>
{
    public required IReadOnlyList<TItem> Items { get; init; }

    public required int Page { get; init; }

    public required int PageSize { get; init; }

    public required int TotalCount { get; init; }

    public required int TotalPages { get; init; }

    public required bool HasNextPage { get; init; }
}
