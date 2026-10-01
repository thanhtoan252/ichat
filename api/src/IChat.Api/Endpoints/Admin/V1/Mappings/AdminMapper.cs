namespace IChat.Api.Endpoints.Admin.V1.Mappings;

using IChat.Api.Endpoints.Admin.V1.DTOs;
using IChat.Core.Abstractions;
using IChat.Core.Contracts.Admin;

public static class AdminMapper
{
    public static ProviderCatalogResponse ToResponse(this ModelCatalogSnapshot snapshot) =>
        new()
        {
            Chat = snapshot.Chat.ToResponse(),
            UtilityChat = snapshot.UtilityChat.ToResponse(),
            Embedding = snapshot.Embedding.ToResponse(),
            AllowedChatModels = snapshot.AllowedChatModels
        };

    public static ReindexResponse ToResponse(this ReindexResult result) =>
        new()
        {
            DocumentCount = result.DocumentCount
        };

    private static ProviderResponse ToResponse(this ProviderDescriptor descriptor) =>
        new()
        {
            Kind = descriptor.Kind,
            Provider = descriptor.Provider,
            Model = descriptor.Model,
            Available = descriptor.Available,
            Reason = descriptor.Reason
        };
}
