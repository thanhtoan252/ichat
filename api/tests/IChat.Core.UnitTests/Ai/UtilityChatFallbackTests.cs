namespace IChat.Core.UnitTests.Ai;

using FluentAssertions;
using IChat.Infrastructure.Ai;
using Microsoft.Extensions.Options;
using NUnit.Framework;

[TestFixture]
public sealed class UtilityChatFallbackTests
{
    private const string PresentKey = "sk-present";

[Test]
    public void Snapshot_UtilityProvider_InheritsFromChat_WhenNotConfigured()
    {

var options = OptionsFor();

var snapshot = new ModelCatalog(options).GetSnapshot();

snapshot.UtilityChat.Provider.Should().Be("OpenAI");
    }

[Test]
    public void Snapshot_UtilityApiKey_InheritsFromChat_SoAvailabilityIsNotAFalseAlarm()
    {

var options = OptionsFor(chatApiKey: PresentKey, utilityApiKey: null);

var snapshot = new ModelCatalog(options).GetSnapshot();

snapshot.UtilityChat.Available.Should().BeTrue();
        snapshot.UtilityChat.Reason.Should().BeNull();
    }

[Test]
    public void Snapshot_UtilityApiKey_MissingOnBothSides_ReportsTheReason()
    {

var options = OptionsFor(chatApiKey: null, utilityApiKey: null);

var snapshot = new ModelCatalog(options).GetSnapshot();

snapshot.UtilityChat.Available.Should().BeFalse();
        snapshot.UtilityChat.Reason.Should().Be("No API key is configured.");
    }

[Test]
    public void Validator_ReportsTheUtilitySection_WhenTheInheritedApiKeyIsMissing()
    {

var options = OptionsFor(chatApiKey: null, utilityApiKey: null).Value;

var result = new AiOptionsValidator().Validate(name: null, options);

result.Failed.Should().BeTrue();
        result.Failures.Should().Contain(failure => failure.Contains("Ai:UtilityChat"));
    }

[Test]
    public void Validator_AcceptsTheUtilitySection_WhenTheKeyIsInheritedFromChat()
    {

var options = OptionsFor(chatApiKey: PresentKey, utilityApiKey: null).Value;

var result = new AiOptionsValidator().Validate(name: null, options);

result.Succeeded.Should().BeTrue();
    }

private static IOptions<AiOptions> OptionsFor(
        string? chatApiKey = null,
        string? utilityApiKey = null)
    {
        return Options.Create(new AiOptions
        {
            Chat = new ChatModelOptions
            {
                Model = "chat-model",
                ApiKey = chatApiKey
            },
            UtilityChat = new UtilityChatOptions
            {
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
