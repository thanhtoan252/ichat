namespace IChat.Infrastructure.Ai.Providers;

using Anthropic;
using Microsoft.Extensions.AI;

// SDK chính thức của Anthropic không tự implement IChatClient; nó cung cấp extension
// AsIChatClient và nhận defaultMaxOutputTokens ngay tại đây, đúng chỗ mà Anthropic
// bắt buộc phải có max_tokens.
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
