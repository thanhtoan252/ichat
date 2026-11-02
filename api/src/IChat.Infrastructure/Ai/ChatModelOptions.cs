namespace IChat.Infrastructure.Ai;

using System.ComponentModel.DataAnnotations;

public sealed class ChatModelOptions
{
    /// <summary>Never has a default value in C# — OpenAI model IDs change over time.</summary>
    [Required(AllowEmptyStrings = false)]
    public string Model { get; set; } = string.Empty;

    /// <summary>
    /// No default value: OpenAI reasoning models only accept their default temperature. Leaving it empty
    /// means the parameter is not sent.
    /// </summary>
    [Range(0d, 2d)]
    public double? Temperature { get; set; }

    [Range(1, 200_000)]
    public int MaxOutputTokens { get; set; } = 2048;

    [Range(1, 600)]
    public int TimeoutSeconds { get; set; } = 120;

    /// <summary>Bound straight through IConfiguration: the Ai__Chat__ApiKey env var, user secrets, or a secret store.</summary>
    public string? ApiKey { get; set; }
}
