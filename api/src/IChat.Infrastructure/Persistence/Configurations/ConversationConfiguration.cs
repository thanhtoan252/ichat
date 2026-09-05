namespace IChat.Infrastructure.Persistence.Configurations;

using IChat.Core.Domain.Conversations;
using IChat.Core.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public sealed class ConversationConfiguration : IEntityTypeConfiguration<Conversation>
{
    public void Configure(EntityTypeBuilder<Conversation> builder)
    {
        builder.ToTable("conversations");

        builder.HasKey(conversation => conversation.Id);

        builder.Property(conversation => conversation.Title).IsRequired();
        builder.Property(conversation => conversation.CreatedAt).IsRequired();
        builder.Property(conversation => conversation.UpdatedAt).IsRequired();

        // Không khai báo navigation trên Conversation: aggregate hội thoại không cần
        // đọc ngược sang User, chỉ cần ràng buộc rằng chủ sở hữu có thật.
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(conversation => conversation.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(conversation => conversation.Messages)
            .WithOne(message => message.Conversation)
            .HasForeignKey(message => message.ConversationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(conversation => conversation.UpdatedAt);
        builder.HasIndex(conversation => conversation.UserId);

        builder.Navigation(conversation => conversation.Messages).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
