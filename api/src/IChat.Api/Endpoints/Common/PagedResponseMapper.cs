namespace IChat.Api.Endpoints.Common;

using IChat.Core.Common;

public static class PagedResponseMapper
{
    public static PagedResponse<TItem> ToPagedResponse<TSource, TItem>(
        this PaginatedList<TSource> source,
        Func<TSource, TItem> map)
    {
        return new PagedResponse<TItem>
        {
            Items = source.Items.Select(map).ToArray(),
            Offset = source.Offset,
            Limit = source.Limit,
            TotalCount = source.TotalCount,
            HasMore = source.HasMore
        };
    }
}
