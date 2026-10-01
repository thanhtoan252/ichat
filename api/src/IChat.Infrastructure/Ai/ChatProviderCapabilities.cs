namespace IChat.Infrastructure.Ai;

/// <summary>
/// IChatClient hides most of the differences between vendors, but not all of them.
/// What is left lives here, and must not leak up into Core.
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
            // Anthropic merges every system block into one separate `system` parameter, and messages have to
            // alternate user/assistant. It also requires max_tokens, but we always send MaxOutputTokens for
            // EVERY provider, so that needs no flag of its own.
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

            // Gemini through the OpenAI-compatible endpoint: it accepts only one system block.
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
