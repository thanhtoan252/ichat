namespace IChat.Core.Rag;

public static class ChatHistoryWindow
{

public const int MessagesPerTurn = 2;

public static int MessageCountFor(int turns)
    {
        return turns * MessagesPerTurn;
    }
}
