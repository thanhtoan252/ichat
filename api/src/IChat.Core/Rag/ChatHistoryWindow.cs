namespace IChat.Core.Rag;

/// <summary>
/// Cấu hình đếm lịch sử theo LƯỢT, còn mọi thứ dưới nó đếm theo MESSAGE.
/// Phép quy đổi giữa hai đơn vị nằm ở đây thay vì rải rác dưới dạng `* 2`.
/// </summary>
public static class ChatHistoryWindow
{
    /// <summary>Một lượt = một câu hỏi của người dùng + một câu trả lời của trợ lý.</summary>
    public const int MessagesPerTurn = 2;

    public static int MessageCountFor(int turns)
    {
        return turns * MessagesPerTurn;
    }
}
