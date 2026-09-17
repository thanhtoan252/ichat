namespace IChat.Infrastructure.Ai.Providers;

using System.ClientModel;
using OpenAI;

/// <summary>
/// The OpenAIClient construction with an optional endpoint, shared by OpenAI chat, Google chat and both
/// OpenAI-compatible embedding factories — it used to be copied three times.
/// </summary>
public static class OpenAICompatibleClientFactory
{
    // Gemini goes through the OpenAI-compatible endpoint to avoid depending on a beta package;
    // switching to the Vertex SDK would only mean editing the two Google factories.
    public const string GoogleOpenAICompatibleEndpoint = "https://generativelanguage.googleapis.com/v1beta/openai/";

    public static OpenAIClient Create(string apiKey, string? endpoint)
    {
        var clientOptions = new OpenAIClientOptions();

        if (!string.IsNullOrWhiteSpace(endpoint))
        {
            clientOptions.Endpoint = new Uri(endpoint);
        }

        return new OpenAIClient(new ApiKeyCredential(apiKey), clientOptions);
    }
}
