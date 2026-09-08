namespace IChat.Core.Abstractions;

using IChat.Core.Common;
using IChat.Core.Contracts.Conversations;

public interface IConversationService
{
    Task<Result<ConversationView>> CreateAsync(CreateConversationRequest request, CancellationToken cancellationToken);

    Task<Result<PaginatedList<ConversationView>>> GetListAsync(int offset, int limit, CancellationToken cancellationToken);

    Task<Result<PaginatedList<MessageView>>> GetMessagesAsync(Guid conversationId, int offset, int limit, CancellationToken cancellationToken);
}
