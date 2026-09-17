namespace IChat.Infrastructure.Ai;

using IChat.Infrastructure.Ai.Providers;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;

/// <summary>
/// THE ENTRY POINT: reads config, decides whether to pass Dimensions down, then delegates to exactly one
/// <see cref="IEmbeddingProviderClientFactory"/>.
/// </summary>
public sealed class EmbeddingGeneratorFactory(
    IOptions<AiOptions> options,
    IEnumerable<IEmbeddingProviderClientFactory> providers) : IEmbeddingGeneratorFactory
{
    private readonly EmbeddingOptions _embedding = options.Value.Embedding;

    private readonly Dictionary<EmbeddingProvider, IEmbeddingProviderClientFactory> _providers =
        providers.ToDictionary(factory => factory.Provider);

    public EmbeddingProviderCapabilities Capabilities => EmbeddingProviderCapabilities.For(_embedding.Provider);

    public IEmbeddingGenerator<string, Embedding<float>> Create()
    {
        // When the provider supports reducing the output dimensionality, ALWAYS pass Dimensions down so the
        // vectors match the vector(N) column the schema fixed.
        var dimensions = Capabilities.SupportsEmbeddingDimensions ? _embedding.Dimensions : (int?)null;

        if (!_providers.TryGetValue(_embedding.Provider, out var factory))
        {
            throw new InvalidOperationException($"Unsupported embedding provider: {_embedding.Provider}.");
        }

        return factory.Create(_embedding, dimensions);
    }
}
