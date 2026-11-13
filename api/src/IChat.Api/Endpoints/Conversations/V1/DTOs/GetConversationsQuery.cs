namespace IChat.Api.Endpoints.Conversations.V1.DTOs;

using Microsoft.AspNetCore.Mvc;

public sealed class GetConversationsQuery(
    [FromQuery(Name = "offset")] int offset = 0,
    [FromQuery(Name = "limit")] int limit = 20)
{
    public int Offset { get; } = Math.Max(offset, 0);

public int Limit { get; } = limit <= 0 ? 20 : Math.Min(limit, 100);
}
