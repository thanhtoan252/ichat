namespace IChat.Infrastructure.Ai;

using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;

/// <summary>
/// Gọi embedding theo batch. Gọi từng chunk một sẽ chậm và tốn tiền gấp nhiều lần.
/// </summary>
public sealed class BatchingEmbeddingService(
    IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator,
    IEmbeddingGeneratorFactory embeddingGeneratorFactory,
    IOptions<AiOptions> options)
{
    private readonly EmbeddingOptions _embedding = options.Value.Embedding;

    public string QualifiedModel => _embedding.Qualified;

    public int Dimensions => _embedding.Dimensions;

    public async Task<IReadOnlyList<float[]>> EmbedAsync(IReadOnlyList<string> texts, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(texts);

        if (texts.Count == 0)
        {
            return [];
        }

        var generationOptions = BuildGenerationOptions();
        var results = new List<float[]>(texts.Count);

        for (var offset = 0; offset < texts.Count; offset += _embedding.BatchSize)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var batch = texts.Skip(offset).Take(_embedding.BatchSize).ToList();
            var embeddings = await embeddingGenerator.GenerateAsync(batch, generationOptions, cancellationToken);

            foreach (var embedding in embeddings)
            {
                results.Add(embedding.Vector.ToArray());
            }
        }

        if (results.Count != texts.Count)
        {
            throw new InvalidOperationException(
                $"The embedding provider returned {results.Count} vectors for {texts.Count} texts.");
        }

        return results;
    }

    public async Task<float[]> EmbedSingleAsync(string text, CancellationToken cancellationToken)
    {
        var result = await EmbedAsync([text], cancellationToken);

        return result[0];
    }

    private EmbeddingGenerationOptions? BuildGenerationOptions()
    {
        // Chỉ truyền Dimensions khi provider thật sự hỗ trợ giảm chiều đầu ra.
        if (!embeddingGeneratorFactory.Capabilities.SupportsEmbeddingDimensions)
        {
            return null;
        }

        return new EmbeddingGenerationOptions { Dimensions = _embedding.Dimensions };
    }
}
