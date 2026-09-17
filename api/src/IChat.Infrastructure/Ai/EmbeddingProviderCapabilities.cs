namespace IChat.Infrastructure.Ai;

/// <summary>
/// The differences between vendors on the embedding side. Kept apart from chat because the two share
/// almost no questions: chat cares about system blocks, embedding cares about output dimensionality.
/// </summary>
public sealed record EmbeddingProviderCapabilities
{
    /// <summary>The vendor allows forcing the output dimensionality to match the schema's vector(N) column.</summary>
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
