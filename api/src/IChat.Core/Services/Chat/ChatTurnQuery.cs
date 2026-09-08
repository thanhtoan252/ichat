namespace IChat.Core.Services.Chat;

using Microsoft.Extensions.AI;

/// <summary>Kết quả của nửa đầu một lượt chat: đã có history và câu hỏi dùng để đi tìm.</summary>
public sealed record ChatTurnQuery
{
    public required IReadOnlyList<ChatMessage> History { get; init; }

    public required string RewrittenQuery { get; init; }
}
