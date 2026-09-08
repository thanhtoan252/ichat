namespace IChat.Core.Rag;

using Microsoft.Extensions.AI;

/// <summary>
/// Anthropic yêu cầu message luân phiên user/assistant và message đầu tiên (sau system)
/// phải là user. OpenAI dễ tính hơn nên lỗi này chỉ lộ ra khi đổi provider.
/// </summary>
public static class ChatHistoryNormalizer
{
    public static IReadOnlyList<ChatMessage> Normalize(IReadOnlyList<ChatMessage> history)
    {
        ArgumentNullException.ThrowIfNull(history);

        var cleaned = new List<ChatMessage>(history.Count);

        foreach (var message in history)
        {
            if (message.Role == ChatRole.System)
            {
                continue;
            }

            var text = message.Text;
            if (string.IsNullOrWhiteSpace(text))
            {
                continue;
            }

            var role = message.Role == ChatRole.Assistant ? ChatRole.Assistant : ChatRole.User;

            if (cleaned.Count > 0 && cleaned[^1].Role == role)
            {
                cleaned[^1] = new ChatMessage(role, $"{cleaned[^1].Text}\n\n{text.Trim()}");
                continue;
            }

            cleaned.Add(new ChatMessage(role, text.Trim()));
        }

        // Lịch sử không được mở đầu bằng assistant.
        while (cleaned.Count > 0 && cleaned[0].Role == ChatRole.Assistant)
        {
            cleaned.RemoveAt(0);
        }

        return cleaned;
    }
}
