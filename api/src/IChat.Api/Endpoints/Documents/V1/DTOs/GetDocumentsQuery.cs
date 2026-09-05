namespace IChat.Api.Endpoints.Documents.V1.DTOs;

using IChat.Api.Endpoints.Common;
using Microsoft.AspNetCore.Mvc;

public sealed class GetDocumentsQuery(
    [FromQuery(Name = "status")] DocumentStatusFilter? status,
    [FromQuery(Name = "offset")] int? offset,
    [FromQuery(Name = "limit")] int? limit)
{
    public DocumentStatusFilter? Status { get; } = status;

    public int Offset { get; } = PagingQuery.ResolveOffset(offset);

    public int Limit { get; } = PagingQuery.ResolveLimit(limit, defaultLimit: 20, maxLimit: 100);
}
