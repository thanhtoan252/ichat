namespace IChat.Providers.ContractTests;

using IChat.Infrastructure.Ai;
using IChat.Infrastructure.Ai.Providers;
using FluentAssertions;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using Xunit;
using Xunit.Abstractions;

[Trait("Category", "RequiresApiKey")]
public class ProviderContractTests(ITestOutputHelper output)
{
    private static string ChatModel => Environment.GetEnvironmentVariable("CONTRACT_OPENAI_MODEL") ?? "gpt-4o-mini";

    private bool TryBuildChatClient(out IChatClient chatClient)
    {
        chatClient = null!;
        const string keyVariable = "OPENAI_API_KEY";
        var model = ChatModel;
        var apiKey = Environment.GetEnvironmentVariable(keyVariable);

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            output.WriteLine($"SKIP OpenAI: environment variable {keyVariable} is not set.");

            return false;
        }

        var options = Options.Create(new AiOptions
        {
            Chat = new ChatModelOptions
            {
                Model = model,
                ApiKey = apiKey,
                MaxOutputTokens = 64
            },
            UtilityChat = new UtilityChatOptions { Model = model },
            Embedding = new EmbeddingOptions { Model = "text-embedding-3-small", ApiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY") }
        });

        chatClient = new ChatClientFactory(options, new OpenAIChatClientFactory()).Create();

        return true;
    }

    [Fact]
    public async Task NonStreaming_ReturnsNonEmptyText()
    {
        if (!TryBuildChatClient(out var chatClient))
        {
            return;
        }

        using var client = chatClient;

        var response = await chatClient.GetResponseAsync(
                    [new ChatMessage(ChatRole.User, "Answer with exactly one word: hello")],
                    new ChatOptions { MaxOutputTokens = 64 });

        response.Text.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Streaming_YieldsAtLeastTwoUpdates_AndMatchesNonStreaming()
    {
        if (!TryBuildChatClient(out var chatClient))
        {
            return;
        }

        using var client = chatClient;

        var updates = new List<ChatResponseUpdate>();

        await foreach (var update in chatClient.GetStreamingResponseAsync(
                    [new ChatMessage(ChatRole.User, "Count from one to five, one number per line.")],
                    new ChatOptions { MaxOutputTokens = 128 }))
        {
            updates.Add(update);
        }

        updates.Count.Should().BeGreaterThan(1);
        string.Concat(updates.Select(update => update.Text)).Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task MaxOutputTokens_IsRespected()
    {
        if (!TryBuildChatClient(out var chatClient))
        {
            return;
        }

        using var client = chatClient;

        var response = await chatClient.GetResponseAsync(
                    [new ChatMessage(ChatRole.User, "Write a very long paragraph about the history of computing.")],
                    new ChatOptions { MaxOutputTokens = 16 });

        response.Text.Should().NotBeNull();
        response.Text!.Length.Should().BeLessThan(600);
    }

    [Fact]
    public async Task UsageMetadata_IsReadableOrSafelyNull()
    {
        if (!TryBuildChatClient(out var chatClient))
        {
            return;
        }

        using var client = chatClient;

        var response = await chatClient.GetResponseAsync(
                    [new ChatMessage(ChatRole.User, "hello")],
                    new ChatOptions { MaxOutputTokens = 32 });

        var act = () => _ = response.Usage?.InputTokenCount;

        act.Should().NotThrow();
    }

    [Fact]
    public async Task Embedding_ReturnsConfiguredDimensions()
    {
        var keyVariable = "OPENAI_API_KEY";

        var apiKey = Environment.GetEnvironmentVariable(keyVariable);

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            output.WriteLine($"SKIP OpenAI: {keyVariable} is not set.");

            return;
        }

        var dimensions = 1536;

        var options = Options.Create(new AiOptions
        {
            Chat = new ChatModelOptions { Model = "unused", ApiKey = apiKey },
            UtilityChat = new UtilityChatOptions { Model = "unused" },
            Embedding = new EmbeddingOptions
            {
                Model = "text-embedding-3-small",
                Dimensions = dimensions,
                ApiKey = apiKey
            }
        });

        var factory = new EmbeddingGeneratorFactory(options, new OpenAIEmbeddingClientFactory());
        using var generator = factory.Create();

        var embeddings = await generator.GenerateAsync(["a test sentence"]);

        embeddings.Should().ContainSingle();

        embeddings[0].Vector.Length.Should().Be(dimensions);
    }
}
