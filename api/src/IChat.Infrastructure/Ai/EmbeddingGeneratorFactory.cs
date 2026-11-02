namespace IChat.Infrastructure.Ai;

using IChat.Infrastructure.Ai.Providers;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;

/// <summary>
/// Builds the OpenAI embedding generator with the schema's configured vector dimensions.
/// </summary>
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
