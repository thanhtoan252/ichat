namespace IChat.Core.Rag;

public static class TextSnippet
{
    public const int MaxLength = 240;

    private const string Ellipsis = "…";

    public static string From(string text)
    {
        return text.Length <= MaxLength ? text : text[..MaxLength] + Ellipsis;
    }
}
