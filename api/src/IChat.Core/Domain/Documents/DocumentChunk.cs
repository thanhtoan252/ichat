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

    /// <summary>The original content shown to the user — it carries no heading prefix.</summary>
    public string Content { get; private set; } = string.Empty;

    public string? HeadingPath { get; private set; }

    /// <summary>The text actually sent to the embedding model — title/heading context already prepended.</summary>
    public string EmbeddedText { get; private set; } = string.Empty;

    public int TokenCount { get; private set; }

    // Core must not know about Pgvector.Vector; Infrastructure maps float[] <-> vector(N) with a
    // ValueConverter. Vector search uses raw SQL, so no Vector type is needed here.
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
