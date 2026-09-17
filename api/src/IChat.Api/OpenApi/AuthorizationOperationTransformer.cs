namespace IChat.Api.OpenApi;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

/// <summary>
/// Reads the endpoint's real metadata instead of guessing from the path: an endpoint that carries
/// <see cref="IAuthorizeData"/> without being overridden by <see cref="IAllowAnonymousMetadata"/> gets
/// a security requirement plus 401/403. That way AllowAnonymous on /auth/login is reflected in the
/// document automatically, with nothing to maintain in two places.
/// </summary>
public sealed class AuthorizationOperationTransformer : IOpenApiOperationTransformer
{
    public Task TransformAsync(OpenApiOperation operation, OpenApiOperationTransformerContext context, CancellationToken cancellationToken)
    {
        var metadata = context.Description.ActionDescriptor.EndpointMetadata;

        if (metadata.OfType<IAllowAnonymous>().Any() || !metadata.OfType<IAuthorizeData>().Any())
        {
            return Task.CompletedTask;
        }

        operation.Security ??= [];
        operation.Security.Add(new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference(JwtBearerSecuritySchemeTransformer.SchemeName, context.Document)] = []
        });

        operation.Responses ??= new OpenApiResponses();
        AddResponseIfMissing(operation.Responses, StatusCodes.Status401Unauthorized, "The access token is missing or invalid.");

        var requiresPolicy = metadata.OfType<IAuthorizeData>().Any(data => !string.IsNullOrEmpty(data.Policy) || !string.IsNullOrEmpty(data.Roles));

        if (requiresPolicy)
        {
            AddResponseIfMissing(operation.Responses, StatusCodes.Status403Forbidden, "The token is valid but lacks the required permission.");
        }

        return Task.CompletedTask;
    }

    private static void AddResponseIfMissing(OpenApiResponses responses, int statusCode, string description)
    {
        var key = statusCode.ToString(System.Globalization.CultureInfo.InvariantCulture);

        if (responses.ContainsKey(key))
        {
            return;
        }

        responses[key] = new OpenApiResponse { Description = description };
    }
}
