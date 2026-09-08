namespace IChat.Core.Domain.Conversations;

/// <summary>
/// The naming rule of a conversation, kept in one place because both the creation
/// endpoint and the first chat turn have to agree on what "untitled" means.
/// </summary>
public static class ConversationTitle
{
    /// <summary>Name given to a conversation created without one.</summary>
    public const string Default = "New conversation";

    private const int MaxLength = 60;

    /// <summary>
    /// True when the conversation still carries the placeholder name, so the first
    /// question may take it over.
    /// </summary>
    public static bool IsDefault(string? title) =>
        string.IsNullOrWhiteSpace(title) || string.Equals(title.Trim(), Default, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Derives a title from the first question: whitespace collapsed and cut to
    /// <see cref="MaxLength"/> characters with an ellipsis.
    /// </summary>
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

        // Không cắt giữa một surrogate pair, nếu không emoji cuối tiêu đề thành ký tự hỏng.
        if (char.IsHighSurrogate(condensed[cut - 1]))
        {
            cut--;
        }

        return $"{condensed[..cut]}…";
    }
}
