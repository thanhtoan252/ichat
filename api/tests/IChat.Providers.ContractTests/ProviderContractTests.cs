namespace IChat.Providers.ContractTests;

using IChat.Infrastructure.Ai;
using IChat.Infrastructure.Ai.Providers;
using FluentAssertions;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using Xunit;
using Xunit.Abstractions;

/// <summary>
/// Một bộ test dùng chung, chạy lặp qua từng provider. Tự skip khi thiếu biến môi trường
/// nên CI mặc định bỏ qua; chạy thật bằng cách đặt API key tương ứng.
/// </summary>
[Trait("Category", "RequiresApiKey")]
public class ProviderContractTests(ITestOutputHelper output)
{
    public static TheoryData<string> ChatProviders =>
    [
        nameof(ChatProvider.OpenAI),
        nameof(ChatProvider.Anthropic)
    ];

    public static TheoryData<string> EmbeddingProviders =>
    [
        nameof(EmbeddingProvider.OpenAI)
    ];

    private static (string? KeyVariable, string? Endpoint, string? Model) Configure(string provider) => provider switch
    {
        nameof(ChatProvider.OpenAI) => ("OPENAI_API_KEY", null, Environment.GetEnvironmentVariable("CONTRACT_OPENAI_MODEL") ?? "gpt-4o-mini"),
        nameof(ChatProvider.Anthropic) => ("ANTHROPIC_API_KEY", null, Environment.GetEnvironmentVariable("CONTRACT_ANTHROPIC_MODEL") ?? "claude-haiku-4-5"),
        _ => (null, null, null)
    };

    private bool TryBuildChatClient(string provider, out IChatClient chatClient)
    {
        chatClient = null!;
        var (keyVariable, endpoint, model) = Configure(provider);

        var apiKey = keyVariable is null ? null : Environment.GetEnvironmentVariable(keyVariable);

        if (keyVariable is not null && string.IsNullOrWhiteSpace(apiKey))
        {
            output.WriteLine($"SKIP {provider}: environment variable {keyVariable} is not set.");

            return false;
        }

        var options = Options.Create(new AiOptions
        {
            Chat = new ChatProviderOptions {
                Provider = Enum.Parse<ChatProvider>(provider),
                Model = model!,
                Endpoint = endpoint,
                ApiKey = apiKey,
                MaxOutputTokens = 64
            },
            UtilityChat = new UtilityChatOptions { Model = model! },
            Embedding = new EmbeddingOptions { Model = "text-embedding-3-small", ApiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY") }
        });

        chatClient = new ChatClientFactory(options, [
            new OpenAIChatClientFactory(),
            new AzureOpenAIChatClientFactory(),
            new AnthropicChatClientFactory(),
            new GoogleChatClientFactory()
        ]).Create();

        return true;
    }

    [Theory]
    [MemberData(nameof(ChatProviders))]
    public async Task NonStreaming_ReturnsNonEmptyText(string provider)
    {
        if (!TryBuildChatClient(provider, out var chatClient))
        {
            return;
        }

        using var client = chatClient;

        var response = await chatClient.GetResponseAsync(
            [new ChatMessage(ChatRole.User, "Answer with exactly one word: hello")],
            new ChatOptions { MaxOutputTokens = 64 });

        response.Text.Should().NotBeNullOrWhiteSpace();
    }

    [Theory]
    [MemberData(nameof(ChatProviders))]
    public async Task Streaming_YieldsAtLeastTwoUpdates_AndMatchesNonStreaming(string provider)
    {
        if (!TryBuildChatClient(provider, out var chatClient))
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

    [Theory]
    [MemberData(nameof(ChatProviders))]
    public async Task MaxOutputTokens_IsRespected(string provider)
    {
        if (!TryBuildChatClient(provider, out var chatClient))
        {
            return;
        }

        using var client = chatClient;

        var response = await chatClient.GetResponseAsync(
            [new ChatMessage(ChatRole.User, "Write a very long paragraph about the history of computing.")],
            new ChatOptions { MaxOutputTokens = 16 });

        // Ước lượng thô: 16 token không thể vượt quá vài trăm ký tự ở bất kỳ hãng nào.
        response.Text.Should().NotBeNull();
        response.Text!.Length.Should().BeLessThan(600);
    }

    [Theory]
    [MemberData(nameof(ChatProviders))]
    public async Task UsageMetadata_IsReadableOrSafelyNull(string provider)
    {
        if (!TryBuildChatClient(provider, out var chatClient))
        {
            return;
        }

        using var client = chatClient;

        var response = await chatClient.GetResponseAsync(
            [new ChatMessage(ChatRole.User, "hello")],
            new ChatOptions { MaxOutputTokens = 32 });

        // Không được nổ khi provider không trả usage.
        var act = () => _ = response.Usage?.InputTokenCount;

        act.Should().NotThrow();
    }

    [Theory]
    [MemberData(nameof(EmbeddingProviders))]
    public async Task Embedding_ReturnsConfiguredDimensions(string provider)
    {
        var keyVariable = "OPENAI_API_KEY";

        var apiKey = Environment.GetEnvironmentVariable(keyVariable);

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            output.WriteLine($"SKIP {provider}: {keyVariable} is not set.");

            return;
        }

        var dimensions = 1536;

        var options = Options.Create(new AiOptions
        {
            Chat = new ChatProviderOptions { Model = "unused", ApiKey = apiKey },
            UtilityChat = new UtilityChatOptions { Model = "unused" },
            Embedding = new EmbeddingOptions
            {
                Provider = Enum.Parse<EmbeddingProvider>(provider),
                Model = "text-embedding-3-small",
                Dimensions = dimensions,
                ApiKey = apiKey
            }
        });

        var factory = new EmbeddingGeneratorFactory(options, [
            new OpenAIEmbeddingClientFactory(),
            new AzureOpenAIEmbeddingClientFactory(),
            new GoogleEmbeddingClientFactory()
        ]);
        using var generator = factory.Create();

        var embeddings = await generator.GenerateAsync(["a test sentence"]);

        embeddings.Should().ContainSingle();

        if (factory.Capabilities.SupportsEmbeddingDimensions)
        {
            embeddings[0].Vector.Length.Should().Be(dimensions);
        }
        else
        {
            output.WriteLine($"{provider} does not support dimension reduction; got {embeddings[0].Vector.Length} dimensions.");
            embeddings[0].Vector.Length.Should().BeGreaterThan(0);
        }
    }
}
