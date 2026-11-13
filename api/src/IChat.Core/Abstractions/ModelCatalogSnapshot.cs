namespace IChat.Core.Abstractions;

public sealed class ModelCatalogSnapshot
{
    public required ProviderDescriptor Chat { get; init; }

    public required ProviderDescriptor UtilityChat { get; init; }

    public required ProviderDescriptor Embedding { get; init; }

    public required IReadOnlyList<string> AllowedChatModels { get; init; }
}
