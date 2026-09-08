namespace IChat.Infrastructure.Ai;

using System.ComponentModel.DataAnnotations;

public sealed class EmbeddingOptions
{
    public EmbeddingProvider Provider { get; set; } = EmbeddingProvider.OpenAI;

    [Required(AllowEmptyStrings = false)]
    public string Model { get; set; } = string.Empty;

    [Range(1, 8192)]
    public int Dimensions { get; set; } = 1536;

    [Range(1, 2048)]
    public int BatchSize { get; set; } = 64;

    [Range(1, 600)]
    public int TimeoutSeconds { get; set; } = 60;

    public string? Endpoint { get; set; }

    public string? ApiKey { get; set; }

    public string Qualified => $"{Provider.ToString().ToLowerInvariant()}:{Model}";

    /// <summary>Lưới an toàn giống ResolvedChatSettings.RequireApiKey.</summary>
    public string RequireApiKey()
    {
        if (string.IsNullOrWhiteSpace(ApiKey))
        {
            throw new InvalidOperationException(
                $"Missing API key for embedding provider {Provider}. Set Ai__Embedding__ApiKey " +
                "as an environment variable, in user secrets, or in a secret store.");
        }

        return ApiKey;
    }
}
