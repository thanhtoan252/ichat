namespace IChat.Core.Rag;

/// <summary>
/// Khoá của từng chặng trong response debug của endpoint search. Contract đã công bố:
/// đổi một giá trị ở đây làm hỏng công cụ debug retrieval đang đọc theo tên chặng.
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
