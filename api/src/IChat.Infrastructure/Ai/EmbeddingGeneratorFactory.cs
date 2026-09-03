namespace IChat.Infrastructure.Ai;

using IChat.Infrastructure.Ai.Providers;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;

/// <summary>
/// CỬA VÀO: đọc config, quyết định có gửi Dimensions xuống hay không, rồi uỷ quyền
/// xuống đúng một <see cref="IEmbeddingProviderClientFactory"/>.
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
        // Khi provider hỗ trợ giảm chiều đầu ra, LUÔN truyền Dimensions xuống để
        // vector khớp với cột vector(N) đã cố định ở schema.
        var dimensions = Capabilities.SupportsEmbeddingDimensions ? _embedding.Dimensions : (int?)null;

        if (!_providers.TryGetValue(_embedding.Provider, out var factory))
        {
            throw new InvalidOperationException($"Unsupported embedding provider: {_embedding.Provider}.");
        }

        return factory.Create(_embedding, dimensions);
    }
}
