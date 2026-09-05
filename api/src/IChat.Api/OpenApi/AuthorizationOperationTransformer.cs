namespace IChat.Api.OpenApi;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

/// <summary>
/// Đọc metadata thật của endpoint thay vì đoán theo đường dẫn: endpoint nào có
/// <see cref="IAuthorizeData"/> mà không bị <see cref="IAllowAnonymousMetadata"/> ghi đè thì
/// được gắn security requirement + 401/403. Nhờ vậy AllowAnonymous trên /auth/login tự động
/// phản ánh vào document, không phải bảo trì hai nơi.
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
        AddResponseIfMissing(operation.Responses, StatusCodes.Status401Unauthorized, "Thiếu access token hoặc token không hợp lệ.");

        var requiresPolicy = metadata.OfType<IAuthorizeData>().Any(data => !string.IsNullOrEmpty(data.Policy) || !string.IsNullOrEmpty(data.Roles));

        if (requiresPolicy)
        {
            AddResponseIfMissing(operation.Responses, StatusCodes.Status403Forbidden, "Token hợp lệ nhưng không đủ quyền.");
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
