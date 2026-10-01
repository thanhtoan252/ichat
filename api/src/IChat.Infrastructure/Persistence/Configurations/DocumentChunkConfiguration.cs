namespace IChat.Infrastructure.Persistence.Configurations;

using IChat.Core.Domain.Documents;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using NpgsqlTypes;
using Pgvector;

public sealed class DocumentChunkConfiguration : IEntityTypeConfiguration<DocumentChunk>
{
    public const string ContentTsVector = "content_tsv";

    public void Configure(EntityTypeBuilder<DocumentChunk> builder)
    {
        builder.ToTable("document_chunks");

        builder.HasKey(chunk => chunk.Id);

        builder.Property(chunk => chunk.ChunkIndex).IsRequired();
        builder.Property(chunk => chunk.Content).IsRequired();
        builder.Property(chunk => chunk.EmbeddedText).IsRequired();
        builder.Property(chunk => chunk.TokenCount).IsRequired();
        builder.Property(chunk => chunk.EmbeddingModel).IsRequired();
        builder.Property(chunk => chunk.EmbeddingDimensions).IsRequired();
        builder.Property(chunk => chunk.CreatedAt).IsRequired();

        builder.Property(chunk => chunk.Metadata)
            .HasColumnType("jsonb")
            .IsRequired()
            .HasDefaultValueSql("'{}'::jsonb");

        // Core keeps float[] so it never has to reference Pgvector; the converter handles the mapping to vector(N).
        builder.Property(chunk => chunk.Embedding)
            .HasColumnType(EmbeddingDimensions.ColumnType)
            .HasConversion(
                new ValueConverter<float[], Vector>(
                    value => new Vector(value),
                    value => value.ToArray()),
                // Without a comparer EF treats float[] by reference and misses changes to the array's contents.
                new ValueComparer<float[]>(
                    (left, right) => left != null && right != null && left.SequenceEqual(right),
                    value => value.Aggregate(0, (hash, item) => HashCode.Combine(hash, item.GetHashCode())),
                    value => value.ToArray()))
            .IsRequired();

        // content_tsv is a generated column in the database, not a property of the domain entity.
        // It uses 'simple' rather than 'english' because the content may be Vietnamese and PostgreSQL has no
        // text search configuration for Vietnamese.
        // immutable_unaccent (created in a migration) is mandatory: the built-in unaccent() is only STABLE,
        // while a generated column requires an IMMUTABLE expression.
        builder.Property<NpgsqlTsVector>(ContentTsVector)
            .HasColumnName(ContentTsVector)
            .HasComputedColumnSql(
                "to_tsvector('simple', immutable_unaccent(coalesce(heading_path,'') || ' ' || content))",
                stored: true);

        builder.HasIndex(chunk => new { chunk.DocumentId, chunk.ChunkIndex }).IsUnique();
        builder.HasIndex(chunk => chunk.EmbeddingModel);

        builder.HasIndex(ContentTsVector)
            .HasDatabaseName("ix_chunks_tsv")
            .HasMethod("gin");

        builder.HasIndex(chunk => chunk.Content)
            .HasDatabaseName("ix_chunks_content_trgm")
            .HasMethod("gin")
            .HasOperators("gin_trgm_ops");

        builder.HasIndex(chunk => chunk.Embedding)
            .HasDatabaseName("ix_chunks_embedding_hnsw")
            .HasMethod("hnsw")
            .HasOperators("vector_cosine_ops")
            .HasStorageParameter("m", 16)
            .HasStorageParameter("ef_construction", 64);
    }
}
