namespace IChat.Infrastructure.Ai;

using IChat.Core.Abstractions;
using Microsoft.Extensions.Options;

/// <summary>
/// The source of truth for Core and the admin endpoint on "which model is running, are the credentials there".
/// It never returns an API key, not even masked — only a yes/no status.
/// </summary>
public sealed class ModelCatalog : IModelCatalog
{
    private const string ChatKind = "chat";
    private const string UtilityKind = "utility";
    private const string EmbeddingKind = "embedding";

    private readonly AiOptions _options;
    private readonly ChatProviderCapabilities _chatCapabilities;

    // AiOptions is a singleton and was validated at startup, so the allowlist is computed once;
    // IsChatModelAllowed runs on every request and must not rebuild the list each time.
    private readonly IReadOnlyList<string> _allowedChatModels;
    private readonly HashSet<string> _allowedChatModelLookup;

    public ModelCatalog(IOptions<AiOptions> options)
    {
        _options = options.Value;
        _chatCapabilities = _options.ResolveChat().Capabilities;
        _allowedChatModels = ResolveAllowedChatModels(_options);
        _allowedChatModelLookup = new HashSet<string>(_allowedChatModels, StringComparer.OrdinalIgnoreCase);
    }

    public bool SupportsMultipleSystemMessages => _chatCapabilities.SupportsMultipleSystemMessages;

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

    private ProviderEntry ChatEntry()
    {
        return ToEntry(ChatKind, _options.ResolveChat());
    }

    private ProviderEntry UtilityChatEntry()
    {
        return ToEntry(UtilityKind, _options.ResolveUtilityChat());
    }

    private static ProviderEntry ToEntry(string kind, ResolvedChatSettings settings)
    {
        return new ProviderEntry
        {
            Kind = kind,
            Provider = settings.Provider.ToString(),
            Model = settings.Model,
            RequiresApiKey = settings.Capabilities.RequiresApiKey,
            RequiresEndpoint = settings.Capabilities.RequiresEndpoint,
            ApiKey = settings.ApiKey,
            Endpoint = settings.Endpoint
        };
    }

    private ProviderEntry EmbeddingEntry()
    {
        var embedding = _options.Embedding;
        var capabilities = EmbeddingProviderCapabilities.For(embedding.Provider);

        return new ProviderEntry
        {
            Kind = EmbeddingKind,
            Provider = embedding.Provider.ToString(),
            Model = embedding.Model,
            RequiresApiKey = capabilities.RequiresApiKey,
            RequiresEndpoint = capabilities.RequiresEndpoint,
            ApiKey = embedding.ApiKey,
            Endpoint = embedding.Endpoint
        };
    }

    private static ProviderDescriptor Describe(ProviderEntry entry)
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

    /// <summary>Returning null means the provider is usable; anything else is the reason shown to an admin.</summary>
    private static string? FindUnavailableReason(ProviderEntry entry)
    {
        if (entry.RequiresApiKey && string.IsNullOrWhiteSpace(entry.ApiKey))
        {
            return "No API key is configured.";
        }

        if (entry.RequiresEndpoint && string.IsNullOrWhiteSpace(entry.Endpoint))
        {
            return "Endpoint is missing.";
        }

        return null;
    }

    // The configured model is always part of the allowlist, even when AllowedChatModels is empty.
    private static IReadOnlyList<string> ResolveAllowedChatModels(AiOptions options)
    {
        return new[] { options.Chat.Model }
            .Concat(options.AllowedChatModels)
            .Where(model => !string.IsNullOrWhiteSpace(model))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    /// <summary>One provider's configuration with the fallback already resolved — enough to describe it without rereading AiOptions.</summary>
    private sealed record ProviderEntry
    {
        public required string Kind { get; init; }

        public required string Provider { get; init; }

        public required string Model { get; init; }

        public required bool RequiresApiKey { get; init; }

        public required bool RequiresEndpoint { get; init; }

        /// <summary>Used only to answer "is there a key or not". NEVER map it into a ProviderDescriptor
        /// or any other DTO — /api/v1/providers never returns a key, not even a masked one.</summary>
        public string? ApiKey { get; init; }

        public string? Endpoint { get; init; }
    }
}
