namespace IChat.Core.UnitTests.Ai;

using IChat.Infrastructure.Ai;
using IChat.Infrastructure.Ai.Providers;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

/// <summary>
/// Thêm một hãng vào enum mà quên đăng ký factory sẽ chỉ vỡ lúc chạy thật, ngay giữa một
/// request của người dùng. Test này bắt nó ngay lúc build.
/// </summary>
public class ProviderFactoryRegistryTests
{
    [Fact]
    public void EveryChatProvider_HasARegisteredFactory()
    {
        using var provider = BuildServiceProvider();

        var registered = provider.GetServices<IChatProviderClientFactory>().Select(factory => factory.Provider);

        registered.Should().BeEquivalentTo(Enum.GetValues<ChatProvider>());
    }

    [Fact]
    public void EveryEmbeddingProvider_HasARegisteredFactory()
    {
        using var provider = BuildServiceProvider();

        var registered = provider.GetServices<IEmbeddingProviderClientFactory>().Select(factory => factory.Provider);

        registered.Should().BeEquivalentTo(Enum.GetValues<EmbeddingProvider>());
    }

    [Fact]
    public void ChatProviderFactories_AreRegisteredExactlyOncePerProvider()
    {
        using var provider = BuildServiceProvider();

        var registered = provider.GetServices<IChatProviderClientFactory>().Select(factory => factory.Provider).ToList();

        registered.Should().OnlyHaveUniqueItems("ChatClientFactory dựng dictionary theo Provider và sẽ ném lỗi nếu trùng khoá");
    }

    [Fact]
    public void EmbeddingProviderFactories_AreRegisteredExactlyOncePerProvider()
    {
        using var provider = BuildServiceProvider();

        var registered = provider.GetServices<IEmbeddingProviderClientFactory>().Select(factory => factory.Provider).ToList();

        registered.Should().OnlyHaveUniqueItems();
    }

    private static ServiceProvider BuildServiceProvider()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Ai:Chat:Model"] = "gpt-4o-mini",
                ["Ai:UtilityChat:Model"] = "gpt-4o-mini",
                ["Ai:Embedding:Model"] = "text-embedding-3-small"
            })
            .Build();

        return new ServiceCollection()
            .AddSingleton<IConfiguration>(configuration)
            .AddLogging()
            .AddIChatAi(configuration)
            .BuildServiceProvider();
    }
}
