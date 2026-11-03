namespace IChat.Api.Endpoints;

using IChat.Api.Endpoints.Admin.V1;
using IChat.Api.Endpoints.Auth.V1;
using IChat.Api.Endpoints.Conversations.V1;
using IChat.Api.Endpoints.Documents.V1;
using IChat.Api.Endpoints.Health;
using IChat.Api.Endpoints.Search.V1;

public static class EndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapIChatEndpoints(this IEndpointRouteBuilder app)
    {

app.MapHealthEndpoints();

var v1 = app.MapGroup("/api/v1").RequireAuthorization();

v1.MapAuthV1Endpoints();
        v1.MapAdminV1Endpoints();
        v1.MapDocumentV1Endpoints();
        v1.MapSearchV1Endpoints();
        v1.MapConversationV1Endpoints();

return app;
    }
}
