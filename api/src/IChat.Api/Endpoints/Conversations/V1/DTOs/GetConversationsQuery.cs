namespace IChat.Api.Endpoints.Conversations.V1.DTOs;

using Microsoft.AspNetCore.Mvc;

/// <summary>
/// No userId filter any more: the list is always the caller's own conversations (an admin sees
/// all of them), decided by the token rather than by the query string.
/// </summary>
public sealed class GetConversationsQuery(
    [FromQuery(Name = "offset")] int offset = 0,
    [FromQuery(Name = "limit")] int limit = 20)
{
    public int Offset { get; } = Math.Max(offset, 0);

    // Asking for too many is clamped to the ceiling rather than rejected with a 400; the response repeats the limit actually used.
    public int Limit { get; } = limit <= 0 ? 20 : Math.Min(limit, 100);
}
