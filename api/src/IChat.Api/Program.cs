using IChat.Api.Authorization;
using IChat.Api.Endpoints;
using IChat.Api.HealthChecks;
using IChat.Api.Middleware;
using IChat.Api.Security;
using System.Security.Claims;
using System.Text;
using System.Text.Json.Serialization;
using FluentValidation;
using IChat.Core;
using IChat.Core.Abstractions;
using IChat.Core.Domain.Identity;
using IChat.Infrastructure;
using IChat.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Scalar.AspNetCore;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Compact;

// Bootstrap logger: mọi thứ log ra trước khi host build xong (config sai, DI hỏng) vẫn
// ra JSON thay vì biến mất. Sẽ bị thay bằng logger đọc từ appsettings sau khi Build().
Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .WriteTo.Console(new CompactJsonFormatter())
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // Sink/enricher/định dạng đều nằm trong section "Serilog" của appsettings: đổi giữa
    // JSON và text chỉ là sửa config, không phải sửa code rồi build lại.
    builder.Services.AddSerilog((services, configuration) => configuration
        .ReadFrom.Configuration(builder.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext());

    builder.Services.AddSingleton(TimeProvider.System);
    builder.Services.AddIChatCore();
    // Validator của DTO tầng API (Endpoints/<Feature>/V1/Validators); validator của
    // service model nằm trong Core và được AddIChatCore đăng ký riêng.
    builder.Services.AddValidatorsFromAssemblyContaining<Program>(ServiceLifetime.Scoped);
    builder.Services.AddIChatInfrastructure(builder.Configuration);

    builder.Services.AddOptions<JwtOptions>()
        .BindConfiguration(JwtOptions.SectionName)
        .ValidateDataAnnotations()
        .ValidateOnStart();

    builder.Services.AddHttpContextAccessor();
    builder.Services.AddScoped<ICurrentUser, HttpContextCurrentUser>();
    builder.Services.AddSingleton<IAccessTokenService, JwtAccessTokenService>();

    // Đọc sớm để dựng TokenValidationParameters; ValidateOnStart ở trên chỉ chạy sau
    // khi host build xong nên khoá rỗng phải bị chặn ngay tại đây với thông điệp rõ ràng.
    var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
        ?? throw new InvalidOperationException($"Missing configuration section '{JwtOptions.SectionName}'.");

    if (jwtOptions.SigningKey.Length < 32)
    {
        throw new InvalidOperationException(
            "Jwt:SigningKey must be at least 32 characters. Set it through user-secrets or the Jwt__SigningKey environment variable.");
    }

    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = jwtOptions.Issuer,
                ValidateAudience = true,
                ValidAudience = jwtOptions.Audience,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey)),
                ValidateLifetime = true,
                // Access token chỉ sống 15 phút, cộng thêm 5 phút mặc định của thư viện
                // là biến hết hạn thành chuyện không kiểm chứng được ở phía client.
                ClockSkew = TimeSpan.Zero,
                RoleClaimType = ClaimTypes.Role,
                NameClaimType = ClaimTypes.Name
            };
        });

    builder.Services.AddAuthorization(options =>
        options.AddPolicy(AuthPolicies.Admin, policy => policy.RequireRole(nameof(UserRole.Admin))));

    // Enum đi/về dưới dạng chuỗi ("Hybrid", "FullText") thay vì số thứ tự.
    builder.Services.ConfigureHttpJsonOptions(options =>
        options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

    builder.Services.AddProblemDetails();
    builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
    builder.Services.AddOpenApi();

    builder.Services.AddHealthChecks()
        .AddDbContextCheck<IChatDbContext>("postgres", tags: ["ready"])
        .AddCheck<VectorExtensionHealthCheck>("pgvector", tags: ["ready"])
        .AddCheck<ProviderHealthCheck>("ai-providers", tags: ["ready"]);

    builder.Services.AddOpenTelemetry()
        .ConfigureResource(resource => resource.AddService("IChat.Api"))
        .WithTracing(tracing => tracing
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddSource("IChat.Chat")
            .AddSource("IChat.UtilityChat")
            .AddSource("IChat.Embedding")
            .AddSource(IChat.Api.Telemetry.RagActivitySource.Name))
        .WithMetrics(metrics => metrics
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation());

    builder.Services.AddRateLimiter(options =>
    {
        options.AddFixedWindowLimiter("chat", limiter =>
        {
            limiter.PermitLimit = 30;
            limiter.Window = TimeSpan.FromMinutes(1);
            limiter.QueueLimit = 0;
        });
    });

    var app = builder.Build();

    app.UseExceptionHandler();

    // Một dòng tổng kết cho mỗi request thay vì 3-4 dòng của logger mặc định ASP.NET Core.
    app.UseSerilogRequestLogging(options =>
    {
        options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
        options.GetLevel = GetRequestLogLevel;
        options.EnrichDiagnosticContext = EnrichRequestLog;
    });

    app.UseRateLimiter();

    app.UseAuthentication();
    app.UseAuthorization();

    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
        // UI đọc trực tiếp document ở /openapi/v1.json, chỉ bật ở Development.
        app.MapScalarApiReference("/docs", options => options.WithTitle("IChat API"));
    }

    app.MapIChatEndpoints();

    // Migration tự chạy lúc startup CHỈ ở Development; production dùng `dotnet ef database update`.
    if (app.Configuration.GetValue<bool>("Database:AutoMigrate"))
    {
        using var scope = app.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IChatDbContext>();
        await dbContext.Database.MigrateAsync();
    }

    await app.RunAsync();
}
catch (Exception exception) when (exception is not HostAbortedException)
{
    Log.Fatal(exception, "Host terminated unexpectedly");

    return 1;
}
finally
{
    await Log.CloseAndFlushAsync();
}

return 0;

// Health check bị poll liên tục bởi orchestrator; giữ ở Information sẽ nhấn chìm log thật.
static LogEventLevel GetRequestLogLevel(HttpContext httpContext, double elapsedMs, Exception? exception)
{
    if (exception is not null || httpContext.Response.StatusCode >= StatusCodes.Status500InternalServerError)
    {
        return LogEventLevel.Error;
    }

    if (httpContext.Request.Path.StartsWithSegments("/health"))
    {
        return LogEventLevel.Debug;
    }

    return LogEventLevel.Information;
}

static void EnrichRequestLog(IDiagnosticContext diagnosticContext, HttpContext httpContext)
{
    var request = httpContext.Request;

    diagnosticContext.Set("RequestHost", request.Host.Value);
    diagnosticContext.Set("RequestScheme", request.Scheme);
    diagnosticContext.Set("RequestProtocol", request.Protocol);
    diagnosticContext.Set("ClientIp", httpContext.Connection.RemoteIpAddress?.ToString());
    diagnosticContext.Set("UserAgent", request.Headers.UserAgent.ToString());
    diagnosticContext.Set("TraceIdentifier", httpContext.TraceIdentifier);
    diagnosticContext.Set("UserId", httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier));

    if (request.QueryString.HasValue)
    {
        diagnosticContext.Set("QueryString", request.QueryString.Value);
    }

    var endpoint = httpContext.GetEndpoint();

    if (endpoint is not null)
    {
        diagnosticContext.Set("EndpointName", endpoint.DisplayName);
    }

    if (httpContext.Response.ContentType is { Length: > 0 } contentType)
    {
        diagnosticContext.Set("ResponseContentType", contentType);
    }
}

public partial class Program;
