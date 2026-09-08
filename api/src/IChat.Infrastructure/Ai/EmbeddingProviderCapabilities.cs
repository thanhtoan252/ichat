namespace IChat.Infrastructure.Ai;

/// <summary>
/// Khác biệt giữa các hãng ở phía embedding. Tách khỏi chat vì hai bên gần như không
/// chung câu hỏi nào: chat quan tâm khối system, embedding quan tâm số chiều đầu ra.
/// </summary>
public sealed record EmbeddingProviderCapabilities
{
    /// <summary>Hãng cho phép ép số chiều đầu ra để khớp cột vector(N) của schema.</summary>
    public required bool SupportsEmbeddingDimensions { get; init; }

    public required bool RequiresEndpoint { get; init; }

    public required bool RequiresApiKey { get; init; }

    public static EmbeddingProviderCapabilities For(EmbeddingProvider provider)
    {
        return provider switch
        {
            EmbeddingProvider.OpenAI => new EmbeddingProviderCapabilities
            {
                SupportsEmbeddingDimensions = true,
                RequiresEndpoint = false,
                RequiresApiKey = true
            },

            EmbeddingProvider.AzureOpenAI => new EmbeddingProviderCapabilities
            {
                SupportsEmbeddingDimensions = true,
                RequiresEndpoint = true,
                RequiresApiKey = true
            },

            EmbeddingProvider.Google => new EmbeddingProviderCapabilities
            {
                SupportsEmbeddingDimensions = false,
                RequiresEndpoint = false,
                RequiresApiKey = true
            },

            _ => new EmbeddingProviderCapabilities
            {
                SupportsEmbeddingDimensions = false,
                RequiresEndpoint = false,
                RequiresApiKey = true
            }
        };
    }
}
