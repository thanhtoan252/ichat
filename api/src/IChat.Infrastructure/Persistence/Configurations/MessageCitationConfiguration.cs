namespace IChat.Infrastructure.Persistence.Configurations;

using IChat.Core.Domain.Conversations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public sealed class MessageCitationConfiguration : IEntityTypeConfiguration<MessageCitation>
{
    public void Configure(EntityTypeBuilder<MessageCitation> builder)
    {
        builder.ToTable("message_citations");

        builder.HasKey(citation => new { citation.MessageId, citation.ChunkId });

        builder.Property(citation => citation.MarkerIndex).IsRequired();
        builder.Property(citation => citation.Score).IsRequired();

        builder.HasOne(citation => citation.Chunk)
            .WithMany()
            .HasForeignKey(citation => citation.ChunkId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
