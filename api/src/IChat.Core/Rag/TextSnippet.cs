namespace IChat.Core.Rag;

/// <summary>
/// A short excerpt for displaying a source and for debugging retrieval. It is cut to the same length
/// everywhere; otherwise the snippet in the "sources" event and the snippet in the search response
/// would differ in length for one and the same chunk.
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
