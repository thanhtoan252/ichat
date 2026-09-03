namespace IChat.Api.Endpoints.Documents.V1.DTOs;

using IChat.Api.Endpoints.Common;
using Microsoft.AspNetCore.Mvc;

public sealed class GetDocumentChunksQuery(
    [FromQuery(Name = "page")] int? page,
    [FromQuery(Name = "pageSize")] int? pageSize) : IPagedQuery
{
    private const int DefaultPageSize = 50;

    public int? Page { get; } = page;

    public int? PageSize { get; } = pageSize;

    public int EffectivePage => PageQuery.ResolvePage(Page);

    public int EffectivePageSize => PageQuery.ResolvePageSize(PageSize, DefaultPageSize);
}
