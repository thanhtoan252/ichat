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
            // Fake AI tất định: test không phụ thuộc API key hay mạng.
            services.RemoveAll<IEmbeddingGenerator<string, Embedding<float>>>();
            services.AddSingleton<IEmbeddingGenerator<string, Embedding<float>>>(new FakeEmbeddingGenerator());

            services.RemoveAll<IChatClient>();
            services.AddSingleton<IChatClient>(MainChat);
            services.AddKeyedSingleton<IChatClient>(AiServiceKeys.UtilityChat, UtilityChat);
        });
    }

    public async Task InitializeAsync()
    {
        // API key bind thẳng vào AiOptions qua IConfiguration, nên phải đặt đúng khoá
        // cấu hình chứ không phải tên biến quy ước của từng hãng. Thiếu key thì
        // AiOptionsValidator chặn ngay ở ValidateOnStart và host không dựng được.
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
    /// Client đã đăng nhập sẵn. Token được ký thẳng từ DI thay vì gọi /auth/login: test
    /// nào cũng cần một danh tính, và đi qua HTTP mỗi lần chỉ thêm một điểm hỏng.
    /// Mọi endpoint giờ mặc định đóng nên đây là cách duy nhất gọi được API.
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

        var user = await dbContext.Users.FirstOrDefaultAsync(item => item.UserName == normalized);

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
