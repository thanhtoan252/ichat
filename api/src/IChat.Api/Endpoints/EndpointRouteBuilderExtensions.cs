namespace IChat.Api.Endpoints;

using IChat.Api.Endpoints.Admin.V1;
using IChat.Api.Endpoints.Auth.V1;
using IChat.Api.Endpoints.Conversations.V1;
using IChat.Api.Endpoints.Documents.V1;
using IChat.Api.Endpoints.Health;
using IChat.Api.Endpoints.Search.V1;

/// <summary>
/// The only place the version prefix is applied: adding a V2 means one new group here, and
/// each feature's endpoints stay unaware of the path above them.
/// </summary>
public static class EndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapIChatEndpoints(this IEndpointRouteBuilder app)
    {
        // Health checks sit outside the v1 group and stay anonymous: an orchestrator has no token.
        app.MapHealthEndpoints();

        // Closed by DEFAULT. An endpoint that needs to be open must say so with AllowAnonymous, so
        // adding a route without thinking about authorization yields a 401 rather than a hole.
        var v1 = app.MapGroup("/api/v1").RequireAuthorization();

        v1.MapAuthV1Endpoints();
        v1.MapAdminV1Endpoints();
        v1.MapDocumentV1Endpoints();
        v1.MapSearchV1Endpoints();
        v1.MapConversationV1Endpoints();

        return app;
    }
}
