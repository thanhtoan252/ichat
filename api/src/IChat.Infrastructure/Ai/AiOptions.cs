namespace IChat.Infrastructure.Ai;

using System.ComponentModel.DataAnnotations;

public sealed class AiOptions
{
    public const string SectionName = "Ai";

    [Required]
    public ChatModelOptions Chat { get; set; } = new();

    [Required]
    public UtilityChatOptions UtilityChat { get; set; } = new();

    [Required]
    public EmbeddingOptions Embedding { get; set; } = new();

    public PipelineOptions Pipeline { get; set; } = new();

    public IList<string> AllowedChatModels { get; set; } = [];

    public ResolvedChatSettings ResolveChat()
    {
        return new ResolvedChatSettings
        {
            Model = Chat.Model,
            MaxOutputTokens = Chat.MaxOutputTokens,
            TimeoutSeconds = Chat.TimeoutSeconds,
            ApiKey = Chat.ApiKey
        };
    }

    public ResolvedChatSettings ResolveUtilityChat()
    {
        return new ResolvedChatSettings
        {
            Model = UtilityChat.Model,
            MaxOutputTokens = UtilityChat.MaxOutputTokens,
            TimeoutSeconds = UtilityChat.TimeoutSeconds,
            ApiKey = UtilityChat.ApiKey ?? Chat.ApiKey
        };
    }
}
