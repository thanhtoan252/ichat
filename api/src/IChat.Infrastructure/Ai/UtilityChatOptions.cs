namespace IChat.Infrastructure.Ai;

using System.ComponentModel.DataAnnotations;

public sealed class UtilityChatOptions
{
    public ChatProvider? Provider { get; set; }

    [Required(AllowEmptyStrings = false)]
    public string Model { get; set; } = string.Empty;

    [Range(1, 200_000)]
    public int MaxOutputTokens { get; set; } = 256;

    [Range(1, 600)]
    public int TimeoutSeconds { get; set; } = 15;

    public string? Endpoint { get; set; }

    public string? ApiKey { get; set; }
}
