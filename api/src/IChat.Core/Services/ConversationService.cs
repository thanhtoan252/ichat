namespace IChat.Core.Services;

using System.Linq.Expressions;
using IChat.Core.Abstractions;
using IChat.Core.Common;
using IChat.Core.Contracts.Conversations;
using IChat.Core.Domain.Conversations;
using Microsoft.EntityFrameworkCore;

public sealed class ConversationService(
    IApplicationDbContext dbContext,
    ICurrentUser currentUser,
    TimeProvider timeProvider) : IConversationService
{

    private static readonly Expression<Func<Conversation, ConversationView>> ToViewExpression =
            conversation => new ConversationView
            {
                Id = conversation.Id,
                Title = conversation.Title,
                UserId = conversation.UserId,
                CreatedAt = conversation.CreatedAt,
                UpdatedAt = conversation.UpdatedAt
            };

    private static readonly Func<Conversation, ConversationView> ToView = ToViewExpression.Compile();

    public async Task<Result<ConversationView>> CreateAsync(CreateConversationRequest request, CancellationToken cancellationToken)
    {
        var title = string.IsNullOrWhiteSpace(request.Title) ? ConversationTitle.Default : request.Title.Trim();
        if (currentUser.Id is not { } ownerId)
        {
            return Result.Failure<ConversationView>(Error.Unauthorized("There is no active session."));
        }

        var conversation = Conversation.Create(ownerId, title, timeProvider.GetUtcNow());

        dbContext.Conversations.Add(conversation);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success(ToView(conversation));
    }

    public async Task<Result<PaginatedList<ConversationView>>> GetListAsync(int offset, int limit, CancellationToken cancellationToken)
    {

        var ownerId = currentUser.Id;
        var source = dbContext.Conversations
            .AsNoTracking()
            .Where(conversation => conversation.UserId == ownerId);

        var totalCount = await source.CountAsync(cancellationToken);

        var items = await source
                    .OrderByDescending(conversation => conversation.UpdatedAt)
                    .Skip(offset)
                    .Take(limit)
                    .Select(ToViewExpression)
                    .ToListAsync(cancellationToken);

        return Result.Success(new PaginatedList<ConversationView>
        {
            Items = items,
            Offset = offset,
            Limit = limit,
            TotalCount = totalCount
        });
    }

    public async Task<Result<PaginatedList<MessageView>>> GetMessagesAsync(Guid conversationId, int offset, int limit, CancellationToken cancellationToken)
    {
        var ownerId = await dbContext.Conversations
            .AsNoTracking()
            .Where(item => item.Id == conversationId)
            .Select(item => (Guid?)item.UserId)
            .SingleOrDefaultAsync(cancellationToken);

        if (ownerId is null || !IsOwner(ownerId.Value))
        {
            return Result.Failure<PaginatedList<MessageView>>(Error.NotFound("Conversation", conversationId));
        }

        var source = dbContext.Messages.AsNoTracking().Where(message => message.ConversationId == conversationId);
        var totalCount = await source.CountAsync(cancellationToken);

        var items = await source
                    .OrderBy(message => message.CreatedAt)
                    .Skip(offset)
                    .Take(limit)
                    .Select(message => new MessageView
                    {
                        Id = message.Id,
                        Role = message.Role.ToString(),
                        Content = message.Content,
                        RewrittenQuery = message.RewrittenQuery,
                        Provider = message.Provider,
                        Model = message.Model,
                        InputTokens = message.InputTokens,
                        OutputTokens = message.OutputTokens,
                        LatencyMs = message.LatencyMs,
                        RetrievalMs = message.RetrievalMs,
                        CreatedAt = message.CreatedAt,
                        Citations = message.Citations
                            .OrderBy(citation => citation.MarkerIndex)
                            .Select(citation => new CitationView
                            {
                                ChunkId = citation.ChunkId,
                                MarkerIndex = citation.MarkerIndex,
                                Score = citation.Score,
                                DocumentId = citation.Chunk!.DocumentId,
                                DocumentTitle = citation.Chunk.Document!.Title,
                                HeadingPath = citation.Chunk.HeadingPath
                            })
                            .ToList()
                    })
                    .ToListAsync(cancellationToken);

        return Result.Success(new PaginatedList<MessageView>
        {
            Items = items,
            Offset = offset,
            Limit = limit,
            TotalCount = totalCount
        });
    }

    private bool IsOwner(Guid ownerId) => ownerId == currentUser.Id;
}
