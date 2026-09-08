namespace IChat.Infrastructure.Ai;

using Microsoft.Extensions.AI;

public interface IEmbeddingGeneratorFactory
{
    IEmbeddingGenerator<string, Embedding<float>> Create();

    EmbeddingProviderCapabilities Capabilities { get; }
}
