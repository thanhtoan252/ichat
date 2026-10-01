namespace IChat.Api.OpenApi;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

/// <summary>
/// Declares the "Bearer" scheme at the document level. It deliberately sets no global security
/// requirement: attaching one per operation is <see cref="AuthorizationOperationTransformer"/>'s job,
/// based on the endpoint's real metadata, so /auth/login still shows up as anonymous.
/// </summary>
public sealed class JwtBearerSecuritySchemeTransformer(IAuthenticationSchemeProvider schemeProvider)
    : IOpenApiDocumentTransformer
{
    public const string SchemeName = JwtBearerDefaults.AuthenticationScheme;

    public async Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        var schemes = await schemeProvider.GetAllSchemesAsync();

        if (!schemes.Any(scheme => scheme.Name == SchemeName))
        {
            return;
        }

        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
        document.Components.SecuritySchemes[SchemeName] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "The access token from POST /api/v1/auth/login. Paste the token itself, without the 'Bearer' prefix."
        };
    }
}
