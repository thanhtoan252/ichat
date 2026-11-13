namespace IChat.Api.OpenApi;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

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
