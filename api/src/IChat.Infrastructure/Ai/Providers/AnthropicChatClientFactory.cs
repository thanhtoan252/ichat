namespace IChat.Infrastructure.Ai.Providers;

using Anthropic;
using Microsoft.Extensions.AI;

// Anthropic's official SDK does not implement IChatClient itself; it offers an AsIChatClient extension
// that takes defaultMaxOutputTokens right here — exactly where Anthropic requires max_tokens to be set.
public sealed class AnthropicChatClientFactory : IChatProviderClientFactory
{
    public ChatProvider Provider => ChatProvider.Anthropic;

    public IChatClient Create(ResolvedChatSettings settings)
    {
        var clientOptions = new Anthropic.Core.ClientOptions
        {
            ApiKey = settings.RequireApiKey()
        };

        return new AnthropicClient(clientOptions).AsIChatClient(settings.Model, settings.MaxOutputTokens);
    }
}
