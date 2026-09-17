namespace IChat.Core.Abstractions;

using IChat.Core.Domain.Conversations;
using IChat.Core.Domain.Documents;
using IChat.Core.Domain.Identity;
using Microsoft.EntityFrameworkCore;

/// <summary>The DbContext IS the unit of work — no generic repository wrapped around it.</summary>
public interface IApplicationDbContext
{
    DbSet<Document> Documents { get; }

    DbSet<DocumentChunk> DocumentChunks { get; }

    DbSet<Conversation> Conversations { get; }

    DbSet<Message> Messages { get; }

    DbSet<MessageCitation> MessageCitations { get; }

    DbSet<User> Users { get; }

    DbSet<RefreshToken> RefreshTokens { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
