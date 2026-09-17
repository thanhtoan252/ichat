namespace IChat.Infrastructure.Ai.Providers;

using Microsoft.Extensions.AI;

public sealed class GoogleEmbeddingClientFactory : IEmbeddingProviderClientFactory
{
    public EmbeddingProvider Provider => EmbeddingProvider.Google;

    public IEmbeddingGenerator<string, Embedding<float>> Create(EmbeddingOptions options, int? dimensions)
    {
        // Same warning as in GoogleChatClientFactory: docker-compose passes the endpoint as an EMPTY string
        // rather than as null, so the `??` branch does not run under compose.
        var endpoint = options.Endpoint ?? OpenAICompatibleClientFactory.GoogleOpenAICompatibleEndpoint;

        return OpenAICompatibleClientFactory
            .Create(options.RequireApiKey(), endpoint)
            .GetEmbeddingClient(options.Model)
            .AsIEmbeddingGenerator(dimensions);
    }
}
