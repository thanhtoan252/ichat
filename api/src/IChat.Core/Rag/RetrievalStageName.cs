namespace IChat.Core.Rag;

/// <summary>
/// The key of each stage in the search endpoint's debug response. A published contract:
/// changing a value here breaks the retrieval debugging tools that read stages by name.
/// </summary>
public static class RetrievalStageName
{
    public const string Vector = "vector";

    public const string FullText = "fulltext";

    public const string Trigram = "trigram";

    public const string Fused = "fused";

    public const string AfterMmr = "afterMmr";

    public const string Reranked = "reranked";

    public const string Final = "final";
}
