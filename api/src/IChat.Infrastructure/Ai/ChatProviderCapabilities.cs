namespace IChat.Infrastructure.Ai;

/// <summary>
/// IChatClient che được phần lớn khác biệt giữa các hãng nhưng không phải tất cả.
/// Những khác biệt còn lại nằm ở đây, không được rò rỉ lên Core.
/// </summary>
public sealed record ChatProviderCapabilities
{
    public required bool SupportsMultipleSystemMessages { get; init; }

    public required bool RequiresEndpoint { get; init; }

    public required bool RequiresApiKey { get; init; }

    public static ChatProviderCapabilities For(ChatProvider provider)
    {
        return provider switch
        {
            // Anthropic gộp mọi khối system vào một tham số `system` riêng; message phải
            // luân phiên user/assistant. Hãng này còn bắt buộc max_tokens, nhưng ta luôn
            // gửi MaxOutputTokens cho MỌI provider nên không cần một cờ riêng cho nó.
            ChatProvider.Anthropic => new ChatProviderCapabilities
            {
                SupportsMultipleSystemMessages = false,
                RequiresEndpoint = false,
                RequiresApiKey = true
            },

            ChatProvider.OpenAI => new ChatProviderCapabilities
            {
                SupportsMultipleSystemMessages = true,
                RequiresEndpoint = false,
                RequiresApiKey = true
            },

            ChatProvider.AzureOpenAI => new ChatProviderCapabilities
            {
                SupportsMultipleSystemMessages = true,
                RequiresEndpoint = true,
                RequiresApiKey = true
            },

            // Gemini qua endpoint OpenAI-compatible: chỉ nhận một khối system.
            ChatProvider.Google => new ChatProviderCapabilities
            {
                SupportsMultipleSystemMessages = false,
                RequiresEndpoint = false,
                RequiresApiKey = true
            },

            _ => new ChatProviderCapabilities
            {
                SupportsMultipleSystemMessages = true,
                RequiresEndpoint = false,
                RequiresApiKey = true
            }
        };
    }
}
