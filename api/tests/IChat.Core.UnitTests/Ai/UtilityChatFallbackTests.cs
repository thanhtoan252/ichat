namespace IChat.Core.UnitTests.Ai;

using FluentAssertions;
using IChat.Infrastructure.Ai;
using IChat.Infrastructure.Ai.Providers;
using Microsoft.Extensions.Options;
using Xunit;

/// <summary>
/// Khoá quy tắc "mọi field bỏ trống của UtilityChat kế thừa từ Chat".
/// Quy tắc này được ba nơi đọc lại một cách độc lập — ChatClientFactory dựng client,
/// ModelCatalog trả trạng thái cho admin, AiOptionsValidator chặn khởi động — nên nếu
/// chúng lệch nhau thì admin endpoint sẽ báo "thiếu key" cho một provider đang chạy tốt.
/// </summary>
public sealed class UtilityChatFallbackTests
{
    private const string PresentKey = "sk-present";

    [Fact]
    public void Snapshot_UtilityProvider_InheritsFromChat_WhenNotConfigured()
    {
        var options = OptionsFor(utilityProvider: null);

        var snapshot = new ModelCatalog(options).GetSnapshot();

        snapshot.UtilityChat.Provider.Should().Be(ChatProvider.Anthropic.ToString());
    }

    [Fact]
    public void Snapshot_UtilityProvider_WinsOverChat_WhenConfigured()
    {
        var options = OptionsFor(utilityProvider: ChatProvider.OpenAI);

        var snapshot = new ModelCatalog(options).GetSnapshot();

        snapshot.UtilityChat.Provider.Should().Be(ChatProvider.OpenAI.ToString());
    }

    [Fact]
    public void Snapshot_UtilityApiKey_InheritsFromChat_SoAvailabilityIsNotAFalseAlarm()
    {
        var options = OptionsFor(utilityProvider: null, chatApiKey: PresentKey, utilityApiKey: null);

        var snapshot = new ModelCatalog(options).GetSnapshot();

        snapshot.UtilityChat.Available.Should().BeTrue();
        snapshot.UtilityChat.Reason.Should().BeNull();
    }

    [Fact]
    public void Snapshot_UtilityEndpoint_InheritsFromChat_ForAProviderThatRequiresOne()
    {
        var options = OptionsFor(
            utilityProvider: null,
            chatApiKey: PresentKey,
            utilityApiKey: null,
            chatProvider: ChatProvider.AzureOpenAI,
            chatEndpoint: "https://contoso.openai.azure.com/");

        var snapshot = new ModelCatalog(options).GetSnapshot();

        snapshot.UtilityChat.Available.Should().BeTrue();
    }

    [Fact]
    public void Snapshot_UtilityEndpoint_MissingOnBothSides_ReportsTheReason()
    {
        var options = OptionsFor(
            utilityProvider: null,
            chatApiKey: PresentKey,
            utilityApiKey: null,
            chatProvider: ChatProvider.AzureOpenAI,
            chatEndpoint: null);

        var snapshot = new ModelCatalog(options).GetSnapshot();

        snapshot.UtilityChat.Available.Should().BeFalse();
        snapshot.UtilityChat.Reason.Should().Be("Endpoint is missing.");
    }

    [Fact]
    public void Snapshot_UtilityApiKey_MissingOnBothSides_ReportsTheReason()
    {
        var options = OptionsFor(utilityProvider: null, chatApiKey: null, utilityApiKey: null);

        var snapshot = new ModelCatalog(options).GetSnapshot();

        snapshot.UtilityChat.Available.Should().BeFalse();
        snapshot.UtilityChat.Reason.Should().Be("No API key is configured.");
    }

    [Fact]
    public void Factory_UtilityCapabilities_FollowTheInheritedProvider()
    {
        var options = OptionsFor(utilityProvider: null);

        var factory = new ChatClientFactory(options, [
            new OpenAIChatClientFactory(),
            new AzureOpenAIChatClientFactory(),
            new AnthropicChatClientFactory(),
            new GoogleChatClientFactory()
        ]);

        // Anthropic gộp mọi khối system làm một; kế thừa sai provider sẽ dựng prompt sai.
        factory.UtilityCapabilities.SupportsMultipleSystemMessages.Should().BeFalse();
    }

    [Fact]
    public void Validator_ReportsTheUtilitySection_WhenTheInheritedApiKeyIsMissing()
    {
        var options = OptionsFor(utilityProvider: null, chatApiKey: null, utilityApiKey: null).Value;

        var result = new AiOptionsValidator().Validate(name: null, options);

        result.Failed.Should().BeTrue();
        result.Failures.Should().Contain(failure => failure.Contains("Ai:UtilityChat"));
    }

    [Fact]
    public void Validator_AcceptsTheUtilitySection_WhenTheKeyIsInheritedFromChat()
    {
        var options = OptionsFor(utilityProvider: null, chatApiKey: PresentKey, utilityApiKey: null).Value;

        var result = new AiOptionsValidator().Validate(name: null, options);

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
