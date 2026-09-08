namespace IChat.Infrastructure.Persistence.Configurations;

using IChat.Core.Domain.Documents;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public sealed class DocumentConfiguration : IEntityTypeConfiguration<Document>
{
    public void Configure(EntityTypeBuilder<Document> builder)
    {
        builder.ToTable("documents");

        builder.HasKey(document => document.Id);

        builder.Property(document => document.Title).IsRequired();
        builder.Property(document => document.FileName).IsRequired();
        builder.Property(document => document.ContentType).IsRequired();
        builder.Property(document => document.SizeInBytes).IsRequired();
        builder.Property(document => document.StoragePath).IsRequired();
        builder.Property(document => document.Status).HasConversion<int>().IsRequired();
        builder.Property(document => document.ChunkCount).IsRequired().HasDefaultValue(0);
        builder.Property(document => document.CreatedAt).IsRequired();

        builder.HasMany(document => document.Chunks)
            .WithOne(chunk => chunk.Document)
            .HasForeignKey(chunk => chunk.DocumentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(document => document.Status);
        builder.HasIndex(document => document.CreatedAt);

        builder.Navigation(document => document.Chunks).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
