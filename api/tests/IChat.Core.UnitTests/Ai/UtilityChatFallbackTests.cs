namespace IChat.Core.UnitTests.Ai;

using FluentAssertions;
using IChat.Infrastructure.Ai;
using IChat.Infrastructure.Ai.Providers;
using Microsoft.Extensions.Options;
using NUnit.Framework;

/// <summary>
/// Pins the rule that every field UtilityChat leaves empty is inherited from Chat.
/// Three places read that rule independently — ChatClientFactory builds the client, ModelCatalog reports the
/// status to an admin, AiOptionsValidator blocks startup — so if they drift apart the admin endpoint reports
/// "missing key" for a provider that works perfectly.
/// </summary>
[TestFixture]
public sealed class UtilityChatFallbackTests
{
    private const string PresentKey = "sk-present";

    [Test]
    public void Snapshot_UtilityProvider_InheritsFromChat_WhenNotConfigured()
    {
        // Arrange
        var options = OptionsFor(utilityProvider: null);

        // Act
        var snapshot = new ModelCatalog(options).GetSnapshot();

        // Assert
        snapshot.UtilityChat.Provider.Should().Be(ChatProvider.Anthropic.ToString());
    }

    [Test]
    public void Snapshot_UtilityProvider_WinsOverChat_WhenConfigured()
    {
        // Arrange
        var options = OptionsFor(utilityProvider: ChatProvider.OpenAI);

        // Act
        var snapshot = new ModelCatalog(options).GetSnapshot();

        // Assert
        snapshot.UtilityChat.Provider.Should().Be(ChatProvider.OpenAI.ToString());
    }

    [Test]
    public void Snapshot_UtilityApiKey_InheritsFromChat_SoAvailabilityIsNotAFalseAlarm()
    {
        // Arrange
        var options = OptionsFor(utilityProvider: null, chatApiKey: PresentKey, utilityApiKey: null);

        // Act
        var snapshot = new ModelCatalog(options).GetSnapshot();

        // Assert
        snapshot.UtilityChat.Available.Should().BeTrue();
        snapshot.UtilityChat.Reason.Should().BeNull();
    }

    [Test]
    public void Snapshot_UtilityEndpoint_InheritsFromChat_ForAProviderThatRequiresOne()
    {
        // Arrange
        var options = OptionsFor(
            utilityProvider: null,
            chatApiKey: PresentKey,
            utilityApiKey: null,
            chatProvider: ChatProvider.AzureOpenAI,
            chatEndpoint: "https://contoso.openai.azure.com/");

        // Act
        var snapshot = new ModelCatalog(options).GetSnapshot();

        // Assert
        snapshot.UtilityChat.Available.Should().BeTrue();
    }

    [Test]
    public void Snapshot_UtilityEndpoint_MissingOnBothSides_ReportsTheReason()
    {
        // Arrange
        var options = OptionsFor(
            utilityProvider: null,
            chatApiKey: PresentKey,
            utilityApiKey: null,
            chatProvider: ChatProvider.AzureOpenAI,
            chatEndpoint: null);

        // Act
        var snapshot = new ModelCatalog(options).GetSnapshot();

        // Assert
        snapshot.UtilityChat.Available.Should().BeFalse();
        snapshot.UtilityChat.Reason.Should().Be("Endpoint is missing.");
    }

    [Test]
    public void Snapshot_UtilityApiKey_MissingOnBothSides_ReportsTheReason()
    {
        // Arrange
        var options = OptionsFor(utilityProvider: null, chatApiKey: null, utilityApiKey: null);

        // Act
        var snapshot = new ModelCatalog(options).GetSnapshot();

        // Assert
        snapshot.UtilityChat.Available.Should().BeFalse();
        snapshot.UtilityChat.Reason.Should().Be("No API key is configured.");
    }

    [Test]
    public void Factory_UtilityCapabilities_FollowTheInheritedProvider()
    {
        // Arrange
        var options = OptionsFor(utilityProvider: null);

        // Act
        var factory = new ChatClientFactory(options, [
            new OpenAIChatClientFactory(),
            new AzureOpenAIChatClientFactory(),
            new AnthropicChatClientFactory(),
            new GoogleChatClientFactory()
        ]);

        // Assert
        // Anthropic merges every system block into one; inheriting the wrong provider builds the wrong prompt.
        factory.UtilityCapabilities.SupportsMultipleSystemMessages.Should().BeFalse();
    }

    [Test]
    public void Validator_ReportsTheUtilitySection_WhenTheInheritedApiKeyIsMissing()
    {
        // Arrange
        var options = OptionsFor(utilityProvider: null, chatApiKey: null, utilityApiKey: null).Value;

        // Act
        var result = new AiOptionsValidator().Validate(name: null, options);

        // Assert
        result.Failed.Should().BeTrue();
        result.Failures.Should().Contain(failure => failure.Contains("Ai:UtilityChat"));
    }

    [Test]
    public void Validator_AcceptsTheUtilitySection_WhenTheKeyIsInheritedFromChat()
    {
        // Arrange
        var options = OptionsFor(utilityProvider: null, chatApiKey: PresentKey, utilityApiKey: null).Value;

        // Act
        var result = new AiOptionsValidator().Validate(name: null, options);

        // Assert
        result.Succeeded.Should().BeTrue();
    }

    private static IOptions<AiOptions> OptionsFor(
        ChatProvider? utilityProvider,
        string? chatApiKey = null,
        string? utilityApiKey = null,
        ChatProvider chatProvider = ChatProvider.Anthropic,
        string? chatEndpoint = null)
    {
        return Options.Create(new AiOptions
        {
            Chat = new ChatProviderOptions
            {
                Provider = chatProvider,
                Model = "chat-model",
                Endpoint = chatEndpoint,
                ApiKey = chatApiKey
            },
            UtilityChat = new UtilityChatOptions
            {
                Provider = utilityProvider,
                Model = "utility-model",
                ApiKey = utilityApiKey
            },
            Embedding = new EmbeddingOptions
            {
                Model = "embedding-model",
                ApiKey = chatApiKey
            }
        });
    }
}
