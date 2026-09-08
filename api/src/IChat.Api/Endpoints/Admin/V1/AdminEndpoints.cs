namespace IChat.Api.Endpoints.Admin.V1;

using IChat.Api.Authorization;
using IChat.Api.Endpoints.Admin.V1.DTOs;
using IChat.Api.Endpoints.Admin.V1.Mappings;
using IChat.Api.Extensions;
using IChat.Core.Abstractions;

public static class AdminEndpoints
{
    public static IEndpointRouteBuilder MapAdminV1Endpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(string.Empty).WithTags("Admin").RequireAuthorization(AuthPolicies.Admin);

        group.MapGet("/providers", GetProviders)
            .WithName("GetProviders")
            .WithSummary("Active providers.")
            .WithDescription("Returns the active provider/model for chat, utility chat and embedding. Never returns an API key, not even masked.")
            .Produces<ProviderCatalogResponse>();

        group.MapPost("/admin/reindex", ReindexAsync)
            .WithName("Reindex")
            .WithSummary("Reindex every document.")
            .WithDescription("Resets every document to Pending and pushes it back onto the ingestion queue. Returns 202 because the work runs in the background.")
            .Produces<ReindexResponse>(StatusCodes.Status202Accepted);

        return app;
    }

    private static IResult GetProviders(IModelCatalog modelCatalog)
    {
        return Results.Ok(modelCatalog.GetSnapshot().ToResponse());
    }

    private static async Task<IResult> ReindexAsync(IAdminService admin, CancellationToken cancellationToken)
    {
        var result = await admin.ReindexAsync(cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.ToProblemResult();
        }

        return Results.Accepted("/api/v1/documents", result.Value.ToResponse());
    }
}
