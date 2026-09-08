namespace IChat.Infrastructure.Ai.Providers;

using Microsoft.Extensions.AI;

public sealed class OpenAIEmbeddingClientFactory : IEmbeddingProviderClientFactory
{
    public EmbeddingProvider Provider => EmbeddingProvider.OpenAI;

    public IEmbeddingGenerator<string, Embedding<float>> Create(EmbeddingOptions options, int? dimensions)
    {
        return OpenAICompatibleClientFactory
            .Create(options.RequireApiKey(), options.Endpoint)
            .GetEmbeddingClient(options.Model)
            .AsIEmbeddingGenerator(dimensions);
    }
}
