namespace IChat.Core.Rag;

using Microsoft.Extensions.AI;

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

while (cleaned.Count > 0 && cleaned[0].Role == ChatRole.Assistant)
        {
            cleaned.RemoveAt(0);
        }

return cleaned;
    }
}
