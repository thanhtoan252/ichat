namespace IChat.Api.Endpoints.Conversations.V1.DTOs;

/// <summary>
/// Không có UserId: chủ sở hữu lấy từ access token, không bao giờ từ body — nếu không
/// thì bất kỳ ai cũng tạo được hội thoại đứng tên người khác.
/// </summary>
public sealed class CreateConversationDto
{
    public string? Title { get; init; }
}
