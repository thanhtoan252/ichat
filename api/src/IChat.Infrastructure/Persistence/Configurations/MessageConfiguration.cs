namespace IChat.Infrastructure.Persistence.Configurations;

using IChat.Core.Domain.Conversations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public sealed class MessageConfiguration : IEntityTypeConfiguration<Message>
{
    public void Configure(EntityTypeBuilder<Message> builder)
    {
        builder.ToTable("messages");

        builder.HasKey(message => message.Id);

        builder.Property(message => message.Role).HasConversion<int>().IsRequired();
        builder.Property(message => message.Content).IsRequired();
        builder.Property(message => message.CreatedAt).IsRequired();

        builder.HasMany(message => message.Citations)
            .WithOne(citation => citation.Message)
            .HasForeignKey(citation => citation.MessageId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(message => new { message.ConversationId, message.CreatedAt });

        builder.Navigation(message => message.Citations).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
