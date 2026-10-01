namespace IChat.Api.IntegrationTests;

using System.Net.Http.Headers;
using IChat.Api.IntegrationTests.Fakes;
using IChat.Api.Security;
using IChat.Core.Abstractions;
using IChat.Core.Contracts.Auth;
using IChat.Core.Domain.Identity;
using IChat.Infrastructure.Ai;
using IChat.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Testcontainers.PostgreSql;
using Xunit;

public sealed class IChatApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("pgvector/pgvector:pg17")
        .WithDatabase("ichat")
        .WithUsername("ichat")
        .WithPassword("ichat")
        .Build();

    public string ConnectionString => _postgres.GetConnectionString();

    public FakeChatClient MainChat { get; } = new();

    public FakeChatClient UtilityChat { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.UseSetting("ConnectionStrings:Default", _postgres.GetConnectionString());
        builder.UseSetting("Database:AutoMigrate", "true");
        builder.UseSetting("Storage:RootPath", Path.Combine(Path.GetTempPath(), "ichat-tests", Guid.NewGuid().ToString("N")));
        builder.UseSetting("Jwt:SigningKey", "integration-tests-signing-key-at-least-32-chars");

        builder.ConfigureServices(services =>
        {
            // Deterministic fake AI: the tests depend on neither an API key nor the network.
            services.RemoveAll<IEmbeddingGenerator<string, Embedding<float>>>();
            services.AddSingleton<IEmbeddingGenerator<string, Embedding<float>>>(new FakeEmbeddingGenerator());

            services.RemoveAll<IChatClient>();
            services.AddSingleton<IChatClient>(MainChat);
            services.AddKeyedSingleton<IChatClient>(AiServiceKeys.UtilityChat, UtilityChat);
        });
    }

    public async Task InitializeAsync()
    {
        // API keys bind straight into AiOptions through IConfiguration, so they have to be set under the real
        // configuration keys rather than each vendor's conventional env var name. Without a key,
        // AiOptionsValidator stops startup at ValidateOnStart and the host never comes up.
        Environment.SetEnvironmentVariable("Ai__Chat__ApiKey", "sk-test-fake");
        Environment.SetEnvironmentVariable("Ai__Embedding__ApiKey", "sk-test-fake");

        await _postgres.StartAsync();
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await _postgres.DisposeAsync();
    }

    public async Task ResetDatabaseAsync()
    {
        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IChatDbContext>();

        await dbContext.Database.ExecuteSqlRawAsync(
            "TRUNCATE message_citations, messages, conversations, document_chunks, documents, refresh_tokens, users CASCADE;");
    }

    /// <summary>
    /// An already signed-in client. The token is signed straight from DI instead of calling /auth/login: every
    /// test needs an identity, and going over HTTP each time only adds another point of failure.
    /// Every endpoint is now closed by default, so this is the only way to reach the API at all.
    /// </summary>
    public async Task<HttpClient> CreateClientAsync(UserRole role = UserRole.Admin, string userName = "tester")
    {
        var user = await EnsureUserAsync(role, userName);

        using var scope = Services.CreateScope();
        var accessToken = scope.ServiceProvider.GetRequiredService<IAccessTokenService>().Create(user);

        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken.Value);

        return client;
    }

    public async Task<UserView> EnsureUserAsync(UserRole role, string userName)
    {
        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IChatDbContext>();
        var normalized = User.Normalize(userName);

        var user = await dbContext.Users.SingleOrDefaultAsync(item => item.UserName == normalized);

        if (user is null)
        {
            var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

            user = User.Create(normalized, normalized, passwordHasher.Hash("password"), role, DateTimeOffset.UtcNow);
            dbContext.Users.Add(user);
            await dbContext.SaveChangesAsync();
        }

        return new UserView
        {
            Id = user.Id,
            UserName = user.UserName,
            DisplayName = user.DisplayName,
            Role = user.Role,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt
        };
    }
}
