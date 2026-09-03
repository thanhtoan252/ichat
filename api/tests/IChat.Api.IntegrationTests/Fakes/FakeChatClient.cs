namespace IChat.Api.IntegrationTests.Fakes;

using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;

public sealed class FakeChatClient : IChatClient
{
    /// <summary>State theo từng instance: client chính và client "utility" phải độc lập.</summary>
    public volatile string ResponseText = "This is a sample answer [1].";

    public volatile bool ShouldTimeout;

    public async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        if (ShouldTimeout)
        {
            await Task.Delay(Timeout.Infinite, cancellationToken);
        }

        return new ChatResponse(new ChatMessage(ChatRole.Assistant, ResponseText))
        {
            Usage = new UsageDetails { InputTokenCount = 100, OutputTokenCount = 20 }
        };
    }

    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (ShouldTimeout)
        {
            await Task.Delay(Timeout.Infinite, cancellationToken);
        }

        // Chia thành nhiều update để test SSE thấy được nhiều delta.
        foreach (var word in ResponseText.Split(' '))
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return new ChatResponseUpdate(ChatRole.Assistant, word + " ");
        }

        yield return new ChatResponseUpdate
        {
            Role = ChatRole.Assistant,
            Contents = [new UsageContent(new UsageDetails { InputTokenCount = 100, OutputTokenCount = 20 })]
        };
    }

    public object? GetService(Type serviceType, object? serviceKey = null) => null;

    public void Dispose()
    {
    }
}
