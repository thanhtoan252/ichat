using IChat.Api.Authorization;
using IChat.Api.Endpoints;
using IChat.Api.Endpoints.Documents.V1.Validators;
using IChat.Api.HealthChecks;
using IChat.Api.Middleware;
using IChat.Api.OpenApi;
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

// Bootstrap logger: anything logged before the host finishes building (bad config, broken DI)
// still comes out as JSON instead of vanishing. Replaced by the appsettings logger after Build().
Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .WriteTo.Console(new CompactJsonFormatter())
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // Kestrel caps the request body at 30MB by default, which is before the validator even runs: an
    // oversized file would get a bare 413 instead of a readable message. Take the validator's own
    // ceiling and leave room for the multipart envelope (boundary plus each part's headers) so the
    // two numbers cannot drift apart when the limit changes.
    builder.WebHost.ConfigureKestrel(options =>
        options.Limits.MaxRequestBodySize = UploadDocumentDtoValidator.MaxSizeInBytes + 1024 * 1024);

    // Sinks, enrichers and formatting all live in the "Serilog" section of appsettings: switching
    // between JSON and text is a config change, not a code change plus a rebuild.
    builder.Services.AddSerilog((services, configuration) => configuration
        .ReadFrom.Configuration(builder.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext());

    builder.Services.AddSingleton(TimeProvider.System);
    builder.Services.AddIChatCore();
    // Validators for the API-layer DTOs (Endpoints/<Feature>/V1/Validators); validators for service
    // models live in Core and are registered separately by AddIChatCore.
    builder.Services.AddValidatorsFromAssemblyContaining<Program>(ServiceLifetime.Scoped);
    builder.Services.AddIChatInfrastructure(builder.Configuration);

    builder.Services.AddOptions<JwtOptions>()
        .BindConfiguration(JwtOptions.SectionName)
        .ValidateDataAnnotations()
        .ValidateOnStart();

    builder.Services.AddHttpContextAccessor();
    builder.Services.AddScoped<ICurrentUser, HttpContextCurrentUser>();
    builder.Services.AddSingleton<IAccessTokenService, JwtAccessTokenService>();

    // Read early to build TokenValidationParameters; ValidateOnStart above only runs once the host
    // is built, so an empty key has to be rejected right here with a clear message.
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
                // The access token only lives 15 minutes, and the library's default 5-minute skew on top
                // of that would make expiry something the client cannot reason about.
                ClockSkew = TimeSpan.Zero,
                RoleClaimType = ClaimNames.Role,
                NameClaimType = ClaimNames.Name
            };

            // By default the handler translates "sub"/"role" back into the long ClaimTypes URIs, so
            // FindFirstValue("sub") finds nothing. Turn it off to read claims under the names they were signed with.
            options.MapInboundClaims = false;
        });

    builder.Services.AddAuthorization(options =>
        options.AddPolicy(AuthPolicies.Admin, policy => policy.RequireRole(nameof(UserRole.Admin))));

    // Enums travel as strings ("Hybrid", "FullText") rather than ordinals.
    builder.Services.ConfigureHttpJsonOptions(options =>
        options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

    builder.Services.AddProblemDetails();
    builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
    builder.Services.AddOpenApi(options =>
    {
        options.AddDocumentTransformer<JwtBearerSecuritySchemeTransformer>();
        options.AddOperationTransformer<AuthorizationOperationTransformer>();
    });

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

    // One summary line per request instead of the three or four ASP.NET Core's default logger writes.
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
        // The UI reads the document straight from /openapi/v1.json; only enabled in Development.
        app.MapScalarApiReference("/docs", options => options
            .WithTitle("IChat API")
            // Preselect the Bearer box and keep the token across reloads, so trying an authenticated
            // endpoint does not mean pasting the token again after every F5.
            .AddPreferredSecuritySchemes(JwtBearerSecuritySchemeTransformer.SchemeName)
            .EnablePersistentAuthentication());
    }

    app.MapIChatEndpoints();

    // Migrations run at startup ONLY in Development; production uses `dotnet ef database update`.
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

// Health checks are polled constantly by the orchestrator; leaving them at Information would drown the real logs.
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
    diagnosticContext.Set("UserId", httpContext.User.FindFirstValue(ClaimNames.Sub));

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
