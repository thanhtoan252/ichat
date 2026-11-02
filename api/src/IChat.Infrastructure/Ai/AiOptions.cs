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

    /// <summary>Allowlist for the optional `model` field in a request body.</summary>
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

    /// <summary>
    /// Every field UtilityChat leaves empty is inherited from Chat, so the utility configuration only has
    /// to declare what differs (usually a cheaper model). This is the ONLY place that rule is defined:
    /// the client factory, the admin catalog and the startup validator all go through here, otherwise three
    /// places would describe the same system in three different ways — and the admin endpoint would report
    /// "missing key" despite a key being configured for chat.
    /// Model and MaxOutputTokens are NOT inherited: utility must always declare its own model.
    /// </summary>
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
