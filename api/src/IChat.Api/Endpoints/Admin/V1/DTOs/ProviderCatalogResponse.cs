namespace IChat.Api.Endpoints.Admin.V1.DTOs;

/// <summary>Never carries an API key, not even a masked one.</summary>
public sealed class ProviderCatalogResponse
{
    public required ProviderResponse Chat { get; init; }

    public required ProviderResponse UtilityChat { get; init; }

    public required ProviderResponse Embedding { get; init; }

    public required IReadOnlyList<string> AllowedChatModels { get; init; }
}
