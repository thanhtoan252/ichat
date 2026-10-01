namespace IChat.Infrastructure.Ai.Providers;

using Microsoft.Extensions.AI;

public sealed class GoogleChatClientFactory : IChatProviderClientFactory
{
    public ChatProvider Provider => ChatProvider.Google;

    public IChatClient Create(ResolvedChatSettings settings)
    {
        // WARNING: docker-compose always passes Ai__*__Endpoint into the container as an EMPTY string rather
        // than as null, so the `?? default` branch below does NOT run under docker-compose — the env file has
        // to set Endpoint explicitly (.env.gemini.example).
        var endpoint = settings.Endpoint ?? OpenAICompatibleClientFactory.GoogleOpenAICompatibleEndpoint;

        return OpenAICompatibleClientFactory
            .Create(settings.RequireApiKey(), endpoint)
            .GetChatClient(settings.Model)
            .AsIChatClient();
    }
}
