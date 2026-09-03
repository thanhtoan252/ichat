namespace IChat.Api.Endpoints.Admin.V1.DTOs;

/// <summary>Không bao giờ chứa API key, kể cả đã mask.</summary>
public sealed class ProviderCatalogResponse
{
    public required ProviderResponse Chat { get; init; }

    public required ProviderResponse UtilityChat { get; init; }

    public required ProviderResponse Embedding { get; init; }

    public required IReadOnlyList<string> AllowedChatModels { get; init; }
}
