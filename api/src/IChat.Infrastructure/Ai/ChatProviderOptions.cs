namespace IChat.Infrastructure.Ai;

using System.ComponentModel.DataAnnotations;

public sealed class ChatProviderOptions
{
    public ChatProvider Provider { get; set; } = ChatProvider.OpenAI;

    /// <summary>Never has a default value in C# — every vendor's model IDs change constantly.</summary>
    [Required(AllowEmptyStrings = false)]
    public string Model { get; set; } = string.Empty;

    /// <summary>
    /// No default value: the reasoning-family models only accept the vendor's own default temperature and
    /// answer with HTTP 400 for anything else. Leaving it empty means the parameter is not sent at all,
    /// letting the vendor decide.
    /// </summary>
    [Range(0d, 2d)]
    public double? Temperature { get; set; }

    // Anthropic requires MaxOutputTokens, OpenAI treats it as optional.
    // Always set it for every provider so nobody has to remember the difference.
    [Range(1, 200_000)]
    public int MaxOutputTokens { get; set; } = 2048;

    [Range(1, 600)]
    public int TimeoutSeconds { get; set; } = 120;

    /// <summary>Required for AzureOpenAI.</summary>
    public string? Endpoint { get; set; }

    /// <summary>Bound straight through IConfiguration: the Ai__Chat__ApiKey env var, user secrets, or a secret store.</summary>
    public string? ApiKey { get; set; }
}
