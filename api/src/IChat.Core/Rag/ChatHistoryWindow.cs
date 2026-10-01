namespace IChat.Core.Rag;

/// <summary>
/// Configuration counts history in TURNS, while everything below it counts MESSAGES.
/// The conversion between the two units lives here instead of being scattered around as `* 2`.
/// </summary>
public static class ChatHistoryWindow
{
    /// <summary>One turn = one user question plus one assistant answer.</summary>
    public const int MessagesPerTurn = 2;

    public static int MessageCountFor(int turns)
    {
        return turns * MessagesPerTurn;
    }
}
