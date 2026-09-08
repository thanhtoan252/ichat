namespace IChat.Core.Domain.Documents;

public sealed class DocumentChunk
{
    private DocumentChunk()
    {
    }

    public Guid Id { get; private set; }

    public Guid DocumentId { get; private set; }

    public Document? Document { get; private set; }

    public int ChunkIndex { get; private set; }

    /// <summary>Nội dung gốc để hiển thị cho người dùng — không chứa tiền tố heading.</summary>
    public string Content { get; private set; } = string.Empty;

    public string? HeadingPath { get; private set; }

    /// <summary>Văn bản thực sự đem đi embed — đã thêm ngữ cảnh title/heading.</summary>
    public string EmbeddedText { get; private set; } = string.Empty;

    public int TokenCount { get; private set; }

    // Core không được biết tới Pgvector.Vector; Infrastructure map float[] <-> vector(N)
    // bằng ValueConverter. Vector search dùng raw SQL nên không cần kiểu Vector ở đây.
    public float[] Embedding { get; private set; } = [];

    public string EmbeddingModel { get; private set; } = string.Empty;

    public int EmbeddingDimensions { get; private set; }

    public string Metadata { get; private set; } = "{}";

    public DateTimeOffset CreatedAt { get; private set; }

    public static DocumentChunk Create(
        Guid documentId,
        int chunkIndex,
        string content,
        string? headingPath,
        string embeddedText,
        int tokenCount,
        float[] embedding,
        string embeddingModel,
        int embeddingDimensions,
        string metadataJson,
        DateTimeOffset createdAt)
    {
        return new DocumentChunk
        {
            Id = Guid.CreateVersion7(),
            DocumentId = documentId,
            ChunkIndex = chunkIndex,
            Content = content,
            HeadingPath = headingPath,
            EmbeddedText = embeddedText,
            TokenCount = tokenCount,
            Embedding = embedding,
            EmbeddingModel = embeddingModel,
            EmbeddingDimensions = embeddingDimensions,
            Metadata = metadataJson,
            CreatedAt = createdAt
        };
    }
}
