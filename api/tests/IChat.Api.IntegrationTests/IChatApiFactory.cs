namespace IChat.Api.IntegrationTests;

using IChat.Api.IntegrationTests.Fakes;
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
            "TRUNCATE message_citations, messages, conversations, document_chunks, documents CASCADE;");
    }
}
