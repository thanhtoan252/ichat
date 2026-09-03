namespace IChat.Api.Endpoints.Conversations.V1.DTOs;

using IChat.Api.Endpoints.Common;
using Microsoft.AspNetCore.Mvc;

public sealed class GetConversationsQuery(
    [FromQuery(Name = "page")] int? page,
    [FromQuery(Name = "pageSize")] int? pageSize,
    [FromQuery(Name = "userId")] string? userId) : IPagedQuery
{
    private const int DefaultPageSize = 20;

    public int? Page { get; } = page;

    public int? PageSize { get; } = pageSize;

    public string? UserId { get; } = userId;

    public int EffectivePage => PageQuery.ResolvePage(Page);

    public int EffectivePageSize => PageQuery.ResolvePageSize(PageSize, DefaultPageSize);
}
