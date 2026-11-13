namespace IChat.Infrastructure.Ai;

using System.ComponentModel.DataAnnotations;

public sealed class EmbeddingOptions
{
    [Required(AllowEmptyStrings = false)]
    public string Model { get; set; } = string.Empty;

[Range(1, 8192)]
    public int Dimensions { get; set; } = 1536;

[Range(1, 2048)]
    public int BatchSize { get; set; } = 64;

[Range(1, 600)]
    public int TimeoutSeconds { get; set; } = 60;

public string? ApiKey { get; set; }

public string Qualified => $"openai:{Model}";

public string RequireApiKey()
    {
        if (string.IsNullOrWhiteSpace(ApiKey))
        {
            throw new InvalidOperationException(
                "Missing OpenAI API key for embeddings. Set Ai__Embedding__ApiKey " +
                "as an environment variable, in user secrets, or in a secret store.");
        }

return ApiKey;
    }
}
