namespace IChat.Infrastructure.Ai.Providers;

using System.ClientModel;
using OpenAI;

/// <summary>
/// Shared construction for OpenAI chat and embedding clients.
/// </summary>
public static class OpenAIClientFactory
{
    public static OpenAIClient Create(string apiKey)
    {
        return new OpenAIClient(new ApiKeyCredential(apiKey));
    }
}
