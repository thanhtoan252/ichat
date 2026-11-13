namespace IChat.Infrastructure.Ai;

using IChat.Core.Abstractions;
using Microsoft.Extensions.Options;

public sealed class ModelCatalog : IModelCatalog
{
    private const string ChatKind = "chat";
    private const string UtilityKind = "utility";
    private const string EmbeddingKind = "embedding";

    private readonly AiOptions _options;

    private readonly IReadOnlyList<string> _allowedChatModels;
    private readonly HashSet<string> _allowedChatModelLookup;

    public ModelCatalog(IOptions<AiOptions> options)
    {
        _options = options.Value;
        _allowedChatModels = ResolveAllowedChatModels(_options);
        _allowedChatModelLookup = new HashSet<string>(_allowedChatModels, StringComparer.OrdinalIgnoreCase);
    }

    public bool SupportsMultipleSystemMessages => true;

    public int ChatTimeoutSeconds => _options.Chat.TimeoutSeconds;

    public int ChatMaxOutputTokens => _options.Chat.MaxOutputTokens;

    public double? ChatTemperature => _options.Chat.Temperature;

    public ModelCatalogSnapshot GetSnapshot()
    {
        return new ModelCatalogSnapshot
        {
            Chat = Describe(ChatEntry()),
            UtilityChat = Describe(UtilityChatEntry()),
            Embedding = Describe(EmbeddingEntry()),
            AllowedChatModels = _allowedChatModels
        };
    }

    public bool IsChatModelAllowed(string model)
    {
        return !string.IsNullOrWhiteSpace(model) && _allowedChatModelLookup.Contains(model);
    }

    private ModelEntry ChatEntry()
    {
        return ToEntry(ChatKind, _options.ResolveChat());
    }

    private ModelEntry UtilityChatEntry()
    {
        return ToEntry(UtilityKind, _options.ResolveUtilityChat());
    }

    private static ModelEntry ToEntry(string kind, ResolvedChatSettings settings)
    {
        return new ModelEntry
        {
            Kind = kind,
            Provider = "OpenAI",
            Model = settings.Model,
            ApiKey = settings.ApiKey
        };
    }

    private ModelEntry EmbeddingEntry()
    {
        var embedding = _options.Embedding;
        return new ModelEntry
        {
            Kind = EmbeddingKind,
            Provider = "OpenAI",
            Model = embedding.Model,
            ApiKey = embedding.ApiKey
        };
    }

    private static ProviderDescriptor Describe(ModelEntry entry)
    {
        var reason = FindUnavailableReason(entry);

        return new ProviderDescriptor
        {
            Kind = entry.Kind,
            Provider = entry.Provider,
            Model = entry.Model,
            Available = reason is null,
            Reason = reason
        };
    }

    private static string? FindUnavailableReason(ModelEntry entry)
    {
        if (string.IsNullOrWhiteSpace(entry.ApiKey))
        {
            return "No API key is configured.";
        }

        return null;
    }

    private static IReadOnlyList<string> ResolveAllowedChatModels(AiOptions options)
    {
        return new[] { options.Chat.Model }
            .Concat(options.AllowedChatModels)
            .Where(model => !string.IsNullOrWhiteSpace(model))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private sealed record ModelEntry
    {
        public required string Kind { get; init; }

        public required string Provider { get; init; }

        public required string Model { get; init; }

        public string? ApiKey { get; init; }
    }
}
