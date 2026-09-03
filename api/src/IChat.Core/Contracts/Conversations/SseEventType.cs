namespace IChat.Core.Contracts.Conversations;

/// <summary>
/// Tên event của kênh SSE. Đây là contract đã công bố cho client — client phân nhánh
/// trên đúng các chuỗi này, nên đổi một giá trị ở đây là breaking change.
/// </summary>
public static class SseEventType
{
    public const string Status = "status";

    public const string Sources = "sources";

    public const string Delta = "delta";

    public const string Done = "done";

    public const string Error = "error";
}
