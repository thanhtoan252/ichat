namespace IChat.Infrastructure.Ai;

using IChat.Infrastructure.Ai.Providers;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;

public sealed class EmbeddingGeneratorFactory(
    IOptions<AiOptions> options,
    OpenAIEmbeddingClientFactory openAi) : IEmbeddingGeneratorFactory
{
    private readonly EmbeddingOptions _embedding = options.Value.Embedding;

public IEmbeddingGenerator<string, Embedding<float>> Create()
    {
        return openAi.Create(_embedding, _embedding.Dimensions);
    }
}
