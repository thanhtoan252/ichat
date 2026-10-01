namespace IChat.Infrastructure.Ai.Providers;

using Microsoft.Extensions.AI;

/// <summary>
/// One implementation per vendor. Unlike <see cref="IEmbeddingGeneratorFactory"/>, which is THE ENTRY
/// POINT: it reads config, decides whether to send Dimensions, then delegates down to here.
/// </summary>
public interface IEmbeddingProviderClientFactory
{
    EmbeddingProvider Provider { get; }

    IEmbeddingGenerator<string, Embedding<float>> Create(EmbeddingOptions options, int? dimensions);
}
