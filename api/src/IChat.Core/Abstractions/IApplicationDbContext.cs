namespace IChat.Core.Abstractions;

using IChat.Core.Domain.Conversations;
using IChat.Core.Domain.Documents;
using Microsoft.EntityFrameworkCore;

/// <summary>DbContext chính là Unit of Work — không bọc thêm generic repository.</summary>
public interface IApplicationDbContext
{
    DbSet<Document> Documents { get; }

    DbSet<DocumentChunk> DocumentChunks { get; }

    DbSet<Conversation> Conversations { get; }

    DbSet<Message> Messages { get; }

    DbSet<MessageCitation> MessageCitations { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
