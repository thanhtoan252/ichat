namespace IChat.Core.Abstractions;

using IChat.Core.Contracts.Conversations;

public interface IChatService
{

IAsyncEnumerable<SseEvent> StreamAnswerAsync(SendMessageRequest request, CancellationToken cancellationToken);
}
