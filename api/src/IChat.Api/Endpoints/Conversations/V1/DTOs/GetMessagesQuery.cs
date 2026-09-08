namespace IChat.Api.Endpoints.Conversations.V1.DTOs;

using Microsoft.AspNetCore.Mvc;

public sealed class GetMessagesQuery(
    [FromQuery(Name = "offset")] int offset = 0,
    [FromQuery(Name = "limit")] int limit = 50)
{
    public int Offset { get; } = Math.Max(offset, 0);

    // Xin quá nhiều thì bị cắt về trần chứ không phải 400; phản hồi nói lại limit thật sự đã dùng.
    public int Limit { get; } = limit <= 0 ? 50 : Math.Min(limit, 200);
}
