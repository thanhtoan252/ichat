namespace IChat.Api.Endpoints.Conversations.V1.DTOs;

using Microsoft.AspNetCore.Mvc;

/// <summary>
/// Không còn lọc theo userId: danh sách luôn là hội thoại của chính người gọi
/// (admin thì thấy tất cả), quyết định bởi token chứ không bởi query string.
/// </summary>
public sealed class GetConversationsQuery(
    [FromQuery(Name = "offset")] int offset = 0,
    [FromQuery(Name = "limit")] int limit = 20)
{
    public int Offset { get; } = Math.Max(offset, 0);

    // Xin quá nhiều thì bị cắt về trần chứ không phải 400; phản hồi nói lại limit thật sự đã dùng.
    public int Limit { get; } = limit <= 0 ? 20 : Math.Min(limit, 100);
}
