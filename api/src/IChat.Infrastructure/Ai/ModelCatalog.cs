namespace IChat.Infrastructure.Ai;

using IChat.Core.Abstractions;
using Microsoft.Extensions.Options;

/// <summary>
/// Nguồn sự thật cho Core và endpoint admin về "đang chạy model nào, đã đủ credential chưa".
/// Không bao giờ trả API key, kể cả đã mask — chỉ trả trạng thái có/không.
/// </summary>
public sealed class ModelCatalog : IModelCatalog
{
    private const string ChatKind = "chat";
    private const string UtilityKind = "utility";
    private const string EmbeddingKind = "embedding";

    private readonly AiOptions _options;
    private readonly ChatProviderCapabilities _chatCapabilities;

    // AiOptions là singleton và đã validate lúc startup, nên allowlist tính một lần;
    // IsChatModelAllowed chạy trên mỗi request nên không dựng lại danh sách mỗi lần.
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

    /// <summary>Trả về null nghĩa là provider dùng được; ngược lại là lý do hiển thị cho admin.</summary>
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

    // Model đang cấu hình luôn nằm trong allowlist, kể cả khi AllowedChatModels để trống.
    private static IReadOnlyList<string> ResolveAllowedChatModels(AiOptions options)
    {
        return new[] { options.Chat.Model }
            .Concat(options.AllowedChatModels)
            .Where(model => !string.IsNullOrWhiteSpace(model))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    /// <summary>Cấu hình của một provider đã giải quyết xong fallback — đủ để mô tả mà không đọc lại AiOptions.</summary>
    private sealed record ProviderEntry
    {
        public required string Kind { get; init; }

        public required string Provider { get; init; }

        public required string Model { get; init; }

        public required bool RequiresApiKey { get; init; }

        public required bool RequiresEndpoint { get; init; }

        /// <summary>Chỉ dùng để trả lời "có key hay chưa". TUYỆT ĐỐI không map ra ProviderDescriptor
        /// hay bất kỳ DTO nào — endpoint /api/v1/providers không bao giờ trả key, kể cả đã mask.</summary>
        public string? ApiKey { get; init; }

        public string? Endpoint { get; init; }
    }
}
