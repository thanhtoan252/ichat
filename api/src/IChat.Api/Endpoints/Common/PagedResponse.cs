namespace IChat.Api.Endpoints.Common;

/// <summary>
/// Bao phân trang dùng chung cho mọi feature; giữ đúng shape của PaginatedList, kèm
/// offset/limit thật sự đã dùng để client biết cửa sổ nào vừa được áp.
/// </summary>
public sealed class PagedResponse<TItem>
{
    public required IReadOnlyList<TItem> Items { get; init; }

    public required int Offset { get; init; }

    public required int Limit { get; init; }

    public required int TotalCount { get; init; }

    public required bool HasMore { get; init; }
}
