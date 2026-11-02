namespace IChat.Infrastructure.Ai.Providers;

using Microsoft.Extensions.AI;

public sealed class OpenAIChatClientFactory
{
    public IChatClient Create(ResolvedChatSettings settings)
    {
        return OpenAIClientFactory
            .Create(settings.RequireApiKey())
            .GetChatClient(settings.Model)
            .AsIChatClient();
    }
}
