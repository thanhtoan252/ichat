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
    /// <summary>
    /// Một định nghĩa duy nhất cho shape của ConversationView. Giữ dạng Expression để EF
    /// dịch được thành SELECT khi liệt kê, và compile sẵn một lần cho nhánh tạo mới — nơi
    /// entity đã nằm trong bộ nhớ nên không có truy vấn nào để dịch.
    /// </summary>
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
            return Result.Failure<ConversationView>(Error.Unauthorized("Không có phiên đăng nhập nào."));
        }

        var conversation = Conversation.Create(ownerId, title, timeProvider.GetUtcNow());

        dbContext.Conversations.Add(conversation);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success(ToView(conversation));
    }

    public async Task<Result<PaginatedList<ConversationView>>> GetListAsync(int page, int pageSize, CancellationToken cancellationToken)
    {
        // Hội thoại là riêng tư TUYỆT ĐỐI: kể cả admin cũng không đọc được của người khác.
        // Quản trị viên quản lý tài khoản và tri thức, không đọc nội dung người ta hỏi.
        // Đừng thêm nhánh "nếu IsAdmin thì thấy tất cả" vào đây.
        var ownerId = currentUser.Id;
        var source = dbContext.Conversations
            .AsNoTracking()
            .Where(conversation => conversation.UserId == ownerId);

        var totalCount = await source.CountAsync(cancellationToken);

        var items = await source
            .OrderByDescending(conversation => conversation.UpdatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(ToViewExpression)
            .ToListAsync(cancellationToken);

        return Result.Success(new PaginatedList<ConversationView>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        });
    }

    public async Task<Result<PaginatedList<MessageView>>> GetMessagesAsync(Guid conversationId, int page, int pageSize, CancellationToken cancellationToken)
    {
        var ownerId = await dbContext.Conversations
            .AsNoTracking()
            .Where(item => item.Id == conversationId)
            .Select(item => (Guid?)item.UserId)
            .FirstOrDefaultAsync(cancellationToken);

        // Hội thoại của người khác trả 404 chứ không phải 403: 403 sẽ xác nhận rằng
        // id đó có tồn tại, cho phép dò ra ai đang hỏi gì.
        if (ownerId is null || !IsOwner(ownerId.Value))
        {
            return Result.Failure<PaginatedList<MessageView>>(Error.NotFound("Conversation", conversationId));
        }

        var source = dbContext.Messages.AsNoTracking().Where(message => message.ConversationId == conversationId);
        var totalCount = await source.CountAsync(cancellationToken);

        var items = await source
            .OrderBy(message => message.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
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
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        });
    }

    /// <summary>Không có ngoại lệ cho admin — xem ghi chú ở GetListAsync.</summary>
    private bool IsOwner(Guid ownerId) => ownerId == currentUser.Id;
}
