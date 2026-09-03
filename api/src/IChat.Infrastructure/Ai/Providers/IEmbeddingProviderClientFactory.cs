namespace IChat.Infrastructure.Ai.Providers;

using Microsoft.Extensions.AI;

/// <summary>
/// Một hãng một implementation. Khác với <see cref="IEmbeddingGeneratorFactory"/> — cái đó là
/// CỬA VÀO: đọc config, quyết định có gửi Dimensions hay không, rồi uỷ quyền xuống đây.
/// </summary>
public interface IEmbeddingProviderClientFactory
{
    EmbeddingProvider Provider { get; }

    IEmbeddingGenerator<string, Embedding<float>> Create(EmbeddingOptions options, int? dimensions);
}
