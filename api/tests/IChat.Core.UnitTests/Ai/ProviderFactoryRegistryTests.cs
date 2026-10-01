namespace IChat.Core.UnitTests.Ai;

using IChat.Infrastructure.Ai;
using IChat.Infrastructure.Ai.Providers;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

/// <summary>
/// Adding a vendor to the enum but forgetting to register its factory would only break at runtime, in the
/// middle of a real user's request. This test catches it at build time.
/// </summary>
[TestFixture]
public class ProviderFactoryRegistryTests
{
    [Test]
    public void EveryChatProvider_HasARegisteredFactory()
    {
        // Arrange
        using var provider = BuildServiceProvider();

        // Act
        var registered = provider.GetServices<IChatProviderClientFactory>().Select(factory => factory.Provider);

        // Assert
        registered.Should().BeEquivalentTo(Enum.GetValues<ChatProvider>());
    }

    [Test]
    public void EveryEmbeddingProvider_HasARegisteredFactory()
    {
        // Arrange
        using var provider = BuildServiceProvider();

        // Act
        var registered = provider.GetServices<IEmbeddingProviderClientFactory>().Select(factory => factory.Provider);

        // Assert
        registered.Should().BeEquivalentTo(Enum.GetValues<EmbeddingProvider>());
    }

    [Test]
    public void ChatProviderFactories_AreRegisteredExactlyOncePerProvider()
    {
        // Arrange
        using var provider = BuildServiceProvider();

        // Act
        var registered = provider.GetServices<IChatProviderClientFactory>().Select(factory => factory.Provider).ToList();

        // Assert
        registered.Should().OnlyHaveUniqueItems("ChatClientFactory builds a dictionary keyed by Provider and throws on a duplicate key");
    }

    [Test]
    public void EmbeddingProviderFactories_AreRegisteredExactlyOncePerProvider()
    {
        // Arrange
        using var provider = BuildServiceProvider();

        // Act
        var registered = provider.GetServices<IEmbeddingProviderClientFactory>().Select(factory => factory.Provider).ToList();

        // Assert
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
