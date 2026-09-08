namespace IChat.Infrastructure.Ai.Providers;

using Microsoft.Extensions.AI;

public sealed class OpenAIChatClientFactory : IChatProviderClientFactory
{
    public ChatProvider Provider => ChatProvider.OpenAI;

    public IChatClient Create(ResolvedChatSettings settings)
    {
        return OpenAICompatibleClientFactory
            .Create(settings.RequireApiKey(), settings.Endpoint)
            .GetChatClient(settings.Model)
            .AsIChatClient();
    }
}
