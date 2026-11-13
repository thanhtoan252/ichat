namespace IChat.Core.Domain.Conversations;

public static class ConversationTitle
{

    public const string Default = "New conversation";

    private const int MaxLength = 60;

    public static bool IsDefault(string? title) =>
            string.IsNullOrWhiteSpace(title) || string.Equals(title.Trim(), Default, StringComparison.OrdinalIgnoreCase);

    public static string FromQuestion(string question)
    {
        var condensed = string.Join(' ', question.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

        if (condensed.Length == 0)
        {
            return Default;
        }

        if (condensed.Length <= MaxLength)
        {
            return condensed;
        }

        var cut = MaxLength - 3;

        if (char.IsHighSurrogate(condensed[cut - 1]))
        {
            cut--;
        }

        return $"{condensed[..cut]}…";
    }
}
