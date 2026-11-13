namespace IChat.Infrastructure.Ai.Providers;

using System.ClientModel;
using OpenAI;

public static class OpenAIClientFactory
{
    public static OpenAIClient Create(string apiKey)
    {
        return new OpenAIClient(new ApiKeyCredential(apiKey));
    }
}
