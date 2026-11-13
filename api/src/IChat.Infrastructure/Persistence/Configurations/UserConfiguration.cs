namespace IChat.Infrastructure.Persistence.Configurations;

using IChat.Core.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");

        builder.HasKey(user => user.Id);

        builder.Property(user => user.UserName).IsRequired().HasMaxLength(64);
        builder.Property(user => user.DisplayName).IsRequired().HasMaxLength(200);
        builder.Property(user => user.PasswordHash).IsRequired().HasMaxLength(200);

        builder.Property(user => user.Role).IsRequired().HasConversion<string>().HasMaxLength(20);

        builder.Property(user => user.IsActive).IsRequired();
        builder.Property(user => user.CreatedAt).IsRequired();
        builder.Property(user => user.UpdatedAt).IsRequired();

        builder.HasIndex(user => user.UserName).IsUnique();
    }
}
