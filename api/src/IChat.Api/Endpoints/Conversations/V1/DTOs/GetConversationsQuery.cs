namespace IChat.Api.Endpoints.Conversations.V1.DTOs;

using IChat.Api.Endpoints.Common;
using Microsoft.AspNetCore.Mvc;

/// <summary>
/// Không còn lọc theo userId: danh sách luôn là hội thoại của chính người gọi
/// (admin thì thấy tất cả), quyết định bởi token chứ không bởi query string.
/// </summary>
public sealed class GetConversationsQuery(
    [FromQuery(Name = "page")] int? page,
    [FromQuery(Name = "pageSize")] int? pageSize) : IPagedQuery
{
    private const int DefaultPageSize = 20;

    public int? Page { get; } = page;

    public int? PageSize { get; } = pageSize;

    public int EffectivePage => PageQuery.ResolvePage(Page);

    public int EffectivePageSize => PageQuery.ResolvePageSize(PageSize, DefaultPageSize);
}
