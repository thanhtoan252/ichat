namespace IChat.Api.OpenApi;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

/// <summary>
/// Khai báo scheme "Bearer" ở cấp document. Không đặt security requirement toàn cục:
/// việc gắn requirement cho từng operation do <see cref="AuthorizationOperationTransformer"/>
/// làm dựa trên metadata thật của endpoint, nên /auth/login vẫn hiện là ẩn danh.
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
            Description = "Access token lấy từ POST /api/v1/auth/login. Dán nguyên token, không kèm tiền tố 'Bearer'."
        };
    }
}
