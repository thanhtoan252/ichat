namespace IChat.Api.Endpoints.Conversations.V1.DTOs;

using Microsoft.AspNetCore.Mvc;

public sealed class GetMessagesQuery(
    [FromQuery(Name = "offset")] int offset = 0,
    [FromQuery(Name = "limit")] int limit = 50)
{
    public int Offset { get; } = Math.Max(offset, 0);

    // Asking for too many is clamped to the ceiling rather than rejected with a 400; the response repeats the limit actually used.
    public int Limit { get; } = limit <= 0 ? 50 : Math.Min(limit, 200);
}
