namespace IChat.Infrastructure.Ai;

using System.ComponentModel.DataAnnotations;

public sealed class AiOptions
{
    public const string SectionName = "Ai";

    [Required]
    public ChatProviderOptions Chat { get; set; } = new();

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
            Provider = Chat.Provider,
            Model = Chat.Model,
            MaxOutputTokens = Chat.MaxOutputTokens,
            TimeoutSeconds = Chat.TimeoutSeconds,
            Endpoint = Chat.Endpoint,
            ApiKey = Chat.ApiKey
        };
    }

    /// <summary>
    /// Every field UtilityChat leaves empty is inherited from Chat, so the utility configuration only has
    /// to declare what differs (usually a cheaper model). This is the ONLY place that rule is defined:
    /// the client factory, the admin catalog and the startup validator all go through here, otherwise three
    /// places would describe the same system in three different ways — and the admin endpoint would report
    /// "missing key" for a provider that works perfectly.
    /// Model and MaxOutputTokens are NOT inherited: utility must always declare its own model.
    /// </summary>
    public ResolvedChatSettings ResolveUtilityChat()
    {
        return new ResolvedChatSettings
        {
            Provider = UtilityChat.Provider ?? Chat.Provider,
            Model = UtilityChat.Model,
            MaxOutputTokens = UtilityChat.MaxOutputTokens,
            TimeoutSeconds = UtilityChat.TimeoutSeconds,
            Endpoint = UtilityChat.Endpoint ?? Chat.Endpoint,
            ApiKey = UtilityChat.ApiKey ?? Chat.ApiKey
        };
    }
}
