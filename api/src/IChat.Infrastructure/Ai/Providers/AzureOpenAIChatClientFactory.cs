namespace IChat.Infrastructure.Ai.Providers;

using System.ClientModel;
using Azure.AI.OpenAI;
using Microsoft.Extensions.AI;

public sealed class AzureOpenAIChatClientFactory : IChatProviderClientFactory
{
    public ChatProvider Provider => ChatProvider.AzureOpenAI;

    public IChatClient Create(ResolvedChatSettings settings)
    {
        var endpoint = settings.Endpoint;

        if (string.IsNullOrWhiteSpace(endpoint))
        {
            throw new InvalidOperationException($"Provider {settings.Label} requires Ai:*:Endpoint.");
        }

        var credential = new ApiKeyCredential(settings.RequireApiKey());

        return new AzureOpenAIClient(new Uri(endpoint), credential)
            .GetChatClient(settings.Model)
            .AsIChatClient();
    }
}
