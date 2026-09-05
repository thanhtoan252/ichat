namespace IChat.Api.Endpoints.Conversations.V1.DTOs;

using IChat.Api.Endpoints.Common;
using Microsoft.AspNetCore.Mvc;

public sealed class GetMessagesQuery(
    [FromQuery(Name = "offset")] int? offset,
    [FromQuery(Name = "limit")] int? limit)
{
    public int Offset { get; } = PagingQuery.ResolveOffset(offset);

    public int Limit { get; } = PagingQuery.ResolveLimit(limit, defaultLimit: 50, maxLimit: 200);
}
