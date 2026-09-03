namespace IChat.Infrastructure.Ai;

/// <summary>
/// Tách rời khỏi <see cref="ChatProvider"/> một cách có chủ đích: Anthropic KHÔNG có
/// embedding API, nên gộp hai thứ vào một enum sẽ chết ngay ở cấu hình phổ biến nhất
/// (chat = Claude, embedding = OpenAI). Vì vậy ở đây không có phần tử Anthropic.
/// </summary>
public enum EmbeddingProvider
{
    OpenAI,
    AzureOpenAI,
    Google
}
