namespace IChat.Infrastructure.Persistence;

using IChat.Core.Abstractions;
using IChat.Core.Domain.Conversations;
using IChat.Core.Domain.Documents;
using IChat.Core.Domain.Identity;
using Microsoft.EntityFrameworkCore;

public sealed class IChatDbContext(DbContextOptions<IChatDbContext> options) : DbContext(options), IApplicationDbContext
{
    public DbSet<Document> Documents => Set<Document>();

    public DbSet<DocumentChunk> DocumentChunks => Set<DocumentChunk>();

    public DbSet<Conversation> Conversations => Set<Conversation>();

    public DbSet<Message> Messages => Set<Message>();

    public DbSet<MessageCitation> MessageCitations => Set<MessageCitation>();

    public DbSet<User> Users => Set<User>();

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    Task<int> IApplicationDbContext.SaveChangesAsync(CancellationToken cancellationToken)
    {
        return SaveChangesAsync(cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("vector");
        modelBuilder.HasPostgresExtension("unaccent");
        modelBuilder.HasPostgresExtension("pg_trgm");

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(IChatDbContext).Assembly);

        base.OnModelCreating(modelBuilder);
    }
}
