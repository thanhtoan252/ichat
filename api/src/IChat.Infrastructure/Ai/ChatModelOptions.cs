namespace IChat.Infrastructure.Ai;

using System.ComponentModel.DataAnnotations;

public sealed class ChatModelOptions
{

    [Required(AllowEmptyStrings = false)]
    public string Model { get; set; } = string.Empty;

    [Range(0d, 2d)]
    public double? Temperature { get; set; }

    [Range(1, 200_000)]
    public int MaxOutputTokens { get; set; } = 2048;

    [Range(1, 600)]
    public int TimeoutSeconds { get; set; } = 120;

    public string? ApiKey { get; set; }
}
