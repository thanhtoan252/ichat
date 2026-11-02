namespace IChat.Infrastructure.Ai.Providers;

using Microsoft.Extensions.AI;

public sealed class OpenAIEmbeddingClientFactory
{
    public IEmbeddingGenerator<string, Embedding<float>> Create(EmbeddingOptions options, int dimensions)
    {
        return OpenAIClientFactory
            .Create(options.RequireApiKey())
            .GetEmbeddingClient(options.Model)
            .AsIEmbeddingGenerator(dimensions);
    }
}
