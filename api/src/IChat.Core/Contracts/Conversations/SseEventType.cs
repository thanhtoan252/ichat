namespace IChat.Core.Contracts.Conversations;

public static class SseEventType
{
    public const string Status = "status";

    public const string Sources = "sources";

    public const string Delta = "delta";

    public const string Done = "done";

    public const string Error = "error";
}
