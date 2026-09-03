namespace IChat.Core.Abstractions;

using IChat.Core.Common;
using IChat.Core.Contracts.Conversations;

public interface IConversationService
{
    Task<Result<ConversationView>> CreateAsync(CreateConversationRequest request, CancellationToken cancellationToken);

    Task<Result<PaginatedList<ConversationView>>> GetListAsync(int page, int pageSize, string? userId, CancellationToken cancellationToken);

    Task<Result<PaginatedList<MessageView>>> GetMessagesAsync(Guid conversationId, int page, int pageSize, CancellationToken cancellationToken);
}
