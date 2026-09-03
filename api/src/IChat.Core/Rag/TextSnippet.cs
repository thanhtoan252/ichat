namespace IChat.Core.Rag;

/// <summary>
/// Đoạn trích ngắn để hiển thị nguồn và để debug retrieval. Cắt cùng một độ dài ở mọi
/// nơi, nếu không thì snippet trong event "sources" và snippet trong response search sẽ
/// dài ngắn khác nhau cho cùng một chunk.
/// </summary>
public static class TextSnippet
{
    public const int MaxLength = 240;

    private const string Ellipsis = "…";

    public static string From(string text)
    {
        return text.Length <= MaxLength ? text : text[..MaxLength] + Ellipsis;
    }
}
