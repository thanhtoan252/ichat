namespace IChat.Core.Abstractions;

using IChat.Core.Contracts.Conversations;

public interface IChatService
{
    /// <summary>
    /// Generates a RAG answer and emits the SSE event stream: status, sources, delta, done.
    /// Cancelling midway still persists what was generated, marked [interrupted].
    /// </summary>
    IAsyncEnumerable<SseEvent> StreamAnswerAsync(SendMessageRequest request, CancellationToken cancellationToken);
}
