namespace IChat.Api.Endpoints.Conversations.V1.DTOs;

/// <summary>ConversationId lấy từ route nên không nằm trong body.</summary>
public sealed class SendMessageDto
{
    // Không đánh dấu required: thiếu content phải rơi vào validator của ChatService
    // để lỗi đi qua kênh SSE, chứ không thành 400 ngay ở tầng deserialize.
    public string Content { get; init; } = string.Empty;

    public string? Model { get; init; }
}
