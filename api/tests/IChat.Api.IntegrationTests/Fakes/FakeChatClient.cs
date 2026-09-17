namespace IChat.Api.IntegrationTests.Fakes;

using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;

public sealed class FakeChatClient : IChatClient
{
    /// <summary>Per-instance state: the main client and the "utility" client have to stay independent.</summary>
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

        // Split into several updates so the SSE tests can observe more than one delta.
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
