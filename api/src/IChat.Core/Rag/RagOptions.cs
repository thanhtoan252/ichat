namespace IChat.Core.Rag;

public sealed class RagOptions
{
    public const string SectionName = "Rag";

    public ChunkingOptions Chunking { get; set; } = new();

    public QueryRewritingOptions QueryRewriting { get; set; } = new();

    public RetrievalOptions Retrieval { get; set; } = new();

    public DiversityOptions Diversity { get; set; } = new();

    public NeighborExpansionOptions NeighborExpansion { get; set; } = new();

    public RerankingOptions Reranking { get; set; } = new();

    public ContextOptions Context { get; set; } = new();
}
